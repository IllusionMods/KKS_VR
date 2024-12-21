//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using VRGIN.Core;

//namespace KK_VR.Handlers
//{
//    internal class CameraLag
//    {
//        private readonly Transform _origin = VR.Camera.SteamCam.origin;
//        private readonly Transform _head = VR.Camera.SteamCam.head;
//        private readonly int _frameFloor;
//        private readonly int _frameCeiling;
//        //internal int _frameAvg;
//        private int _frameIndex;
//        private int _frameCurAmount;
//        private float _frameCurAmountCoef;
//        private readonly float[] _frameCoefs;
//        private readonly Quaternion[] _prevRotations;
//        private readonly Vector3[] _prevPositions;
//        internal CameraLag(int frameAvg)
//        {
//            _frameCurAmount = frameAvg;
//            _frameCurAmountCoef = 1f / frameAvg;
//            _frameFloor = frameAvg - 5;
//            _frameCeiling = frameAvg + 5;
//            _prevRotations = new Quaternion[_frameCeiling];
//            _prevPositions = new Vector3[_frameCeiling];
//            _frameCoefs = new float[_frameCeiling];

//            // Coefficients can be customized to change rotation follow type, even non-linear should look good.
//            for (var i = 0; i < _frameCeiling; i++)
//            {
//                _frameCoefs[i] = 1f / (i + 2f);
//            }

//            for (var i = 0; i < _frameCurAmount; i++)
//            {
//                _prevRotations[i] = _head.rotation;
//                _prevPositions[i] = _head.position;
//            }
//        }

//        internal void SetPosition(Vector3 position)
//        {
//            _prevPositions[_frameIndex] = position;
//            _frameIndex++;
//            if (_frameIndex == _frameCurAmount) _frameIndex = 0;
//            UpdatePosition();
//        }
//        internal void SetPositionAndRotation(Vector3 position, Quaternion rotation)
//        {
//            _prevRotations[_frameIndex] = rotation;
//            _prevPositions[_frameIndex] = position;
//            _frameIndex++;
//            if (_frameIndex == _frameCurAmount) _frameIndex = 0;
//            UpdatePositionAndRotation();
//        }

//        private void UpdatePositionAndRotation()
//        {
//            var avgRot = _prevRotations[_frameIndex];
//            var avgPos = _prevPositions[_frameCurAmount];

//            var j = _frameIndex;
//            var count = _frameCurAmount - 1;
//            for (var i = 0; i < count; i++)
//            {
//                if (++j == _frameCurAmount) j = 0;
//                avgRot = Quaternion.Lerp(avgRot, _prevRotations[j], _frameCoefs[i]);
//                avgPos += _prevPositions[i];
//            }
//            _origin.rotation = avgRot;
//            _origin.position += avgPos * _frameCurAmountCoef - _head.position;
//        }
//        private void UpdatePosition()
//        {
//            var avgPos = Vector3.zero;
//            for (var i = 0; i < _frameCurAmount; i++)
//            {
//                avgPos += _prevPositions[i];
//            }
//            _origin.position += avgPos * _frameCurAmountCoef - _head.position;
//        }

//        internal void ChangeLagAmount(bool increase)
//        {
//            if (increase)
//            {
//                if (_frameCurAmount != _frameCeiling)
//                {
//                    _frameCurAmount++;
//                    _frameCurAmountCoef = 1f / _frameCurAmount;
//                }
//            }
//            else
//            {
//                if (_frameCurAmount != _frameFloor)
//                {
//                    _frameCurAmount--;
//                    _frameCurAmountCoef = 1f / _frameCurAmount;
//                }
//            }
//        }
//    }
//}
