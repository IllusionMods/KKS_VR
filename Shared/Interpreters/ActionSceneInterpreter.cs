using UnityEngine;
using VRGIN.Core;
using StrayTech;
using KK_VR.Settings;
using KK_VR.Features;
using KK_VR.Camera;
using Manager;
using VRGIN.Controls;
using Valve.VR;
using KK_VR.Handlers;
using static VRGIN.Controls.Controller;
using System.Collections.Generic;
using KK_VR.Controls;
using ADV.Commands.Object;
using WindowsInput.Native;

namespace KK_VR.Interpreters
{
    class ActionSceneInterpreter : SceneInterpreter
    {
        private ActionScene actionScene;

        public static Transform FakeCamera;
        private GameObject _map;
        private GameObject _cameraSystem;
        private Transform _eyes;
        private bool _resetCamera;
        private bool _standing = true;
        private bool _walking;
        private bool _dashing; // ダッシュ時は_Walkingと両方trueになる
        private State _state;
        private float _continuousRotation;
        private float _originAngle;
        private TrackpadDirection _lastDirection;
        private EVRButtonId[] _modifierList = new EVRButtonId[2];
        enum State
        {
            None,
            Walking,
            Striding,
        }
        //private ModelHandler _modelHandler;

        public override void OnStart()
        {
            VRLog.Info("ActionScene OnStart");

            _settings = VR.Context.Settings as KoikatuSettings;

#if KK
            actionScene = Game.Instance.actScene;
#else
            actionScene = ActionScene.instance;
#endif


            ResetState();
            HoldCamera();
            //var height = VR.Camera.Head.position.y - actionScene.Player.chaCtrl.transform.position.y;
            //VRPlugin.Logger.LogWarning($"Interpreter:Action:Start:{height}");
            //_handlers = AddControllerComponent<ActionSceneHandler>();
            //_modelHandler = new ModelHandler();
            //ModelHandler.SetHandColor(Game.Instance.actScene.Player.chaCtrl);
        }

        public override void OnDisable()
        {
            VRLog.Info("ActionScene OnDisable");

            ResetState();
            ReleaseCamera();
        }
        public override void OnUpdate()
        {
            var map = actionScene.Map.mapRoot?.gameObject;

            if (map != _map)
            {

                VRLog.Info("! map changed.");

                ResetState();
                _map = map;
                _resetCamera = true;
            }

            if (_walking)
            {
                MoveCameraToPlayer(true);
            }

            if (_resetCamera)
            {
                ResetCamera();
            }
            if (_continuousRotation != 0f)
            {
                ContinuousRotation(_continuousRotation);
            }
            UpdateCrouch();
        }
        private readonly bool[] _mouseState = new bool[3];
        public override bool OnDirectionDown(int index, TrackpadDirection direction)
        {
            _lastDirection = direction;
            switch (direction)
            {
                case TrackpadDirection.Up:
                    if (!_mouseState[1] && (actionScene.Player.isGateHit || actionScene.Player.actionTarget != null
                        || actionScene.Player.isActionPointHit))
                    {
                        VR.Input.Mouse.RightButtonDown();
                        VRPlugin.Logger.LogDebug($"InteractionAttempt");
                        _mouseState[1] = true;
                    }
                    break;
                case TrackpadDirection.Down:
                    switch (_state)
                    {
                        case State.None:
                        case State.Striding:
                            break;
                        case State.Walking:
                            Crouch();
                            break;
                    }
                    break;
                case TrackpadDirection.Left:
                    if (_state != State.Striding)
                    {
                        Rotation(-_settings.RotationAngle);
                    }
                    break;
                case TrackpadDirection.Right:
                    if (_state != State.Striding)
                    {
                        Rotation(_settings.RotationAngle);
                    }
                    break;
            }
            return false;
        }
        public override void OnDirectionUp(int index, TrackpadDirection direction)
        {
            StopRotation();
            if (_mouseState[0])
            {
                VR.Input.Mouse.LeftButtonUp();
                _mouseState[0] = false;
            }
            if (_mouseState[1])
            {
                VR.Input.Mouse.RightButtonUp();
                _mouseState[1] = false;
            }
            if (_mouseState[2])
            {
                VR.Input.Mouse.MiddleButtonUp();
                _mouseState[2] = false;
            }
        }
        public override bool OnButtonDown(int index, EVRButtonId buttonId, TrackpadDirection direction)
        {
            switch (buttonId)
            {
                case EVRButtonId.k_EButton_SteamVR_Trigger:
                    if (_state == State.None)
                    {
                        StartWalking();
                    }
                    break;
            }
            EvaluateModifiers();
            return false;
        }
        public override void OnButtonUp(int index, EVRButtonId buttonId, TrackpadDirection direction)
        {

            switch (buttonId)
            {
                case EVRButtonId.k_EButton_SteamVR_Trigger:
                    StopWalking();
                    break;
            }
            EvaluateModifiers();
            StandUp();
        }
        private void StartStride()
        {
            _state = State.Striding;
            _originAngle = VR.Camera.Origin.rotation.eulerAngles.y;
        }
        private void EvaluateModifiers()
        {
            switch (_state)
            {
                case State.Walking:
                    if (_modifierList[1] > 0)
                    {
                        StartWalking(dash: true);
                    }
                    else
                    {
                        StartWalking(dash: false);
                    }
                    break;
            }
        }
        private void Stride()
        {

        }
        private void CreateFakeCamera()
        {
            if (FakeCamera == null)
            {
                FakeCamera = new GameObject("FakeCamera").transform;
                FakeCamera.SetParent(MonoBehaviourSingleton<CameraSystem>.Instance.CurrentCamera.transform, worldPositionStays: false);
            }
            VRPlugin.Logger.LogDebug($"Interpreter:Create:FakeCamera");
        }
        private void Rotation(float degrees)
        {
            if (_settings.ContinuousRotation)
            {
                _continuousRotation = degrees * (Mathf.Min(Time.deltaTime, 0.04f) * 2f);
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
            MoveCameraToPlayer(true);
            
            var camera = VR.Camera.transform;
            var newRotation = Quaternion.AngleAxis(degrees, Vector3.up) * camera.rotation;
            VRCameraMover.Instance.MoveTo(camera.position, newRotation);
            MovePlayerToCamera();
            
        }
        private void ContinuousRotation(float degrees)
        {
            var origin = VR.Camera.Origin;
            var head = VR.Camera.Head;
            var newRotation = Quaternion.AngleAxis(degrees, Vector3.up) * origin.rotation;
            var oldPos = head.position;
            origin.rotation = newRotation;
            origin.position += oldPos - head.position;

            if (!_walking)
            {
                MovePlayerToCamera();
            }
        }


        private void ResetState()
        {
            VRLog.Info("ActionScene ResetState");

            StandUp();
            StopWalking();
            _resetCamera = false;
        }

        private void ResetCamera()
        {

            if (actionScene.Player.chaCtrl.objTop.activeSelf)
            {
                _cameraSystem = MonoBehaviourSingleton<CameraSystem>.Instance.gameObject;

                // トイレなどでFPS視点になっている場合にTPS視点に戻す
                _cameraSystem.GetComponent<ActionGame.CameraStateDefinitionChange>().ModeChangeForce((ActionGame.CameraMode?)ActionGame.CameraMode.TPS, true);
                //scene.GetComponent<ActionScene>().isCursorLock = false;

                // カメラをプレイヤーの位置に移動
                MoveCameraToPlayer();

                _resetCamera = false;
                VRLog.Info("ResetCamera succeeded");
            }
        }
        private void HoldCamera()
        {
            VRLog.Info("ActionScene HoldCamera");

            _cameraSystem = MonoBehaviourSingleton<CameraSystem>.Instance.gameObject;

            if (_cameraSystem != null)
            {
                _cameraSystem.SetActive(false);

                VRLog.Info("succeeded");
            }
        }

        private void ReleaseCamera()
        {
            VRLog.Info("ActionScene ReleaseCamera");

            if (_cameraSystem != null)
            {
                _cameraSystem.SetActive(true);

                VRLog.Info("succeeded");
            }
        }


        private void UpdateCrouch()
        {
            var pl = actionScene.Player?.chaCtrl.objTop;

            if (_settings.CrouchByHMDPos && pl?.activeInHierarchy == true)
            {
                var cam = VR.Camera.transform;
                var delta_y = cam.position.y - pl.transform.position.y;

                if (_standing && delta_y < _settings.CrouchThreshold)
                {
                    Crouch();
                }
                else if (!_standing && delta_y > _settings.StandUpThreshold)
                {
                    StandUp();
                }
            }
        }

        public void MoveCameraToPlayer(bool onlyPosition = false)
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

        private Vector3 GetEyesPosition()
        {
            if (_eyes == null)
            {
                _eyes = actionScene.Player.chaCtrl.objHeadBone.transform.Find("cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_tz");
            }
            return _eyes.TransformPoint(0f, _settings.PositionOffsetY, _settings.PositionOffsetZ);
        }
        public void MovePlayerToCamera()
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
            var rot = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
            player.rotation = rot;
            player.position = head.position + vec;
        }

        public void Crouch()
        {
            if (_standing)
            {
                _standing = false;
#if KK
                if (Manager.Config.Instance.xmlCtrl.datas[2] is Config.ActionSystem config
#else
                if (Manager.Config.xmlCtrl.datas[4] is Config.ActionSystem config
#endif
                    && !config.CrouchCtrlKey)
                {
                    VR.Input.Keyboard.KeyDown(VirtualKeyCode.VK_Z);
                }
                else
                {
                    VR.Input.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                }
            }
        }

        public void StandUp()
        {
            if (!_standing)
            {
                _standing = true;
                if (!Manager.Config.ActData.CrouchCtrlKey)
                {
                    VR.Input.Keyboard.KeyUp(VirtualKeyCode.VK_Z);
                }
                else
                {
                    VR.Input.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                }
            }
        }


        public void StartWalking(bool dash = false)
        {
            MovePlayerToCamera();

            if (!dash)
            {
                VR.Input.Keyboard.KeyDown(VirtualKeyCode.LSHIFT);
                _dashing = true;
            }

            VR.Input.Mouse.LeftButtonDown();
            _walking = true;

            HideMaleHead.ForceHideHead = true;
        }

        public void StopWalking()
        {
            VR.Input.Mouse.LeftButtonUp();

            if (_dashing)
            {
                VR.Input.Keyboard.KeyUp(VirtualKeyCode.LSHIFT);
                _dashing = false;
            }

            _walking = false;
            HideMaleHead.ForceHideHead = false;
        }
    }
}
