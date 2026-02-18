using ADV.Commands.Base;
using KK_VR.Interpreters;
using KK_VR.Settings;
using Manager;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KK_VR.Handlers
{
    // Supposed to be at disposal of component directly under controller's control.
    class ControllerTracker : Tracker
    {
        internal bool firstTrack;
        internal ReactionType reactionType;

        private readonly List<Body> _reactOncePerTrack = [];
        private float _lastTrack;

        // The more familiar the less frequent are reactions.
        private float GetFamiliarity
        {
            get
            {
                // Add exp/weak point influence?
                SaveData.Heroine heroine = null;
                var flag = HSceneInterp.hFlag;

                if (flag != null)
                {
                    heroine = flag.lstHeroine
                        .Where(h => h.chaCtrl == _colliderInfo.chara)
                        .FirstOrDefault();
                }
#if KK
            heroine??= Game.Instance.HeroineList
#else
                heroine ??= Game.HeroineList
#endif
                    .Where(h => h.chaCtrl == _colliderInfo.chara ||
                    (h.chaCtrl != null
                    && h.chaCtrl.fileParam.fullname == _colliderInfo.chara.fileParam.fullname
                    && h.chaCtrl.fileParam.personality == _colliderInfo.chara.fileParam.personality))
                    .FirstOrDefault();
                if (heroine != null)
                {
                    // Uncapped from 0..1 for easier reach of ceiling for actions (1)
                    // and even bigger possible window for "FirstTouch" reaction.
                    var familiarity =
                        // Caps at 0.5
                        0.5f * Mathf.Clamp(heroine.lewdness, 0, 100) +
                        // Caps at 0.75
                        0.25f * (int)heroine.HExperience +
                        // Caps at 0.5
                        0.5f * Mathf.Clamp(flag.gaugeFemale, 0f, 100f);

                    if (heroine.isGirlfriend)
                        familiarity += 0.25f;

                    return familiarity;

                    //*
                    //  (HSceneInterp.hFlag != null && HSceneInterp.hFlag.isFreeH ?
                    //1f : (0.5f + heroine.intimacy * 0.005f));
                }
                else
                {
                    // Extra characters/player.
                    return 0.75f;
                }
            }
        }

        internal override bool AddCollider(Collider other)
        {
            if (_referenceTrackDic.TryGetValue(other, out var info))
            {
                // Temporal clutch until we can grab objects.
                // KKS doesn't update chara.visibleAll.
                if (info.chara != null && info.chara.rendBody.isVisible && !IsInBlacklist(info.chara, info.behavior.part))
                {
                    _colliderInfo = info;
                    SetReaction();
                    _trackList.Add(other);
                    return true;
                }
            }
            return false;
        }

        internal override bool RemoveCollider(Collider other)
        {
            if (_trackList.Remove(other))
            {
                if (!IsBusy)
                {
                    _lastTrack = Time.time;
                    _reactOncePerTrack.Clear();
                    _colliderInfo = null;
                }
                else
                    _colliderInfo = _referenceTrackDic[_trackList.Last()];

                return true;
            }
            return false;
        }

        private bool IsSoftPart(Body part)
        {
            return part switch
            {
                Body.MuneL or Body.MuneR or Body.Groin or Body.ThighL or Body.ThighR => true,
                _ => false
            };
        }

        private void SetReaction()
        {
            // The idea is:
            // On FirstTouch at low familiarity play HitReaction,
            // at high familiarity Laugh or Short.
            // On 
            var settingVoice = KoikSettings.AutoTouchVoice.Value;
            var settingReact = KoikSettings.AutoTouchReaction.Value;

            var familiarity = GetFamiliarity;
            // 0..1
            var familiarityHalf = familiarity * 0.5f;

            if (!IsBusy)
            {
                firstTrack = true;
                // ConsecutiveTouch
                if (_lastTrack + (5f + 10f * familiarity) > Time.time)
                {
                    var randomValue = Random.value;
                    reactionType =
                        randomValue < settingVoice ?
                        // Setting allowed voice.
                        // Check familiarity (0..2).
                        randomValue < familiarity ?
                        // Success, higher the familiarity the more chances for None.
                        Random.value < familiarityHalf ? ReactionType.None : ReactionType.Laugh :
                        // Fail, play Short.
                        ReactionType.Short :
                        // Setting didn't allow voice.
                        ReactionType.None;
                }
                // FirstTouch
                else 
                {
                    reactionType = GetImportantPlaceReaction();
                }
            }
            else
            {
                // Continuous groping
                firstTrack = false;
                if (ReactOncePerTrack(_colliderInfo.behavior.part))
                {
                    // Important place touch, once per track.
                    reactionType = GetImportantPlaceReaction();
                }
                else
                {
                    reactionType = ReactionType.None;
                }
            }

            ReactionType GetImportantPlaceReaction()
            {
                var randomValue = Random.value;
                return
                        // Check Familiarity (0..2) and settingVoice
                        randomValue < familiarity && randomValue < settingVoice ?
                        // Success Familiarity and settingVoice check, play Laugh or None for higher Familiarity and Short for lower.
                        randomValue < familiarityHalf ? Random.value < familiarityHalf ? ReactionType.None : ReactionType.Laugh : ReactionType.Short :
                        // Fail Familiarity or settingVoice check, check settingReaction and play Reaction or None. 
                        randomValue < settingReact ? ReactionType.Reaction : ReactionType.None;
            }
        }

        private bool ReactOncePerTrack(Body part)
        {
            if (part < Body.HandL && !_reactOncePerTrack.Contains(part))
            {
                _reactOncePerTrack.Add(part);
                if (part == Body.MuneL)
                {
                    _reactOncePerTrack.Add(Body.MuneR);
                }
                else if (part == Body.MuneR)
                {
                    _reactOncePerTrack.Add(Body.MuneL);
                }
                return true;
            }
            return false;
        }
    }
}
