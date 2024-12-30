using UnityEngine;
using static VRGIN.Controls.Controller;
using Valve.VR;

namespace KK_VR.Interpreters
{
    internal readonly struct InputWait
    {
        internal InputWait(int _index, TrackpadDirection _direction, bool _manipulateSpeed, float _duration)
        {
            index = _index;
            direction = _direction;
            manipulateSpeed = _manipulateSpeed;

            duration = _duration;
            timestamp = Time.time;
            finish = Time.time + _duration;
        }
        internal InputWait(int _index, TrackpadDirection _direction, float _duration)
        {
            index = _index;
            direction = _direction;

            duration = _duration;
            timestamp = Time.time;
            finish = Time.time + _duration;
        }
        internal InputWait(int _index, EVRButtonId _button, float _duration)
        {
            index = _index;
            button = _button;

            duration = _duration;
            timestamp = Time.time;
            finish = Time.time + _duration;
        }
        internal readonly int index;
        internal readonly TrackpadDirection direction;
        internal readonly EVRButtonId button;
        internal readonly bool manipulateSpeed;
        internal readonly float timestamp;
        internal readonly float duration;
        internal readonly float finish;
    }
}
