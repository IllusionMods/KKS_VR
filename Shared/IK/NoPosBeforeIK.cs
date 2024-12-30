using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KK_VR.IK
{
    /// <summary>
    /// Component that assumes rotation of the target bone just before IK solver reads it. 
    /// </summary>
    [DefaultExecutionOrder(9900)]
    internal class NoPosBeforeIK : MonoBehaviour
    {
        private Transform _bone;
        internal void Init(Transform bone)
        {
            _bone = bone;
        }
        internal void LateUpdate()
        {
            if (_bone == null) return;
            transform.rotation = _bone.rotation;
        }
    }
}
