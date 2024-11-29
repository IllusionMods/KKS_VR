using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Visuals;
using Object = UnityEngine.Object;


namespace KK_VR.Controls
{
    /// <summary>
    /// MenuTool that supports right clicks by pressing the trackpad
    /// </summary>
    internal class BetterMenuTool : MenuTool
    {
        //public override List<HelpText> GetHelpTexts()
        //{
        //    return new List<HelpText>(new[]
        //    {
        //        ToolUtil.HelpTrackpadCenter(Owner, "Press to Right Click"),
        //        ToolUtil.HelpTrackpadUp(Owner, "Slide to Move Cursor"),
        //        ToolUtil.HelpTrigger(Owner, "Left Click"),
        //        ToolUtil.HelpGrip(Owner, "Take/release screen")
        //    }.Where(x => x != null));
        //}

        #region Override base functionality to add right clicks

        private float pressDownTime;
        private Vector2 touchDownPosition;
        private double _DeltaX;
        private double _DeltaY;

        protected override void OnUpdate()
        {
            //base.OnUpdate();
            var controller = Controller;

            if (controller.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                VR.Input.Mouse.RightButtonDown();
                pressDownTime = Time.unscaledTime;
            }
            if (controller.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                VR.Input.Mouse.LeftButtonDown();
                pressDownTime = Time.unscaledTime;
            }

            if (controller.GetPressUp(EVRButtonId.k_EButton_Grip))
            {
                if ((bool)(Object)Gui)
                    AbandonGUI();
                else
                    TakeGUI(GUIQuadRegistry.Quads.FirstOrDefault(q => !q.IsOwned));
            }
            if (controller.GetTouchDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                touchDownPosition = controller.GetAxis();
                //this.touchDownMousePosition = MouseOperations.GetClientCursorPosition();
            }
            if (controller.GetTouch(EVRButtonId.k_EButton_SteamVR_Touchpad) && Time.unscaledTime - pressDownTime > 0.3f)
            {
                var axis = controller.GetAxis();
                var vector2 = axis - (VR.HMD == HMDType.Oculus ? Vector2.zero : touchDownPosition);
                var num = VR.HMD == HMDType.Oculus ? Time.unscaledDeltaTime * 5f : 1f;
                _DeltaX += vector2.x * (double)VRGUI.Width * 0.1 * num;
                _DeltaY += -(double)vector2.y * VRGUI.Height * 0.2 * num;
                var pixelDeltaX = _DeltaX > 0.0 ? (int)Math.Floor(_DeltaX) : (int)Math.Ceiling(_DeltaX);
                var pixelDeltaY = _DeltaY > 0.0 ? (int)Math.Floor(_DeltaY) : (int)Math.Ceiling(_DeltaY);
                _DeltaX -= pixelDeltaX;
                _DeltaY -= pixelDeltaY;
                VR.Input.Mouse.MoveMouseBy(pixelDeltaX, pixelDeltaY);
                touchDownPosition = axis;
            }

            if (controller.GetPressUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                VR.Input.Mouse.RightButtonUp();
                pressDownTime = 0.0f;
            }
            if (controller.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                VR.Input.Mouse.LeftButtonUp();
                pressDownTime = 0.0f;
            }
        }

        #endregion
    }
}
