using KK.RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace KK_VR.IK
{
    // Requires proper implementation with added scripts to manage it in SensibleH (WIP).
    internal class LookAt
    {
        internal static LookAtController SetupLookAtIK(ChaControl chara)
        {
            return null;
            var fbbik = chara.objAnim.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>();
            if (fbbik == null) return null;
            var lookAt = chara.objAnim.AddComponent<KK.RootMotion.FinalIK.LookAtIK>();
            Transform[] spine =
                [
                fbbik.references.spine[0],
                fbbik.references.spine[1],
                fbbik.references.spine[1].Find("cf_j_spine03"),
                fbbik.references.spine[2]
                ];
            lookAt.solver.SetChain(spine, fbbik.references.head, null, fbbik.references.root);
            lookAt.solver.bodyWeight = 0.6f;
            lookAt.solver.headWeight = 0.8f;
            var lookAtController = chara.objAnim.AddComponent<LookAtController>();
            lookAtController.ik = lookAt;
            lookAtController.weightSmoothTime = 1f;
            lookAtController.targetSwitchSmoothTime = 1f;
            lookAtController.maxRadiansDelta = 0.25f;
            lookAtController.maxMagnitudeDelta = 0.25f;
            lookAtController.slerpSpeed = 1f;
            lookAtController.maxRootAngle = 180f;

            //lookController.target = VR.Camera.Head;
            //_spine03 = fbbik.references.spine[1].Find("cf_j_spine03");
            lookAtController.target = VR.Camera.Head;  //hFlag.ctrlCamera.transform;
            return lookAtController;
        }
    }
}
