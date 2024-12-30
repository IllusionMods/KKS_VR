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

namespace KK_VR.Handlers
{
    /// <summary>
    /// Component responsible for the management of functions associated with colliders
    /// </summary>
    internal class Handler : MonoBehaviour
    {
        protected virtual Tracker Tracker { get; set; }
        /// <summary>
        /// True if something is being tracked. Track for recently blacklisted items continues, but new ones don't get added.
        /// </summary>
        internal virtual bool IsBusy => Tracker.IsBusy;
        /// <summary>
        /// Can be true only after 'UpdateNoBlacks()' if every item in track is blacklisted.
        /// </summary>
        internal bool InBlack => Tracker.colliderInfo == null;
        internal Transform GetTrackTransform => Tracker.colliderInfo.collider.transform;
        internal ChaControl GetChara => Tracker.colliderInfo.chara;


        protected virtual void OnDisable()
        {
            Tracker.ClearTracker();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            Tracker.AddCollider(other);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            Tracker.RemoveCollider(other);
        }
        internal void ClearBlacks()
        {
            Tracker.RemoveBlacks();
        }
        /// <summary>
        /// Null-ghost panacea. No clue what they are.
        /// </summary>
        internal void ClearTracker()
        {
            Tracker.ClearTracker();
        }
        internal void UpdateTrackerNoBlacks()
        {
            Tracker.SetSuggestedInfoNoBlacks();
        }
        /// <summary>
        /// Pick the most interesting bodyPart from the current track.
        /// </summary>
        internal void UpdateTracker(ChaControl tryToAvoid = null)
        {
            Tracker.SetSuggestedInfo(tryToAvoid);
#if DEBUG
            Tracker.DebugShowActive();
#endif
        }

    }
}