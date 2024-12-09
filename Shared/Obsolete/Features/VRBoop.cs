//using System;
//using System.Collections.Generic;
//using HarmonyLib;
//using UnityEngine;
//using UnityEngine.XR;
//using VRGIN.Controls;

// A bit different implementation currently used. This is a reference.
//namespace KKS_VR.Features
//{
//    /// <summary>
//    /// Adds colliders to the controllers so you can boop things
//    /// Based on a feature in KK_VREnhancement by thojmr
//    /// https://github.com/thojmr/KK_VREnhancement/blob/5e46bc9a89bf2517c5482bc9df097c7f0274730f/KK_VREnhancement/VRController.Collider.cs
//    /// </summary>
//    public static class VRBoop
//    {
//        internal const string LeftColliderName = "Left_Boop_Collider";
//        internal const string RightColliderName = "Right_Boop_Collider";

//        private static DynamicBoneCollider _leftCollider;
//        private static DynamicBoneCollider _rightCollider;

//        public static void Initialize(Controller controller, EyeSide controllerSide)
//        {
//            // Hooks in here don't get patched by the whole assembly PatchAll since the class has no HarmonyPatch attribute
//            Harmony.CreateAndPatchAll(typeof(VRBoop), typeof(VRBoop).FullName);

//            switch (controllerSide)
//            {
//                case EyeSide.Left:
//                    _leftCollider = GetOrAttachCollider(controller.gameObject, LeftColliderName);
//                    break;
//                case EyeSide.Right:
//                    _rightCollider = GetOrAttachCollider(controller.gameObject, RightColliderName);
//                    break;
//                default:
//                    throw new ArgumentOutOfRangeException(nameof(controllerSide), controllerSide, null);
//            }
//        }

//        [HarmonyPostfix]
//        [HarmonyWrapSafe]
//        [HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.SetupParticles))]
//        [HarmonyPatch(typeof(DynamicBone_Ver01), nameof(DynamicBone_Ver01.SetupParticles))]
//        [HarmonyPatch(typeof(DynamicBone_Ver02), nameof(DynamicBone_Ver02.SetupParticles))]
//        private static void OnDynamicBoneInit(MonoBehaviour __instance)
//        {
//            AttachControllerColliders(__instance);
//        }

//        [HarmonyPrefix]
//        [HarmonyWrapSafe]
//        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataAsync))]
//        // [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataNoAsync))] // unnecessary, the collider array is reset before the SetupParticles hook
//        private static void OnClothesChanged(ref Action<GameObject> actObj)
//        {
//            // This action is called with the loaded object after the colliders on it are set up
//            // This needs to be done despite the SetupParticles hook because LoadCharaFbxData resets the collider list
//            actObj += newObj =>
//            {
//                if (newObj == null) return;
//                foreach (var newBone in newObj.GetComponentsInChildren<DynamicBone>())
//                {
//                    var colliders = newBone.m_Colliders;
//                    if (colliders != null)
//                        AttachControllerColliders(colliders);
//                }
//            };
//        }

//        private static void AttachControllerColliders(MonoBehaviour dynamicBone)
//        {
//            var colliderList = GetColliderList(dynamicBone);
//            if (colliderList == null) return;
//            AttachControllerColliders(colliderList);
//        }

//        private static void AttachControllerColliders(List<DynamicBoneCollider> colliderList)
//        {
//            if (colliderList == null) throw new ArgumentNullException(nameof(colliderList));

//            if (_leftCollider && !colliderList.Contains(_leftCollider))
//                colliderList.Add(_leftCollider);
//            if (_rightCollider && !colliderList.Contains(_rightCollider))
//                colliderList.Add(_rightCollider);
//        }

//        private static List<DynamicBoneCollider> GetColliderList(MonoBehaviour dynamicBone)
//        {
//            return dynamicBone switch
//            {
//                DynamicBone d => d.m_Colliders,
//                DynamicBone_Ver01 d => d.m_Colliders,
//                DynamicBone_Ver02 d => d.Colliders,
//                null => throw new ArgumentNullException(nameof(dynamicBone)),
//                _ => throw new ArgumentException(@"Not a DynamicBone - " + dynamicBone.GetType(), nameof(dynamicBone)),
//            };
//        }

//        private static DynamicBoneCollider GetOrAttachCollider(GameObject controllerGameObject, string colliderName)
//        {
//            if (controllerGameObject == null) throw new ArgumentNullException(nameof(controllerGameObject));
//            if (colliderName == null) throw new ArgumentNullException(nameof(colliderName));

//            //Check for existing DB collider that may have been attached earlier
//            var existingCollider = controllerGameObject.GetComponentInChildren<DynamicBoneCollider>();
//            if (existingCollider == null)
//            {
//                //Add a DB collider to the controller
//                return AddDbCollider(controllerGameObject, colliderName);
//            }

//            return existingCollider;
//        }

//        private static DynamicBoneCollider AddDbCollider(GameObject controllerGameObject, string colliderName,
//            float colliderRadius = 0.05f, float collierHeight = 0f, Vector3 colliderCenter = new Vector3(), DynamicBoneCollider.Direction colliderDirection = default)
//        {
//            //Build the dynamic bone collider
//            var colliderObject = new GameObject(colliderName);
//            var collider = colliderObject.AddComponent<DynamicBoneCollider>();
//            collider.m_Radius = colliderRadius;
//            collider.m_Height = collierHeight;
//            collider.m_Center = colliderCenter;
//            collider.m_Direction = colliderDirection;
//            colliderObject.transform.SetParent(controllerGameObject.transform, false);
//            return collider;
//        }
//    }
//}
