using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KK_VR.Grasp
{
    internal class TranslateCompensate : Translate
    {
        internal TranslateCompensate(Action onStep, Action onFinish, Transform shouldMove, Transform shouldStay) 
            : base(onStep: onStep, onFinish: onFinish)
        {
            _shouldMove = shouldMove;
            _shouldStay = shouldStay;
            _oldPos = _shouldStay.position;
        }
        // Part to compensate movement with.
        private readonly Transform _shouldMove;

        // Part that should stay put.
        private readonly Transform _shouldStay;
        private Vector3 _oldPos;

        internal override void DoStep()
        {
            base.DoStep();
            _shouldMove.position += _oldPos - _shouldStay.position;
        }
    }
}
