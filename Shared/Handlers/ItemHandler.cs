using KK_VR.Features;
using KK_VR.Holders;
using KK_VR.Interpreters;
using KK_VR.Settings;
using System.Reflection;
using UnityEngine;
using VRGIN.Controls;
using static HandCtrl;
using static KK.RootMotion.Demos.Turret;

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
        protected Controller _controller;
        private bool _unwind;
        private float _timer;
        private Rigidbody _rigidBody;
        internal override bool IsBusy
        {
            get
            {
                var info = _tracker.GetColliderInfo;
                return info != null && info.chara != null;
            }
        }


        // Default velocity is in the local space of a controller or camera origin.
#if KK
        protected Vector3 GetVelocity => _controller.Input.velocity;
#else
        protected Vector3 GetVelocity => _controller.Tracking.GetVelocity();
#endif
        protected virtual bool IsInteractionEligible => true;

        internal void Init(HandHolder hand)
        {
            _rigidBody = GetComponent<Rigidbody>();
            _hand = hand;
            _tracker = new ControllerTracker();
            _tracker.SetBlacklistDic(_hand.Grasp.GetBlacklistDic);

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
                var info = _tracker.GetColliderInfo;
                if (info.behavior.touch > AibuColliderKind.mouth
                    && info.behavior.touch < AibuColliderKind.reac_head)
                {
                    _hand.SetCollisionState(false);
                }

                if (!IsInteractionEligible)
                {
#if DEBUG
                    VRPlugin.Logger.LogDebug($"{GetType().Name}.{MethodInfo.GetCurrentMethod().Name}:Collider[{other.name}] But interaction isn't eligible.");
#endif
                    return;
                }
                else
                {
#if DEBUG
                    VRPlugin.Logger.LogDebug($"{GetType().Name}.{MethodInfo.GetCurrentMethod().Name}:Collider[{other.name}] And interaction is eligible.");
#endif
                }

                var velocity = GetVelocity.sqrMagnitude;
                // Velocity > 1.5f is basically a guaranteed slap reaction.
                if (velocity > 1.5f || _tracker.reactionType != Tracker.ReactionType.None)
                {
                    DoReaction(velocity);
                }
                if (_tracker.firstTrack)
                {
                    DoTapSlapSfx(velocity);
                }
                else if (!_hand.SFX.IsPlaying)
                {
                    DoTapTraverseSfx(velocity);
                }
            }
        }

        protected void DoTapSlapSfx(float velocity)
        {
            var part = _tracker.GetColliderInfo.behavior.part;

            var fast = velocity > 1.5f;
            _hand.SFX.PlaySfx(
                fast ? 0.5f + velocity * 0.2f : 1f,
                fast ? SFXLoader.Sfx.Slap : SFXLoader.Sfx.Tap,
                GetSurfaceType(_tracker.GetColliderInfo.chara, part),
                GetIntensityType(part),
                overwrite: true
                );
        }

        /// <summary>
        /// Play Tap or Traverse SFX based on velocity for currently tracked chara's BodyPart.
        /// </summary>
        protected void DoTapTraverseSfx(float velocity)
        {
            _tracker.SetSuggestedInfo();

            var part = _tracker.GetColliderInfo.behavior.part;
            InvokeTapTraverseSfx(_tracker.GetColliderInfo.chara, part, velocity);
        }

        /// <summary>
        /// Play Tap or Traverse SFX based on velocity for provided chara's BodyPart.
        /// </summary>
        protected void DoTapTraverseSfx(ChaControl chara, Tracker.Body part, float velocity)
        {
            InvokeTapTraverseSfx(chara, part, velocity);
        }

        protected void InvokeTapTraverseSfx(ChaControl chara, Tracker.Body part, float velocity)
        {
            var fast = velocity > 1.5f;
            _hand.SFX.PlaySfx(
                fast ? 0.5f + velocity * 0.2f : 1f,
                fast ? SFXLoader.Sfx.Tap : SFXLoader.Sfx.Traverse,
                GetSurfaceType(chara, part),
                GetIntensityType(part),
                overwrite: false
                );
        }

        protected SFXLoader.Surface GetSurfaceType(ChaControl chara, Tracker.Body part)
        {
            return part switch
            {
                Tracker.Body.Head => SFXLoader.Surface.Hair,
                _ => Undresser.IsBodyPartClothed(chara, part) ? SFXLoader.Surface.Cloth : SFXLoader.Surface.Skin
            };
        }

        protected SFXLoader.Intensity GetIntensityType(Tracker.Body part)
        {
            return part switch
            {
                Tracker.Body.Asoko => SFXLoader.Intensity.Wet,
                Tracker.Body.LowerBody => SFXLoader.Intensity.Hollow,
                Tracker.Body.MuneL or Tracker.Body.MuneR or Tracker.Body.ThighL or Tracker.Body.ThighR or Tracker.Body.Groin => SFXLoader.Intensity.Soft,
                _ => SFXLoader.Intensity.Hard
            };
        }

        protected bool IsReactionEligible(ChaControl chara)
        {
            var config = KoikSettings.AutoTouch.Value;
            if ((chara.sex == 0 && (config & KoikSettings.Genders.Boys) != 0)
                || (chara.sex == 1 && (config & KoikSettings.Genders.Girls) != 0))
            {
                return true;
            }
            return false;
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
                    HSceneInterp.SetSelectKindTouch(AibuColliderKind.none);
                    _hand.SetCollisionState(true);
                }
            }
        }

        internal Tracker.Body GetTrackPartName()
        //internal Tracker.Body GetTrackPartName(ChaControl tryToAvoidChara = null, int preferredSex = -1)
        {
            // Should be fine without extra sorting and such as the caller usually does it with 'UpdateTracker()'
            //return tryToAvoidChara == null && preferredSex == -1 ? _tracker.GetTrackedBodyPart() : _tracker.GetTrackedBodyPart(tryToAvoidChara, preferredSex);
            return _tracker.GetTrackedBodyPart();
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
            var chara = _tracker.GetColliderInfo.chara;
            if (!IsReactionEligible(chara)) return;
        }
    }
}