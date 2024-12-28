using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using IllusionUtility.SetUtility;
using KK_VR.Caress;
using KK_VR.Features;
using KK_VR.Grasp;
using KK_VR.Interactors;
using KK_VR.Interpreters;
using UnityEngine;
using VRGIN.Core;

// This file is a collection of hooks to move the VR camera at appropriate
// points of the game.

namespace KK_VR.Camera
{
    [HarmonyPatch]
    internal class TextScenarioPatches1
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ADV.TextScenario), nameof(ADV.TextScenario.ADVCameraSetting))]
        private static void PostADVCameraSetting(ADV.TextScenario __instance)
        {
#if KK

            if (Manager.Scene.IsInstance() && Manager.Scene.Instance.NowSceneNames[0] == "Talk")
#elif KKS
            if (Manager.Scene.initialized && Manager.Scene.NowSceneNames[0] == "Talk")
#endif
            {
                // Talk scenes are handled separately.
                return;
            }

            VRLog.Debug("PostADVCameraSetting");
            var backTrans = __instance.BackCamera?.transform;
            if (backTrans == null)
            {
                // backTrans can be null in Roaming. We don't want to move the
                // camera anyway in that case.
                return;
            }
            VRCameraMover.Instance.MaybeMoveADV(__instance, backTrans.position, backTrans.rotation);
        }
    }

    [HarmonyPatch]
    internal class RequestNextLinePatches
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            // In some versions of the base game, MainScenario._RequestNextLine
            // duplicates the logic found in TextScenario._RequestNextLine.
            // In other versions, the former simply calls the latter.
            // We want to patch both methods or the latter alone depending on
            // the version.
            yield return AccessTools.Method(typeof(ADV.TextScenario), "_RequestNextLine");
            if (AccessTools.Field(typeof(ADV.MainScenario), "textHash") == null)
            {
                yield return AccessTools.Method(typeof(ADV.MainScenario), "_RequestNextLine");
            }
        }

        private static void Postfix(ADV.TextScenario __instance, ref IEnumerator __result)
        {
#if KK

            if (Manager.Scene.IsInstance() && Manager.Scene.Instance.NowSceneNames[0] == "Talk")
#elif KKS
            if (Manager.Scene.initialized && Manager.Scene.NowSceneNames[0] == "Talk")
#endif
            {
                // Talk scenes are handled separately.
                return;
            }
            if (__instance.advScene == null)
            {
                // Outside ADV scene (probably roaming), ignore.
                return;
            }
            __result = new[] { __result, Postfix() }.GetEnumerator();

            IEnumerator Postfix()
            {
                VRCameraMover.Instance.HandleTextScenarioProgress(__instance);
                yield break;
            }
        }
    }

    [HarmonyPatch]
    internal class ProgramPatches1
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ADV.Program), nameof(ADV.Program.SetNull))]
        private static void PostSetNull(Transform transform)
        {
            VRLog.Debug("PostSetNull");
            VRCameraMover.Instance.MaybeMoveTo(transform.position, transform.rotation);
        }

#if KKS
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeAnimator))]
        public static void ChangeAnimatorPrefix(HSceneProc __instance)
        {
            __instance.ctrlObi.solver.gameObject.SetActive(true);
        }
#endif
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeAnimator))]
        public static void PostChangeAnimator(HSceneProc.AnimationListInfo _nextAinmInfo, bool _isForceCameraReset, HSceneProc __instance, List<ChaControl> ___lstFemale)
        {
            HSceneInterpreter.OnPoseChange(_nextAinmInfo);
            UpdateVRCamera(__instance, ___lstFemale);
#if KKS
            Fixes.ObiCtrlFix.SetFluidsState(false);
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeCategory))]
        public static void PostChangeCategory(HSceneProc __instance, List<ChaControl> ___lstFemale)//, float __state)
        {
            HSceneInterpreter.OnSpotChange();
            UpdateVRCamera(__instance, ___lstFemale);

#if KKS
            Fixes.ObiCtrlFix.SetFluidsState(false);
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.GotoPointMoveScene))]
        public static void GotoPointMoveScenePostfix()
        {
            if (SmoothMover.Instance != null)
            {
                SmoothMover.Instance.MakeUpright();
            }
        }

        private static void UpdateVRCamera(HSceneProc instance, List<ChaControl> lstFemale)
        {
            var spotChange = Vector3.Distance(VR.Camera.transform.position, lstFemale[0].transform.position) > 2f;
           //VRPlugin.Logger.LogDebug($"UpdateVRCamera:spotChange = {spotChange}");
            var baseTransform = lstFemale[0].objTop.transform;
            var camDat = instance.flags.ctrlCamera.CamDat;// new Traverse(instance.flags.ctrlCamera).Field<BaseCameraControl_Ver2.CameraData>("CamDat").Value;
            var cameraRotation = baseTransform.rotation * Quaternion.Euler(camDat.Rot);
            Vector3 dir;
            switch (instance.flags.mode)
            {
                case HFlag.EMode.masturbation:
                case HFlag.EMode.peeping:
                case HFlag.EMode.lesbian:
                    // Use the default distance for 3rd-person scenes.
                    dir = camDat.Dir;
                    break;
                default:
                    // Start closer otherwise.
                    dir = Vector3.back * 0.8f;
                    break;
            }

            var cameraPosition = cameraRotation * dir + baseTransform.TransformPoint(camDat.Pos);
            //if (previousFemaleY is float prevY)
            //{
            //    // Keep the relative Y coordinate from the female.
            //    var cameraHeight = VR.Camera.transform.position.y + baseTransform.position.y - prevY;
            //    var destination = new Vector3(cameraPosition.x, cameraHeight, cameraPosition.z);

            //    if (VRMoverH.Instance != null && VRMoverH.Instance._settings.FlyInH)
            //    {
            //        VRMoverH.Instance.MoveToInH(position: destination);
            //    }
            //    else
            //    {
            //        VRCameraMover.Instance.MaybeMoveTo(destination, cameraRotation, false);
            //    }
            //}
            //else
            {
                // We are starting from scratch.
                // TODO: the height calculation below assumes standing mode.

                if (KoikatuInterpreter.Settings.SmoothTransition && SmoothMover.Instance != null && !VRFade.IsFade)
                {
                    SmoothMover.Instance.MoveToInH(cameraPosition, cameraRotation, spotChange);
                }
                else
                {
                    var cameraHeight = lstFemale[0].transform.position.y + VR.Camera.transform.localPosition.y;
                    var destination = new Vector3(cameraPosition.x, cameraHeight, cameraPosition.z);
                    VRCameraMover.Instance.MoveTo(destination, cameraRotation);
                }


            }
        }
    }

    //[HarmonyPatch(typeof(HSceneProc))]
    //internal class HSceneProcPatches
    //{


    //    //[HarmonyPatch("ChangeAnimator")]
    //    //[HarmonyPostfix]
    //    //public static void PostChangeAnimator(HSceneProc.AnimationListInfo _nextAinmInfo, bool _isForceCameraReset, HSceneProc __instance, List<ChaControl> ___lstFemale)
    //    //{
    //    //    UpdateVRCamera(__instance, ___lstFemale, null);
    //    //    HSceneInterpreter.OnPoseChange(_nextAinmInfo);

    //    //    Fixes.ObiCtrlFix.SetFluidsState(false);
    //    //}


    //    //[HarmonyPatch("ChangeCategory")]
    //    //[HarmonyPrefix]
    //    //public static void PreChangeCategory()
    //    //{
    //    //    if (GraspHelper.Instance != null) GraspHelper.Instance.OnSpotChangePre();
    //    //}

    //    //[HarmonyPatch("ChangeCategory")]
    //    //[HarmonyPostfix]
    //    //public static void PostChangeCategory(HSceneProc __instance, List<ChaControl> ___lstFemale)//, float __state)
    //    //{
    //    //    if (KoikatuInterpreter.SceneInterpreter is HSceneInterpreter hScene)
    //    //        hScene.OnSpotChangePost();
    //    //    UpdateVRCamera(__instance, ___lstFemale, null);// __state);

    //    //    Fixes.ObiCtrlFix.SetFluidsState(false);
    //    //}
    //    //[HarmonyPatch(nameof(HSceneProc.GotoPointMoveScene))]
    //    //[HarmonyPostfix]
    //    //public static void GotoPointMoveScenePostfix()
    //    //{
    //    //    if (VRMoverH.Instance != null)
    //    //    {
    //    //        VRMoverH.Instance.MakeUpright();
    //    //    }
    //    //}

    //    /// <summary>
    //    /// Update the transform of the VR camera.
    //    /// </summary>
    //    //private static void UpdateVRCamera(HSceneProc instance, List<ChaControl> lstFemale, float? previousFemaleY)
    //    //{
    //    //    var baseTransform = lstFemale[0].objTop.transform;
    //    //    var camDat = instance.flags.ctrlCamera.CamDat;// new Traverse(instance.flags.ctrlCamera).Field<BaseCameraControl_Ver2.CameraData>("CamDat").Value;
    //    //    var cameraRotation = baseTransform.rotation * Quaternion.Euler(camDat.Rot);
    //    //    Vector3 dir;
    //    //    switch (instance.flags.mode)
    //    //    {
    //    //        case HFlag.EMode.masturbation:
    //    //        case HFlag.EMode.peeping:
    //    //        case HFlag.EMode.lesbian:
    //    //            // Use the default distance for 3rd-person scenes.
    //    //            dir = camDat.Dir;
    //    //            break;
    //    //        default:
    //    //            // Start closer otherwise.
    //    //            dir = Vector3.back * 0.8f;
    //    //            break;
    //    //    }

    //    //    var cameraPosition = cameraRotation * dir + baseTransform.TransformPoint(camDat.Pos);
    //    //    //if (previousFemaleY is float prevY)
    //    //    //{
    //    //    //    // Keep the relative Y coordinate from the female.
    //    //    //    var cameraHeight = VR.Camera.transform.position.y + baseTransform.position.y - prevY;
    //    //    //    var destination = new Vector3(cameraPosition.x, cameraHeight, cameraPosition.z);

    //    //    //    if (VRMoverH.Instance != null && VRMoverH.Instance._settings.FlyInH)
    //    //    //    {
    //    //    //        VRMoverH.Instance.MoveToInH(position: destination);
    //    //    //    }
    //    //    //    else
    //    //    //    {
    //    //    //        VRCameraMover.Instance.MaybeMoveTo(destination, cameraRotation, false);
    //    //    //    }
    //    //    //}
    //    //    //else
    //    //    {
    //    //        // We are starting from scratch.
    //    //        // TODO: the height calculation below assumes standing mode.

    //    //        if (VRMoverH.Instance != null && KoikatuInterpreter.settings.FlyInH)
    //    //        {
    //    //            VRMoverH.Instance.MoveToInH(cameraPosition, cameraRotation, previousFemaleY == null, instance.flags.mode);
    //    //        }
    //    //        else
    //    //        {
    //    //            var cameraHeight = lstFemale[0].transform.position.y + VR.Camera.transform.localPosition.y;
    //    //            var destination = new Vector3(cameraPosition.x, cameraHeight, cameraPosition.z);
    //    //            VRCameraMover.Instance.MoveTo(destination, cameraRotation);
    //    //        }

                
    //    //    }
    //    //}
    //}
}
