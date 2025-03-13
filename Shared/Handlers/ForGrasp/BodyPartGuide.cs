using KK_VR.Holders;
using KK_VR.Settings;
using System;
using UnityEngine;
using static KK_VR.Grasp.GraspController;
using BodyPart = KK_VR.Grasp.BodyPart;

namespace KK_VR.Handlers
{
    internal class BodyPartGuide : PartGuide
    {
        private Vector3 _translateExOffset;
        private bool _translateEx;
        private KK.RootMotion.FinalIK.IKEffector _effector;
        private bool _maintainRot;
        private BodyPart _bodyPart;
        private Quaternion _prevRotOffset;


        /// <summary>
        /// Return relative position weight over period of 1 second.
        /// </summary>
        private void TranslateOnFollow()
        {
            // Way too tricky to do it sneaky. Barely noticeable as it is, not worth it.
            _effector.maintainRelativePositionWeight = Mathf.Clamp01(_effector.maintainRelativePositionWeight + Time.deltaTime);
            if (_effector.maintainRelativePositionWeight == 1f)
            {
                _translateEx = false;
            }
        }
        ///////////////////////////////////////////
        ///                                     ///
        ///   ANCHOR = IKObject                ///
        ///                                     ///
        ///   THIS.TRANSOFRM = EFFECTOR.BONE   ///
        ///   THIS.TRANSOFRM != ANCHOR          ///
        ///                                     ///
        ///////////////////////////////////////////

        internal override void Init(BodyPart bodyPart)
        {
            base.Init(bodyPart);
            _bodyPart = bodyPart;
            _effector = bodyPart.effector;
        }
        internal override void Follow(Transform target, HandHolder hand)
        {
            _hand = hand;
            _attach = false;
            _follow = true;
            _target = target;
            if (!_anchor.gameObject.activeSelf)
            {
                _anchor.gameObject.SetActive(true);
            }
            if (_bodyPart.goal != null && !_bodyPart.goal.IsBusy)
            {
                _bodyPart.chain.bendConstraint.weight = KoikSettings.IKDefaultBendConstraint.Value;
            }
            if (_bodyPart.effector.maintainRelativePositionWeight != 1f && KoikSettings.IKMaintainRelativePosition.Value && _bodyPart.IsHand)
            {
                _translateEx = true;
                //_translateOffset = _bodyPart.afterIK.position - transform.position;
            }
            else
            {
                // Turning it off just in case.
                _translateEx = false;
            }
            _offsetRot = Quaternion.Inverse(target.rotation) * _anchor.rotation;
            _offsetPos = target.InverseTransformPoint(_anchor.position);

            _bodyPart.ResetState();
            _bodyPart.AddState(State.Active | State.Grasped);

            if (hand != null)
            {
                if (KoikSettings.IKShowGuideObjects.Value) _bodyPart.visual.Show();
                Tracker.SetBlacklistDic(hand.Grasp.GetBlacklistDic);
                ClearBlacks();
                _bodyPart.visual.SetColor(IsBusy);
                _wasBusy = false;
            }
        }

        internal override void Stay()
        {
            _hand = null;
            _follow = false;
            _attach = false;
            _bodyPart.visual.Hide();
            _bodyPart.RemoveState(State.Grasped);
            ClearTracker();
        }

        internal void StartRelativeRotation()
        {
            if (!_maintainRot)
            {
                _maintainRot = true;
                _prevRotOffset = _offsetRot;
                _offsetRot = Quaternion.Inverse(_bodyPart.chain.nodes[1].transform.rotation) * _anchor.rotation;
                //_offsetRot = Quaternion.Inverse(Quaternion.LookRotation(_bodyPart.afterIK.position - _bodyPart.chain.nodes[1].transform.position)) * _anchor.rotation;
            }
        }

        internal void StopRelativeRotation()
        {
            _maintainRot = false;
            _offsetRot = _prevRotOffset;
            _anchor.rotation = _bodyPart.beforeIK.rotation * _offsetRot;
        }

        private void TranslateOnAttach()
        {
            // By default we want to have "effector.maintainRelativePositionWeight" in full weight, but on attached we want it at zero,
            // but doing so changes calculations of IK Solver quite a bit, thus we compensate over the course of 1 second.
            // We look at initial vector between OffsetEffector (this gameObject) and actual bone that we see rendered after IK,
            // then each frame we adjust our position based on change vector change. As result there is only a miniscule offset(at least there should be, it's impossible to notice)
            // between desired position with full 'maintainRelativePositionWeight' and actual without 'maintainRelativePositionWeight'.

            _effector.maintainRelativePositionWeight = Mathf.Clamp01(_effector.maintainRelativePositionWeight - Time.deltaTime);
            _anchor.position = _target.TransformPoint(_offsetPos) - (_translateExOffset - (_anchor.position - _bodyPart.afterIK.position));
            if (_effector.maintainRelativePositionWeight == 0f)
            {
                _translateEx = false;
                _offsetRot = Quaternion.Inverse(_target.rotation) * _anchor.rotation;
                _offsetPos = _target.InverseTransformPoint(_anchor.position);
            }
        }
        internal override void Attach(Transform target)
        {
            if (target.name.StartsWith("hand", StringComparison.Ordinal))
            {
                _hand.OnBecomingParent();
            }
            _hand = null;
            if (_bodyPart.IsHand)
            {
                _translateEx = true;
                _translateExOffset = _anchor.position - _bodyPart.afterIK.position;
            }
            _follow = true;
            _attach = true;
            _target = target;
            _bodyPart.visual.Hide();
            _bodyPart.RemoveState(State.Grasped);
            _bodyPart.AddState(State.Attached);


            _offsetRot = Quaternion.Inverse(_target.rotation) * _anchor.rotation;
            _offsetPos = _target.InverseTransformPoint(_anchor.position);
        }

        internal void OnSyncStart()
        {
            _bodyPart.ResetState();
            _bodyPart.AddState(State.Synced);
            _translate = new Translate(_anchor, () => _effector.maintainRelativePositionWeight -= Time.deltaTime, () => _translate = null);
        }

        private void Update()
        {
            if (_follow)
            {
                if (_translateEx)
                {
                    if (!_attach)
                    {
                        if (KoikSettings.IKMaintainRelativePosition.Value)
                        {
                            TranslateOnFollow();
                        }
                        _anchor.SetPositionAndRotation(
                            _target.TransformPoint(_offsetPos),
                            _target.rotation * _offsetRot
                            );
                    }
                    else
                    {
                        TranslateOnAttach();
                    }
                }
                else
                {
                    _anchor.SetPositionAndRotation(
                        _target.TransformPoint(_offsetPos),
                        _target.rotation * _offsetRot
                        );
                }
            }
            else
            {
                if (_maintainRot)
                {
                    _anchor.rotation = _bodyPart.chain.nodes[1].transform.rotation * _offsetRot;
                }
                else if (_translate != null)
                {
                    _translate.DoStep();
                }
            }
        }
        private void LateUpdate()
        {
            _effector.positionOffset += _anchor.position - _effector.bone.position;
        }
    }
}

