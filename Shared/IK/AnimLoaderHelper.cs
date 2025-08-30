using Illusion.Component.Correct;
using Illusion.Component.Correct.Process;
using KK_VR.Grasp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_VR.IK
{
    // AnimationLoader doesn't implement MotionIK data, and as result game doesn't set up FBBIK properly.
    // We set it up to salvage some references and abandon after. Have to do this after every position change on postfix.
    // Fixing it properly would require filling all the motion data by hand and storing with plugin assets, while not that big of an endeavor,
    // it only responsible for offsets from attachment points, and VR hardly cares about it, we have Grasp and soon VRIK.

    /// <summary>
    /// Fixes IK on animations added by AnimationLoader plugin. 
    /// Doesn't add MotionIK data i.e. offsets for different body sizes
    /// </summary>
    public class AnimLoaderHelper
    {
        internal static void FixExtraAnim(ChaControl chara, List<BodyPart> bodyPartList)
        {
            for (var i = 5; i < 9; i++)
            {
                var bodyPart = bodyPartList[i];
                if (bodyPart.baseData.bone != null) continue;

                var ikBeforeProcess = bodyPart.baseData.gameObject.GetComponent<IKBeforeProcess>();
                if (ikBeforeProcess != null)
                {
                    bodyPart.baseData.bone = chara.objAnim.transform.Find(cf_pv_bones_efTargets[i - 5]);
                    bodyPart.baseData.enabled = true;
                    ikBeforeProcess.enabled = true;
                    ikBeforeProcess.type = BaseProcess.Type.Sync;
                }
                var bendGoal = bodyPart.chain.bendConstraint.bendGoal;
                if (bendGoal == null)
                {
                    VRPlugin.Logger.LogWarning($"FBBIK(native) - {chara.name} - {bodyPart.name} doesn't have bendGoal");
                    continue;
                }

                var baseData = bendGoal.GetComponent<BaseData>();
                if (baseData == null)
                {
                    VRPlugin.Logger.LogWarning($"FBBIK(native) - {chara.name} - {bodyPart.name} doesn't have BaseData");
                    continue;
                }

                var ikBeforeProcessBendGoal = bendGoal.GetComponent<IKBeforeProcess>();
                if (ikBeforeProcessBendGoal == null)
                {
                    VRPlugin.Logger.LogWarning($"FBBIK(native) - {chara.name} - {bodyPart.name} doesn't have IKBeforeProcess (bendGoal)");
                    continue;
                }

                baseData.bone = chara.objAnim.transform.Find(cf_pv_bones_bendGoals[i - 5]);
                if (baseData.bone == null)
                {
                    VRPlugin.Logger.LogWarning($"Failed to find bendGoal bone at {cf_pv_bones_bendGoals[i - 5]}");
                    continue;
                }
                baseData.enabled = true;
                ikBeforeProcessBendGoal.enabled = true;
                ikBeforeProcessBendGoal.type = BaseProcess.Type.Sync;
            }
        }

        public static void FindMissingBones(RootMotion.FinalIK.FullBodyBipedIK ik)
        {
            // Limbs only.
            for (var i = 5; i < 9; i++)
            //for (var i = 0; i < ik.solver.effectors.Length; i++)
            {
                var target = ik.solver.effectors[i].target;
                //if (target == null)
                //{
                //    ik.solver.effectors[i].target = ik.transform.Find(cf_t_bones_efTargets[i]);
                //}
                // We want only limbs.
                if (target != null)
                //if (target != null && i > 4)
                {
                    // Our IK anchor. Look at its parent instead.
                    if (target.name.StartsWith("ik_", StringComparison.Ordinal))
                    {
                        target = target.parent;
                    }
                    if (target != null && target.name.StartsWith("cf_t", StringComparison.Ordinal))
                    {
                        var baseData = target.GetComponent<BaseData>();
                        var ikBeforeProcess = target.GetComponent<IKBeforeProcess>();
                        if (baseData != null && ikBeforeProcess != null)
                        {
                            if (baseData.bone == null)
                            {
                                baseData.bone = ik.transform.Find(cf_pv_bones_efTargets[i - 5]);
                            }
                            //VRPlugin.Logger.LogWarning($"FindMissingBones[{i}]");
                            //baseData.pos = ik.transform.InverseTransformDirection(ik.solver.effectors[i].bone.position - baseData.bone.position);
                            baseData.enabled = true;
                            ikBeforeProcess.enabled = true;
                            ikBeforeProcess.type = BaseProcess.Type.Sync;
                        }
                    }
                }
            }
            // Set bend constraint bones too.
            // 1st chain is body, nothing there.
            for (var i = 1; i < ik.solver.chain.Length; i++)
            {
                var bendGoal = ik.solver.chain[i].bendConstraint.bendGoal;
                // Game should do this.
                //if (bendGoal == null)
                //{
                //    bendGoal = ik.transform.Find(cf_t_bones_bendGoals[i - 1]);
                //}
                if (bendGoal != null)
                {
                    var baseData = bendGoal.GetComponent<BaseData>();
                    var ikBeforeProcess = bendGoal.GetComponent<IKBeforeProcess>();
                    if (baseData != null && ikBeforeProcess != null)
                    {
                        if (baseData.bone == null)
                        {
                            baseData.bone = ik.transform.Find(cf_pv_bones_bendGoals[i - 1]);
                        }
                        baseData.enabled = true;
                        ikBeforeProcess.enabled = true;
                        ikBeforeProcess.type = BaseProcess.Type.Sync;
                    }
                }
            }
        }
        private static readonly List<string> cf_pv_bones_efTargets =
            [ 
            // No clue what moves those bones. They are offset through MotionIK though.
            // They move separately with the body, on some controllers/states don't move at all, and that's all under default anim controller.

            "cf_j_root/cf_n_height/cf_pv_root/cf_pv_hand_L",
            "cf_j_root/cf_n_height/cf_pv_root/cf_pv_hand_R",
            "cf_j_root/cf_n_height/cf_pv_root/cf_pv_leg_L",
            "cf_j_root/cf_n_height/cf_pv_root/cf_pv_leg_R"
            ];
        //private static readonly List<string> cf_t_bones_efTargets =
        //    [ 
        //    // We have to have them all on corresponding effector.target with their baseData when animator changes state,
        //    // otherwise the native code will be out for a spanking.

        //    "cf_t_root/cf_t_hips",
        //    "cf_t_root/cf_t_hips/cf_t_shoulder_L",
        //    "cf_t_root/cf_t_hips/cf_t_shoulder_R",
        //    "cf_t_root/cf_t_hips/cf_t_waist_L",
        //    "cf_t_root/cf_t_hips/cf_t_waist_R",

        //    "cf_t_root/cf_t_hand_L",
        //    "cf_t_root/cf_t_hand_R",
        //    "cf_t_root/cf_t_leg_L",
        //    "cf_t_root/cf_t_leg_R"
        //    ];

        //private static readonly List<string> cf_t_bones_bendGoals =
        //    [ 
        //    "cf_t_root/cf_t_elbo_L",
        //    "cf_t_root/cf_t_elbo_R",
        //    "cf_t_root/cf_t_knee_L",
        //    "cf_t_root/cf_t_knee_R"
        //    ];

        private static readonly List<string> cf_pv_bones_bendGoals =
            [
            "cf_j_root/cf_n_height/cf_pv_root/cf_pv_elbo_L",
            "cf_j_root/cf_n_height/cf_pv_root/cf_pv_elbo_R",
            "cf_j_root/cf_n_height/cf_pv_root/cf_pv_knee_L",
            "cf_j_root/cf_n_height/cf_pv_root/cf_pv_knee_R"
            ];
    }
}
