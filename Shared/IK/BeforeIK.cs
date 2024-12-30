using KK_VR.Fixes;
using KK_VR.Interactors;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KK_VR.IK
{
    /// <summary>
    /// Component that assumes orientation of the target bone just before IK solver reads it. 
    /// </summary>
    [DefaultExecutionOrder(9900)]
    public class BeforeIK : MonoBehaviour
    {
        private Transform _bone;
        /// <summary>
        /// Creates new GameObject with this component, initiates it
        /// and attaches to "cf_t_root" bone of particular chara.
        /// </summary>
        internal static Transform CreateObj(string name, ChaControl chara, Transform targetBone)
        {

            var beforeIKObj = new GameObject("ik_b4_" + name).transform;
            beforeIKObj.SetPositionAndRotation(targetBone.position, targetBone.rotation);
            beforeIKObj.parent = chara.transform.Find("BodyTop/p_cf_body_bone/cf_t_root");
            beforeIKObj.gameObject.AddComponent<BeforeIK>().Init(targetBone);
            return beforeIKObj;
        }
        internal static Transform CreateObj(string name, ChaControl chara, Transform targetBone, Transform targetRotation)
        {

            var beforeIKObj = new GameObject("ik_b4_" + name).transform;
            beforeIKObj.SetPositionAndRotation(targetBone.position, targetBone.rotation);
            beforeIKObj.parent = chara.transform.Find("BodyTop/p_cf_body_bone/cf_t_root");
            beforeIKObj.gameObject.AddComponent<BeforeIK>().Init(targetBone);
            return beforeIKObj;
        }
        //private Vector3 _offsetPos;

        // Default quaternion isn't identity.
        //private Quaternion _offsetRot = Quaternion.identity;
        public void Init(Transform bone)
        {
            _bone = bone;
        }
        //public void SetOffsets(Vector3 offsetPosition, Quaternion offsetRotation)
        //{
        //    _offsetPos = offsetPosition;
        //    _offsetRot = offsetRotation;
        //}
        //public void SetDebug(float x, float y, float z)
        //{
        //    _offsetRot = Quaternion.Euler(x, y, z);
        //}
        //public void Retarget(Transform bone) => _bone = bone;

        //public void UpdateTransform()
        //{
        //    if (_bone == null || _chara == null) return;
        //    transform.SetPositionAndRotation(_bone.TransformPoint(_offsetPos), _bone.rotation * _offsetRot);
        //}
        public void LateUpdate()
        {
            if (_bone == null) return;
            transform.SetPositionAndRotation(_bone.position, _bone.rotation);
        }
    }
}
