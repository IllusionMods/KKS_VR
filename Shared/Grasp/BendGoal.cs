using System;
using System.Collections.Generic;
using System.Text;
using Illusion.Component.Correct;
using KK_VR.Interpreters;
using UnityEngine;

namespace KK_VR.Grasp
{
    internal class BendGoal : MonoBehaviour
    {
        internal bool IsBusy => transform.localPosition != Vector3.zero;

        private KK.RootMotion.FinalIK.IKConstraintBend _bendConstraint;
        private Translate _translate;
        private Transform _origGoal;
        private Transform _target;
        private Vector3 _offsetPos;
        private bool _follow;

        internal bool IsClose(Vector3 position)
        {
            return Vector3.Distance(_bendConstraint.bone2.transform.position, position) < 0.1f;
        }

        internal static BendGoal Create(BodyPart bodyPart)
        {
            var gameObject = new GameObject("BendGoal_" + bodyPart.GetLowerCaseName());
            gameObject.transform.SetParent(bodyPart.chain.bendConstraint.bendGoal, false);
            var goal = gameObject.AddComponent<BendGoal>();
            var baseData = bodyPart.chain.bendConstraint.bendGoal.GetComponent<BaseData>();
            // Male doesn't have bendGoals by default outside of H.
            if (KoikatuInterpreter.CurrentScene != KoikatuInterpreter.SceneType.HScene && baseData.bone == null)
            {
                baseData.bone = bodyPart.chain.bendConstraint.bone2;
            }
            goal.Init(bodyPart);
            return goal;
        }


        private void Init(BodyPart bodyPart)
        {
            _bendConstraint = bodyPart.chain.bendConstraint;
            _origGoal = _bendConstraint.bendGoal;
            _bendConstraint.bendGoal = this.transform;
        }

        private void OnDestroy()
        {
            if (_bendConstraint != null && _origGoal != null)
            {
                _bendConstraint.bendGoal = _origGoal;
            }
        }

        internal void Sleep(bool instant = false)
        {
            if (instant || !IsBusy)
            {
                Disable();
            }
            else
            {
                _translate = new TranslateMove(transform, null, Disable);
            }
        }

        internal void Follow(Transform target)
        {
            if (target != null)
            {
                if (_bendConstraint.weight != 1f)
                {
                    _translate = new TranslateCompensate(
                        // This weight doesn't have Clamp01 by default.
                        () => _bendConstraint.weight = Mathf.Clamp01(_bendConstraint.weight + Time.deltaTime),
                        () => Follow(target),
                        transform,
                        _bendConstraint.bone2);
                }
                else
                {
                    _follow = true;
                    _target = target;
                    _translate = null;
                    _offsetPos = target.InverseTransformPoint(transform.position);
                }
            }
        }

        internal void Stay()
        {
            _follow = false;
            _translate = null;
        }

        private void Disable()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            _follow = false;
            _translate = null;
        }

        private void LateUpdate()
        {
            if (_translate != null)
            {
                _translate.DoStep();
            }
            else if (_follow)
            {
                transform.SetPositionAndRotation(_target.TransformPoint(_offsetPos), _target.rotation);
            }
        }

    }
}
