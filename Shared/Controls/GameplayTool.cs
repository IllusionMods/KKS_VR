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

        private KoikatuMenuHandler _menuHandler;

        private Controller.TrackpadDirection _lastDirection;

        private GripMove _grip;

        internal bool IsGrip => _grip != null;

        internal bool IsInit => _init;

        private bool _init;
        public override Texture2D Image
        {
            get;
        }

        protected override void OnDisable()
        {
            DestroyGripMove();
            base.OnDisable();
        }
        protected override void OnDestroy()
        {

        }
        protected override void OnEnable()
        {
            if (!_init
                && Neighbor != null
                && Neighbor.Tools[0] is GameplayTool tool
                && tool.IsInit)
            {
                // Ancient bug, can happen if controller was asleep at the VRGIN's init phase.
                OnRenderModelLoaded();
            }
            base.OnEnable();
        }
        protected override void OnUpdate()
        {
            if (_init)
            {
                HandleInput();
                _grip?.HandleGrabbing();
            }
        }
        internal void OnRenderModelLoaded()
        {
            _init = true;
            _index = Owner == VR.Mode.Left ? 0 : 1;
            _menu = new KoikatuMenuTool(_index);
            _menuHandler = new KoikatuMenuHandler(Owner);
        }
        internal void DestroyGripMove()
        {
            _grip = null;
            KoikatuInterpreter.SceneInput.OnGripMove(_index, active: false);
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
        internal void HideLaser()
        {
            _menuHandler?.SetLaserVisibility(false);
        }

        private void HandleInput()
        {
            var direction = Owner.GetTrackpadDirection();
            var menuInteractable = !_menu.IsAttached && _menuHandler.CheckMenu();

            if (menuInteractable && !_menuHandler.LaserVisible)
            {
                // Don't show laser if something of interest is going on.
                var handler = HandHolder.GetHand(_index).Handler;
                if (KoikatuInterpreter.SceneInput.IsBusy || (handler != null && handler.IsBusy))
                {
                    menuInteractable = false;
                }
                else
                {
                    _menuHandler.SetLaserVisibility(true);
                }
            }

            if (Controller.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
            {
                if (!KoikatuInterpreter.SceneInput.OnButtonDown(_index, EVRButtonId.k_EButton_ApplicationMenu, direction))
                {
                    KoikatuMenuTool.ToggleState();
                }
            }

            if (Controller.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                if (menuInteractable)
                {
                    _menuHandler.OnTrigger(true);
                }
                else if (!KoikatuInterpreter.SceneInput.OnButtonDown(_index, EVRButtonId.k_EButton_SteamVR_Trigger, direction))
                {
                    _grip?.OnTrigger(true);
                }

            }
            else if (Controller.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                if (menuInteractable)
                {
                    _menuHandler.OnTrigger(false);
                }
                else
                {
                    _grip?.OnTrigger(false);
                    KoikatuInterpreter.SceneInput.OnButtonUp(_index, EVRButtonId.k_EButton_SteamVR_Trigger, direction);
                }
            }

            if (Controller.GetPressDown(EVRButtonId.k_EButton_Grip))
            {
                
                if (_menu.IsAttached)
                {
                    _menu.AbandonGUI();
                }
                else if (menuInteractable)
                {
                    _menuHandler.OnGrip(true);
                }
                // If particular interpreter doesn't want grip move right now, it will be blocked.
                else if (!KoikatuInterpreter.SceneInput.OnButtonDown(_index, EVRButtonId.k_EButton_Grip, direction))
                {
                    _grip = new GripMove(HandHolder.GetHand(_index), HandHolder.GetHand(_index == 0 ? 1 : 0));
                    // Grab initial Trigger/Touchpad modifiers, if they were already pressed.
                    if (Controller.GetPress(EVRButtonId.k_EButton_SteamVR_Trigger)) _grip.OnTrigger(true);
                    if (Controller.GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad)) _grip.OnTouchpad(true);
                    KoikatuInterpreter.SceneInput.OnGripMove(_index, active: true);
                }
            }
            else if (Controller.GetPressUp(EVRButtonId.k_EButton_Grip))
            {
                if (menuInteractable)
                {
                    _menuHandler.OnGrip(false);
                }
                else
                {
                    KoikatuInterpreter.SceneInput.OnButtonUp(_index, EVRButtonId.k_EButton_Grip, direction);
                    if (_grip != null)
                    {
                        DestroyGripMove();
                    }
                }
            }

            if (Controller.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                if (menuInteractable)
                {
                    _menuHandler.OnTouchpad(true);
                }
                else if (!KoikatuInterpreter.SceneInput.OnButtonDown(_index, EVRButtonId.k_EButton_SteamVR_Touchpad, direction))
                {
                    _grip?.OnTouchpad(true);
                }
            }
            else if (Controller.GetPressUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                if (menuInteractable)
                {
                    _menuHandler.OnTouchpad(false);
                }
                else
                {
                    _grip?.OnTouchpad(false);
                    KoikatuInterpreter.SceneInput.OnButtonUp(_index, EVRButtonId.k_EButton_SteamVR_Touchpad, direction);
                }
            }

            if (_lastDirection != direction)
            {
                if (menuInteractable)
                {
                    _menuHandler.SetLastDirection(direction);
                }
                else
                {
                    if (_lastDirection != VRGIN.Controls.Controller.TrackpadDirection.Center)
                    {
                        KoikatuInterpreter.SceneInput.OnDirectionUp(_index, _lastDirection);
                    }
                    if (direction != VRGIN.Controls.Controller.TrackpadDirection.Center)
                    {
                        KoikatuInterpreter.SceneInput.OnDirectionDown(_index, direction);
                    }
                }
                _lastDirection = direction;
            }
        }
    }
}
