using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_VR.IK
{
    /// <summary>
    /// Copies config from old FullBodyBipedIK to the new one
    /// </summary>
    internal static class FBBIK
    {
        // IKSolver exec order is 9999
        // NeckLook is  11000.
        // EyeLook is 10800
        internal static KK.RootMotion.FinalIK.FullBodyBipedIK UpdateFBIK(ChaControl chara)
        {
            var newFbik = chara.objAnim.GetComponent<KK.RootMotion.FinalIK.FullBodyBipedIK>();
            if (newFbik != null) return newFbik;

            var oldFbik = chara.objAnim.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>();
            if (oldFbik == null) return null;
            newFbik = chara.objAnim.AddComponent<KK.RootMotion.FinalIK.FullBodyBipedIK>();

            newFbik.references.root = oldFbik.references.root;
            newFbik.references.pelvis = oldFbik.references.pelvis;
            newFbik.references.leftThigh = oldFbik.references.leftThigh;
            newFbik.references.leftCalf = oldFbik.references.leftCalf;
            newFbik.references.leftFoot = oldFbik.references.leftFoot;
            newFbik.references.rightThigh = oldFbik.references.rightThigh;
            newFbik.references.rightCalf = oldFbik.references.rightCalf;
            newFbik.references.rightFoot = oldFbik.references.rightFoot;
            newFbik.references.leftUpperArm = oldFbik.references.leftUpperArm;
            newFbik.references.leftForearm = oldFbik.references.leftForearm;
            newFbik.references.leftHand = oldFbik.references.leftHand;
            newFbik.references.rightUpperArm = oldFbik.references.rightUpperArm;
            newFbik.references.rightForearm = oldFbik.references.rightForearm;
            newFbik.references.rightHand = oldFbik.references.rightHand;
            newFbik.references.head = chara.objHeadBone.transform.parent;
            //newFbik.references.head = oldFbik.references.head;
            newFbik.references.spine = oldFbik.references.spine;
            //newFbik.references.spine =
            //    [
            //    oldFbik.references.spine[1],
            //    oldFbik.references.spine[1].Find("cf_j_spine03"),
            //    oldFbik.references.spine[2]
            //    ];
            newFbik.SetReferences(newFbik.references, oldFbik.solver.rootNode); // newFbik.references.spine[0]); // oldFbik.solver.rootNode);

            for (var i = 0; i < newFbik.solver.effectors.Length; i++)
            {
                newFbik.solver.effectors[i].target = oldFbik.solver.effectors[i].target;
                newFbik.solver.effectors[i].positionWeight = oldFbik.solver.effectors[i].positionWeight;
                newFbik.solver.effectors[i].rotationWeight = oldFbik.solver.effectors[i].rotationWeight;
            }
            for (var i = 0; i < newFbik.solver.chain.Length; i++)
            {
                newFbik.solver.chain[i].bendConstraint.bendGoal = oldFbik.solver.chain[i].bendConstraint.bendGoal;
                newFbik.solver.chain[i].bendConstraint.weight = oldFbik.solver.chain[i].bendConstraint.weight;
                newFbik.solver.chain[i].reach = oldFbik.solver.chain[i].reach;
                newFbik.solver.chain[i].pull = oldFbik.solver.chain[i].pull;
                newFbik.solver.chain[i].pin = oldFbik.solver.chain[i].pin;
                newFbik.solver.chain[i].push = oldFbik.solver.chain[i].push;

            }
            oldFbik.enabled = false;
            newFbik.fixTransforms = true;
            newFbik.solver.pullBodyHorizontal = 0.5f;
            chara.objTop.SetActive(false);
            chara.objTop.SetActive(true);
            return newFbik;
        }

        internal static KK.RootMotion.FinalIK.FBBIKHeadEffector CreateHeadEffector(ChaControl chara, Transform anchor)
        {
            // We don't use actual root-head bone, as neck-aim script gets in the way there on LateUpdate/FixedUpdate,
            // instead we use direct descendant. While not mazing-amazing, script is just fine, i'd rather not tinker/rewrite it.

            var newFbik = chara.objAnim.GetComponent<KK.RootMotion.FinalIK.FullBodyBipedIK>();
            var oldFbik = chara.objAnim.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>();

            // Head effector is a target in of itself, therefore we use it as an anchor.
            //var headBone = new GameObject("ank_ik_head");
            //headBone.transform.SetParent(newFbik.references.head, false);

            var headEffector = anchor.gameObject.AddComponent<KK.RootMotion.FinalIK.FBBIKHeadEffector>();
            headEffector.ik = newFbik;
            headEffector.positionWeight = 0.1f;
            headEffector.rotationWeight = 1f;
            headEffector.bodyWeight = 0.6f;
            headEffector.thighWeight = 0.5f;
            headEffector.bodyClampWeight = 0f;
            headEffector.headClampWeight = 0f;
            headEffector.bendWeight = 1f;


            // The most important thing.
            // Whole behavior is dictated by it.
            // Must have picks:
            //     cf_j_waist01,
            //     
            headEffector.bendBones =
                [
                // cf_j_waist01 a game changer
                // Can be a hit, or requires a bit of adjustment to translate from miss into an even bigger hit, depends on the animator. AnimStates can be generalized.
                //new() { transform = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01"), weight = 1f },

                //new() { transform = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02"), weight = 0.5f },

                // cf_j_spine01
                new() { transform = oldFbik.references.spine[0], weight = 0.6f },   // 0.8f 
                
                // cf_j_spine02
                new() { transform = oldFbik.references.spine[1], weight = 0.8f  },

                //new() { transform = oldFbik.references.spine[1].Find("cf_j_spine03"), weight = 0.9f  },

                // cf_j_neck
                new() { transform = oldFbik.references.spine[2], weight = 1f }
                ];

            headEffector.CCDWeight = 0.5f;
            headEffector.stretchBones =
                [
                //chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01"),
                //chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02"),
                oldFbik.references.spine[0],
                oldFbik.references.spine[1],
                //oldFbik.references.spine[1].Find("cf_j_spine03"),
                oldFbik.references.spine[2]
                ];
            headEffector.postStretchWeight = 0.2f;
            headEffector.maxStretch = 0.07f;
            headEffector.CCDBones =
                [
                // cf_j_waist01 - solid pick
                //chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01"), 
                
                //// cf_j_waist02 - good pick when together with cf_j_waist01.
                //chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02"),

                // cf_j_spine01
                oldFbik.references.spine[0],

                // cf_j_spine02
                oldFbik.references.spine[1],

                // cf_j_spine03
                //oldFbik.references.spine[1].Find("cf_j_spine03"),

                // cf_j_neck
                oldFbik.references.spine[2]
                ];
            return headEffector;
        }
    }
}
