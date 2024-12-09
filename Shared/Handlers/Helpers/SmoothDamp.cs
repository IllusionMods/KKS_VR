using ADV.Commands.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_VR.Handlers
{
    internal class SmoothDamp
    {
        internal SmoothDamp(float smoothTime = 1f, float target = 1f)
        {
            _smoothTime = smoothTime;
            _target = target;
        }
        private float _current;
        private float _currentVelocity;
        private readonly float _target;
        private readonly float _smoothTime;
        internal float Increase()
        {
            return _current = Mathf.SmoothDamp(_current, _target, ref _currentVelocity, _smoothTime);
        }
        internal float Decrease()
        {
            return _current = Mathf.SmoothDamp(_current, 0f, ref _currentVelocity, _smoothTime);
        }
    }
}
