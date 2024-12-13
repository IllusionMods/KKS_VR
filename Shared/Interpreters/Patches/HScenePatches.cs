using HarmonyLib;
using KK_VR.Camera;
using KK_VR.Caress;
using KK_VR.Features;
using KK_VR.Grasp;
using KK_VR.Interpreters;
using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace KK_VR.Patches
{
    [HarmonyPatch]
    internal class HScenePatches
    {
        internal static bool suppressSetIdle;
        private static bool _fakeKiss;
        private static float _timeStamp;
        private static bool _fakeDislike;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HAibu), nameof(HAibu.SetIdleForItem))]
        public static void SetIdleForItemPrefix(ref bool _setplay, HAibu __instance)
        {
            if (suppressSetIdle)
            {
                __instance.backIdle = HSceneInterpreter.GetBackIdle;
                _setplay = false;
                suppressSetIdle = false;
            }
        }
        internal static void FakeDislike()
        {
            _fakeDislike = true;
        }
        internal static void HoldKissLoop()
        {
            _fakeKiss = true;
            _timeStamp = Time.time + 4f + UnityEngine.Random.value * 2f;
        }
        public static int GetBackIdle()
        {
            var list = new List<int>();
            foreach (var item in HSceneInterpreter.handCtrl.useItems)
            {
                if (item != null)
                {
                    list.Add((int)item.kindTouch - 2);
                }
            }
            var count = list.Count;
            if (count != 0)
            {
                var slot = list[UnityEngine.Random.Range(0, count)];
                if (slot < 2)
                {
                    return 1;
                }
                if (slot > 2)
                {
                    return 3;
                }
                return slot;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Return True to branch away, or False to go in.
        /// </summary>
        public static bool KissEndCondition(HandCtrl hand)
        {
            if (_fakeKiss)
            {
                var result = _timeStamp > Time.time;
                if (!result)
                {
                    _fakeKiss = false;

                    // Temporal solution.
                    if (HSceneInterpreter.hAibu.backIdle == -1 || !HSceneInterpreter.IsHandAttached)
                    {
                        HSceneInterpreter.hAibu.backIdle = GetBackIdle();
                    }

                    if (hand.action == HandCtrl.HandAction.none)
                    {
                        HSceneInterpreter.SetPlay("Idle");
                        //CaressHelper.Instance.Halt(disengage: false, haltVRMouth: false);
                        if (!HSceneInterpreter.IsVoiceActive)
                        {
                            HSceneInterpreter.hFlag.voice.playVoices[0] = 100;
                        }
                        return true;
                    }
                    else
                    {

                        return hand.action != HandCtrl.HandAction.none;
                    }
                }
                return result;
            }
            else
            {
                return hand.action != HandCtrl.HandAction.none;
            }
        }
        /// <summary>
        /// We allow the girl to stay in K_Loop animation for a bit outside of actual kiss if there was a trigger. 
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(HAibu), nameof(HAibu.Proc))]
        public static IEnumerable<CodeInstruction> HAibuProcTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var counter = 0;
            var done = false;
            foreach (var code in instructions)
            {
                if (!done)
                {

                    if (counter == 0)
                    {
                        if (code.opcode == OpCodes.Ldfld
                        && code.operand is FieldInfo info && info.Name.Equals("hand"))
                        {
                            counter++;
                        }
                    }
                    else
                    {
                        if (code.opcode == OpCodes.Ldfld
                        && code.operand is FieldInfo info && info.Name.Equals("action"))
                        {
                            done = true;
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.FirstMethod(typeof(HScenePatches), m => m.Name.Equals(nameof(HScenePatches.KissEndCondition))));
                            continue;
                        }
                        else
                        {
                            counter = 0;
                        }
                    }
                }
                yield return code;
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(HitReaction.HitPointEffector.EffectorLink), nameof(HitReaction.HitPointEffector.EffectorLink.Apply))]
        public static void HitReactionApplyPostfix(IKSolverFullBodyBiped solver, HitReaction.HitPointEffector.EffectorLink __instance)
        {
            if (GraspHelper.Instance != null)
            {
                GraspHelper.Instance.CatchHitReaction(solver, __instance.current, (int)__instance.effector);
            }
        }
    }
}
