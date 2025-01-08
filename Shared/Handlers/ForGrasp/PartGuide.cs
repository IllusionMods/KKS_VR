using IllusionUtility.GetUtility;
using KK_VR.Holders;
using KK_VR.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Helpers;
using static ActionGame.VisibleController;
using BodyPart = KK_VR.Grasp.BodyPart;

namespace KK_VR.Handlers
{
    // Component for the actual character bone (bodyPart.afterIK). Can be repositioned at a whim.
    // Controls collider tracker and movement of IK object. 
    // When IK object is being set and there are appropriate colliders within range of this component, 
    // IK object may be attached to intersecting collider. Due to nature of IK setup,
    // IK object is always somewhere not where you'd expect it to be,
    // thus we manage it through this component attached to the bone that would better represent particular IK point visually.
    /// <summary>
    /// Component responsible for orientation of IK driven body part.
    /// </summary>
    internal abstract class PartGuide : Handler
    {
        protected class Translate
        {
            internal Translate(Transform anchor, Action onStep, Action onFinish)
            {
                _anchor = anchor;
                _offsetPos = anchor.localPosition;
                _offsetRot = anchor.localRotation;
                _onStep = onStep;
                _onFinish = onFinish;
            }
            private float _lerp;
            private readonly Transform _anchor;
            private readonly Quaternion _offsetRot;
            private readonly Vector3 _offsetPos;
            private readonly Action _onStep;
            private readonly Action _onFinish;

            internal void DoStep()
            {
                _lerp += Time.deltaTime;
                var step = Mathf.SmoothStep(0f, 1f, _lerp);
                _anchor.localPosition = Vector3.Lerp(_offsetPos, Vector3.zero, step);
                _anchor.localRotation = Quaternion.Lerp(_offsetRot, Quaternion.identity, step);
                _onStep?.Invoke();
                if (_lerp >= 1f)
                {
                    _onFinish?.Invoke();
                }
            }
        }

        protected HandHolder _hand;
        protected virtual BodyPart BodyPart { get; set; }

        // Transform that guides IK, separate gameObject.
        protected Transform _anchor;

        protected Transform _target;
        protected Rigidbody _rigidBody;

        protected Vector3 _offsetPos;
        protected Quaternion _offsetRot;

        protected Translate _translate;

        protected bool _wasBusy;

        // If are not in default state.
        protected bool _follow;
        // If we follow particular transform.
        protected bool _attach;

        protected virtual void Awake()
        {
            _rigidBody = gameObject.AddComponent<Rigidbody>();
            _rigidBody.isKinematic = true;
            _rigidBody.freezeRotation = true;

            _rigidBody.useGravity = false;

            // Default primitive's collider-trigger.
            var colliderTrigger = gameObject.GetComponent<SphereCollider>();
            colliderTrigger.isTrigger = true;

            // RigidBody's slave.
            //var sphere = gameObject.AddComponent<SphereCollider>();
            //sphere.isTrigger = false;
            //sphere.radius = Mathf.Round(1000f * (colliderTrigger.radius * 0.7f)) * 0.001f;
            Tracker = new Tracker();
        }
        protected virtual void OnEnable()
        {
            _wasBusy = false;
        }

        internal virtual void Init(BodyPart bodyPart)
        {
            BodyPart = bodyPart;
            _anchor = bodyPart.anchor;
        }
        /// <summary>
        /// Sets the transform (controller?) as the parent of the part for further manipulation. 
        /// </summary>
        internal abstract void Follow(Transform target, HandHolder hand);
        /// <summary>
        /// Sets part's parent to itself before IK, to follow it with offset.
        /// </summary>
        internal abstract void Stay();
        /// <summary>
        /// Returns part to the default state.
        /// </summary>
        internal void Sleep(bool instant)
        {
            BodyPart.state = Grasp.GraspController.State.Active;
            _hand = null;
            _follow = false;
            _attach = false;
            if (instant)
            {
                _anchor.localPosition = Vector3.zero;
                _anchor.localRotation = Quaternion.identity;
                Disable();
            }
            else
            {
                BodyPart.state = Grasp.GraspController.State.Translation;
                _translate = new(_anchor, null, Disable);
            }
            BodyPart.visual.Hide();
            ClearTracker();

            if (BodyPart.goal != null)
            {
                BodyPart.goal.Sleep(instant);
            }    
        }
        protected virtual void Disable()
        {
            _translate = null;
            if (BodyPart.chain != null)
            {
                BodyPart.chain.bendConstraint.weight = 1f;
            }
            BodyPart.state = Grasp.GraspController.State.Default;
            _anchor.gameObject.SetActive(BodyPart.GetDefaultState());
        }

        /// <summary>
        /// Sets the part to follow motion of particular transform.
        /// </summary>
        internal abstract void Attach(Transform target);

        //internal void SetBodyPartCollidersToTrigger(bool active)
        //{
        //    // To let rigidBody run free. Currently on hold, we use 'isKinematic' for a moment.
        //    foreach (var kv in _bodyPart.colliders)
        //    {
        //        kv.Key.isTrigger = active || kv.Value;
        //        //VRPlugin.Logger.LogDebug($"{_bodyPart.name} set {kv.Key.name}.Trigger = {kv.Key.isTrigger}[{kv.Value}]");
        //    }
        //}



        protected override void OnTriggerEnter(Collider other)
        {
            if (Tracker.AddCollider(other))
            {
                if (!_wasBusy)
                {
                    _wasBusy = true;
                    if (_hand != null)
                    {
                        _hand.Controller.StartRumble(new RumbleImpulse(500));
                        BodyPart.visual.SetColor(true);
                    }
                }
            }
        }

        protected override void OnTriggerExit(Collider other)
        {
            if (Tracker.RemoveCollider(other))
            {
                if (!IsBusy)
                {
                    _wasBusy = false;
                    //_unwind = true;
                    //_timer = 1f;
                    if (_hand != null)
                    {
                        BodyPart.visual.SetColor(false);
                    }
                }
            }
        }

    }
}

