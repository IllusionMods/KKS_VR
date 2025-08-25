using KK_VR.Interpreters;
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
        private readonly List<Body> _reactOncePerTrack = [];
        private float _familiarity;
        private float _lastTrack;
        internal bool firstTrack;
        internal ReactionType reactionType;

        internal override bool AddCollider(Collider other)
        {
            if (_referenceTrackDic.TryGetValue(other, out var info))
            {
                // Temporal clutch until we can grab objects.
                // KKS doesn't update chara.visibleAll.
                if (info.chara != null && info.chara.rendBody.isVisible && !IsInBlacklist(info.chara, info.behavior.part))
                {
                    colliderInfo = info;
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
                    colliderInfo = null;
                }
                else
                    colliderInfo = _referenceTrackDic[_trackList.Last()];

                return true;
            }
            return false;
        }
        private void GetFamiliarity()
        {
            // Add exp/weak point influence?
            SaveData.Heroine heroine = null;
            if (HSceneInterp.hFlag != null)
            {
                heroine = HSceneInterp.hFlag.lstHeroine
                    .Where(h => h.chaCtrl == colliderInfo.chara)
                    .FirstOrDefault();
            }
#if KK
            heroine??= Game.Instance.HeroineList
#else
            heroine??= Game.HeroineList
#endif
                .Where(h => h.chaCtrl == colliderInfo.chara ||
                (h.chaCtrl != null
                && h.chaCtrl.fileParam.fullname == colliderInfo.chara.fileParam.fullname
                && h.chaCtrl.fileParam.personality == colliderInfo.chara.fileParam.personality))
                .FirstOrDefault();
            if (heroine != null)
            {
                _familiarity = (0.55f + (0.15f * (int)heroine.HExperience));
                //*
                //  (HSceneInterp.hFlag != null && HSceneInterp.hFlag.isFreeH ?
                //1f : (0.5f + heroine.intimacy * 0.005f));
            }
            else
            {
                // Extra characters/player.
                _familiarity = 0.75f;
            }
        }
        // Add mouth tracking? for starters head parts tracking at all.
        //internal bool IsTrackingWetPart(out Body part)
        //{
        //    foreach (var collider in _trackList)
        //    {
        //        if (_referenceTrackDic[collider].behavior.part == Body.Asoko)
        //        {
        //            part = _referenceTrackDic[collider].behavior.part;
        //            return true;
        //        }
        //    }
        //    part = Body.None;
        //    return false;
        //}
        //internal bool IsTrackingSoftPart(out Body part)
        //{
        //    foreach (var collider in _trackList)
        //    {
        //        if (IsSoftPart(_referenceTrackDic[collider].behavior.part))
        //        {
        //            part = _referenceTrackDic[collider].behavior.part;
        //            return true;
        //        }
        //    }
        //    part = Body.None;
        //    return false;
        //}
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
            if (!IsBusy)
            {
                GetFamiliarity();
                firstTrack = true;
                // A windows after last touch during which we go for "soft reaction".
                if (_lastTrack + (3f * _familiarity) > Time.time)
                {
                    // Consecutive touch within up to 2 seconds from the last touch.
                    reactionType = Random.value < _familiarity - 0.5f ? (Random.value < 0.5f ? ReactionType.Laugh : ReactionType.None) : ReactionType.Short;
                }
                else
                {
                    reactionType = ReactionType.HitReaction;
                }
            }
            else
            {
                firstTrack = false;
                if (ReactOncePerTrack(colliderInfo.behavior.part))
                {
                    // Important part touch, once per track.
                    reactionType = Random.value < _familiarity - 0.5f ? (Random.value < 0.5f ? ReactionType.Laugh : ReactionType.Short) : ReactionType.HitReaction;
                }
                else
                {
                    reactionType = ReactionType.None;
                }
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
        //internal void RemoveGuideObjects()
        //{
        //    // This one is obsolete most likely.
        //    for (var i = 0; i < _trackList.Count; i++)
        //    {
        //        if (_trackList[i].name.EndsWith("Guide", StringComparison.Ordinal))
        //        {
        //            _trackList.RemoveAt(i);
        //            i--;
        //        }

        //    }
        //    SetState();
        //}

        //Simplified and moved to Tracker.cs
        ///// <param name="preferredSex">0 - male, 1 - female, -1 ignore</param>
        //internal Body GetGraspBodyPart(ChaControl tryToAvoidChara = null, int preferredSex = -1)
        //{
        //    return GetCollidersInfo()
        //        .OrderBy(info => info.chara.sex != preferredSex)
        //        .ThenBy(info => info.chara != tryToAvoidChara)
        //        .ThenBy(info => info.behavior.part)
        //        .First().behavior.part;
        //}
        //internal Body GetGraspBodyPart()
        //{
        //    return GetCollidersInfo()
        //        .OrderBy(info => info.behavior.part)
        //        .First().behavior.part;
        //}

    }
}
