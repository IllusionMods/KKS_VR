using System;
using System.Collections.Generic;
using System.Text;
using KK_VR.IK;
using static KK_VR.Grasp.GraspController;
using UnityEngine;

namespace KK_VR.Grasp
{
    internal class BodyPartHead : BodyPart
    {
        internal readonly KK.RootMotion.FinalIK.FBBIKHeadEffector headEffector;
        internal BodyPartHead(
            PartName _name,
            ChaControl _chara,
            Transform _afterIK,
            Transform _beforeIK) : base(_name, _afterIK, _beforeIK)
        {
            headEffector = FBBIK.CreateHeadEffector(_chara, anchor);
        }
    }
}
