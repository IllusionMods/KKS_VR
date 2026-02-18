using ADV.Commands.Base;
using KK_VR.Caress;
using KK_VR.Features;
using KK_VR.Grasp;
using KK_VR.Interpreters;
using KK_VR.Settings;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRGIN.Helpers;
using static HandCtrl;
using static KK_VR.Handlers.Tracker;

namespace KK_VR.Handlers
{
    class HSceneHandler : ItemHandler
    {
        private bool _injectLMB;
        private IKCaress _ikCaress;
        // Value stored at the beginning of manual caress to play sfx for it later.
        private AibuColliderKind _manualCaressKind;

        // Don't attempt to play reaction/sfx when doing manual caress item movement.
        protected override bool IsInteractionEligible => _ikCaress == null;

        protected override void OnDisable()
        {
            base.OnDisable();
            TriggerRelease();
        }

        internal void StartMovingAibuItem(AibuColliderKind touch)
        {
            _hand.Shackle(touch == AibuColliderKind.kokan || touch == AibuColliderKind.anal ? 6 : 10);
            _ikCaress = GraspHelper.Instance.StartIKCaress(touch, HSceneInterp.lstFemale[0], _hand);
            _manualCaressKind = touch;
        }

        internal void StopMovingAibuItem()
        {
            if (_ikCaress != null)
            {
                _ikCaress.End();
                _ikCaress = null;
                _hand.Unshackle();
                _hand.SetItemRenderer(true);
            }
        }

        protected bool AibuKindAllowed(AibuColliderKind kind, ChaControl chara)
        {
            var heroine = HSceneInterp.hFlag.lstHeroine
                .Where(h => h.chaCtrl == chara)
                .FirstOrDefault();
            return kind switch
            {
                AibuColliderKind.mouth => heroine.isGirlfriend || heroine.isKiss || heroine.denial.kiss,
                AibuColliderKind.anal => heroine.hAreaExps[3] > 0f || heroine.denial.anal,
                _ => true
            };
        }

        protected override void Update()
        {
            base.Update();
            // Play sfx when doing manual caress.
            if (_ikCaress != null && !_hand.SFX.IsPlaying)
            {
                var velocityAdjusted = _ikCaress.GetVelocity * KoikGameInterp.GetCurrentFPS;
#if DEBUG
                VRPlugin.Logger.LogDebug($"{GetType().Name}.Update:velocityAdjusted[{velocityAdjusted}]");
#endif
                if (velocityAdjusted < 0.002f) return;
                // Value < 1.5 = play "Traverse" at full volume, > 1.5 - 1.8 = play "Tap" at not full volume.
                // So that we get mostly traverses with occasional taps during IK caress.
                DoCaressSfx(_manualCaressKind, Random.value * 1.8f);
            }
        }

        protected void DoCaressSfx(AibuColliderKind caressKind, float velocity)
        {
            var chara = HSceneInterp.lstFemale[0];

            Tracker.Body part = caressKind switch
            {
                AibuColliderKind.muneL or AibuColliderKind.muneR => Tracker.Body.MuneL,
                AibuColliderKind.kokan or AibuColliderKind.anal => Tracker.Body.Asoko,
                AibuColliderKind.siriL or AibuColliderKind.siriR => Tracker.Body.Groin,
                _ => 0
            };
            InvokeTapTraverseSfx(chara, part, velocity);
        }

        public bool DoUndress(bool decrease)
        {
            var info = _tracker.GetColliderInfo;
            var setting = KoikSettings.SfxDelay.Value;

            // Detach an aibu item if present.
            if (decrease && HSceneInterp.handCtrl.IsItemTouch() && IsAibuItemPresent(out var touch))
            {
                HSceneInterp.ShowAibuHand(touch, true);
                HSceneInterp.handCtrl.DetachItemByUseAreaItem(touch - AibuColliderKind.muneL);
                HSceneInterp.hFlag.click = HFlag.ClickKind.de_muneL + (int)touch - 2;
            }
            // If setting and we can undress current body part - play sfx and wait for it to finish to actually perform an (un)dress action(s).
            else if (setting && Undresser.DelayedUndress(info.behavior.part, info.chara, decrease, out var changedSlot, out var proposedAction))
            {
                var clipLength = PlaySfx(changedSlot);
                KoikGameInterp.RunAfterTimer(proposedAction, false, clipLength);
            }
            // Or to (un)dress and play sfx all at once.
            else if (!setting && Undresser.Undress(info.behavior.part, info.chara, decrease, out changedSlot))
            {
                PlaySfx(changedSlot);
            }
            // Fail to perform an action.
            else
            {
                return false;
            }
            _controller.StartRumble(new RumbleImpulse(1000));
            return true;

            // Local function for readability.
            float PlaySfx(int changedSlot)
            {
                return _hand.SFX.PlaySfx(1f, decrease ? SFXLoader.Sfx.Undress : SFXLoader.Sfx.Dress, SFXLoader.Surface.Cloth,
                   changedSlot switch
                   {
                       // TODO Come up with a condition to stuff in wet for panties.
                       // Top or Bottom
                       0 or 1 => SFXLoader.Intensity.Hard,
                       // Panties, Gloves, Pantyhose or Stockings
                       2 or 3 or 4 or 5 or 6 => SFXLoader.Intensity.Soft,
                       // Either type of Shoes
                       7 or 8 => SFXLoader.Intensity.Hollow,
                       _ => 0
                   },
                   true);
            }
        }

        /// <summary>
        /// Does tracker has lock on attached aibu item?
        /// </summary>
        /// <param name="touch"></param>
        /// <returns></returns>
        internal bool IsAibuItemPresent(out AibuColliderKind touch)
        {
            touch = _tracker.GetColliderInfo.behavior.touch;
            if (touch > AibuColliderKind.mouth && touch < AibuColliderKind.reac_head)
            {
                return HSceneInterp.handCtrl.useAreaItems[touch - AibuColliderKind.muneL] != null;
            }
            return false;
        }

        internal bool TriggerPress()
        {
            var info = _tracker.GetColliderInfo;
            var touch = info.behavior.touch;
            var chara = info.chara;
#if DEBUG
            VRPlugin.Logger.LogDebug($"{GetType().Name}.{MethodInfo.GetCurrentMethod().Name}:infoTouch[{touch}]handCtrlTouch[{HSceneInterp.handCtrl.selectKindTouch}]");
#endif
            if (touch > AibuColliderKind.mouth
                && touch < AibuColliderKind.reac_head
                && chara == HSceneInterp.lstFemale[0])
            {
                if (IntegrationSensibleH.IsActive && !MouthGuide.Instance.IsActive && HSceneInterp.handCtrl.GetUseAreaItemActive() != -1)
                {
                    // If VRMouth isn't active but automatic caress is going. Disable it.
                    IntegrationSensibleH.OnKissEnd();
                }
                else
                {
                    HSceneInterp.SetSelectKindTouch(touch);
                    HandCtrlHooks.InjectMouseButtonDown(0);
                    _injectLMB = true;
                    // Do reaction occasionally when we do click aibu action.
                    if (Random.value < 0.33f)
                    {
                        HSceneInterp.HitReactionPlay(info.behavior.react, chara, voiceWait: false);
                    }
                }
            }
            else
            {
                HSceneInterp.HitReactionPlay(info.behavior.react, chara, voiceWait: false);
            }
            return true;
        }

        internal void TriggerRelease()
        {
            if (_injectLMB)
            {
                HSceneInterp.SetSelectKindTouch(AibuColliderKind.none);
                HandCtrlHooks.InjectMouseButtonUp(0);
                _injectLMB = false;
            }
        }
        
        protected override void DoReaction(float velocity)
        {
            var info = _tracker.GetColliderInfo;
            var reactionType = _tracker.reactionType;
            var chara = info.chara;

#if DEBUG
            VRPlugin.Logger.LogDebug($"{GetType().Name}.{MethodInfo.GetCurrentMethod().Name}:" +
                $"reactionType[{reactionType}] isIkCaress[{_ikCaress != null}]");
#endif

            if (!IsReactionEligible(chara)) return;

            if (velocity > 1.5f || (reactionType == ReactionType.HitReaction && !IsAibuItemPresent(out _)))
            {
                var touchReaction = GameSettings.AutoTouchAltReaction.Value;
                // Try to play alternative Touch Reaction implemented via fancy IK tweaks.
                if (touchReaction != 0f
                    && HSceneInterp.mode == HFlag.EMode.aibu
                    && GraspHelper.Instance != null
                    && !GraspHelper.Instance.IsGraspActive(chara)
                    && Random.value < touchReaction)
                {
                    GraspHelper.Instance.TouchReaction(chara, _hand.Anchor.position, info.behavior.part);
                }
                // Or fall back to native in-game reaction.
                else
                {
                    HSceneInterp.HitReactionPlay(info.behavior.react, chara, voiceWait: true);
                }
            }
            else if (reactionType == ReactionType.Short)
            {
                LoadGameVoice.PlayVoice(LoadGameVoice.VoiceType.Short, chara, voiceWait: true);
            }
            else //if (_tracker.reactionType == ControllerTracker.ReactionType.Laugh)
            {
                LoadGameVoice.PlayVoice(LoadGameVoice.VoiceType.Laugh, chara, voiceWait: true);
            }
            _controller.StartRumble(new RumbleImpulse(1000));
        }

    }
}