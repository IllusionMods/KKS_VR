using VRGIN.Core;
using UnityEngine;
using VRGIN.Controls;
using Valve.VR;
using KK_VR.Settings;
using System.Collections.Generic;
using System.Linq;
using KK_VR.Camera;
using KK_VR.Holders;

namespace KK_VR.Interpreters
{
    abstract class SceneInterpreter
    {
        protected KoikatuSettings _settings = VR.Context.Settings as KoikatuSettings;
        protected readonly List<InputWait> _waitList = [];
        protected bool IsWait => _waitList.Count != 0;
        protected Grip _grip;

        /// <summary>
        /// 0 - Trigger. 
        /// 1 - Grip. 
        /// 2 - Touchpad (Joystick click).
        /// </summary>
        protected readonly bool[,] _pressedButtons = new bool[2, 3];
        protected enum Grip
        {
            None,
            Caress,
            Grasp,
            Move,
        }
        internal virtual bool IsTriggerPress(int index) => _pressedButtons[index, 0];
        internal virtual bool IsGripPress(int index) => _pressedButtons[index, 1];
        internal virtual bool IsTouchpadPress(int index) => _pressedButtons[index, 2];
        internal virtual bool IsGripMove() => _pressedButtons[0, 1] || _pressedButtons[1, 1];
        internal virtual void OnStart()
        {

        }
        internal virtual void OnDisable()
        {

        }
        internal virtual void OnUpdate()
        {
            HandleInput();
        }
        internal virtual void OnLateUpdate()
        {

        }

        internal virtual bool OnDirectionDown(int index, Controller.TrackpadDirection direction)
        {
            return false;
        }

        internal virtual void OnDirectionUp(int index, Controller.TrackpadDirection direction)
        {
            if (IsWait)
                PickAction(index, direction);
        }
        protected virtual bool OnTrigger(int index, bool press)
        {
            if (press)
            {
                _pressedButtons[index, 0] = true;
                if (!IsTouchpadPress(index) && IsWait)
                {
                    PickAction();
                }
            }
            else
            {
                _pressedButtons[index, 0] = false;
                PickAction(index, EVRButtonId.k_EButton_SteamVR_Trigger);
            }
            return false;
        }
        protected virtual bool OnGrip(int index, bool press)
        {
            if (press)
            {
                _grip = Grip.Move;
                _pressedButtons[index, 1] = true;
            }
            else
            {
                _grip = Grip.None;
                _pressedButtons[index, 1] = false;
            }
            // Return false for GripMove.
            return false;
        }
        protected virtual bool OnTouchpad(int index, bool press)
        {
            if (press)
            {
                if (_grip == Grip.Move)
                {
                    AddWait(index, EVRButtonId.k_EButton_SteamVR_Touchpad, 0.6f);
                }
                else
                {
                    if (IsTriggerPress(index))
                    {
                        HandHolder.GetHand(index).ChangeItem();
                    }
                }
                _pressedButtons[index, 2] = true;
            }
            else
            {
                PickAction(index, EVRButtonId.k_EButton_SteamVR_Touchpad);
                _pressedButtons[index, 2] = false;
            }
            return false;
        }
        protected virtual bool OnMenu(int index, bool press)
        {
            return false;
        }
        internal bool OnButtonDown(int index, EVRButtonId buttonId, Controller.TrackpadDirection direction)
        {
            return buttonId switch
            {
                EVRButtonId.k_EButton_SteamVR_Trigger => OnTrigger(index, press: true),
                EVRButtonId.k_EButton_Grip => OnGrip(index, press: true) || IsWait,
                EVRButtonId.k_EButton_SteamVR_Touchpad => OnTouchpad(index, press: true),
                EVRButtonId.k_EButton_ApplicationMenu => OnMenu(index, press: true),
                _ => false,
            };
        }

        internal void OnButtonUp(int index, EVRButtonId buttonId, Controller.TrackpadDirection direction)
        {
            switch (buttonId)
            {
                case EVRButtonId.k_EButton_SteamVR_Trigger:
                    OnTrigger(index, press: false);
                    break;
                case EVRButtonId.k_EButton_Grip:
                    OnGrip(index, press: false);
                    break;
                case EVRButtonId.k_EButton_SteamVR_Touchpad:
                    OnTouchpad(index, press: false);
                    break;
            }
        }
        private void HandleInput()
        {
            foreach (var wait in _waitList)
            {
                if (wait.finish < Time.time)
                {
                    PickAction(wait);
                    return;
                }
            }
        }
        private Timing GetTiming(float timestamp, float duration)
        {
            var timing = Time.time - timestamp;
            if (timing > duration) return Timing.Full;
            if (timing > duration * 0.5f) return Timing.Half;
            return Timing.Fraction;
        }
        private void RemoveWait(InputWait wait)
        {
            _waitList.Remove(wait);
        }
        protected void RemoveWait(int index, EVRButtonId button)
        {
            RemoveWait(_waitList
                .Where(w => w.index == index && w.button == button)
                .FirstOrDefault());
        }
        protected void RemoveWait(int index, Controller.TrackpadDirection direction)
        {
            RemoveWait(_waitList
                .Where(w => w.index == index && w.direction == direction)
                .FirstOrDefault());
        }
        private void PickAction(InputWait wait)
        {
            // Main entry.
            if (wait.button == EVRButtonId.k_EButton_System)
                PickDirectionAction(wait, GetTiming(wait.timestamp, wait.duration));
            else
            {
                if (_grip == Grip.Move)
                {
                    PickButtonActionGripMove(wait, GetTiming(wait.timestamp, wait.duration));
                }
                else
                {
                    PickButtonAction(wait, GetTiming(wait.timestamp, wait.duration));
                }
            }
            RemoveWait(wait);
        }
        protected virtual void PickDirectionAction(InputWait wait, Timing timing)
        {


        }
        protected virtual void PickButtonAction(InputWait wait, Timing timing)
        {

        }
        protected virtual void PickButtonActionGripMove(InputWait wait, Timing timing)
        {
            switch (wait.button)
            {
                case EVRButtonId.k_EButton_SteamVR_Touchpad:
                    if (timing == Timing.Full)
                    {
                        if (!IsTriggerPress(wait.index))
                        {
                            SmoothMover.Instance.MakeUpright();
                        }
                    }
                    break;

            }

        }
        protected void PickAction()
        {
            PickAction(
                _waitList
                .OrderByDescending(w => w.button)
                .First());
        }

        protected void PickAction(int index, EVRButtonId button)
        {
            if (_waitList.Count == 0) return;
            PickAction(
                _waitList
                .Where(w => w.button == button && w.index == index)
                .FirstOrDefault());
        }

        protected void PickAction(int index, Controller.TrackpadDirection direction)
        {
            if (_waitList.Count == 0) return;
            PickAction(
                _waitList
                .Where(w => w.direction == direction && w.index == index)
                .FirstOrDefault());
        }
        protected enum Timing
        {
            Fraction,
            Half,
            Full
        }
        internal virtual void OnGripMove(int index, bool active)
        {

        }

        protected void AddWait(int index, EVRButtonId button, float duration)
        {
            _waitList.Add(new InputWait(index, button, duration));
        }
        protected void AddWait(int index, Controller.TrackpadDirection direction, bool manipulateSpeed, float duration)
        {
            _waitList.Add(new InputWait(index, direction, manipulateSpeed, duration));
        }
        protected void AddWait(int index, Controller.TrackpadDirection direction, float duration)
        {
            _waitList.Add(new InputWait(index, direction, duration));
        }
    }
}
