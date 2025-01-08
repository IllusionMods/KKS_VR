using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KK_VR.Grasp
{
    internal abstract class Translate
    {
        internal Translate(Action onStep, Action onFinish)
        {
            _onStep = onStep;
            _onFinish = onFinish;
        }

        protected float _lerp;
        private readonly Action _onStep;
        private readonly Action _onFinish;

        internal virtual void DoStep()
        {
            _lerp += Time.deltaTime;
            _onStep?.Invoke();

            if (_lerp >= 1f)
            {
                _onFinish?.Invoke();
            }
        }
    }
}
