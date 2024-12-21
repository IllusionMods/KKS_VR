using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace KK_VR.Handlers
{
    internal class ItemLag
    {
        private readonly Transform _transform;
        private readonly Quaternion[] _prevRotations;
        private readonly Vector3[] _prevPositions;

        //private readonly int _frameFloor;
        //private readonly int _frameCeiling;
        private readonly float[] _frameCoefs;

        private int _frameIndex;
        private readonly int _frameCurAmount;
        private readonly float _frameCurAmountCoef;

        internal ItemLag(Transform transform, int frameAvg)
        {
            _transform = transform;
            _frameCurAmount = frameAvg;
            _frameCurAmountCoef = 1f / frameAvg;
            //_frameFloor = frameAvg - 5;
            //_frameCeiling = frameAvg + 5;
            _prevRotations = new Quaternion[frameAvg];
            _prevPositions = new Vector3[frameAvg];
            _frameCoefs = new float[frameAvg];

            // Coefficients can be customized to change rotation follow type, even non-linear should look good.
            for (var i = 0; i < frameAvg; i++)
            {
                _frameCoefs[i] = 1f / (i + 2f);
            }
            var pos = _transform.position;
            var rot = _transform.rotation;
            for (var i = 0; i < _frameCurAmount; i++)
            {
                _prevRotations[i] = rot;
                _prevPositions[i] = pos;
            }
        }

        internal void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _prevRotations[_frameIndex] = rotation;
            _prevPositions[_frameIndex] = position;
            //}
            _frameIndex++;
            if (_frameIndex == _frameCurAmount) _frameIndex = 0;
            UpdatePositionAndRotation();
        }

        private void UpdatePositionAndRotation()
        {
            // The most stale frame is grabbed on init part.
            var count = _frameCurAmount - 1;

            // The most stale rotation. Doesn't get touched in the loop.
            var avgRot = _prevRotations[_frameIndex];
            // Position that won't get touched in the loop. Don't care about the order, we use average.
            var avgPos = _prevPositions[count];

            var j = _frameIndex;
            for (var i = 0; i < count; i++)
            {
                if (++j == _frameCurAmount) j = 0;
                avgRot = Quaternion.Lerp(avgRot, _prevRotations[j], _frameCoefs[i]);
                avgPos += _prevPositions[i];
            }

            avgPos *= _frameCurAmountCoef;
            _transform.SetPositionAndRotation(avgPos, avgRot);
        }
        internal void SetPosition(Vector3 position)
        {
            _prevPositions[_frameIndex] = position;
            _frameIndex++;
            if (_frameIndex == _frameCurAmount) _frameIndex = 0;
            UpdatePosition();
        }
        private void UpdatePosition()
        {
            var avgPos = _prevPositions[0];
            for (var i = 1; i < _frameCurAmount; i++)
            {
                avgPos += _prevPositions[i];
            }
            _transform.position = avgPos * _frameCurAmountCoef;
        }
    }
}
