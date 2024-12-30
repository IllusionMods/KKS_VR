using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace KK_VR.Grasp
{
    /// <summary>
    /// Holds/manipulates whole character
    /// </summary>
    internal class BaseHold
    {
        internal BaseHold(BodyPart _bodyPart, Transform _objAnim, Transform _attachPoint)
        {
            bodyPart = _bodyPart;
            objAnim = _objAnim;
            attachPoint = _attachPoint;
            offsetPos = _attachPoint.InverseTransformDirection(_objAnim.transform.position - _attachPoint.position);
            offsetRot = Quaternion.Inverse(_attachPoint.rotation) * _objAnim.transform.rotation;
        }
        private readonly BodyPart bodyPart;
        private readonly Transform objAnim;
        private readonly Transform attachPoint;
        private Quaternion offsetRot;
        private Vector3 offsetPos;
        private int scrollDir;
        private bool scrollInc;

        private readonly Quaternion _left = Quaternion.Euler(0f, 1f, 0f);
        private readonly Quaternion _right = Quaternion.Euler(0f, -1f, 0f);

        internal void Execute()
        {
            if (scrollDir != 0)
            {
                if (scrollDir == 1)
                {
                    DoBaseHoldVerticalScroll(scrollInc);
                }
                else
                {
                    DoBaseHoldHorizontalScroll(scrollInc);
                }
            }
            objAnim.transform.SetPositionAndRotation(
                attachPoint.position + attachPoint.TransformDirection(offsetPos),
                attachPoint.rotation * offsetRot
                );
        }

        internal void StartBaseHoldScroll(int direction, bool increase)
        {
            scrollDir = direction;
            scrollInc = increase;
        }

        internal void StopBaseHoldScroll()
        {
            scrollDir = 0;
        }

        private void DoBaseHoldVerticalScroll(bool increase)
        {
            offsetPos += VR.Camera.Head.forward * (Time.deltaTime * (increase ? 10f : -10f));
        }


        private void DoBaseHoldHorizontalScroll(bool left)
        {
            offsetRot *= (left ? _left : _right);
        }
    }
}
