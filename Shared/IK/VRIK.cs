//using KK.RootMotion.FinalIK;
//using KK_VR.Fixes;
//using KK_VR.Handlers;
//using KK_VR.Holders;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using VRGIN.Core;

//namespace KK_VR.Features
//{
//    public class VRIK
//    {
//        public static VRIK Instance => _instance ?? new();
//        private static VRIK _instance;


//        public void TestRun(ChaControl chara)
//        {
//            PrepareVRIK(chara);
//        }

//        private KK.RootMotion.FinalIK.VRIK PrepareVRIK(ChaControl chara)
//        {
//            var ik = chara.animBody.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>();
//            if (ik == null) return null;
//            ik.enabled = false;
//            var refs = ik.references;
//            var vrik = chara.animBody.gameObject.AddComponent<KK.RootMotion.FinalIK.VRIK>();
//            var vRef = vrik.references;

//            vRef.root = refs.root;
//            vRef.pelvis = refs.pelvis;
//            vRef.spine = refs.spine[1]; // cf_j_spine02
//            vRef.chest = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03");
//            vRef.neck = refs.spine[2];
//            vRef.head = refs.head;

//            vRef.leftShoulder = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_L/cf_j_shoulder_L");
//            vRef.leftUpperArm = refs.leftUpperArm;
//            vRef.leftForearm = refs.leftForearm;
//            vRef.leftHand = refs.leftHand;

//            vRef.rightShoulder = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_R/cf_j_shoulder_R");
//            vRef.rightUpperArm = refs.rightUpperArm;
//            vRef.rightForearm = refs.rightForearm;
//            vRef.rightHand = refs.rightHand;

//            vRef.leftThigh = refs.leftThigh;
//            vRef.leftCalf = refs.leftCalf;
//            vRef.leftFoot = refs.leftFoot;
//            vRef.leftToes = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L/cf_j_leg01_L/cf_j_leg03_L/cf_j_foot_L/cf_j_toes_L");

//            vRef.rightThigh = refs.rightThigh;
//            vRef.rightCalf = refs.rightCalf;
//            vRef.rightFoot = refs.rightFoot;
//            vRef.rightToes = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R/cf_j_leg01_R/cf_j_leg03_R/cf_j_foot_R/cf_j_toes_R");
//            return vrik;
//        }

//        private readonly Vector3[] _handPosOffset =
//            [
//            new(0f, 0f, -0.1f),
//            new(0f, 0f, -0.1f)
//            ];
//        private readonly Quaternion[] _handRotOffset =
//            [
//            Quaternion.Euler(-30f, 90f, 0f),
//            Quaternion.Euler(-30f, -90f, 0f),
//            ];
//        private void SyncWithRig(KK.RootMotion.FinalIK.VRIK vrik)
//        {
//            var hands = HandHolder.GetHands;
//            vrik.solver.leftArm.target = AddHandOffset(0, hands[0].Anchor);
//            vrik.solver.rightArm.target = AddHandOffset(1, hands[1].Anchor);
//            var headTarget = new GameObject("headTarget").transform;
//            headTarget.SetParent(VR.Camera.Head, false);
//            headTarget.localPosition = Vector3.zero;
//            headTarget.localRotation = Quaternion.identity;
//            vrik.solver.spine.headTarget = headTarget;

//            Util.CreatePrimitive(PrimitiveType.Sphere, new Vector3(0.05f, 0.05f, 0.05f), VR.Mode.Left.transform, Color.yellow, 0.5f);
//            Util.CreatePrimitive(PrimitiveType.Sphere, new Vector3(0.05f, 0.05f, 0.05f), VR.Mode.Right.transform, Color.yellow, 0.5f);
//        }

//        private Transform AddHandOffset(int index, Transform parent)
//        {
//            var gameObject = new GameObject("VRIK_hand_anchor");
//            gameObject.transform.SetParent(parent, false);
//            // For some reason when we deal with VRGIN objects, orientation can get weird out of nowhere.
//            gameObject.transform.localPosition = _handPosOffset[index];
//            gameObject.transform.localRotation = _handRotOffset[index];
//            return gameObject.transform;
//        }
//    }
//}
