using System;
using System.Collections.Generic;
using System.Text;
using KK_VR.Camera;
using KK_VR.Features;
using KK_VR.Holders;
using UnityEngine;
using VRGIN.Core;
using WindowsInput.Native;
using static KK_VR.Interpreters.ActionSceneInterpreter;
using static VRGIN.Controls.Controller;

namespace KK_VR.Interpreters
{
    internal class ActionSceneInput : SceneInput
    {
        private bool _standing = true;
        /// <summary>
        /// For button prompted crouch.
        /// </summary>
        private bool _crouching;
        private bool _walking;
        private float _continuousRotation;
        private Pressed _buttons;
        internal Transform _eyes;

        internal Vector3 GetEyesPosition()
        {
            if (_eyes == null)
            {
                _eyes = actionScene.Player.chaCtrl.objHeadBone.transform.Find("cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_tz");
            }
            return _eyes.TransformPoint(0f, _settings.PositionOffsetY, _settings.PositionOffsetZ);
        }

        enum Pressed
        {
            LeftMouse = 1,
            RightMouse = 2,
            Shift = 4,
            Ctrl = 8,
            Z = 16
        }
        private bool IsButtonPressed(Pressed button)
        {
            return (_buttons & button) != 0;
        }
        private void PressButton(Pressed button)
        {
            if (!IsButtonPressed(button))
            {
                _buttons |= button;
                switch (button)
                {
                    case Pressed.LeftMouse:
                        VR.Input.Mouse.LeftButtonDown();
                        break;
                    case Pressed.RightMouse:
                        VR.Input.Mouse.RightButtonDown();
                        break;
                    case Pressed.Shift:
                        VR.Input.Keyboard.KeyDown(VirtualKeyCode.LSHIFT);
                        break;
                    case Pressed.Ctrl:
                        VR.Input.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                        break;
                    case Pressed.Z:
                        VR.Input.Keyboard.KeyDown(VirtualKeyCode.VK_Z);
                        break;
                }
            }
        }
        private void ReleaseButton(Pressed button)
        {
            if (IsButtonPressed(button))
            {
                _buttons &= ~button;
                switch (button)
                {
                    case Pressed.LeftMouse:
                        VR.Input.Mouse.LeftButtonUp();
                        break;
                    case Pressed.RightMouse:
                        VR.Input.Mouse.RightButtonUp();
                        break;
                    case Pressed.Shift:
                        VR.Input.Keyboard.KeyUp(VirtualKeyCode.LSHIFT);
                        break;
                    case Pressed.Ctrl:
                        VR.Input.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                        break;
                    case Pressed.Z:
                        VR.Input.Keyboard.KeyUp(VirtualKeyCode.VK_Z);
                        break;
                }
            }
        }

        internal override void OnDisable()
        {
            ResetState();
        }

        internal override void HandleInput()
        {
            base.HandleInput();
            if (_continuousRotation != 0f)
            {
                ContinuousRotation(_continuousRotation);
            }
            if (_walking)
            {
                CameraToPlayer(true);
            }
            UpdateCrouch();
        }
        internal override bool OnDirectionDown(int index, TrackpadDirection direction)
        {
            switch (direction)
            {
                case TrackpadDirection.Up:
                    if (actionScene.Player.isGateHit 
                        || actionScene.Player.actionTarget != null
                        || actionScene.Player.isActionPointHit)
                    {
                        PressButton(Pressed.RightMouse);
                    }
                    else if (_walking)
                    {
                        ToggleDash();
                    }
                    break;
                case TrackpadDirection.Down:
                    if (!_crouching)
                    {
                        Crouch(buttonPrompt: true);
                    }
                    else
                    {
                        StandUp();
                    }
                    break;
                case TrackpadDirection.Left:
                    Rotation(-_settings.RotationAngle);
                    break;
                case TrackpadDirection.Right:
                    Rotation(_settings.RotationAngle);
                    break;
            }
            return false;
        }

        internal override void OnDirectionUp(int index, TrackpadDirection direction)
        {
            StopRotation();
        }

        protected override bool OnTrigger(int index, bool press)
        {
            if (press)
            {
                _pressedButtons[index, 0] = true;
                StartWalking();
            }
            else
            {
                _pressedButtons[index, 0] = false;
                ResetState();
            }
            return false;
        }

        private void Rotation(float degrees)
        {
            if (_settings.ContinuousRotation)
            {
                _continuousRotation = degrees;
            }
            else
            {
                SnapRotation(degrees);
            }
        }

        private void StopRotation()
        {
            _continuousRotation = 0f;
        }

        /// <summary>
        /// Rotate the camera. If we are in Roaming, rotate the protagonist as well.
        /// </summary>
        private void SnapRotation(float degrees)
        {
            //VRLog.Debug("Rotating {0} degrees", degrees);
            CameraToPlayer(true);

            var camera = VR.Camera.transform;
            var newRotation = Quaternion.AngleAxis(degrees, Vector3.up) * camera.rotation;
            VRCameraMover.Instance.MoveTo(camera.position, newRotation);
            PlayerToCamera();
        }
        private void ContinuousRotation(float degrees)
        {
            var origin = VR.Camera.Origin;
            var head = VR.Camera.Head;
            var oldPos = head.position;
            origin.rotation = Quaternion.Euler(0f, degrees * (Time.deltaTime * 2f), 0f) * origin.rotation;
            origin.position += oldPos - head.position;

            if (!_walking)
            {
                PlayerToCamera();
            }
        }

        public void CameraToPlayer(bool onlyPosition = false)
        {

            //var headCam = VR.Camera.transform;

            var pos = GetEyesPosition();
            if (!_settings.UsingHeadPos)
            {
                var player = actionScene.Player;
                pos.y = player.position.y + (_standing ? _settings.StandingCameraPos : _settings.CrouchingCameraPos);
            }

            VR.Mode.MoveToPosition(pos, onlyPosition ? Quaternion.Euler(0f, VR.Camera.transform.eulerAngles.y, 0f) : _eyes.rotation, false);
            //VRMover.Instance.MoveTo(
            //    //pos + cf * 0.23f, // 首が見えるとうざいのでほんの少し前目にする
            //    pos,
            //    onlyPosition ? headCam.rotation : _eyes.rotation,
            //    false,
            //    quiet);
        }

        public void PlayerToCamera()
        {
            var player = actionScene.Player;
            var head = VR.Camera.Head;

            var vec = player.position - GetEyesPosition();
            if (!_settings.UsingHeadPos)
            {
                var attachPoint = player.position;
                attachPoint.y = _standing ? _settings.StandingCameraPos : _settings.CrouchingCameraPos;
                vec = player.position - attachPoint;
            }
            player.rotation = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
            player.position = head.position + vec;
        }

        private void UpdateCrouch()
        {
            if (!_crouching && actionScene.Player.chaCtrl.objTop != null)
            {
                var objTop = actionScene.Player.chaCtrl.objTop;
                if (_settings.CrouchByCameraPos && objTop.activeInHierarchy == true)
                {
                    var delta_y = VR.Camera.transform.position.y - objTop.transform.position.y;

                    if (_standing && delta_y < 0.8f)
                    {
                        Crouch(buttonPrompt: false);
                    }
                    else if (!_standing && delta_y > 1f)
                    {
                        StandUp();
                    }
                }
            }
        }

        internal void StartWalking()
        {
            PlayerToCamera();
            PressButton(Pressed.Shift);
            PressButton(Pressed.LeftMouse);
            _walking = true;
            HideMaleHead.ForceHideHead = true;
        }

        private void ToggleDash()
        {
            if (IsButtonPressed(Pressed.Shift))
            {
                ReleaseButton(Pressed.Shift);
            }
            else
            {
                PressButton(Pressed.Shift);
            }
        }

        internal void ResetState()
        {
            foreach (Pressed button in Enum.GetValues(typeof(Pressed)))
            {
                ReleaseButton(button);
            }
            _walking = false;
            _standing = true;
            _crouching = false;
            HideMaleHead.ForceHideHead = false;
        }

        internal void Crouch(bool buttonPrompt)
        {
            if (_standing)
            {
                _standing = false;
                _crouching = buttonPrompt;
                if (!Manager.Config.ActData.CrouchCtrlKey)
                {
                    PressButton(Pressed.Z);
                }
                else
                {
                    PressButton(Pressed.Ctrl);
                }
            }
        }
        internal void StandUp()
        {
            if (!_standing)
            {
                _standing = true;
                _crouching = false;
                if (_walking)
                {
                    // Don't run after standing up.
                    PressButton(Pressed.Shift);
                }
                if (!Manager.Config.ActData.CrouchCtrlKey)
                {
                    ReleaseButton(Pressed.Z);
                }
                else
                {
                    ReleaseButton(Pressed.Ctrl);
                }
            }
        }
    }
}
