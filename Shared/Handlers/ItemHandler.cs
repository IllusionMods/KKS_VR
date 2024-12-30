using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using VRGIN.Core;
using VRGIN.Controls;
using VRGIN.Helpers;
using HarmonyLib;
using UnityEngine;
using KK_VR.Interpreters;
using KK_VR.Settings;
using KK_VR.Fixes;
using KK_VR.Features;
using KK_VR.Controls;
using RootMotion.FinalIK;
using static HandCtrl;
using KK_VR.Caress;
using ADV.Commands.Game;
using KK_VR.Trackers;
using System.Runtime.Remoting.Messaging;
using KK_VR.Holders;

namespace KK_VR.Handlers
{
    /// <summary>
    /// Implementation for a controller based component
    /// </summary>
    class ItemHandler : Handler
    {
        protected ControllerTracker _tracker;
        protected override Tracker Tracker
        {
            get => _tracker;
            set => _tracker = value is ControllerTracker t ? t : null;
        }
        protected HandHolder _hand;
        protected KoikatuSettings _settings;
        protected Controller _controller;
        //protected ModelHandler.ItemType _item;
        private bool _unwind;
        private float _timer;
        private Rigidbody _rigidBody;
        internal override bool IsBusy => _tracker.colliderInfo != null && _tracker.colliderInfo.chara != null;

        // Default velocity is in local space of a controller or camera origin.
#if KK
        protected Vector3 GetVelocity => _controller.Input.velocity;
#else
        protected Vector3 GetVelocity => _controller.Tracking.GetVelocity();
#endif
        internal void Init(HandHolder hand)
        {
            _rigidBody = GetComponent<Rigidbody>();
            _hand = hand;
            _tracker = new ControllerTracker();
            _tracker.SetBlacklistDic(_hand.Grasp.GetBlacklistDic);

            _settings = VR.Context.Settings as KoikatuSettings;
            _controller = _hand.Controller;
        }
        protected virtual void Update()
        {
            if (_unwind)
            {
                _timer = Mathf.Clamp01(_timer - Time.deltaTime);
                _rigidBody.velocity *= _timer;
                if (_timer == 0f)
                {
                    _unwind = false;
                }
            }
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (_tracker.AddCollider(other))
            {
                if (_tracker.colliderInfo.behavior.touch > AibuColliderKind.mouth
                    && _tracker.colliderInfo.behavior.touch < AibuColliderKind.reac_head)
                {
                    _hand.SetCollisionState(false);
                }
                var velocity = GetVelocity.sqrMagnitude;
                if (velocity > 1.5f || _tracker.reactionType != Tracker.ReactionType.None)
                {
                    DoReaction(velocity);
                }
                if (_tracker.firstTrack)
                {
                    DoStartSfx(velocity);
                }
                else if (!_hand.Noise.IsPlaying)
                {
                    DoSfx(velocity);
                }
            }
        }

        protected void DoStartSfx(float velocity)
        {
            var fast = velocity > 1.5f;
            _hand.Noise.PlaySfx(
                fast ? 0.5f + velocity * 0.2f : 1f,
                fast ? HandNoise.Sfx.Slap : HandNoise.Sfx.Tap,
                GetSurfaceType(_tracker.colliderInfo.behavior.part),
                GetIntensityType(_tracker.colliderInfo.behavior.part),
                overwrite: true
                );
        }

        protected void DoSfx(float velocity)
        {
            _tracker.SetSuggestedInfo();
            _hand.Noise.PlaySfx(
                velocity > 1.5f ? 0.5f + velocity * 0.2f : 1f,
                velocity > 0.5f ? HandNoise.Sfx.Tap : HandNoise.Sfx.Traverse,
                GetSurfaceType(_tracker.colliderInfo.behavior.part),
                GetIntensityType(_tracker.colliderInfo.behavior.part),
                overwrite: false
                );
        }

        protected HandNoise.Surface GetSurfaceType(Tracker.Body part)
        {
            return part switch
            {
                Tracker.Body.Head => HandNoise.Surface.Hair,
                _ => Interactors.Undresser.IsBodyPartClothed(_tracker.colliderInfo.chara, part) ? HandNoise.Surface.Cloth : HandNoise.Surface.Skin
            };
        }
        protected HandNoise.Intensity GetIntensityType(Tracker.Body part)
        {
            return part switch
            {
                Tracker.Body.Asoko => HandNoise.Intensity.Wet,
                Tracker.Body.MuneL or Tracker.Body.MuneR or Tracker.Body.ThighL or Tracker.Body.ThighR or Tracker.Body.Groin => HandNoise.Intensity.Soft,
                _ => HandNoise.Intensity.Rough
            };
        }

        protected override void OnTriggerExit(Collider other)
        {
            if (_tracker.RemoveCollider(other))
            {
                if (!IsBusy)
                {
                    // RigidBody is being rigid, unwind it.
                    _unwind = true;
                    _timer = 1f;
                    // Do we need this?
                    HSceneInterpreter.SetSelectKindTouch(AibuColliderKind.none);
                    _hand.SetCollisionState(true);
                }
            }
        }

        internal Tracker.Body GetTrackPartName(ChaControl tryToAvoidChara = null, int preferredSex = -1)
        {
            return tryToAvoidChara == null && preferredSex == -1 ? _tracker.GetGraspBodyPart() : _tracker.GetGraspBodyPart(tryToAvoidChara, preferredSex);
        }
        internal void RemoveCollider(Collider other)
        {
            _tracker.RemoveCollider(other);
        }
        internal void DebugShowActive()
        {
            _tracker.DebugShowActive();
        }
        protected virtual void DoReaction(float velocity)
        {

        }
    }
}