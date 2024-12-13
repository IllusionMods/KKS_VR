using System;
using BepInEx.Logging;
using HarmonyLib;
using KK_VR.Interpreters;
using Studio;
using VRGIN.Core;

namespace KK_VR.Fixes
{
    public static class LoadFixHook
    {
        //public static bool forceSetStandingMode;
        //private static bool standingMode;

        public static void InstallHook()
        {
            new Harmony("HS2VRStudioNEOV2VR.LoadFixHook").PatchAll(typeof(LoadFixHook));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad", new Type[] { })]
        public static bool LoadScenePreHook(Studio.Studio __instance)
        {
            try
            {
                VRPlugin.Logger.Log(LogLevel.Debug, "Start Scene Loading.");
                if (VRManager.Instance.Mode is StudioStandingMode) ((KKSCharaStudioInterpreter)VR.Manager.Interpreter).ForceResetVRMode();
            }
            catch (Exception obj)
            {
                VRLog.Error(obj);
            }

            return true;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Scene), "UnloadBaseScene", new Type[] { })]
        //public static void UnloadBaseScenePreHook()
        //{
        //    try
        //    {
        //        KKSCharaStudioVRPlugin.PluginLogger.Log(LogLevel.Debug, "Start Scene or Map Unloading.");
        //        if (VRManager.Instance.Mode is GenericStandingMode)
        //            standingMode = true;
        //        else
        //            standingMode = false;
        //    }
        //    catch (Exception obj)
        //    {
        //        VRLog.Error(obj);
        //    }
        //}

        // todo invalid patch target
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Map), nameof(Map.OnLoadAfter), new Type[] { typeof(string) })]
        //public static void OnLoadAfter(Map __instance, string levelName)
        //{
        //	if (standingMode)
        //	{
        //		KKSCharaStudioVRPlugin.PluginLogger.Log(LogLevel.Debug, "Start Scene or Map Unloading.");
        //		(VR.Manager.Interpreter as KKSCharaStudioInterpreter).ForceResetVRMode();
        //	}
        //}
    }
}
