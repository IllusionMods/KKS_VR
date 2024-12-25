//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using VRGIN.Controls;

//namespace KK_VR.Features
//{
//    // Component to atleast somehow walk in VRIK.
//    // Proper setup would require retargeting bunch of animations on KK body rig, 
//    // and that would require someone who knows their way around the blender. 
//    // Based on FinalIK demo.
//    internal class VRIKGuide : MonoBehaviour
//    {
//        private Animator _animator;
//        private Controller _controller;
//        private Transform _root;
//        private bool _run;
//        private float _speed;
//        private float _angleVel;
//        private float _speedVel;
//        private float _accelerationTime = 0.2f;

//        internal void StartMovement()
//        {

//        }
//        internal void OnTrigger(bool press)
//        {

//        }
//        //private float GetStickAngle()
//        //{
//        //    return 
//        //}

//        private void UpdatePosition()
//        {

//            var xy = _controller.Input.GetAxis();
//            var deg = Mathf.Atan2(xy.y, xy.x) * Mathf.Rad2Deg + 90f;
//            Move(xy.magnitude);
//        }
//        private void Rotate()
//        {
//            // Base current rotation on Head.forward + angle from joystick.
//            //var angle = 

//        }
//        private void Move(float distance)
//        {
//            _speed = Mathf.SmoothDamp(_speed, _run ? 1f : 0.5f, ref _speedVel, _accelerationTime);
//            distance *= _speed;

//            _animator.SetFloat("Locomotion", distance);

//            _root.position += Time.deltaTime * distance * _root.forward;
//        }
//    }
//}
