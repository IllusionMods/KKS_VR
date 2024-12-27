using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Helpers;
using UnityEngine;
using KK_VR.Handlers;
using KK_VR.Holders;
using KK_VR.Interpreters;

namespace KK_VR.Controls
{
    // Placed directly in KK part for comfy use, and easy new features.
    internal class GripMove
    {
        private readonly GameplayTool _other;
        private readonly Transform _controller;

        //private readonly TravelDistanceRumble _travelRumble;


        /// <summary>
        /// If present, orbiting it instead of changing Yaw around controller.
        /// </summary>
        private Transform _attachPoint;
        private Vector3 _prevAttachVec;
        private Vector3 _prevAttachPos;
        //private Quaternion _prevAttachRot;

        private GripMoveLag _moveLag;

        // Is current instance active or superseded by neighbor.
        private bool _main;

        private bool _otherGrip;
        private bool _alterYaw;
        private bool _alterRotation;

        private Vector3 _prevPos;
        private Quaternion _prevRot;
        private readonly bool _rotInPlace = KoikatuInterpreter.Settings.GripMoveLimitRotation;
        
        internal GripMove(HandHolder hand, HandHolder otherHand)
        {
            _main = true;
            _controller = hand.Controller.transform;
            _other = otherHand.Tool;
            _otherGrip = _other.IsGrip;
            // Feels too much with those forced vibrations on trigger/grip - press/release.
            //_travelRumble = new TravelDistanceRumble(500, 0.1f, _owner.transform)
            //{
            //    UseLocalPosition = true
            //};

            // _travelRumble.Reset();
            //_owner.StartRumble(_travelRumble);

            _prevPos = _controller.position;
            _prevRot = _controller.rotation;
        }

        /// <summary>
        /// Used as means to acquire highly precise offsets. Adds extra source to evaluate movements and provides the point we start to "orbit".
        /// </summary>
        internal void AttachGripMove(Transform attachPoint)
        {
            // All calculations are done through deltas due to saturated input.
            _attachPoint = attachPoint;
            _prevAttachPos = _attachPoint.position;
            _moveLag.ResetPositions(Vector3.zero);

            // Necessary is we started with trigger already. 
            _prevAttachVec = VR.Camera.Head.TransformPoint(new Vector3(0f, 0.05f, 0f)) - attachPoint.position;
            // With full trigger + touchpad.
            //_prevAttachRot = _attachPoint.rotation;
        }

        public void HandleGrabbing()
        {
            // We check if other controller wants to joint us, or override control if other has ended gripMove.
            // Then we use deltas of orientation to setup origin orientation directly, or through evaluation of multiple frames and "averaging it out" if current action requests it.
            if (_main)
            {
                if (_otherGrip && !_other.IsGrip)
                {
                    _otherGrip = false;
                }
                if (!_otherGrip && _other.IsGrip)
                {
                    _main = false;
                }
                else
                {
                    var origin = VR.Camera.SteamCam.origin;
                    if (_alterYaw)
                    {
                        var deltaRot = _prevRot * Quaternion.Inverse(_controller.rotation);
                        //var invRot = Quaternion.Inverse(_prevRot) * _owner.transform.rotation;
                        if (_moveLag == null)
                        {
                            if (_alterRotation)
                            {
                                origin.rotation = deltaRot * origin.rotation;
                            }
                            else
                            {
                                origin.RotateAround(_controller.position, Vector3.up, deltaRot.eulerAngles.y);
                            }
                            origin.position += _prevPos - _controller.position;
                        }
                        else
                        {
                            if (_alterRotation)
                            {
                                if (_attachPoint == null)
                                {
                                    if (_rotInPlace)
                                    {
                                        var head = VR.Camera.SteamCam.head;
                                        var preRotPos = head.position;
                                        _moveLag.SetDeltaRotation(deltaRot);
                                        origin.position += preRotPos - head.position;
                                    }
                                    else
                                    {
                                        _moveLag.SetDeltaRotation(deltaRot);
                                        origin.position += (_prevPos - _controller.position);
                                    }
                                    //_moveLag.SetPositionAndRotation(deltaRot);
                                }
                                else
                                {
                                    _moveLag.SetDeltaPositionAndRotation(
                                        (_attachPoint.position - _prevAttachPos),
                                        deltaRot
                                        );
                                    _prevAttachPos = _attachPoint.position;
                                    //_prevAttachRot = _attachPoint.rotation;
                                }
                            }
                            else
                            {
                                if (_attachPoint == null)
                                {
                                    _moveLag.SetDeltaRotation(Quaternion.Euler(0f, deltaRot.eulerAngles.y, 0f));
                                    origin.position += _prevPos - _controller.position;

                                    //_moveLag.SetPositionAndRotation(
                                    //    //origin.position +
                                    //    _controller.transform.position +
                                    //    deltaRotY * vec,
                                    //    //deltaRotY * (origin.position - new Vector3(_controller.position.x, origin.position.y, _controller.position.z)),
                                    //    deltaRotY);
                                }
                                else
                                {
                                    var newAttachVec = deltaRot * _prevAttachVec;
                                    _moveLag.SetDeltaPositionAndRotation(
                                        (newAttachVec - _prevAttachVec) + (_prevPos - _controller.position) + (_attachPoint.position - _prevAttachPos),
                                        deltaRot
                                        );
                                    _prevAttachVec = newAttachVec;
                                    _prevAttachPos = _attachPoint.position;
                                }
                            }

                        }
                    }
                    else
                    {
                        if (_moveLag == null)
                        {
                            origin.position += _prevPos - _controller.position;
                        }
                        else
                        {
                            if (_attachPoint == null)
                            {
                                _moveLag.SetPosition();
                            }
                            else
                            {
                                _moveLag.SetDeltaPosition(_attachPoint.position - _prevAttachPos + (_prevPos - _controller.position));
                                _prevAttachPos = _attachPoint.position;
                            }
                        }
                    }
                }
                _prevPos = _controller.position;
                _prevRot = _controller.rotation;
            }
            else
            {
                if (!_other.IsGrip)
                {
                    _main = true;
                    _otherGrip = false;
                    _prevPos = _controller.position;
                    _prevRot = _controller.rotation;
                }
            }
        }

        internal void StartLag(int avgFrame)
        {
            _moveLag = new GripMoveLag(_controller, avgFrame);
        }
        internal void StopLag()
        {   
            _moveLag = null;
            _attachPoint = null;
        }
        internal void OnTrigger(bool press)
        {
            _alterYaw = press;
            if (press)
            {
                UpdateAttachVec(); 
                if (_moveLag == null && KoikatuInterpreter.Settings.GripMoveStabilize == Settings.KoikatuSettings.GripMoveStabilization.YawAndRotation)
                {
                    _moveLag = new GripMoveLag(_controller, KoikatuInterpreter.ScaleWithFps(KoikatuInterpreter.Settings.GripMoveStabilizationAmount));
                }
            }
        }

        internal void OnTouchpad(bool press)
        {
            if (KoikatuInterpreter.Settings.GripMoveEnableRotation)
            {
                _alterRotation = press;
                if (press)
                {
                    if (_moveLag == null
                        && (KoikatuInterpreter.Settings.GripMoveStabilize == Settings.KoikatuSettings.GripMoveStabilization.YawAndRotation
                        || KoikatuInterpreter.Settings.GripMoveStabilize == Settings.KoikatuSettings.GripMoveStabilization.OnlyRotation))
                    {
                        _moveLag = new GripMoveLag(_controller, KoikatuInterpreter.ScaleWithFps(KoikatuInterpreter.Settings.GripMoveStabilizationAmount));
                    }
                }
                else
                {
                    UpdateAttachVec();
                    if (_attachPoint == null && _moveLag != null && KoikatuInterpreter.Settings.GripMoveStabilize == Settings.KoikatuSettings.GripMoveStabilization.OnlyRotation)
                    {
                        _moveLag = null;
                    }
                }
            }
            
        }
        private void UpdateAttachVec()
        {
            // Due to vec being utilized only by Trigger-mode, other modes don't update it, so we do it on button input.

            if (_moveLag != null && _attachPoint != null)
            {
                _prevAttachVec = VR.Camera.SteamCam.head.TransformPoint(new Vector3(0f, 0.05f, 0f)) - _attachPoint.position;
            }
        }
    }
}
