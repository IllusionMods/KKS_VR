﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsInput.Native;
using VRGIN.Core;
using KoikatuVR.Interpreters;
using UnityEngine;

namespace KoikatuVR.Controls
{
    /// <summary>
    /// A subtool that handles an arbitrary number of simple actions that only
    /// requires a single button.
    /// </summary>
    class ButtonsSubtool
    {
        /// <summary>
        /// The set of keys for which we've sent a down message but not a
        /// corresponding up message.
        /// </summary>
        private readonly HashSet<AssignableFunction> _SentUnmatchedDown
            = new HashSet<AssignableFunction>();
        private readonly KoikatuInterpreter _Interpreter;
        private readonly KoikatuSettings _Settings;

        public ButtonsSubtool(KoikatuInterpreter interpreter, KoikatuSettings settings)
        {
            _Interpreter = interpreter;
            _Settings = settings;
        }

        /// <summary>
        /// A method to be called in Update().
        /// </summary>
        public void Update()
        {
            if (_SentUnmatchedDown.Contains(AssignableFunction.PL2CAM))
            {
                IfActionScene(interpreter => interpreter.MovePlayerToCamera());
            }
        }

        /// <summary>
        /// Whether it's desirable to lock the controller.
        /// </summary>
        /// <returns></returns>
        public bool WantLock()
        {
            return _SentUnmatchedDown.Count > 0;
        }

        /// <summary>
        /// A method to be called when this subtool is destroyed.
        /// </summary>
        public void Destroy()
        {
            // Make a copy because the loop below will modify the HashSet.
            var todo = _SentUnmatchedDown.ToList();
            foreach (var key in todo)
            {
                ButtonUp(key);
            }
        }

        /// <summary>
        /// Process a ButtonDown message.
        /// </summary>
        public void ButtonDown(AssignableFunction fun)
        {
            switch (fun)
            {
                case AssignableFunction.NONE:
                    break;
                case AssignableFunction.WALK:
                    IfActionScene(interpreter => interpreter.StartWalking());
                    break;
                case AssignableFunction.DASH:
                    IfActionScene(interpreter => interpreter.StartWalking(true));
                    break;
                case AssignableFunction.PL2CAM:
                    break;
                case AssignableFunction.LBUTTON:
                    VR.Input.Mouse.LeftButtonDown();
                    break;
                case AssignableFunction.RBUTTON:
                    VR.Input.Mouse.RightButtonDown();
                    break;
                case AssignableFunction.MBUTTON:
                    VR.Input.Mouse.MiddleButtonDown();
                    break;
                case AssignableFunction.LROTATION:
                case AssignableFunction.RROTATION:
                case AssignableFunction.SCROLLUP:
                case AssignableFunction.SCROLLDOWN:
                    // ここでは何もせず、上げたときだけ処理する
                    break;
                case AssignableFunction.CROUCH:
                    IfActionScene(interpreter => interpreter.Crouch());
                    break;
                case AssignableFunction.NEXT:
                    throw new NotSupportedException();
                case AssignableFunction.KEYBOARD_PAGE_DOWN:
                    VR.Input.Keyboard.KeyDown(VirtualKeyCode.NEXT);
                    break;
                default:
                    VR.Input.Keyboard.KeyDown((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), fun.ToString()));
                    break;
            }
            _SentUnmatchedDown.Add(fun);
        }

        /// <summary>
        /// Process a ButtonUp message.
        /// </summary>
        public void ButtonUp(AssignableFunction fun)
        {
            switch (fun)
            {
                case AssignableFunction.NONE:
                    break;
                case AssignableFunction.WALK:
                    IfActionScene(interpreter => interpreter.StopWalking());
                    break;
                case AssignableFunction.DASH:
                    IfActionScene(interpreter => interpreter.StopWalking());
                    break;
                case AssignableFunction.PL2CAM:
                    break;
                case AssignableFunction.LBUTTON:
                    VR.Input.Mouse.LeftButtonUp();
                    break;
                case AssignableFunction.RBUTTON:
                    VR.Input.Mouse.RightButtonUp();
                    break;
                case AssignableFunction.MBUTTON:
                    VR.Input.Mouse.MiddleButtonUp();
                    break;
                case AssignableFunction.LROTATION:
                    Rotate(-_Settings.RotationAngle);
                    break;
                case AssignableFunction.RROTATION:
                    Rotate(_Settings.RotationAngle);
                    break;
                case AssignableFunction.SCROLLUP:
                    VR.Input.Mouse.VerticalScroll(1);
                    break;
                case AssignableFunction.SCROLLDOWN:
                    VR.Input.Mouse.VerticalScroll(-1);
                    break;
                case AssignableFunction.CROUCH:
                    IfActionScene(interpreter => interpreter.StandUp());
                    break;
                case AssignableFunction.NEXT:
                    throw new NotSupportedException();
                case AssignableFunction.KEYBOARD_PAGE_DOWN:
                    VR.Input.Keyboard.KeyUp(VirtualKeyCode.NEXT);
                    break;
                default:
                    VR.Input.Keyboard.KeyUp((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), fun.ToString()));
                    break;
            }
            _SentUnmatchedDown.Remove(fun);
        }

        /// <summary>
        /// Rotate the camera. If we are in Roaming, rotate the protagonist as well.
        /// </summary>
        /// <param name="degrees"></param>
        private void Rotate(float degrees)
        {
            VRLog.Debug($"Rotating {degrees}");
            var actInterpreter = _Interpreter.SceneInterpreter as ActionSceneInterpreter;
            if (actInterpreter != null)
            {
                actInterpreter.MoveCameraToPlayer(onlyPosition: true);
            }
            var camera = VR.Camera.transform;
            var newRotation = Quaternion.AngleAxis(degrees, Vector3.up) * camera.rotation;
            VRMover.Instance.MoveTo(camera.position, newRotation, keepHeight: false);
            if (actInterpreter != null)
            {
                actInterpreter.MovePlayerToCamera();
            }
        }

        private void IfActionScene(Action<ActionSceneInterpreter> a)
        {
            if (_Interpreter.SceneInterpreter is ActionSceneInterpreter actInterpreter)
            {
                a(actInterpreter);
            }
        }
    }
}
