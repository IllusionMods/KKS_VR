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
using ADV.Commands.Base;
using static KKAPI.MainGame.TalkSceneUtils;
using KK_VR.Grasp;
using KK_VR.Handlers.Helpers;

namespace KK_VR.Handlers
{
    class HSceneHandler : ItemHandler
    {
        private bool _injectLMB;
        private IKCaress _ikCaress;

        protected override void OnDisable()
        {
            base.OnDisable();
            TriggerRelease();
        }

        internal void StartMovingAibuItem(AibuColliderKind touch)
        {
            _hand.Shackle(touch == AibuColliderKind.kokan || touch == AibuColliderKind.anal ? 6 : 10);
            _ikCaress = GraspHelper.Instance.StartIKCaress(touch, HSceneInterpreter.lstFemale[0], _hand);
        }
        internal void StopMovingAibuItem()
        {
            if (_ikCaress != null)
            {
                _ikCaress.End();
                _hand.Unshackle();
                _hand.SetItemRenderer(true);
            }
        }

        protected bool AibuKindAllowed(AibuColliderKind kind, ChaControl chara)
        {
            var heroine = HSceneInterpreter.hFlag.lstHeroine
                .Where(h => h.chaCtrl == chara)
                .FirstOrDefault();
            return kind switch
            {
                AibuColliderKind.mouth => heroine.isGirlfriend || heroine.isKiss || heroine.denial.kiss,
                AibuColliderKind.anal => heroine.hAreaExps[3] > 0f || heroine.denial.anal,
                _ => true
            };
        }

        public bool DoUndress(bool decrease)
        {
            if (decrease && HSceneInterpreter.handCtrl.IsItemTouch() && IsAibuItemPresent(out var touch))
            {
                HSceneInterpreter.ShowAibuHand(touch, true);
                HSceneInterpreter.handCtrl.DetachItemByUseAreaItem(touch - AibuColliderKind.muneL);
                HSceneInterpreter.hFlag.click = HFlag.ClickKind.de_muneL + (int)touch - 2;
            }
            else if (Interactors.Undresser.Undress(_tracker.colliderInfo.behavior.part, _tracker.colliderInfo.chara, decrease))
            {
                //HandNoises.PlaySfx(_index, 1f, HandNoises.Sfx.Undress, HandNoises.Surface.Cloth);
            }
            else
            {
                return false;
            }
            _controller.StartRumble(new RumbleImpulse(1000));
            return true;
        }
        /// <summary>
        /// Does tracker has lock on attached aibu item?
        /// </summary>
        /// <param name="touch"></param>
        /// <returns></returns>
        internal bool IsAibuItemPresent(out AibuColliderKind touch)
        {
            touch = _tracker.colliderInfo.behavior.touch;
            if (touch > AibuColliderKind.mouth && touch < AibuColliderKind.reac_head)
            {
                return HSceneInterpreter.handCtrl.useAreaItems[touch - AibuColliderKind.muneL] != null;
            }
            return false;
        }
        internal bool TriggerPress()
        {
            var touch = _tracker.colliderInfo.behavior.touch;
            var chara = _tracker.colliderInfo.chara;
           //VRPlugin.Logger.LogDebug($"HSceneHandler:[{touch}][{HSceneInterpreter.handCtrl.selectKindTouch}]");
            if (touch > AibuColliderKind.mouth
                && touch < AibuColliderKind.reac_head
                && chara == HSceneInterpreter.lstFemale[0])
            {
                if (IntegrationSensibleH.active && !MouthGuide.Instance.IsActive && HSceneInterpreter.handCtrl.GetUseAreaItemActive() != -1)
                {
                    // If VRMouth isn't active but automatic caress is going. Disable it.
                    IntegrationSensibleH.OnKissEnd();
                }
                else
                {
                    HSceneInterpreter.SetSelectKindTouch(touch);
                    HandCtrlHooks.InjectMouseButtonDown(0);
                    _injectLMB = true;
                }
            }
            else
            {
                HSceneInterpreter.HitReactionPlay(_tracker.colliderInfo.behavior.react, chara, voiceWait: false);
            }
            return true;
        }
        internal void TriggerRelease()
        {
            if (_injectLMB)
            {
                HSceneInterpreter.SetSelectKindTouch(AibuColliderKind.none);
                HandCtrlHooks.InjectMouseButtonUp(0);
                _injectLMB = false;
            }
        }
        protected override void DoReaction(float velocity)
        {
           //VRPlugin.Logger.LogDebug($"DoReaction:{_tracker.colliderInfo.behavior.react}:{_tracker.colliderInfo.behavior.touch}:{_tracker.reactionType}:{velocity}");
            if (_settings.AutomaticTouching > KoikatuSettings.SceneType.TalkScene)
            {
                if (velocity > 1.5f || (_tracker.reactionType == Tracker.ReactionType.HitReaction && !IsAibuItemPresent(out _)))
                {
                    if (_settings.TouchReaction != 0f 
                        && HSceneInterpreter.mode == HFlag.EMode.aibu
                        && GraspHelper.Instance != null 
                        && !GraspHelper.Instance.IsGraspActive(_tracker.colliderInfo.chara)
                        && UnityEngine.Random.value < _settings.TouchReaction)
                    {
                        GraspHelper.Instance.TouchReaction(_tracker.colliderInfo.chara, _hand.Anchor.position, _tracker.colliderInfo.behavior.part);
                    }
                    else
                    {
                        HSceneInterpreter.HitReactionPlay(_tracker.colliderInfo.behavior.react, _tracker.colliderInfo.chara, voiceWait: true);
                    }
                }
                else if (_tracker.reactionType == Tracker.ReactionType.Short)
                {
                    Features.LoadVoice.PlayVoice(Features.LoadVoice.VoiceType.Short, _tracker.colliderInfo.chara, voiceWait: true);
                }
                else //if (_tracker.reactionType == ControllerTracker.ReactionType.Laugh)
                {
                    Features.LoadVoice.PlayVoice(Features.LoadVoice.VoiceType.Laugh, _tracker.colliderInfo.chara, voiceWait: true);
                }
                _controller.StartRumble(new RumbleImpulse(1000));
            }
        }
    }
}