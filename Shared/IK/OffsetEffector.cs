//using UnityEngine;
//using System.Collections;
//using KK.RootMotion.FinalIK;
//using System.Collections.Generic;
//using KK_VR.Grasp;
//using RootMotion.FinalIK;

//namespace KK_VR.IK
//{

//    /// <summary>
//    /// Custom positionOffset effector for FBBIK.
//    /// </summary>
//    internal class OffsetEffector : MonoBehaviour
//    {
//        // Main link.
//        private KK.RootMotion.FinalIK.IKEffector _effector;
//        //private readonly float weight = 1f;

//        internal void Init(KK.RootMotion.FinalIK.IKEffector effector)
//        {
//            //_ik = ik;
//            _effector = effector;
//            //_ik.solver.OnPreUpdate += ModifyOffset;
//        }
//        private void LateUpdate()
//        {
//            _effector.positionOffset += transform.position - _effector.bone.position;
//        }
//    }
//}
