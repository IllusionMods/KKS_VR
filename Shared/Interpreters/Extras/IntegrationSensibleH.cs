using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using static HandCtrl;
using static KK_VR.IntegrationMaleBreath;

namespace KK_VR
{
    internal static class IntegrationSensibleH
    {
        internal static bool IsActive => _active;
        private static bool _active;

        // Concedes control of an aibu item.
        internal static Action<AibuColliderKind> ReleaseItem;

        // Way too much illegitimate stuff is going on in auto caress, so we can't use normal one,
        // this one among other things will refuse to do it if we are about to break the game.
        internal static Action<AibuColliderKind> JudgeProc;

        /// <summary>
        /// Uses StartsWith to find and click the button, or picks any if not specified (in this case ignores fast/slow in houshi).
        /// </summary>
        internal static Action<string> ClickButton;

        // Changes current loop between Weak/Strong/Orgasm.
        internal static Action<int> ChangeLoop;

        // Disables H AutoMode, will get re-enabled from inputs/automatically depending on the settings.
        internal static Action StopAuto;

        /// <summary>
        /// -1 for all, (HFlag.EMode) 0...2 for specific, or anything higher(e.g. 3) for current EMode.
        /// </summary>
        internal static Action<int> ChangeAnimation;

        // Prevents SensibleH side from breaking.
        internal static Action<AibuColliderKind> OnLickStart;

        // Provides neat neck and hooks up SensibleH version of CyuVR.
        internal static Action<AibuColliderKind> OnKissStart;

        // Neatly cleans/restores everything.
        internal static Action OnKissEnd;

        // Hook to start AutoMode.
        internal static Action OnUserInput;

        //// Custom top of the excitement gauge to trigger orgasm, set by SensibleH dynamically.
        //internal static Func<float> GetFemaleCeiling;
        //internal static Func<float> GetMaleCeiling;

        internal static void Init()
        {
            var type = AccessTools.TypeByName("KK_SensibleH.AutoMode.LoopController");

            if (type == null) return;

            if (GetMethod(type, "ClickButton", out var clickButton))
            {
                ClickButton = AccessTools.MethodDelegate<Action<string>>(clickButton);
            }

            if (GetMethod(type, "AlterLoop", out var alterLoop))
            {
                ChangeLoop = AccessTools.MethodDelegate<Action<int>>(alterLoop);
            }

            if (GetMethod(type, "PickAnimation", out var pickAnimation))
            {
                ChangeAnimation = AccessTools.MethodDelegate<Action<int>>(pickAnimation);
            }

            if (GetMethod(type, "Sleep", out var sleep))
            {
                StopAuto = AccessTools.MethodDelegate<Action>(sleep);
            }

            if (GetMethod(type, "OnUserInput", out var onUserInput))
            {
                OnUserInput = AccessTools.MethodDelegate<Action>(onUserInput);
            }

            type = AccessTools.TypeByName("KK_SensibleH.Caress.MoMiController");
            if (type == null) return;


            if (GetMethod(type, "ReleaseItem", out var releaseItem))
            {
                ReleaseItem = AccessTools.MethodDelegate<Action<AibuColliderKind>>(releaseItem);
            }

            if (GetMethod(type, "MoMiJudgeProc", out var moMiJudgeProc))
            {
                JudgeProc = AccessTools.MethodDelegate<Action<AibuColliderKind>>(moMiJudgeProc);
            }

            if (GetMethod(type, "OnLickStart", out var onLickStart))
            {
                OnLickStart = AccessTools.MethodDelegate<Action<AibuColliderKind>>(onLickStart);
            }

            if (GetMethod(type, "OnKissStart", out var onKissStart))
            {
                OnKissStart = AccessTools.MethodDelegate<Action<AibuColliderKind>>(onKissStart);
            }

            if (GetMethod(type, "OnKissEnd", out var onKissEnd))
            {
                OnKissEnd = AccessTools.MethodDelegate<Action>(onKissEnd);
            }

            _active = ClickButton != null
                && ChangeLoop != null
                && ChangeAnimation != null
                && StopAuto != null
                && OnUserInput != null
                && ReleaseItem != null
                && JudgeProc != null
                && OnLickStart != null
                && OnKissStart != null
                && OnKissEnd != null;


        }
    }
}
