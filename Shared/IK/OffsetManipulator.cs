using KK_VR.Grasp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_VR.IK
{
    // Simplified version from FinalIK scripts
    internal abstract class OffsetManipulator : OffsetModifier
    {
        protected class EffectorLink
        {
            internal EffectorLink(KK.RootMotion.FinalIK.IKEffector _effector, Vector3 _offset, float _weight)
            {
                effector = _effector;
                defaultWeight = _weight;
                weight = _weight;
                offset = _offset;
            }
            internal readonly KK.RootMotion.FinalIK.IKEffector effector;
            internal readonly Vector3 offset;
            internal readonly float defaultWeight;
            internal float weight;
            internal void MultiplyWeight(float multiplier) => weight = Mathf.Clamp01(weight * multiplier);
        }
        protected readonly  List<EffectorLink> _linkList = [];

        protected void OnInit(KK.RootMotion.FinalIK.FullBodyBipedIK ik)
        {
            _ik = ik;
            _ik.solver.OnPreUpdate += ModifyOffset;
        }
        internal void Add(BodyPart bodyPart, float weight)
        {
            _linkList.Add(new EffectorLink(bodyPart.effector, this.transform.InverseTransformPoint(bodyPart.anchor.position), weight));
        }
        protected override void OnModifyOffset()
        {
            foreach (var link in _linkList)
            {
                link.effector.positionOffset += (transform.TransformPoint(link.offset) - (link.effector.bone.position + link.effector.positionOffset)) * link.weight;
            }
        }

    }
}
