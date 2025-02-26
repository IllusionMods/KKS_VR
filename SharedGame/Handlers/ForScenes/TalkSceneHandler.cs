﻿using VRGIN.Helpers;
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
            if (Undresser.Undress(_tracker.colliderInfo.behavior.part, chara = _tracker.colliderInfo.chara, decrease))
            {
                //HandNoises.PlaySfx(_index, 1f, HandNoises.Sfx.Undress, HandNoises.Surface.Cloth);
                _controller.StartRumble(new RumbleImpulse(1000));
                return true;
            }
            return false;
        }

        protected override void DoReaction(float velocity)
        {
            var chara = _tracker.colliderInfo.chara;
            if (!IsReactionEligible(chara)) return;

            var touch = _tracker.colliderInfo.behavior.touch;
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
            else if (velocity > 1f || _tracker.reactionType == Tracker.ReactionType.HitReaction)
            {
                if (GraspHelper.Instance != null && !GraspHelper.Instance.IsGraspActive(chara) && UnityEngine.Random.value < GameSettings.TouchReaction.Value)
                {
                    GraspHelper.Instance.TouchReaction(chara, _hand.Anchor.position, _tracker.colliderInfo.behavior.part);
                }
                else
                {
                    TalkSceneInterp.HitReactionPlay(_tracker.colliderInfo.behavior.react, chara);
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
            var chara = _tracker.colliderInfo.chara;
            var touch = _tracker.colliderInfo.behavior.touch;
            if (TalkSceneInterp.talkScene != null
                && touch != AibuColliderKind.none
                && chara == TalkSceneInterp.talkScene.targetHeroine.chaCtrl
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
