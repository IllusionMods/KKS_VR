#if KKS
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using HarmonyLib;
using Obi;
using UnityEngine;
using VRGIN.Core;


namespace KK_VR.Fixes
{
    #region Fix display of bodily fluids in H scene.
    [HarmonyPatch]
    public class ObiCtrlFix
    {
        // Due to being quite expensive (about 10 - 15% added gpu load), and vr being extremely gpu hungry, we keep it disabled until we want it.
        // In this state it adds about 1 - 2% to load, further suppression isn't worth it due to intricacy.
        // It puzzles me why they went for gpu implenetation and not cpu. Wasn't available in older version?

        // Native code resets particles every time animController changes state, changed to more persistent behavior in SensibleH.

        public static bool _activeState;
        public static bool _rendererState;
        public static bool VRGINCameraSet;
        public static ObiFluidRenderer _obiComponent;
        public static void OnHSceneEnd()
        {
            _activeState = false;
            VRGINCameraSet = false;
            _rendererState = false;
            if (_obiComponent != null)
            {
                Component.Destroy(VR.Camera.GetComponent<ObiFluidRenderer>());
            }
        }

        public static void SetFluidsState(bool state) => _activeState = state;

        public static void SetRenderer(bool state)
        {
            if (_obiComponent == null)
            {
                _obiComponent = VR.Camera.GetComponent<ObiFluidRenderer>();
            }
            _rendererState = state;
            _obiComponent.enabled = state;
        }

        /// <summary>
        /// We throttle down activity of the fluid system when there is no need for it.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ObiCtrl), nameof(ObiCtrl.Proc))]
        public static bool ObiCtrlProcPrefix(ObiCtrl __instance)
        {
            if (!_activeState)
            {
                if (_rendererState)
                {
                    SetRenderer(false);
                    __instance.solver.gameObject.SetActive(false);
                }
                return false;
            }
            else
            {
                if (!_rendererState)
                {
                    SetRenderer(true);
                    __instance.solver.gameObject.SetActive(true);
                }
                return true;
            }
        }
        // Hooks to enable renderer.
        // The rest is in CameraMoveHooks.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiInside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiOutside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuInside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalInside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuOutside))]
        [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalOutside))]
        public static void AddFinishPostfix()
        {
            SetFluidsState(true);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ObiFluidManager), nameof(ObiFluidManager.Setup))]
        public static void ObiFluidManagerSetupPrefix(ObiFluidManager __instance)
        {
            if (!VRGINCameraSet)
            {
                //VRPlugin.Logger.LogDebug($"ObiFluidManagerSetup[TransferComponentToVRGIN][Initialized = {__instance.obiFluidRenderer[0] != null}]");
                VRGINCameraSet = true;
                _rendererState = true;
                var origComponent = __instance.obiFluidRenderer[0];
                VRGIN.Helpers.UnityHelper.CopyComponent(origComponent, VR.Camera.gameObject);
                __instance.obiFluidRenderer[0] = VR.Camera.GetComponent<ObiFluidRenderer>();
                Component.Destroy(origComponent);
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ObiBaseFluidRenderer), "Awake")]
        public static IEnumerable<CodeInstruction> AwakeCameraSubstitute(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var done = false;
            foreach (var code in instructions)
            {
                if (found && !done)
                {
                    if (code.opcode == OpCodes.Ldarg_0)
                    {
                        yield return new CodeInstruction(OpCodes.Nop);
                        continue;
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ObiCtrlFix), nameof(ObiCtrlFix.GetVRCamera)));
                        done = true;
                        continue;
                    }
                }

                yield return code;
                found = true;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ObiBaseFluidRenderer), "DestroyCommandBuffer")]
        public static IEnumerable<CodeInstruction> DestroyCommandBufferCameraSubstitute(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var done = false;
            var counter = 0;
            foreach (var code in instructions)
            {
                if (!found && code.opcode == OpCodes.Ldarg_0)
                {
                    counter++;
                    if (counter == 2)
                    {
                        found = true;
                    }
                }
                if (found && !done)
                {
                    if (code.opcode == OpCodes.Ldarg_0)
                    {
                        yield return new CodeInstruction(OpCodes.Nop);
                        continue;
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ObiCtrlFix), nameof(ObiCtrlFix.GetVRCamera)));
                        done = true;
                        continue;
                    }
                }
                yield return code;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ObiBaseFluidRenderer), nameof(ObiBaseFluidRenderer.OnEnable))]
        public static void ObiFluidRendererOnEnablePostfix()
        {
            VR.Camera.GetComponent<UnityEngine.Camera>().forceIntoRenderTexture = true;
        }
        public static UnityEngine.Camera GetVRCamera()
        {
            return VR.Camera.GetComponent<UnityEngine.Camera>();
        }
    }
    #endregion
}
#endif