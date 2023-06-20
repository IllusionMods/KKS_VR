﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using ADV;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using KKAPI.Utilities;
using KKS_VR.Interpreters;
using KKS_VR.Settings;
using Sirenix.Serialization.Utilities;
using StrayTech;
using UnityEngine;
using VRGIN.Core;
using Object = UnityEngine.Object;

/*
 * Fixes for issues that are in the base game but are only relevant in VR.
 */
namespace KKS_VR.Fixes
{
    /// <summary>
    /// Suppress character update for invisible characters in some sub-scenes of Roaming.
    /// </summary>
    [HarmonyPatch(typeof(ChaControl))]
    public class ChaControlPatches1
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChaControl.LateUpdateForce))]
        private static bool PreLateUpdateForce(ChaControl __instance)
        {
            return !SafeToSkipUpdate(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChaControl.UpdateForce))]
        private static bool PreUpdateForce(ChaControl __instance)
        {
            return !SafeToSkipUpdate(__instance);
        }

        public static bool SafeToSkipUpdate(ChaControl control)
        {
            return
                VR.Settings is KoikatuSettings settings &&
                settings.OptimizeHInsideRoaming &&
                control.objTop?.activeSelf == false &&
                VR.Interpreter is KoikatuInterpreter interpreter &&
                (interpreter.CurrentScene == KoikatuInterpreter.SceneType.HScene ||
                 interpreter.CurrentScene == KoikatuInterpreter.SceneType.TalkScene);
        }
    }

    /// <summary>
    /// Fix game crash during map load
    /// todo hack, handle properly?
    /// </summary>
    [HarmonyPatch(typeof(SunLightInfo))]
    public class FogHack1
    {
        [HarmonyFinalizer]
        [HarmonyPatch(nameof(SunLightInfo.Set))]
        private static Exception PreLateUpdateForce(Exception __exception)
        {
            if (__exception != null) VRPlugin.Logger.LogDebug("Caught expected crash: " + __exception);
            return null;
        }
    }

    /// <summary>
    /// Fix game crash during map load
    /// todo hack, handle properly?
    /// </summary>
    [HarmonyPatch(typeof(ActionMap))]
    public class FogHack2
    {
        // todo hack, handle properly
        [HarmonyFinalizer]
        [HarmonyPatch(nameof(ActionMap.UpdateCameraFog))]
        private static Exception PreLateUpdateForce(Exception __exception)
        {
            if (__exception != null) VRPlugin.Logger.LogDebug("Caught expected crash: " + __exception);
            return null;
        }
    }

    /// <summary>
    /// Fix game crash during ADV scene load
    /// </summary>
    [HarmonyPatch(typeof(Manager.Game))]
    public class ADVSceneFix1
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Manager.Game.cameraEffector), MethodType.Getter)]
        private static void FixMissingCameraEffector(Manager.Game __instance, ref CameraEffector __result)
        {
            if (__result == null && __instance.isCameraChanged)
                // vr camera doesn't have this component on it, which crashes game code with nullref. Use the component on original advcamera instead
                __instance._cameraEffector = __result = Object.FindObjectOfType<CameraEffector>();
        }
    }

    /// <summary>
    /// Fix being unable to do some actions in roaming mode
    /// </summary>
    [HarmonyPatch(typeof(CameraSystem))]
    public class ADVSceneFix2
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CameraSystem.SystemStatus), MethodType.Getter)]
        private static bool FixNeverEndingTransition(ref CameraSystem.CameraSystemStatus __result)
        {
            __result = CameraSystem.CameraSystemStatus.Inactive;
            return false;
        }
    }

    /// <summary>
    /// Fix ADV scenes messing with the VR camera by moving it or setting flags on it. Feed it the default 2D camera instead so it's happy.
    /// </summary>
    [HarmonyPatch]
    public class ADVSceneFix3
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return CoroutineUtils.GetMoveNext(AccessTools.Method(typeof(TalkScene), nameof(TalkScene.Setup)));
        }

        private static UnityEngine.Camera GetOriginalMainCamera()
        {
            // vr camera doesn't have this component on it
            var originalMainCamera = (Manager.Game.instance.cameraEffector ?? Object.FindObjectOfType<CameraEffector>()).GetComponent<UnityEngine.Camera>();
            VRPlugin.Logger.LogDebug($"GetOriginalMainCamera called, cam found: {originalMainCamera?.GetFullPath()}\n{new StackTrace()}");
            return originalMainCamera;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts, MethodBase __originalMethod)
        {
            var targert = AccessTools.PropertyGetter(typeof(UnityEngine.Camera), nameof(UnityEngine.Camera.main));
            var replacement = AccessTools.Method(typeof(ADVSceneFix3), nameof(GetOriginalMainCamera));
            return insts.Manipulator(
                instr => instr.opcode == OpCodes.Call && (MethodInfo)instr.operand == targert,
                instr =>
                {
                    instr.operand = replacement;
                    VRPlugin.Logger.LogDebug("Patched Camera.main in " + __originalMethod.GetNiceName());
                });
        }
    }

    /// <summary>
    /// Fix ADV scenes messing with the VR camera by moving it or setting flags on it. Feed it the default 2D camera instead so it's happy.
    /// </summary>
    [HarmonyPatch]
    public class ADVSceneFix4
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ADVScene), nameof(ADVScene.Init));
        }

        private static UnityEngine.Camera GetOriginalMainCamera()
        {
            // vr camera doesn't have this component on it
            var originalMainCamera = (Manager.Game.instance.cameraEffector ?? Object.FindObjectOfType<CameraEffector>()).GetComponent<UnityEngine.Camera>();
            VRPlugin.Logger.LogDebug($"GetOriginalMainCamera called, cam found: {originalMainCamera?.GetFullPath()}\n{new StackTrace()}");
            return originalMainCamera;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts, MethodBase __originalMethod)
        {
            var targert = AccessTools.PropertyGetter(typeof(UnityEngine.Camera), nameof(UnityEngine.Camera.main));
            var replacement = AccessTools.Method(typeof(ADVSceneFix4), nameof(GetOriginalMainCamera));
            return insts.Manipulator(
                instr => instr.opcode == OpCodes.Call && (MethodInfo)instr.operand == targert,
                instr =>
                {
                    instr.operand = replacement;
                    VRPlugin.Logger.LogDebug("Patched Camera.main in " + __originalMethod.GetNiceName());
                });
        }

        private static void Postfix(ADVScene __instance)
        {
            Manager.Sound.Listener = UnityEngine.Camera.main.transform;
        }
    }

    /// <summary>
    /// Fix vending machines and some other action points softlocking the game
    /// </summary>
    [HarmonyPatch(typeof(Manager.PlayerAction))]
    public class VendingMachineFix
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Manager.PlayerAction.Action))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts)
        {
            // Multiple methods get this crossFade field and try to fade on it. Problem is, it doesn't exist.
            // Instead of patching everything, create a dummy crossFade when it's being set
            var target = AccessTools.Field(typeof(Manager.PlayerAction), nameof(Manager.PlayerAction.crossFade));
            if (target == null) throw new ArgumentNullException(nameof(target));
            return new CodeMatcher(insts).MatchForward(false, new CodeMatch(OpCodes.Stfld, target))
                                         .ThrowIfInvalid("crossFade not found")
                                         .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(VendingMachineFix), nameof(VendingMachineFix.GiveDummyCrossFade))))
                                         .Instructions();
        }
        
        private static CrossFade _dummyCrossFade;
        private static CrossFade GiveDummyCrossFade(CrossFade existing)
        {
            if (existing) return existing;

            // To disable the fade texBase has to be null. texBase is set in Start so it's delayed from creation.
            if (!_dummyCrossFade)
                _dummyCrossFade = new GameObject("DummyCrossFade").AddComponent<CrossFade>();
            else
                _dummyCrossFade.texBase = null;

            return _dummyCrossFade;
        }
    }

    /// <summary>
    /// Fix wrong position being sometimes set in TalkScene after introduction finishes
    /// </summary>
    [HarmonyPatch]
    public class TalkScenePostAdvFix
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TalkScene), nameof(TalkScene.Introduction), MethodType.Normal)]
        private static void IntroductionPostfix(TalkScene __instance, UniTask __result)
        {
            __instance.StartCoroutine(__result.WaitForFinishCo().AppendCo(() => TalkSceneInterpreter.AdjustPosition(__instance)));
        }
    }

    /// <summary>
    /// Fix crash when playing ADV scenes
    /// </summary>
    [HarmonyPatch]
    public class CycleCrossFadeFix1
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return CoroutineUtils.GetMoveNext(AccessTools.Method(typeof(Cycle), nameof(Cycle.WakeUp)));
        }

        private static bool IsProcessWithNullcheck(CrossFade instance)
        {
            return instance != null && instance.isProcess;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts, MethodBase __originalMethod)
        {
            var targert = AccessTools.PropertyGetter(typeof(CrossFade), nameof(CrossFade.isProcess));
            var replacement = AccessTools.Method(typeof(CycleCrossFadeFix1), nameof(IsProcessWithNullcheck));
            return insts.Manipulator(
                instr => instr.opcode == OpCodes.Callvirt && (MethodInfo)instr.operand == targert,
                instr =>
                {
                    instr.opcode = OpCodes.Call;
                    instr.operand = replacement;
                    VRPlugin.Logger.LogDebug("Patched CrossFade.isProcess in " + __originalMethod.GetFullName());
                });
        }
    }

    /// <summary>
    /// Fix hscene killing the camera at end
    /// </summary>
    [HarmonyPatch]
    public class HSceneFix1
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return CoroutineUtils.GetMoveNext(AccessTools.Method(typeof(HScene), nameof(HScene.Start)));
            yield return CoroutineUtils.GetMoveNext(AccessTools.Method(typeof(HScene), nameof(HScene.ResultTalk)));
        }

        private static UnityEngine.Camera GetOriginalMainCamera()
        {
            // vr camera doesn't have this component on it
            var originalMainCamera = (Manager.Game.instance.cameraEffector ?? Object.FindObjectOfType<CameraEffector>()).GetComponent<UnityEngine.Camera>();
            VRPlugin.Logger.LogDebug($"GetOriginalMainCamera called, cam found: {originalMainCamera?.GetFullPath()}\n{new StackTrace()}");
            return originalMainCamera;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts, MethodBase __originalMethod)
        {
            var targert = AccessTools.PropertyGetter(typeof(UnityEngine.Camera), nameof(UnityEngine.Camera.main));

            VRPlugin.Logger.LogDebug("Patching Camera.main -> null in " + __originalMethod.GetNiceName());

            // Change Camera.main property get to return null instead to skip code that messes with player camera.
            // Only last instance needs to be patched or HScene.ResultTalk will break.
            return new CodeMatcher(insts).End()
                                         .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UnityEngine.Camera), nameof(UnityEngine.Camera.main))))
                                         .ThrowIfInvalid("Camera.main not found in " + __originalMethod.GetNiceName())
                                         .Set(OpCodes.Ldnull, null)
                                         .Instructions();
        }
    }
    /// <summary>
    /// Fix hscene killing the camera at end
    /// </summary>
    [HarmonyPatch(typeof(HScene))]
    public class HSceneFix2
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(HScene.HResultADVCameraSetting), MethodType.Normal)]
        private static bool SkipCameraSetup()
        {
            VRPlugin.Logger.LogDebug("Skipping HScene.HResultADVCameraSetting");
            return false;
        }
    }

    /// <summary>
    /// Fix mainly for character maker, the mask doesn't work properly in VR and goes all over the place.
    /// This removes the mask and applies the amplify effect to the whole camera, with downside of darkening the UI.
    /// This component also exists in some places in main game, but it seems like this patch has no ill effects related to that.
    /// </summary>
    [HarmonyPatch(typeof(CameraEffectorColorMask))]
    public class CameraEffectorColorMaskFix
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CameraEffectorColorMask.Awake), MethodType.Normal)]
        private static bool SkipCameraSetup(CameraEffectorColorMask __instance)
        {
            VRPlugin.Logger.LogDebug("Skipping CameraEffectorColorMask.Awake and destroying the component");
            GameObject.Destroy(__instance);
            return false;
        }
    }
}
