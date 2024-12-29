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

        public static PoV Instance;
        /// <summary>
        /// girlPOV is NOT set proactively, use "active" to monitor state.
        /// </summary>
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
        private ChaControl _prevTarget;

        private Transform _targetEyes;
        private Mode _mode;
        private bool _newAttachPoint;
        private Vector3 _offsetVecNewAttach;
        private bool _rotationRequired;
        private int _rotDeviationThreshold;
        private int _rotDeviationHalf;
        private Vector3 _offsetVecEyes;
        private OneWayTrip _trip;
        private MoveToPoi _moveTo;
        private SmoothDamp _smoothDamp;
        private float _degPerSec;
        private bool _sync;
        private float _syncTimestamp;
        private Vector3 _prevFramePos;
        private bool _forceHideHead;

        private Vector3 GetEyesPosition() => _targetEyes.TransformPoint(_offsetVecEyes);
        private bool IsClimax => HSceneInterpreter.hFlag.nowAnimStateName.EndsWith("_Loop", System.StringComparison.Ordinal);

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
            _degPerSec = 30f * KoikatuInterpreter.Settings.RotationMultiplier;
            _rotDeviationThreshold = KoikatuInterpreter.Settings.RotationDeviationThreshold;
            _rotDeviationHalf = (int)(_rotDeviationThreshold * 0.4f);
            _offsetVecEyes = new Vector3(0f, KoikatuInterpreter.Settings.PositionOffsetY, KoikatuInterpreter.Settings.PositionOffsetZ);
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
                        var moveTowards = Vector3.MoveTowards(VR.Camera.Head.position, GetEyesPosition(), 0.05f);
                        origin.rotation = Quaternion.RotateTowards(origin.rotation, _targetEyes.rotation, Time.deltaTime * _degPerSec * sDamp);
                        origin.position += moveTowards - VR.Camera.Head.position;
                        return;
                    }
                    if (_sync)
                    {
                        var pos = GetEyesPosition();
                        origin.position += (pos - _prevFramePos); // + (Vector3.MoveTowards(VR.Camera.Head.position, pos, 0.01f) - VR.Camera.Head.position);
                        _prevFramePos = pos;
                    }
                    else
                    {
                        // We don't branch here anymore?
                        origin.position += GetEyesPosition() - VR.Camera.Head.position;
                    }
                }
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
            if (MouthGuide.Instance != null)
            {
                MouthGuide.Instance.PauseInteractions = true;
            }
        }
        public void CameraIsNear()
        {
            _mode = Mode.Follow;
            _sync = false;
            _syncTimestamp = 0f;
            _rotationRequired = true;
            _smoothDamp = new SmoothDamp();
            SetVisibility(_prevTarget);
            _prevTarget = null;
            if (_target.sex == 1)
            {
                GirlPoV = true;
                if (MouthGuide.Instance != null)
                {
                    MouthGuide.Instance.PauseInteractions = true;
                }
            }
            else
            {
                GirlPoV = false;
                if (MouthGuide.Instance != null)
                {
                    MouthGuide.Instance.PauseInteractions = false;
                }
            }
        }

        private void StartMoveToHead(float speed = 1f)
        {
            if (KoikatuInterpreter.Settings.FlyInPov == KoikatuSettings.MovementTypeH.Disabled)
            {
                _newAttachPoint = false;
                CameraIsNear();
            }
            else
            {
                // Only one mode is currently operational.
                _trip = new OneWayTrip(Mathf.Min(
                    KoikatuInterpreter.Settings.FlightSpeed * speed / Vector3.Distance(VR.Camera.Head.position, GetEyesPosition()),
                    KoikatuInterpreter.Settings.FlightSpeed * 60f / Quaternion.Angle(VR.Camera.Origin.rotation, _targetEyes.rotation)),
                    _targetEyes.rotation);
            }
        }
        private void MoveToHeadEx()
        {
            if (_trip == null)
            {
                StartMoveToHead();
            }
            else if (_trip.Move(GetEyesPosition()) >= 1f)
            {
                CameraIsNear();
                _newAttachPoint = false;
                _trip = null;
            }
        }

        internal void OnGraspEnd()
        {
            if (_active &&  !_newAttachPoint)
            {
                CameraIsFar(0.25f);
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
                    && c.sex != (KoikatuInterpreter.Settings.PoV == KoikatuSettings.Impersonation.Girls ? 0 : KoikatuInterpreter.Settings.PoV == KoikatuSettings.Impersonation.Boys ? 1 : 2))
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
            if (MouthGuide.Instance != null)
            {
                MouthGuide.Instance.OnImpersonation(_target);
            }
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

            if (MouthGuide.Instance != null)
            {
                MouthGuide.Instance.PauseInteractions = false;
                MouthGuide.Instance.OnUnImpersonation();
            }
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
                    _moveTo = new MoveToPoi(target != null ? target : _target, Sleep );
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
                if (KoikatuInterpreter.SceneInput.IsBusy// || _mouth.IsActive
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
                            MoveToPos();
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
            if (_active && KoikatuInterpreter.Settings.HideHeadInPOV && _target != null)
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
            var head = chara.objHead.transform;
            var wasVisible = chara.fileStatus.visibleHeadAlways;
            var headCenter = head.TransformPoint(0, 0.12f, -0.04f);
            var sqrDistance = (VR.Camera.transform.position - headCenter).sqrMagnitude;
            var visible = 0.0361f < sqrDistance; // 19 centimeters
            chara.fileStatus.visibleHeadAlways = visible;
            if (wasVisible && !visible)
            {
                chara.objHead.SetActive(false);

                foreach (var hair in chara.objHair)
                {
                    hair.SetActive(false);
                }
            }
        }

        internal void TryEnable()
        {
            if (KoikatuInterpreter.Settings.PoV != KoikatuSettings.Impersonation.Disabled)
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

