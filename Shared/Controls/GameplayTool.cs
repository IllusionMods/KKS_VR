using UnityEngine;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using KK_VR.Interpreters;
using Valve.VR;
using KK_VR.Holders;
//using EVRButtonId = Unity.XR.OpenVR.EVRButtonId;

namespace KK_VR.Controls
{
    public class GameplayTool : Tool
    {
        private int _index;

        private KoikatuMenuTool _menu;

        private Controller.TrackpadDirection _lastDirection;

        private GripMove _grip;

        internal bool IsGrip => _grip != null;

        public override Texture2D Image
        {
            get;
        }
        protected override void OnStart()
        {
            base.OnStart();
            //SetScene();

            // Tracking index loves to f us on the init phase. 
            _index = Owner == VR.Mode.Left ? 0 : 1;
            _menu = new KoikatuMenuTool(_index);
        }

        protected override void OnDisable()
        {
            DestroyGrab();
            base.OnDisable();
        }

        protected override void OnUpdate()
        {
            HandleInput();
            _grip?.HandleGrabbing();
        }

        internal void DestroyGrab()
        {
            KoikatuInterpreter.SceneInterpreter.OnGripMove(_index, active: false);
            _grip = null;
        }
        internal void LazyGripMove(int avgFrame)
        {
            // In all honesty tho, the proper name would be retarded, not lazy as it does way more in this mode and lags behind.
            _grip?.StartLag(avgFrame);
        }
        internal void AttachGripMove(Transform attachPoint)
        {
            _grip?.AttachGripMove(attachPoint);
        }
        internal void UnlazyGripMove()
        {
            _grip?.StopLag();
        }

        private void HandleInput()
        {
            var direction = Owner.GetTrackpadDirection();

            if (Controller.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
            {
                if (!KoikatuInterpreter.SceneInterpreter.OnButtonDown(_index, EVRButtonId.k_EButton_ApplicationMenu, direction))
                {
                    _menu.ToggleState();
                }
            }
            if (Controller.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                if (!KoikatuInterpreter.SceneInterpreter.OnButtonDown(_index, EVRButtonId.k_EButton_SteamVR_Trigger, direction))
                {
                    _grip?.OnTrigger(true);
                }

            }
            else if (Controller.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                _grip?.OnTrigger(false);
                KoikatuInterpreter.SceneInterpreter.OnButtonUp(_index, EVRButtonId.k_EButton_SteamVR_Trigger, direction);
            }

            if (Controller.GetPressDown(EVRButtonId.k_EButton_Grip))
            {
                // If particular interpreter doesn't want grip move right now, it will be blocked.
                if (_menu.attached)
                {
                    _menu.AbandonGUI();
                }
                else if (!KoikatuInterpreter.SceneInterpreter.OnButtonDown(_index, EVRButtonId.k_EButton_Grip, direction))
                {
                    _grip = new GripMove(HandHolder.GetHand(_index), HandHolder.GetHand(_index == 0 ? 1 : 0));
                    // Grab initial Trigger/Touchpad modifiers, if they were already pressed.
                    if (Controller.GetPress(EVRButtonId.k_EButton_SteamVR_Trigger)) _grip.OnTrigger(true);
                    if (Controller.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad)) _grip.OnTouchpad(true);
                    KoikatuInterpreter.SceneInterpreter.OnGripMove(_index, active: true);
                }
            }
            else if (Controller.GetPressUp(EVRButtonId.k_EButton_Grip))
            {
                if (_grip != null) DestroyGrab();
                else
                    KoikatuInterpreter.SceneInterpreter.OnButtonUp(_index, EVRButtonId.k_EButton_Grip, direction);
            }
            if (Controller.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                if (!KoikatuInterpreter.SceneInterpreter.OnButtonDown(_index, EVRButtonId.k_EButton_SteamVR_Touchpad, direction))
                {
                    _grip?.OnTouchpad(true);
                }
            }
            else if (Controller.GetPressUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                _grip?.OnTouchpad(false);
                KoikatuInterpreter.SceneInterpreter.OnButtonUp(_index, EVRButtonId.k_EButton_SteamVR_Touchpad, direction);
            }

            if (_lastDirection != direction)
            {
                if (_lastDirection != VRGIN.Controls.Controller.TrackpadDirection.Center)
                {
                    KoikatuInterpreter.SceneInterpreter.OnDirectionUp(_index, _lastDirection);
                }
                if (direction != VRGIN.Controls.Controller.TrackpadDirection.Center)
                {
                    KoikatuInterpreter.SceneInterpreter.OnDirectionDown(_index, direction);
                }
                _lastDirection = direction;
            }
        }
    }
}
