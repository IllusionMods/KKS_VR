//using System.Collections.Generic;
//using System.Linq;
//using HarmonyLib;
//using Valve.VR;
//using VRGIN.Controls;
//using VRGIN.Controls.Tools;


// Warp tool is gone.
//namespace KKS_VR.Controls
//{
//    /// <summary>
//    /// WarpTool version that cancels teleporting when user tries grab moving
//    /// </summary>
//    /// <seealso cref="VRGIN.Controls.Tools.WarpTool" />
//    internal class BetterWarpTool : WarpTool
//    {
//        protected override void OnUpdate()
//        {
//            // If current state is moving/rotating the teleport target, cancel it so that grab moving
//            // can be done (by default it's stuck in teleport mode until you switch tools)
//            if (Controller.GetPressDown(EVRButtonId.k_EButton_Grip))
//            {
//                var tv = Traverse.Create(this);
//                var state = tv.Field("State");
//                var stateVal = state.GetValue<int>();
//                if (stateVal == 1 || stateVal == 2)
//                {
//                    state.SetValue(0);
//                    tv.Method("SetVisibility", new[] { typeof(bool) }).GetValue(false);
//                }
//            }

//            base.OnUpdate();
//        }

//        public override List<HelpText> GetHelpTexts()
//        {
//            return new List<HelpText>(new[]
//            {
//                ToolUtil.HelpTrackpadCenter(Owner, "Press to teleport"),
//                ToolUtil.HelpGrip(Owner, "Hold to move"),
//            }.Where(x => x != null));
//        }
//    }
//}
