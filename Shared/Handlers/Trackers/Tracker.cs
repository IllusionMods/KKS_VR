using Illusion.Extensions;
using KK_VR.Holders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UniRx;
using UnityEngine;
using static HandCtrl;

namespace KK_VR.Handlers
{
    class Tracker
    {
        internal bool IsBusy => _trackList.Count > 0;
        /// <summary>
        /// Stores info about allowed and present colliders from all the charas. Initiated once per scene.
        /// </summary>
        protected static readonly Dictionary<Collider, ColliderInfo> _referenceTrackDic = [];
        protected Dictionary<ChaControl, List<Body>> _blacklistDic;
        protected readonly List<Collider> _trackList = [];
        internal ColliderInfo colliderInfo;

        /// <summary>
        /// Checks if the collider is in use.
        /// </summary>
        internal static bool IsColliderPresent(Collider collider, out ChaControl chara)
        {
            if (_referenceTrackDic.ContainsKey(collider))
            {
                chara = _referenceTrackDic[collider].chara;
                return true;
            }
            chara = null;
            return false;
        }       


        internal void SetBlacklistDic(Dictionary<ChaControl, List<Body>> dic)
        {
            _blacklistDic = dic;
        }
        internal class ColliderInfo
        {
            internal ColliderInfo(Collider _collider, BodyBehavior _behavior, ChaControl _chara)
            {
                collider = _collider;
                behavior = _behavior;
                chara = _chara;
            }
            internal readonly ChaControl chara;
            internal readonly Collider collider;
            internal readonly BodyBehavior behavior;
        }

        internal static void Initialize(IEnumerable<ChaControl> charas)
        {
            _referenceTrackDic.Clear();
            foreach (var chara in charas)
            {
                if (chara == null) continue;
                foreach (var collider in chara.GetComponentsInChildren<Collider>(includeInactive: true))
                {
                    if (_allowedColliders.TryGetValue(collider.name, out var bodyBehavior))
                    {
                        EnableCollider(collider);
                        _referenceTrackDic.Add(collider, new ColliderInfo(collider, bodyBehavior, chara));
                    }
                }
            }
            foreach (var hand in HandHolder.GetHands)
            {
                var collider = hand.GetComponent<Collider>();
                _referenceTrackDic.Add(collider, new ColliderInfo(collider, new BodyBehavior(Body.None, AibuColliderKind.none, AibuColliderKind.none), null));
            }
        }
        private static void EnableCollider(Collider collider)
        {
            // Those are rigidBodies that shall rest for now.
            if (collider.name.StartsWith("ik_", StringComparison.Ordinal)) return;

            collider.enabled = true;
            collider.isTrigger = false;
            collider.gameObject.layer = 10;
            collider.gameObject.SetActive(true);
        }
        internal virtual bool AddCollider(Collider other)
        {
            if (_referenceTrackDic.TryGetValue(other, out var info)
                && (info.chara == null || (info.chara.visibleAll
                && !IsInBlacklist(info.chara, info.behavior.part))))
            {
                colliderInfo = info;
                _trackList.Add(other);
                return true;
            }
            return false;
        }
        protected virtual void SetState()
        {
            if (!IsBusy)
            {
                colliderInfo = null;
            }
            else
            {
                colliderInfo = _referenceTrackDic[_trackList.Last()];
            }

        }
        internal virtual bool RemoveCollider(Collider other)
        {
            if (_trackList.Remove(other))
            {
                SetState();
                return true;
            }
            return false;
        }
        protected IEnumerable<ColliderInfo> GetCollidersInfo()
        {
            return _referenceTrackDic
                .Where(kv => _trackList.Any(collider => collider.Equals(kv.Key)))
                .Select(kv => kv.Value);
        }
        internal void SetSuggestedInfoNoBlacks()
        {
            var infoList = GetCollidersInfo();
            if (_blacklistDic.Count != 0)
            {
                //foreach (var kv in _blacklistDic)
                //{
                //    kv.Value.ForEach(b => VRPlugin.Logger.LogDebug($"Blacklist:{kv.Key.name} - {b}"));
                //}
                infoList = infoList
                    .Where(info => !_blacklistDic.ContainsKey(info.chara) || (!_blacklistDic[info.chara].Contains(Body.None) && !_blacklistDic[info.chara].Contains(info.behavior.part)));
                if (infoList.Count() == 0)
                {
                    colliderInfo = null;
                    return;
                }
            }
            colliderInfo = infoList
                .OrderBy(info => info.behavior.part)
                .First();
        }

        /// <summary>
        /// Sets the most interesting currently tracking body part in the field 'colliderInfo'.
        /// </summary>
        internal void SetSuggestedInfo(ChaControl tryToAvoid = null)
        {
            var infoList = GetCollidersInfo();

            if (tryToAvoid != null)
            {
                infoList = infoList
                    .OrderBy(info => info.chara == tryToAvoid)
                    .ThenBy(info => info.behavior.part);
            }
            else
            {
                infoList = infoList
                    .OrderBy(info => info.behavior.part);
            }

            colliderInfo = infoList.FirstOrDefault(info => info.behavior.touch != AibuColliderKind.none) ?? infoList.First();
        }
        internal void ClearTracker()
        {
            colliderInfo = null;
            _trackList.Clear();
        }
        internal void RemoveBlacks()
        {
            // Turns out flush is nice only for synced limbs, for the rest we want not flush but suppression.

            for (var i = 0; i < _trackList.Count; i++)
            {
                var info = _referenceTrackDic[_trackList[i]];
                if (info.chara != null && _blacklistDic.ContainsKey(info.chara) && _blacklistDic[info.chara].Contains(info.behavior.part))
                {
                    _trackList.RemoveAt(i);
                    i--;
                }
            }
            SetState();
        }
        [Conditional("DEBUG")]
        internal void DebugShowActive()
        {
            VRPlugin.Logger.LogDebug($"Tracker:ActiveTracks");
            foreach (var track in _trackList)
            {
                VRPlugin.Logger.LogDebug($"* {track.name}");
            }
        }

        protected bool IsInBlacklist(ChaControl chara, Body part)
        {
            if (_blacklistDic != null && _blacklistDic.ContainsKey(chara) && (_blacklistDic[chara].Contains(Body.None) || _blacklistDic[chara].Contains(part)))
            {
                return true;
            }
            return false;
        }

        internal class BodyBehavior
        {
            internal readonly Body part;
            /// <summary>
            /// Type of HitReaction
            /// </summary>
            internal readonly AibuColliderKind react;
            /// <summary>
            /// Aibu item attached/TalkScene reaction
            /// </summary>
            internal readonly AibuColliderKind touch;
            internal BodyBehavior(Body _part, AibuColliderKind _react, AibuColliderKind _touch)
            {
                part = _part;
                react = _react;
                touch = _touch;
            }
        }
        private static readonly Dictionary<string, BodyBehavior> _allowedColliders = new()
        {
            { "com_hit_head",         new BodyBehavior(Body.Head, AibuColliderKind.reac_head, AibuColliderKind.reac_head) },
            { "com_hit_cheek",        new BodyBehavior(Body.Head, AibuColliderKind.reac_head, AibuColliderKind.mouth) },
            { "aibu_hit_mouth",       new BodyBehavior(Body.Head, AibuColliderKind.reac_head, AibuColliderKind.mouth) },
            // Far too big
            //{ "aibu_hit_head",      new BodyBehavior(Body.Head, AibuColliderKind.reac_head, AibuColliderKind.none) },

            { "cf_hit_spine01",       new BodyBehavior(Body.UpperBody, AibuColliderKind.reac_bodyup, AibuColliderKind.none) },
            { "cf_hit_spine03",       new BodyBehavior(Body.UpperBody, AibuColliderKind.reac_bodyup, AibuColliderKind.none) },
            { "cf_hit_bust02_L",      new BodyBehavior(Body.MuneL, AibuColliderKind.reac_bodyup, AibuColliderKind.muneL) },
            { "cf_hit_bust02_R",      new BodyBehavior(Body.MuneR, AibuColliderKind.reac_bodyup, AibuColliderKind.muneR) },
            { "cf_hit_arm_L",         new BodyBehavior(Body.ArmL, AibuColliderKind.reac_armL, AibuColliderKind.none) },
            { "cf_hit_wrist_L",       new BodyBehavior(Body.ForearmL, AibuColliderKind.reac_armL, AibuColliderKind.none) },
            { "cf_hit_arm_R",         new BodyBehavior(Body.ArmR, AibuColliderKind.reac_armR, AibuColliderKind.none) },
            { "cf_hit_wrist_R",       new BodyBehavior(Body.ForearmR, AibuColliderKind.reac_armR, AibuColliderKind.none) },
            { "com_hit_hand_L",       new BodyBehavior(Body.HandL, AibuColliderKind.reac_armL, AibuColliderKind.reac_armL) },
            { "com_hit_hand_R",       new BodyBehavior(Body.HandR, AibuColliderKind.reac_armR, AibuColliderKind.reac_armR) },
            { "cf_hit_berry",         new BodyBehavior(Body.LowerBody, AibuColliderKind.reac_bodydown, AibuColliderKind.none) },
            { "cf_hit_waist_L",       new BodyBehavior(Body.LowerBody, AibuColliderKind.reac_bodydown, AibuColliderKind.none) },

            //{ "cf_hit_siri_L",      new BodyBehavior(Body.Groin, AibuColliderKind.reac_bodydown, AibuColliderKind.siriL) },            
            //{ "cf_hit_siri_R",      new BodyBehavior(Body.Groin, AibuColliderKind.reac_bodydown, AibuColliderKind.siriR) },

            { "cf_hit_waist02",       new BodyBehavior(Body.Groin, AibuColliderKind.reac_bodydown, AibuColliderKind.none) },
            { "aibu_hit_siri_L",      new BodyBehavior(Body.Groin, AibuColliderKind.reac_bodydown, AibuColliderKind.siriL) },
            { "aibu_hit_siri_R",      new BodyBehavior(Body.Groin, AibuColliderKind.reac_bodydown, AibuColliderKind.siriR) },
            { "aibu_hit_kokan",       new BodyBehavior(Body.Asoko, AibuColliderKind.reac_bodydown, AibuColliderKind.kokan) },
            { "aibu_hit_ana",         new BodyBehavior(Body.Asoko, AibuColliderKind.reac_bodydown, AibuColliderKind.anal) },
            { "cf_hit_thigh01_L",     new BodyBehavior(Body.ThighL, AibuColliderKind.reac_bodydown, AibuColliderKind.none) },
            { "cf_hit_thigh01_R",     new BodyBehavior(Body.ThighR, AibuColliderKind.reac_bodydown, AibuColliderKind.none) },

            //{ "cf_hit_thigh02_L",   new BodyBehavior(Body.LegL, AibuColliderKind.reac_legL, AibuColliderKind.none) },
            //{ "cf_hit_leg01_L",     new BodyBehavior(Body.LegL, AibuColliderKind.reac_legL, AibuColliderKind.none) },

            //{ "cf_hit_thigh02_R",   new BodyBehavior(Body.LegR, AibuColliderKind.reac_legR, AibuColliderKind.none) },
            //{ "cf_hit_leg01_R",     new BodyBehavior(Body.LegR, AibuColliderKind.reac_legR, AibuColliderKind.none) },
            
            { "aibu_reaction_legL",   new BodyBehavior(Body.LegL, AibuColliderKind.reac_legL, AibuColliderKind.none) },
            { "aibu_reaction_legR",   new BodyBehavior(Body.LegR, AibuColliderKind.reac_legR, AibuColliderKind.none) },
            { "aibu_reaction_thighL", new BodyBehavior(Body.LegL, AibuColliderKind.reac_legL, AibuColliderKind.none) },
            { "aibu_reaction_thighR", new BodyBehavior(Body.LegR, AibuColliderKind.reac_legR, AibuColliderKind.none) },
            { "cf_hit_leg02_L",       new BodyBehavior(Body.FootL, AibuColliderKind.reac_legL, AibuColliderKind.none) },
            { "cf_hit_leg02_R",       new BodyBehavior(Body.FootR, AibuColliderKind.reac_legR, AibuColliderKind.none) },

            // Obsolete.
            // RigidBodies that move limbs.
            //{ "L_HandGuide",          new BodyBehavior(Body.HandL, AibuColliderKind.reac_armL, AibuColliderKind.reac_armL) },
            //{ "R_HandGuide",          new BodyBehavior(Body.HandR, AibuColliderKind.reac_armR, AibuColliderKind.reac_armR) },
            //{ "L_FootGuide",          new BodyBehavior(Body.FootL, AibuColliderKind.reac_legL, AibuColliderKind.none) },
            //{ "R_FootGuide",          new BodyBehavior(Body.FootR, AibuColliderKind.reac_legR, AibuColliderKind.none) }
        };

        internal enum ReactionType
        {
            None,
            Laugh,
            Short,
            HitReaction
            // Slap Reaction? after new hitReaction maybe.
        }
        internal enum Body
        {
            // Ordered according to priorities
            None,
            Head,
            MuneL,
            MuneR,
            Asoko,
            HandL,
            HandR,
            ForearmL,
            ForearmR,
            ArmL,
            ArmR,
            LowerBody,
            UpperBody,
            Groin,
            ThighL,
            ThighR,
            FootL,
            FootR,
            LegL,
            LegR,
        }
    }
}
