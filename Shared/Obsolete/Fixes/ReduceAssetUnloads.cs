//using System.Collections.Generic;
//using System.Reflection;
//using System.Reflection.Emit;
//using HarmonyLib;
//using KKAPI.Utilities;
//using UnityEngine;
//using VRGIN.Core;

//namespace KK_VR.Fixes
//{
//    /// <summary>

//    ///Guess it was moved to and conflicts with the IllusionFix nowadays, as UniRx freaks out all the time because of this.

//    /// Avoid triggering resource unload when loading UI-only scenes.
//    /// todo move into illusionfixes?
//    /// </summary>
//    [HarmonyPatch]
//    public class ReduceAssetUnloads
//    {
//        private static IEnumerable<MethodBase> TargetMethods()
//        {
//            yield return CoroutineUtils.GetMoveNext(AccessTools.Method(typeof(Manager.Scene), nameof(Manager.Scene.LoadStart)));
//        }

//        private static AsyncOperation MaybeUnloadUnusedAssets()
//        {
//#if KK
//            if (Manager.Scene.Instance.IsFadeNow)
//#else
//            if (Manager.Scene.IsFadeNow)
//#endif
//            {
//                return Resources.UnloadUnusedAssets();
//            }
//            else
//            {
//                VRLog.Info("Skipping unload");
//                return null;
//            }
//        }
//        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> insts, MethodBase __originalMethod)
//        {
//            foreach (var inst in insts)
//                if (inst.opcode == OpCodes.Call &&
//                    inst.operand is MethodInfo method &&
//                    method.Name == "UnloadUnusedAssets")
//                {
//                    yield return CodeInstruction.Call(() => MaybeUnloadUnusedAssets());
//                    //VRPlugin.Logger.LogDebug("Patched UnloadUnusedAssets in " + __originalMethod.GetFullName());
//                }
//                else
//                {
//                    yield return inst;
//                }
//        }
//    }
//}
