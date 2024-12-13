#if KK
using HarmonyLib;
using KK_VR.Interpreters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KK_VR.Patches
{
    [HarmonyPatch]
    internal class TalkScenePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TalkScene), nameof(TalkScene.Awake))]
        public static void TalkSceneAwakePrefix(TalkScene __instance)
        {
            // A cheap surefire way to differentiate between TalkScene/ADV.
            //VRPlugin.Logger.LogDebug($"TalkScene:Awake:{KoikatuInterpreter.CurrentScene}");
            if (KoikatuInterpreter.CurrentScene == KoikatuInterpreter.SceneType.TalkScene)
            {
                ((TalkSceneInterpreter)KoikatuInterpreter.SceneInterpreter).OverrideAdv(__instance);
            }
            else
            {
                KoikatuInterpreter.StartScene(KoikatuInterpreter.SceneType.TalkScene, __instance);
            }
        }
    }
}
#endif
