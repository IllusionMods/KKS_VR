using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using static HandCtrl;

namespace KK_VR
{
    internal class IntegrationSensibleH
    {
        internal static bool active;

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

        // Custom top of the excitement gauge to trigger orgasm, set by SensibleH dynamically.
        internal static Func<float> GetFemaleCeiling;
        internal static Func<float> GetMaleCeiling;

        internal static void Init()
        {
            var type = AccessTools.TypeByName("KK_SensibleH.AutoMode.LoopController");
            active = type != null;

            if (active)
            {
                ClickButton = AccessTools.MethodDelegate<Action<string>>(AccessTools.FirstMethod(type, m => m.Name.Equals("ClickButton")));
                ChangeLoop = AccessTools.MethodDelegate<Action<int>>(AccessTools.FirstMethod(type, m => m.Name.Equals("AlterLoop")));
                ChangeAnimation = AccessTools.MethodDelegate<Action<int>>(AccessTools.FirstMethod(type, m => m.Name.Equals("PickAnimation")));
                StopAuto = AccessTools.MethodDelegate<Action>(AccessTools.FirstMethod(type, m => m.Name.Equals("Sleep")));
                OnUserInput = AccessTools.MethodDelegate<Action>(AccessTools.FirstMethod(type, m => m.Name.Equals("OnUserInput")));

                type = AccessTools.TypeByName("KK_SensibleH.Caress.MoMiController");

                ReleaseItem = AccessTools.MethodDelegate<Action<AibuColliderKind>>(AccessTools.FirstMethod(type, m => m.Name.Equals("ReleaseItem")));
                JudgeProc = AccessTools.MethodDelegate<Action<AibuColliderKind>>(AccessTools.FirstMethod(type, m => m.Name.Equals("MoMiJudgeProc")));
                OnLickStart = AccessTools.MethodDelegate<Action<AibuColliderKind>>(AccessTools.FirstMethod(type, m => m.Name.Equals("OnLickStart")));
                OnKissStart = AccessTools.MethodDelegate<Action<AibuColliderKind>>(AccessTools.FirstMethod(type, m => m.Name.Equals("OnKissStart")));
                OnKissEnd = AccessTools.MethodDelegate<Action>(AccessTools.FirstMethod(type, m => m.Name.Equals("OnKissEnd")));

            }
        }
    }
}
