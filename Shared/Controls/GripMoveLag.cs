using ADV.Commands.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using static UnityEngine.UI.GridLayoutGroup;

namespace KK_VR.Handlers
{
    /// <summary>
    /// Evaluates orientation from previous frames and sets as needed.
    /// </summary>
    internal class GripMoveLag
    {
        private readonly Transform _origin = VR.Camera.SteamCam.origin;
        private readonly Transform _head = VR.Camera.SteamCam.head;
        private readonly Transform _controller;
        private readonly int _frameAmount;
        private readonly float _frameAmountCoef;
        private int _frameIndex;
        private readonly float[] _frameCoefs;
        private readonly Quaternion[] _prevRotations;
        private readonly Vector3[] _prevPositions;
        private Vector3 _lastAvgPos;
        private bool _resetRequired;
        internal GripMoveLag(Transform controller, int frameAvg, Vector3 inheritedPosition = default)
        {
            _controller = controller;
            _frameAmount = frameAvg;
            _frameAmountCoef = 1f / frameAvg;
            _prevRotations = new Quaternion[frameAvg];
            _prevPositions = new Vector3[frameAvg];
            _frameCoefs = new float[frameAvg];
            _lastAvgPos = _controller.position;

            // Coefficients can be customized to change rotation follow type, even non-linear should look good.
            for (var i = 0; i < frameAvg; i++)
            {
                _frameCoefs[i] = 1f / (i + 2f);
            }
            var pos = inheritedPosition == Vector3.zero ? _controller.position : inheritedPosition;
            var rot = Quaternion.identity;
            for (var i = 0; i < frameAvg; i++)
            {
                _prevRotations[i] = rot;
                _prevPositions[i] = pos;
            }
        }
        /// <summary>
        /// Fills previous positions with supplied whatever.
        /// </summary>
        internal void ResetPositions(Vector3 position)
        {
            for (var i = 0; i < _frameAmount; i++)
            {
                _prevPositions[i] = position;
            }
            _lastAvgPos = position;
            _resetRequired = false;
        }
        /// <summary>
        /// Sets Origin to a "catching up" rotation. Same as other methods, but rotation only.
        /// </summary>
        internal void SetDeltaRotation(Quaternion rotation)
        {
            _resetRequired = true;
            _prevRotations[_frameIndex] = rotation;
            _frameIndex++;
            if (_frameIndex == _frameAmount) _frameIndex = 0;
            UpdateRotation();
        }
        private void UpdateRotation()
        {
            // Not average at all, is a biased catch-up. All averages I've seen suck tremendously.
            // The most stale frame is grabbed on init part.
            var count = _frameAmount - 1;

            // The most stale rotation. Doesn't get touched in the loop.
            var avgRot = _prevRotations[_frameIndex];
            var j = _frameIndex;
            for (var i = 0; i < count; i++)
            {
                if (++j == _frameAmount) j = 0;
                avgRot = Quaternion.Lerp(avgRot, _prevRotations[j], _frameCoefs[i]);
            }
            _origin.rotation = avgRot * _origin.rotation;
        }
        /// <summary>
        /// Sets Origin to an "average" of supplied orientation.
        /// </summary>
        internal void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _prevRotations[_frameIndex] = rotation;
            if (_resetRequired)
            {
                ResetPositions(position);
            }
            else
            {
                _prevPositions[_frameIndex] = position;
            }
            _frameIndex++;
            if (_frameIndex == _frameAmount) _frameIndex = 0;
            UpdatePositionAndRotation();
        }
        internal void SetDeltaPositionAndRotation(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            _prevRotations[_frameIndex] = deltaRotation;
            _prevPositions[_frameIndex] = deltaPosition;
            _frameIndex++;
            if (_frameIndex == _frameAmount) _frameIndex = 0;
            UpdateDeltaPositionAndRotation();
        }
        private void UpdateDeltaPositionAndRotation()
        {
            // The most stale frame is grabbed on init part.
            var count = _frameAmount - 1;

            // The most stale rotation. Doesn't get touched in the loop.
            var avgRot = _prevRotations[_frameIndex];
            // Position that won't get touched in the loop. Don't care about the order, we use average.
            var avgPos = _prevPositions[count];

            var j = _frameIndex;
            for (var i = 0; i < count; i++)
            {
                if (++j == _frameAmount) j = 0;
                avgRot = Quaternion.Lerp(avgRot, _prevRotations[j], _frameCoefs[i]);
                avgPos += _prevPositions[i];
            }

            avgPos *= _frameAmountCoef;
            var preRotPos = _head.position;
            _origin.rotation = avgRot * _origin.rotation;
            _origin.position += (preRotPos - _head.position) + avgPos;
        }

        /// <summary>
        /// Sets Origin to an "average" of supplied rotation and Controller position.
        /// </summary>
        internal void SetPositionAndRotation(Quaternion rotation)
        {
            // Can also reset rotations when switching away from this method,
            // but hey, those movement look very human-esque,
            // well, as long as they weren't something crazy, like 180 deg/frame.
            _prevRotations[_frameIndex] = rotation;
            if (_resetRequired)
            {
                ResetPositions(_controller.position);
            }
            else
            {
                _prevPositions[_frameIndex] = _controller.position;
            }
            _frameIndex++;
            if (_frameIndex == _frameAmount) _frameIndex = 0;
            UpdatePositionAndRotation();
        }
        private void UpdatePositionAndRotation()
        {
            // The most stale frame is grabbed on init part.
            var count = _frameAmount - 1;

            // The most stale rotation. Doesn't get touched in the loop.
            var avgRot = _prevRotations[_frameIndex];
            // Position that won't get touched in the loop. Don't care about the order, we use average.
            var avgPos = _prevPositions[count];

            var j = _frameIndex;
            for (var i = 0; i < count; i++)
            {
                if (++j == _frameAmount) j = 0;
                avgRot = Quaternion.Lerp(avgRot, _prevRotations[j], _frameCoefs[i]);
                avgPos += _prevPositions[i];
            }

            avgPos *= _frameAmountCoef;
            var preRotPos = _head.position;
            _origin.rotation = avgRot * _origin.rotation;
            //_origin.position += (preRotPos - _origin.position) + (_lastAvgPos - avgPos);
            _origin.position += (preRotPos - _head.position) + (_lastAvgPos - avgPos);
            _lastAvgPos = avgPos;
        }
        /// <summary>
        /// Sets Origin to an average of Controller position.
        /// </summary>
        internal void SetPosition()
        {
            if (_resetRequired)
            {
                ResetPositions(_controller.position);
            }
            else
            {
                _prevPositions[_frameIndex] = _controller.position;
            }
            _frameIndex++;
            if (_frameIndex == _frameAmount) _frameIndex = 0;
            UpdatePosition(inverse: true);
        }
        /// <summary>
        /// Sets Origin to an average of supplied delta of positions.
        /// </summary>
        internal void SetDeltaPosition(Vector3 deltaPosition)
        {
            //if (_resetRequired)
            //{
            //    ResetPositions(deltaPosition);
            //}
            //else
            //{
            _prevPositions[_frameIndex] = deltaPosition;
            //}
            _frameIndex++;
            if (_frameIndex == _frameAmount) _frameIndex = 0;
            UpdatePositionDelta();
        }
        private void UpdatePositionDelta()
        {
            var avgPos = Vector3.zero;
            for (var i = 0; i < _frameAmount; i++)
            {
                avgPos += _prevPositions[i];
            }
            avgPos *= _frameAmountCoef;
            _origin.position += avgPos;
        }

        private void UpdatePosition(bool inverse)
        {
            var avgPos = Vector3.zero;
            for (var i = 0; i < _frameAmount; i++)
            {
                avgPos += _prevPositions[i];
            }
            avgPos *= _frameAmountCoef;
            if (inverse)
            {
                _origin.position += _lastAvgPos - avgPos;
            }
            else
            {
                _origin.position += avgPos - _lastAvgPos;
            }
            _lastAvgPos = avgPos;
        }


        // Way easier to create a new one instead.
        //internal void ChangeLagAmount(bool increase)
        //{
        //    if (increase)
        //    {
        //        if (_frameCurAmount != _frameCeiling)
        //        {
        //            _frameCurAmount++;
        //            _frameCurAmountCoef = 1f / _frameCurAmount;
        //        }
        //    }
        //    else
        //    {
        //        if (_frameCurAmount != _frameFloor)
        //        {
        //            _frameCurAmount--;
        //            _frameCurAmountCoef = 1f / _frameCurAmount;
        //        }
        //    }
        //}

    }
}
