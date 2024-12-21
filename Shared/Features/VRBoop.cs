using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Controls;
using System.Linq;
using System.Collections;

namespace KK_VR.Features
{
    /// <summary>
    /// Adds colliders to the controllers so you can boop things
    /// Based on a feature in KK_VREnhancement by thojmr
    /// https://github.com/thojmr/KK_VREnhancement/blob/5e46bc9a89bf2517c5482bc9df097c7f0274730f/KK_VREnhancement/VRController.Collider.cs
    /// </summary>
    public static class VRBoop
    {
        private readonly static List<DynamicBoneCollider> _activeDBC = [];
        public static void Initialize()
        {
            // Hooks in here don't get patched by the whole assembly PatchAll since the class has no HarmonyPatch attribute
            Harmony.CreateAndPatchAll(typeof(VRBoop), typeof(VRBoop).FullName);
        }
        public static void AddDB(DynamicBoneCollider DBCollider)
        {
            _activeDBC.Add(DBCollider);
        }
        public static void RefreshDynamicBones(IEnumerable<ChaControl> charas)
        {
            // Hooks don't give us BetterPenetration dynamic bones.
            foreach (var chara in charas)
            {
                var dbList = chara.GetComponentsInChildren<DynamicBone>();
                foreach (var db in dbList)
                {
                    AttachControllerColliders(db);
                }
                var dbList01 = chara.GetComponentsInChildren<DynamicBone_Ver01>();
                foreach (var db in dbList01)
                {
                    AttachControllerColliders(db);
                }
                var dbList02 = chara.GetComponentsInChildren<DynamicBone_Ver02>();
                foreach (var db in dbList02)
                {
                    AttachControllerColliders(db);
                }
            }
        }
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.SetupParticles))]
        [HarmonyPatch(typeof(DynamicBone_Ver01), nameof(DynamicBone_Ver01.SetupParticles))]
        [HarmonyPatch(typeof(DynamicBone_Ver02), nameof(DynamicBone_Ver02.SetupParticles))]
        private static void OnDynamicBoneInit(MonoBehaviour __instance)
        {
            AttachControllerColliders(__instance);
        }

        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataAsync))]
        // [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataNoAsync))] // unnecessary, the collider array is reset before the SetupParticles hook
        private static void OnClothesChanged(ref Action<GameObject> actObj)
        {
            // This action is called with the loaded object after the colliders on it are set up
            // This needs to be done despite the SetupParticles hook because LoadCharaFbxData resets the collider list
            actObj += newObj =>
            {
                if (newObj == null) return;
                foreach (var newBone in newObj.GetComponentsInChildren<DynamicBone>())
                {
                    var colliders = newBone.m_Colliders;
                    if (colliders != null)
                        AddColliders(colliders);
                }
            };
        }

        private static void AttachControllerColliders(MonoBehaviour dynamicBone)
        {
            var colliderList = GetColliderList(dynamicBone);
            if (colliderList != null)
            {
                AddColliders(colliderList);
            }
        }
        private static void AddColliders(List<DynamicBoneCollider> colliderList)
        {
            foreach (var dbc in _activeDBC)
            {
                if (!colliderList.Contains(dbc))
                {
                    colliderList.Add(dbc);
                }
            }
        }

        private static List<DynamicBoneCollider> GetColliderList(MonoBehaviour dynamicBone)
        {
            return dynamicBone switch
            {
                DynamicBone d => d.m_Colliders,
                DynamicBone_Ver01 d => d.m_Colliders,
                DynamicBone_Ver02 d => d.Colliders,
                null => throw new ArgumentNullException(nameof(dynamicBone)),
                _ => throw new ArgumentException(@"Not a DynamicBone - " + dynamicBone.GetType(), nameof(dynamicBone)),
            };
        }
    }
}
