using Illusion.Component.Correct;
using KK_VR.Handlers;
using KK_VR.Interpreters;
using System;
using System.Collections.Generic;
using System.Text;
using static KK_VR.Grasp.GraspController;
using UnityEngine;

namespace KK_VR.Grasp
{
    /// <summary>
    /// Convergence of everything we need to manipulate each body part of a character with the IK
    /// </summary>
    internal class BodyPart
    {
        internal readonly PartName name;
        // Personal for each limb.
        internal readonly Transform anchor;
        // Character bone after IK.
        internal readonly Transform afterIK;
        // Character bone before IK. (Extra or native gameObject)
        internal readonly Transform beforeIK;
        // Whatever was in effector.target at the start. We need it to not upset default code when animator changes states (swap done with harmony hooks). Still actual after new IK?
        internal readonly Transform origTarget;
        // Not applicable to head.
        internal readonly KK.RootMotion.FinalIK.IKEffector effector;
        internal readonly KK.RootMotion.FinalIK.FBIKChain chain;
        // Script to keep effector at offset instead of pinning it. Not applicable to head.
        //internal readonly KK_VR.IK.OffsetEffector offsetEffector;
        // Default component. We need it to not upset default code when animator changes state.
        internal readonly BaseData baseData;
        internal State state;
        internal List<Collider> colliders = [];
        // Component responsible for moving and collider tracking.
        internal readonly PartGuide guide;
        internal readonly BendGoal goal;
        // Primitive to show attachment point.
        internal readonly VisualObject visual;
        internal bool IsHand => name == PartName.HandL || name == PartName.HandR;
        internal bool IsLimb => name > PartName.ThighR && name < PartName.Head;
        internal BodyPart(
            PartName _name,
            Transform _afterIK,
            Transform _beforeIK,
            KK.RootMotion.FinalIK.IKEffector _effector = null,
            KK.RootMotion.FinalIK.FBIKChain _chain = null)
        {
            name = _name;
            afterIK = _afterIK;
            beforeIK = _beforeIK;
            visual = new VisualObject(this);
            anchor = new GameObject("ik_ank_" + GetLowerCaseName()).transform;

            if (_name != PartName.Head)
            {
                effector = _effector;
                effector.positionWeight = 0f;
                effector.rotationWeight = 1f;
                origTarget = effector.target;
                baseData = effector.target.GetComponent<BaseData>();
                effector.target = null;
                chain = _chain;
                guide = visual.gameObject.AddComponent<BodyPartGuide>();
                //offsetEffector = anchor.gameObject.AddComponent<KK_VR.IK.OffsetEffector>();
                if (_name == PartName.HandL || _name == PartName.HandR)
                {
                    effector.maintainRelativePositionWeight = KoikatuInterpreter.Settings.MaintainLimbOrientation ? 1f : 0f;
                    if (KoikatuInterpreter.Settings.PushParent != 0f)
                    {
                        chain.push = 1f;
                        chain.pushParent = KoikatuInterpreter.Settings.PushParent;
                    }
                    else
                    {
                        chain.push = 0f;
                        chain.pushParent = 0f;
                    }
                    chain.pushSmoothing = KK.RootMotion.FinalIK.FBIKChain.Smoothing.Cubic;
                    // To my surprise i couldn't make "chain.reach" to run in the game or editor. 
                    // old one has reach working just fine with seemingly the same config.
                    chain.reach = 0f;

                    // Purely aesthetic offset. The gameObject can be moved freely with no repercussions. 
                    guide.transform.localPosition = new(_name == PartName.HandL ? -0.05f : 0.05f, -0.015f, 0f);
                }

            }
            else
            {
                guide = visual.gameObject.AddComponent<HeadPartGuide>();
            }
            if (IsLimb)
            {
                goal = BendGoal.Create(this);
            }
        }
        internal string GetLowerCaseName()
        {
            var chars = name.ToString().ToCharArray();
            chars[0] = Char.ToLower(chars[0]);
            return new string(chars);
        }

        // Limbs/head are always On. The rest are conserving precious ticks.
        // They are very cheap.. Limit IK on hidden targets instead, that stuff is VERY expensive.
        internal bool GetDefaultState() => name > PartName.ThighR;
        internal void Destroy() => GameObject.Destroy(guide.gameObject);
    }
}
