using KK_VR.Handlers;
using KK_VR.Trackers;
using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static KK_VR.Grasp.GraspController;
using KK_VR.Holders;
using KK_VR.Grasp;
using KK_VR.Interpreters;

namespace KK_VR.Handlers
{
    internal class HeadPartGuide : PartGuide
    {

        protected override BodyPart BodyPart
        {
            get => _bodyPart;
            set => _bodyPart = value is BodyPartHead head ? head : null;
        }
        private BodyPartHead _bodyPart;
        internal override void Follow(Transform target, HandHolder hand)
        {
            if (KoikatuInterpreter.Settings.IKHeadEffector == Settings.KoikatuSettings.HeadEffector.Disabled)
            {
                return;
            }
            else if (_bodyPart.headEffector.enabled == false)
            {
                _bodyPart.headEffector.enabled = true;
            }
            _hand = hand;
            _attach = false;
            _follow = true;
            _target = target;

            //if (KoikatuInterpreter.Settings.ShowGuideObjects) _bodyPart.visual.Show();
            _bodyPart.state = State.Grasped;

            _offsetRot = Quaternion.Inverse(target.rotation) * _anchor.rotation;
            _offsetPos = target.InverseTransformPoint(_anchor.position);

            Tracker.SetBlacklistDic(hand.Grasp.GetBlacklistDic);
            ClearBlacks();
            _bodyPart.visual.SetColor(IsBusy);
            _wasBusy = false;
        }
        internal override void Stay()
        {
            _hand = null;
            _follow = false;
            _attach = false;
            //_anchor.parent = _bodyPart.beforeIK;
            //SetBodyPartCollidersToTrigger(false);
        }

        //internal override void Sleep(bool instant)
        //{
        //    _hand = null;
        //    _follow = false;
        //    _attach = false;
        //    //gameObject.SetActive(false);
        //    //transform.localScale = _origScale;
        //    SetBodyPartCollidersToTrigger(false);
        //}

        internal override void Attach(Transform target)
        {
            _hand = null;
            _attach = true;
            _target = target;

            _offsetRot = Quaternion.Inverse(_target.rotation) * _anchor.rotation;
            _offsetPos = _target.InverseTransformPoint(_anchor.position);
            //transform.parent = _objAnim;
        }
        protected override void Disable()
        {
            base.Disable();
            if (KoikatuInterpreter.Settings.IKHeadEffector != Settings.KoikatuSettings.HeadEffector.Always)
            {
                _bodyPart.headEffector.enabled = false;
            }
        }

        private void Update()
        {
            if (_follow)
            {
                _anchor.SetPositionAndRotation(
                    _target.TransformPoint(_offsetPos),
                    _target.rotation * _offsetRot
                   );
            }
            else
            {
                _translate?.DoStep();
            }
        }
    }
}
