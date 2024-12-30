using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KK_VR.Camera;
using KK_VR.Holders;
using KK_VR.Settings;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;
using static VRGIN.Controls.Controller;

namespace KK_VR.Interpreters
{
    /// <summary>
    /// Manages input of the corresponding scene, has access to the interpreter but not vice versa.
    /// </summary>
    internal class SceneInput
    {
        protected readonly KoikatuSettings _settings = VR.Context.Settings as KoikatuSettings;
        protected readonly List<InputWait> _waitList = [];
        protected InputState _inputState;
        protected bool IsWait => _waitList.Count != 0;
        
        /// <summary>
        /// 0 - Trigger. 
        /// 1 - Grip. 
        /// 2 - Touchpad (Joystick click).
        /// </summary>
        protected readonly bool[,] _pressedButtons = new bool[2, 3];
        internal virtual bool IsTriggerPress(int index) => _pressedButtons[index, 0];
        internal virtual bool IsGripPress(int index) => _pressedButtons[index, 1];
        internal virtual bool IsTouchpadPress(int index) => _pressedButtons[index, 2];
        internal virtual bool IsGripMove() => _pressedButtons[0, 1] || _pressedButtons[1, 1];

        /// <summary>
        /// Something doesn't want to share input.
        /// </summary>
        internal bool IsBusy => _busy;// _inputState != InputState.None;
        private bool _busy;
        protected enum InputState
        {
            Caress = 1,
            Grasp = 2,
            Move = 4,
            Busy = 8,
        }
        protected enum Timing
        {
            Fraction,
            Half,
            Full
        }
        internal void SetBusy(bool state)
        {
            if (state)
            {
                AddInputState(InputState.Busy);
            }
            else
            {
                RemoveInputState(InputState.Busy);
            }
        }
        protected void AddInputState(InputState state)
        {
            _inputState |= state;
            var wasBusy = _busy;

            _busy = IsInputStateNotDefault();
            if (!wasBusy && _busy)
            {
                HandHolder.OnBecomingBusy();
            }
        }
        protected void RemoveInputState(InputState state)
        {
            _inputState &= ~state;
            _busy = IsInputStateNotDefault();
        }
        protected bool IsInputState(InputState state) => (_inputState & state) != 0;
        private bool IsInputStateNotDefault()
        {
            return IsInputState(InputState.Caress) || IsInputState(InputState.Grasp) || IsInputState(InputState.Move) || IsInputState(InputState.Busy);
        }
        internal virtual void OnDisable()
        {

        }
        private Timing GetTiming(float timestamp, float duration)
        {
            var timing = Time.time - timestamp;
            if (timing > duration) return Timing.Full;
            if (timing > duration * 0.5f) return Timing.Half;
            return Timing.Fraction;
        }

        internal virtual void HandleInput()
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

        protected void PickAction()
        {
            PickAction(
                _waitList
                .OrderByDescending(w => w.button)
                .FirstOrDefault());
        }

        protected void PickAction(Timing timing)
        {
            PickAction(
                _waitList
                .OrderByDescending(w => w.button)
                .FirstOrDefault(), timing);
        }

        protected void PickAction(int index, EVRButtonId button)
        {
            if (_waitList.Count == 0) return;
            PickAction(
                _waitList
                .Where(w => w.button == button && w.index == index)
                .FirstOrDefault());
        }

        protected void PickAction(int index, TrackpadDirection direction)
        {
            if (_waitList.Count == 0) return;
            PickAction(
                _waitList
                .Where(w => w.direction == direction && w.index == index)
                .FirstOrDefault());
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

        private void PickAction(InputWait wait, Timing timing)
        {
            // Main entry.
            if (wait.button == EVRButtonId.k_EButton_System)
                PickDirectionAction(wait, timing);
            else
            {
                if (IsInputState(InputState.Move))
                {
                    PickButtonActionGripMove(wait, timing);
                }
                else
                {
                    PickButtonAction(wait, timing);
                }
            }
            RemoveWait(wait);
        }
        private void PickAction(InputWait wait)
        {
            PickAction(wait, GetTiming(wait.timestamp, wait.duration));
        }

        internal bool OnButtonDown(int index, EVRButtonId buttonId, TrackpadDirection direction)
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

        internal void OnButtonUp(int index, EVRButtonId buttonId, TrackpadDirection direction)
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

        internal virtual bool OnDirectionDown(int index, TrackpadDirection direction)
        {
            switch (direction)
            {
                case TrackpadDirection.Left:
                case TrackpadDirection.Right:
                    if (IsTriggerPress(index))
                    {
                        HandHolder.GetHand(index).ChangeLayer(direction == TrackpadDirection.Right);
                    }
                    break;
            }

            return false;
        }

        internal virtual void OnDirectionUp(int index, TrackpadDirection direction)
        {
            if (IsWait)
            {
                PickAction(index, direction);
            }
        }

        protected virtual bool OnTrigger(int index, bool press)
        {
            if (press)
            {
                _pressedButtons[index, 0] = true;
                if (IsWait)
                {
                    PickAction(Timing.Full);
                }
            }
            else
            {
                _pressedButtons[index, 0] = false;
                PickAction(index, EVRButtonId.k_EButton_SteamVR_Trigger);
            }
            return false;
        }

        /// <summary>
        /// Return false to start GripMove.
        /// </summary>
        protected virtual bool OnGrip(int index, bool press)
        {
            if (press)
            {
                _pressedButtons[index, 1] = true;
            }
            else
            {
                _pressedButtons[index, 1] = false;
            }
            return false;
        }

        protected virtual bool OnTouchpad(int index, bool press)
        {
            if (press)
            {
                _pressedButtons[index, 2] = true;

                if (IsInputState(InputState.Move))
                {
                    if (!IsTriggerPress(index))
                    {
                        AddWait(index, EVRButtonId.k_EButton_SteamVR_Touchpad, _settings.LongPress - 0.1f);
                    }
                }
                else
                {
                    if (IsTriggerPress(index))
                    {
                        HandHolder.GetHand(index).ChangeItem();
                    }
                }
            }
            else
            {
                _pressedButtons[index, 2] = false;

                PickAction(index, EVRButtonId.k_EButton_SteamVR_Touchpad);
            }
            return false;
        }

        protected virtual bool OnMenu(int index, bool press)
        {
            return false;
        }

        internal virtual void OnGripMove(int index, bool active)
        {
            if (active)
            {
                AddInputState(InputState.Move);
            }
            else
            {
                RemoveInputState(InputState.Move);
            }
        }

        protected void AddWait(int index, EVRButtonId button, float duration)
        {
            _waitList.Add(new InputWait(index, button, duration));
        }

        protected void AddWait(int index, TrackpadDirection direction, bool manipulateSpeed, float duration)
        {
            _waitList.Add(new InputWait(index, direction, manipulateSpeed, duration));
        }

        protected void AddWait(int index, TrackpadDirection direction, float duration)
        {
            _waitList.Add(new InputWait(index, direction, duration));
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
    }
}
