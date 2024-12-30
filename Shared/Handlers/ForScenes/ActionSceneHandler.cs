//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using VRGIN.Controls;

//namespace KK_VR.Handlers
//{
//    // No real use for this one unless we'll get VRIK with custom animController.
//    // LF animation retargeting.
//    internal class ActionSceneHandler : MonoBehaviour
//    {
//        private Controller _controller;
//        private void Awake()
//        {
//            _controller = GetComponent<Controller>();
//        }
//        internal float GetStickRotation()
//        {
//            var xy = _controller.Input.GetAxis();
//            return Mathf.Atan2(xy.y, xy.x) * Mathf.Rad2Deg + 90f;
//        }

//    }
//}
