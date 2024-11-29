//using KK.RootMotion.FinalIK;
//using RootMotion.FinalIK;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using VRGIN.Core;

//namespace KK_VR.Interpreters
//{
//    internal class HSceneHelper
//    {
//        private Transform[] GetFullSpine(RootMotion.FinalIK.FullBodyBipedIK fbbik)
//        {
//            var result = new Transform[4];
//            result[0] = fbbik.references.spine[0];
//            result[1] = fbbik.references.spine[1];
//            result[2] = result[1].Find("cf_j_spine03");
//            result[3] = fbbik.references.spine[2];
//            return result;
//        }
//        internal void SetupLookAtIK(ChaControl chara)
//        {
//            var fbbik = chara.objAnim.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>();
//            if (fbbik == null) return;
//            var lookAt = chara.objAnim.AddComponent<KK.RootMotion.FinalIK.LookAtIK>();
//            lookAt.solver.SetChain(GetFullSpine(fbbik), fbbik.references.head, null, fbbik.solver.root);
//            var lookController = chara.objAnim.AddComponent<LookAtController>();
//            lookController.ik = lookAt;
//            lookController.weightSmoothTime = 1f;
//            lookController.targetSwitchSmoothTime = 1f;
//            lookController.maxRadiansDelta = 0.25f;
//            lookController.maxMagnitudeDelta = 0.25f;
//            lookController.slerpSpeed = 1f;
//            lookController.target = VR.Camera.Head;

//        }
//    }
//}
