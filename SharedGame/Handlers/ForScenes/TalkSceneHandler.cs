using VRGIN.Helpers;
using UnityEngine;
using KK_VR.Interpreters;
using KK_VR.Settings;
using KK_VR.Features;
using static HandCtrl;
using KK_VR.Grasp;

namespace KK_VR.Handlers
{
    class TalkSceneHandler : ItemHandler
    {
        internal bool DoUndress(bool decrease, out ChaControl chara)
        {
            var info = _tracker.GetColliderInfo;
            var setting = KoikSettings.SfxDelay.Value;
            chara = info.chara;

            // The idea is simple.
            // If we can undress current body part - play sfx and wait for it to finish to actually perform an (un)dress action(s).
            if (setting && Undresser.DelayedUndress(info.behavior.part, chara, decrease, out var changedSlot, out var proposedAction))
            {
                var clipLength = PlaySfx(changedSlot);
                KoikGameInterp.RunAfterTimer(proposedAction, false, clipLength);
            }
            // Or to (un)dress and play sfx at once.
            else if (!setting && Undresser.Undress(info.behavior.part, chara, decrease, out changedSlot))
            {
                PlaySfx(changedSlot);
            }
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

        protected override void DoReaction(float velocity)
        {
            var info = _tracker.GetColliderInfo;
            var chara = info.chara;
            if (!IsReactionEligible(chara)) return;

            var touch = info.behavior.touch;
            if (TalkSceneInterp.talkScene != null
                && touch != AibuColliderKind.none
                && chara == TalkSceneInterp.talkScene.targetHeroine.chaCtrl
                && !CrossFader.AdvHooks.Reaction
                // Add familiarity here too ? prob
                && (velocity > 1f || UnityEngine.Random.value < 0.3f)
                && (GraspHelper.Instance == null || !GraspHelper.Instance.IsGraspActive(chara)))
            {
                TalkSceneInterp.talkScene.TouchFunc(TouchReaction(touch), Vector3.zero);
            }
            else if (velocity > 1f || _tracker.reactionType == Tracker.ReactionType.Reaction)
            {
                if (GraspHelper.Instance != null && !GraspHelper.Instance.IsGraspActive(chara) && UnityEngine.Random.value < GameSettings.AutoTouchAltReaction.Value)
                {
                    GraspHelper.Instance.TouchReaction(chara, _hand.Anchor.position, info.behavior.part);
                }
                else
                {
                    TalkSceneInterp.HitReactionPlay(info.behavior.react, chara);
                }
            }
            else if (_tracker.reactionType == Tracker.ReactionType.Short)
            {
                Features.LoadGameVoice.PlayVoice(Features.LoadGameVoice.VoiceType.Short, chara, voiceWait: UnityEngine.Random.value < 0.5f);
            }
            else if (_tracker.reactionType == Tracker.ReactionType.Laugh)
            {
                Features.LoadGameVoice.PlayVoice(Features.LoadGameVoice.VoiceType.Laugh, chara, voiceWait: UnityEngine.Random.value < 0.5f);
            }
            _controller.StartRumble(new RumbleImpulse(1000));
            
        }

        public bool TriggerPress()
        {
            var info = _tracker.GetColliderInfo;
            var touch = info.behavior.touch;

            if (TalkSceneInterp.talkScene != null
                && touch != AibuColliderKind.none
                && info.chara == TalkSceneInterp.talkScene.targetHeroine.chaCtrl
                && !CrossFader.AdvHooks.Reaction)
            {
                TalkSceneInterp.talkScene.TouchFunc(TouchReaction(touch), Vector3.zero);
                return true;
            }
            return false;
        }

        public void TriggerRelease()
        {

        }

        private string TouchReaction(AibuColliderKind colliderKind)
        {
            return colliderKind switch
            {
                AibuColliderKind.mouth => "Cheek",
                AibuColliderKind.muneL => "MuneL",
                AibuColliderKind.muneR => "MuneR",
                AibuColliderKind.reac_head => "Head",
                AibuColliderKind.reac_armL => "HandL",
                AibuColliderKind.reac_armR => "HandR",
                _ => null

            };
        }
    }

}
