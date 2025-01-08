using System;
using System.Collections.Generic;
using System.Text;
using ADV.Commands.Base;
using UnityEngine;

namespace KK_VR.Grasp
{
    internal class TranslateMove : Translate
    {
        internal TranslateMove(Transform anchor, Action onStep, Action onFinish) 
            : base(onStep: onStep, onFinish: onFinish)
        {

            _anchor = anchor;
            _offsetPos = anchor.localPosition;
            _offsetRot = anchor.localRotation;
        }

        private readonly Transform _anchor;
        private readonly Quaternion _offsetRot;
        private readonly Vector3 _offsetPos;


        internal override void DoStep()
        {
            base.DoStep();
            var step = Mathf.SmoothStep(0f, 1f, _lerp);
            _anchor.localPosition = Vector3.Lerp(_offsetPos, Vector3.zero, step);
            _anchor.localRotation = Quaternion.Lerp(_offsetRot, Quaternion.identity, step);
        }
    }
}
