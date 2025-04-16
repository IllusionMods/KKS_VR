using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Core;
using UniRx;
using Manager;
using KK_VR.Settings;
using KK_VR.Interpreters;
using KK_VR.Handlers;
using KK_VR.Camera;
using System;
using UnityEngine.Events;

// Will make proper namespace once fixed in SensibleH
namespace KK_VR.Features
{
    public class PoV : MonoBehaviour
    {
        private class OneWayTrip
        {
            internal OneWayTrip(float lerpMultiplier, Quaternion targetRotation)
            {
                _lerpMultiplier = lerpMultiplier;
                _startPosition = VR.Camera.Head.position;
                _startRotation = VR.Camera.Origin.rotation;
                _targetRotation = targetRotation;
            }
            private float _lerp;
            private readonly float _lerpMultiplier;
            private readonly Quaternion _startRotation;
            private readonly Vector3 _startPosition;

            // Quaternion (S)Lerp will go awry with constantly changing end point.
            private readonly Quaternion _targetRotation;

            internal float Move(Vector3 position)
            {
                var smoothStep = Mathf.SmoothStep(0f, 1f, _lerp += Time.deltaTime * _lerpMultiplier);
                position = Vector3.Lerp(_startPosition, position, smoothStep);

                VR.Camera.Origin.rotation = Quaternion.Slerp(_startRotation, _targetRotation, smoothStep);
                VR.Camera.Origin.position += position - VR.Camera.Head.position;
                return smoothStep;
            }
        }

        public static PoV Instance {  get; private set; }

        // Gotta fix SensibleH first to change this.
        public static bool GirlPoV;
        public static bool Active => Instance != null && Instance._active;
        public static ChaControl Target => _target;

        enum Mode
        {
            Disable,
            Move,
            Follow
        }


        private bool _active;
        private static ChaControl _target;

        // Used after switch between characters to keep the head hidden while the camera still in it.
        private ChaControl _prevTarget;

        // cf_J_FaceUp_tz
        private Transform _targetEyes;

        // Assuming position of the head / following the head / disengaging from the head.
        private Mode _mode;

        // Indicates follow with custom vec offset.
        private bool _newAttachPoint;

        // Vec to follow the head from ~remote position with custom offset.
        private Vector3 _offsetVecNewAttach;

        // If rotation isn't required only delta vec applied on each frame.
        private bool _rotationRequired;

        // Threshold for the start of incrementally aggressive camera follow.
        private int _rotDeviationThreshold;

        // Threshold to stop incrementing aggressiveness of the camera follow
        private int _rotDeviationHalf;

        // Vec to follow the head with offset from the settings.
        private Vector3 _offsetVecEyes;

        // Type that handles travel of the camera to the head.
        private OneWayTrip _trip;

        // Type that handles travel of the camera from the head after disable.
        private MoveToPoi _moveTo;

        // Wrapped Mathf.SmoothDamp for comfy use.
        private SmoothDamp _smoothDamp;

        // How fast camera is allowed to rotate per frame, fps + setting driven.
        private float _degPerSec;

        // State of synchronization of camera with the head. Go for relaxed mode if available after achieved.
        private bool _sync;

        // Start of sync, we wait for ~1 sec of stability before declaring it stable '_sync'.
        private float _syncTimestamp;

        // For delta position to follow in relaxed mode.
        private Vector3 _prevFramePos;
        private bool _forceHideHead;


        // EventHandler with custom generic arguments gives a hard time, thus the unity implementation.
        public event UnityAction<bool> CameraBusy;
        public event UnityAction<bool, ChaControl> Impersonation;

        private Vector3 GetEyesPosition => _targetEyes.TransformPoint(_offsetVecEyes);
        private bool IsClimax => HSceneInterp.hFlag.nowAnimStateName.EndsWith("_Loop", System.StringComparison.Ordinal);

        internal static PoV Create()
        {
            var component = VR.Camera.gameObject.GetComponent<PoV>();
            if (component != null)
            {
                return component;
            }
            return VR.Camera.gameObject.AddComponent<PoV>();
        }
        private void Awake()
        {
            Instance = this;
        }

        private void UpdateSettings()
        {
            _sync = false;
            _syncTimestamp = 0f;
            _smoothDamp = new SmoothDamp();
            _degPerSec = 30f * (KoikSettings.RotationAngle.Value / 45f);
            _rotDeviationThreshold = KoikSettings.PovDeviationThreshold.Value;
            _rotDeviationHalf = (int)(_rotDeviationThreshold * 0.4f);
            _offsetVecEyes = new Vector3(0f, KoikSettings.PositionOffsetY.Value, KoikSettings.PositionOffsetZ.Value);
        }

        private void SetVisibility(ChaControl chara)
        {
            if (chara != null) chara.fileStatus.visibleHeadAlways = true;
        }

        private void MoveToPos()
        {
            var origin = VR.Camera.Origin;
            if (_newAttachPoint)
            {
                if (!IsClimax)
                {
                    //origin.rotation = _offsetRotNewAttach;
                    origin.position += _targetEyes.position + _offsetVecNewAttach - VR.Camera.Head.position;
                }
            }
            else
            {
                if (IsClimax)
                {
                    if (_rotationRequired)
                    {
                        _rotationRequired = false;
                        _smoothDamp = null;
                        //_synced = false;
                    }
                }
                else
                {
                    var angle = Quaternion.Angle(origin.rotation, _targetEyes.rotation);
                    if (!_rotationRequired)
                    {
                        if (angle > _rotDeviationThreshold)
                        {
                            _sync = false;
                            _syncTimestamp = 0f;
                            _rotationRequired = true;
                            _smoothDamp = new SmoothDamp();
                        }
                    }
                    else
                    {
                        float sDamp;
                        if (angle < _rotDeviationHalf)
                        {
                            sDamp = _smoothDamp.Current;
                            if (angle < 1f) // && sDamp < 0.01f)
                            {
                                if (_syncTimestamp == 0f)
                                {
                                    if (Quaternion.Angle(VR.Camera.Head.rotation, _targetEyes.rotation) < 30f)
                                    {
                                        _syncTimestamp = Time.time + 2f;
                                    }
                                }
                                else
                                {
                                    if (_syncTimestamp < Time.time)
                                    {
                                        if (Quaternion.Angle(VR.Camera.Head.rotation, _targetEyes.rotation) < 30f)
                                        {
                                            _sync = true;
                                            _syncTimestamp = 0f;
                                            _smoothDamp = null;
                                            _rotationRequired = false;
                                            _prevFramePos = VR.Camera.Head.position;
                                        }
                                        else
                                        {
                                            _syncTimestamp = 0f;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            sDamp = _smoothDamp.Increase();
                        }
                        var moveTowards = Vector3.MoveTowards(VR.Camera.Head.position, GetEyesPosition, 0.05f);
                        origin.rotation = Quaternion.RotateTowards(origin.rotation, _targetEyes.rotation, Time.deltaTime * _degPerSec * sDamp);
                        origin.position += moveTowards - VR.Camera.Head.position;
                        return;
                    }
                    if (_sync)
                    {
                        var pos = GetEyesPosition;
                        origin.position += (pos - _prevFramePos); // + (Vector3.MoveTowards(VR.Camera.Head.position, pos, 0.01f) - VR.Camera.Head.position);
                        _prevFramePos = pos;
                    }
                    else
                    {
                        // We don't branch here anymore?
                        origin.position += GetEyesPosition - VR.Camera.Head.position;
                    }
                }
            }
        }

        private void MoveToPosNoRot()
        {
            if (IsClimax) return;

            if (_newAttachPoint)
            {
                VR.Camera.Origin.position += _targetEyes.position + _offsetVecNewAttach - VR.Camera.Head.position;
                
            }
            else
            {
                var pos = GetEyesPosition;
                VR.Camera.Origin.position += pos - _prevFramePos;
                _prevFramePos = pos;
            }
        }

        public void StartPov()
        {
            _active = true;
            NextChara(keepChara: true);
        }

        public void OnSpotChange()
        {
            StartPov();
            CameraIsFar(3f);
        }
        public void CameraIsFar(float speed = 1f)
        {
            _mode = Mode.Move;
            if (speed != 1f)
            {
                StartMoveToHead(speed);
            }
        }
        public void CameraIsFarAndBusy()
        {
            CameraIsFar();
            CameraBusy?.Invoke(true);
            //if (MouthGuide.Instance != null)
            //{
            //    MouthGuide.Instance.PauseInteractions = true;
            //}
        }
        public void CameraIsNear()
        {
            // Switch to following.
            _mode = Mode.Follow;

            // Update setting for adaptive rotation.
            _sync = false;
            _syncTimestamp = 0f;
            _rotationRequired = true;
            _smoothDamp = new SmoothDamp();

            // Update position for no rotation mode.
            _prevFramePos = VR.Camera.Head.position;

            // Hide previous target
            SetVisibility(_prevTarget);
            _prevTarget = null;

            // Hook for SensibleH, as it does something with this.
            if (_target.sex == 1)
            {
                GirlPoV = true;
            }
            else
            {
                GirlPoV = false;
            }

            // Invoke delegates.
            Impersonation?.Invoke(true, _target);
        }

        private void StartMoveToHead(float speed = 1f)
        {
            if (KoikSettings.FlyInPov.Value == KoikSettings.PovMovementType.Disabled)
            {
                // Teleport camera to the head, as 'follow' method uses delta vector between frames.
                VR.Camera.Origin.position += GetEyesPosition - VR.Camera.Head.position;

                _newAttachPoint = false;
                CameraIsNear();
            }
            else
            {
                // Only one mode is currently operational.

                var targetRot = KoikSettings.PovNoRotation.Value ? Quaternion.Euler(0f, _targetEyes.eulerAngles.y, 0f) : _targetEyes.rotation;

                _trip = new OneWayTrip(Mathf.Min(
                    KoikSettings.FlightSpeed.Value * speed / Vector3.Distance(VR.Camera.Head.position, GetEyesPosition),
                    KoikSettings.FlightSpeed.Value * 60f / Quaternion.Angle(VR.Camera.Origin.rotation, targetRot)),
                    targetRot);
            }
        }

        private void MoveToHeadEx()
        {
            if (_trip == null)
            {
                StartMoveToHead();
            }
            else if (_trip.Move(GetEyesPosition) >= 1f)
            {
                CameraIsNear();
                _newAttachPoint = false;
                _trip = null;
            }
        }

        internal void OnGraspEnd()
        {
            if (_active)
            {
                if (_newAttachPoint)
                {
                    // A bit of a clutch.
                    _mode = Mode.Follow;
                }
                else
                {
                    CameraIsFar(0.25f);
                }
            }
        }


        private int GetCurrentCharaIndex(List<ChaControl> _chaControls)
        {
            if (_target != null)
            {
                for (int i = 0; i < _chaControls.Count; i++)
                {
                    if (_chaControls[i] == _target)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }

        private void NextChara(bool keepChara = false)
        {
            // As some may add extra characters with kPlug, we look them all up.
            var charas = FindObjectsOfType<ChaControl>()
                    .Where(c => c.objTop.activeSelf && c.visibleAll
                    && c.sex != (KoikSettings.Pov.Value == KoikSettings.Genders.Girls ? 0 : KoikSettings.Pov.Value == KoikSettings.Genders.Boys ? 1 : 2))
                    .ToList();

            if (charas.Count == 0)
            {
                Sleep();
                VRPlugin.Logger.LogWarning("Can't impersonate, no appropriate targets. To extend allowed genders change setting.");
                return;
            }
            var currentCharaIndex = GetCurrentCharaIndex(charas);

            if (keepChara)
            {
                _target = charas[currentCharaIndex];
            }
            else if (currentCharaIndex == charas.Count - 1)
            {
                // No point in switching with only one active character, disable instead.

                _prevTarget = _target;
                _target = charas[0];

                _mode = Mode.Disable;
                return;
            }
            else
            {
                _prevTarget = _target;
                _target = charas[currentCharaIndex + 1];
            }
            Impersonation?.Invoke(true, _target);
            //if (MouthGuide.Instance != null)
            //{
            //    MouthGuide.Instance.OnImpersonation(_target);
            //}
            _targetEyes = _target.objHeadBone.transform.Find("cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz");
            CameraIsFarAndBusy();
            UpdateSettings();
        }

        private void NewPosition()
        {
            // Most likely a bad idea to kiss/lick when detached from the head but still inheriting all movements.
            CameraIsNear();
            _offsetVecNewAttach = VR.Camera.Head.position - _targetEyes.position;
        }

        internal void OnGripMove(bool press)
        {
            //_gripMove = press;
            if (_active)
            {
                if (press)
                {
                    CameraIsFar();
                }
                else if (_newAttachPoint)
                {
                    NewPosition();
                }
            }
        }

        internal bool OnTouchpad(bool press)
        {
            // We call it only in gripMove state.
            if (press)
            {
                if (_active && !_newAttachPoint)
                {
                    _newAttachPoint = true;
                    Impersonation?.Invoke(false, _target);
                    //if (IntegrationMaleBreath.IsActive)
                    //{
                    //    IntegrationMaleBreath.OnPov(false, _target);
                    //}
                    return true;
                }
            }
            return false;
        }

        private void Sleep()
        {
            _active = false;
            SetVisibility(_target);
            _mode = Mode.Disable;
            _newAttachPoint = false;
            _forceHideHead = false;
            _moveTo = null;

            Impersonation?.Invoke(false, _target);
            //if (IntegrationMaleBreath.IsActive)
            //{
            //    IntegrationMaleBreath.OnPov(false, _target);
            //}
            //if (MouthGuide.Instance != null)
            //{
            //    MouthGuide.Instance.OnUnImpersonation();
            //}
        }

        private void Disable(bool moveTo)
        {
            if (_moveTo == null)
            {
                if (!moveTo || _target == null)
                {
                    Sleep();
                }
                else
                {
                    var target = _target.sex == 1 ? _target : FindObjectsOfType<ChaControl>()
                        .Where(c => c.sex == 1 && c.objTop.activeSelf && c.visibleAll)
                        .FirstOrDefault();
                    _moveTo = new MoveToPoi(target != null ? target : _target, onFinish: Sleep);
                }
            }
            else
            {
                _moveTo.Move();
            }
        }

        private void HandleDisable(bool moveTo = true)
        {
            if (_newAttachPoint)
            {
                _newAttachPoint = false;
                CameraIsFarAndBusy();
            }
            else
            {
                Disable(moveTo);
            }
        }

        internal bool TryDisable(bool moveTo)
        {
            if (_active)
            {
                if (!moveTo)
                {
                    Sleep();
                }
                else
                {
                    Disable(moveTo);
                }
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (_active)
            {
                if (KoikGameInterp.SceneInput.IsBusy// || _mouth.IsActive
#if KK
                    || !Scene.Instance.AddSceneName.Equals("HProc"))
#else
                    || !Scene.AddSceneName.Equals("HProc")) 
#endif
                //    !Scene.AddSceneName.Equals("HProc")) // SceneApi.GetIsOverlap()) KKS option KK has it broken.
                {
                    // We don't want pov while kissing/licking or if config/pointmove scene pops up.
                    CameraIsFar();
                }
                else
                {
                    switch (_mode)
                    {
                        case Mode.Disable:
                            HandleDisable();
                            break;
                        case Mode.Follow:
                            if (KoikSettings.PovNoRotation.Value)
                            {
                                MoveToPosNoRot();
                            }
                            else
                            {
                                MoveToPos();
                            }
                            break;
                        case Mode.Move:
                            MoveToHeadEx();
                            break;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (_active && KoikSettings.HideHeadInPov.Value && _target != null)
            {
                HideHeadEx(_target);
                if (_prevTarget != null)
                {
                    HideHead(_prevTarget);
                }
            }
        }

        private void HideHeadEx(ChaControl chara)
        {
            if (_forceHideHead)
            {
                chara.fileStatus.visibleHeadAlways = false;
            }
            else
            {
                HideHead(chara);
            }
        }

        private void HideHead(ChaControl chara)
        {
            if (chara.fileStatus.visibleHeadAlways)
            {
                if (IsHeadClose(chara.objHead.transform))
                {
                    SetHeadActive(chara, false);
                }
            }
            else
            {
                if (!IsHeadClose(chara.objHead.transform))
                {
                    SetHeadActive(chara, true);
                }
            }
        }

        private void SetHeadActive(ChaControl chara, bool active)
        {
            chara.fileStatus.visibleHeadAlways = active;
            foreach (var hair in chara.objHair)
            {
                hair.SetActive(active);
            }
        }
        private bool IsHeadClose(Transform head)
        {
            return Vector3.SqrMagnitude(VR.Camera.transform.position - head.TransformPoint(0, 0.12f, -0.04f)) < 0.0361f;
        }

        internal void TryEnable()
        {
            if (KoikSettings.Pov.Value != KoikSettings.Genders.Disable)
            {
                if (_newAttachPoint)
                {
                    CameraIsFarAndBusy();
                    _newAttachPoint = false;
                }
                else if (_active)
                    NextChara();
                else
                    StartPov();
            }
        }

        internal void OnLimbSync(bool start)
        {
            _forceHideHead = start;
        }
    }
}

