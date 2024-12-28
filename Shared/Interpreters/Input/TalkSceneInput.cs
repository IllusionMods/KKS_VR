using System;
using System.Collections.Generic;
using System.Text;
using ADV;
using KK_VR.Handlers;
using KK_VR.Holders;
using KK_VR.Interpreters;
using Valve.VR;
using static VRGIN.Controls.Controller;
using static KK_VR.Interpreters.TalkSceneInterpreter;
using Manager;
using UnityEngine.UI;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using VRGIN.Core;
using KK_VR.Controls;

namespace KK_VR.Interpreters
{
    internal class TalkSceneInput : SceneInput
    {
        TalkSceneInterpreter _interpreter;
        private Button _lastSelectedCategory;
        private Button _lastSelectedButton;
        internal TalkSceneInput(TalkSceneInterpreter interpreter)
        {
            _interpreter = interpreter;
        }
        private bool IsADV => advScene.isActiveAndEnabled;
        private bool IsADVChoice => advScene.scenario.isChoice;
#if KK   
        enum State
        {
            Talk,
            None,
            Event
        }
#else
        enum State
        {
            None = -1,
            Talk,
            Listen,
            Topic,
            Event,
            R18
        }
#endif
        private TalkSceneHandler GetHandler(int index) => (TalkSceneHandler)HandHolder.GetHand(index).Handler;
        internal override void OnDirectionUp(int index, TrackpadDirection direction)
        {
            if (_interpreter.IsStart) return;
            base.OnDirectionUp(index, direction);
            HandHolder.GetHand(index).Grasp.OnScrollRelease();
        }
        protected override bool OnTrigger(int index, bool press)
        {
            if (_interpreter.IsStart) return false;
            var handler = GetHandler(index);
            var grasp = HandHolder.GetHand(index).Grasp;
            if (press)
            {
                _pressedButtons[index, 0] = true;
                if (IsInputState(InputState.Grasp))
                {
                    AddWait(index, EVRButtonId.k_EButton_SteamVR_Trigger, _settings.ShortPress);
                }
                else
                {
                    if (IsWait && !IsTouchpadPress(index))
                    {
                        PickAction();
                    }
                    else if (handler.IsBusy)
                    {
                        if (IsTouchpadPress(index) && grasp.OnTouchpadResetEverything(handler.GetChara))
                        {
                            // Touchpad pressed + trigger = premature total reset of tracked character.
                            RemoveWait(index, EVRButtonId.k_EButton_SteamVR_Touchpad);
                        }
                        else
                        {
                            // Send synthetic click.
                            handler.UpdateTracker();
                            handler.TriggerPress();
                        }
                    }
                }
            }
            else
            {
                _pressedButtons[index, 0] = false;

                grasp.OnTriggerRelease();
                handler.TriggerRelease();
                PickAction(index, EVRButtonId.k_EButton_SteamVR_Trigger);
            }
            return false;
        }

        protected override bool OnGrip(int index, bool press)
        {
            if (_interpreter.IsStart) return false;
            var handler = GetHandler(index);
            if (press)
            {
                _pressedButtons[index, 1] = true;

                if (HandHolder.GetHand(index).IsParent)
                {
                    HandHolder.GetHand(index).Grasp.OnGripRelease();
                }
                else if (handler.IsBusy) // if (!handler.InBlack)
                {
                    AddInputState(InputState.Grasp);
                    HandHolder.GetHand(index).Grasp.OnGripPress(handler.GetTrackPartName(), handler.GetChara);
                }
                else
                {
                    return false;
                }
                return true;

            }
            else
            {
                RemoveInputState(InputState.Move);
                RemoveInputState(InputState.Grasp);

                _pressedButtons[index, 1] = false;
                HandHolder.GetHand(index).Grasp.OnGripRelease();
                return false;
            }
        }

        protected override bool OnTouchpad(int index, bool press)
        {
            if (_interpreter.IsStart) return false;
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
                    else if (!HandHolder.GetHand(index).Grasp.OnTouchpadResetHeld())
                    {
                        AddWait(index, EVRButtonId.k_EButton_SteamVR_Touchpad, _settings.ShortPress);
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

        internal override bool OnDirectionDown(int index, TrackpadDirection direction)
        {
            if (_interpreter.IsStart) return false;
            var adv = IsADV;
            var handler = GetHandler(index);
            var grasp = HandHolder.GetHand(index).Grasp;
            switch (direction)
            {
                case TrackpadDirection.Up:
                case TrackpadDirection.Down:
                    if (grasp.IsBusy)
                    {
                        grasp.OnVerticalScroll(direction == TrackpadDirection.Up);
                    }
                    else if (handler.IsBusy)
                    {
                        handler.UpdateTracker();
                        if (handler.DoUndress(direction == TrackpadDirection.Down, out var chara))
                        {
                            _interpreter.SynchronizeClothes(chara);
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        if (!adv || IsADVChoice)
                        {
                            ScrollButtons(direction == TrackpadDirection.Down, adv);
                        }
                        else
                        {

                        }
                    }
                    break;
                case TrackpadDirection.Left:
                case TrackpadDirection.Right:
                    if (grasp.IsBusy)
                    {
                        grasp.OnBusyHorizontalScroll(direction == TrackpadDirection.Right);
                    }
                    else if (handler.IsBusy)
                    {
                        handler.UpdateTracker();
                        grasp.OnFreeHorizontalScroll(handler.GetTrackPartName(), handler.GetChara, direction == TrackpadDirection.Right);

                    }
                    else
                    {
                        if (IsTriggerPress(index))
                        {
                            HandHolder.GetHand(index).ChangeLayer(direction == TrackpadDirection.Right);
                        }
                        else
                        {
                            AddWait(index, direction, _settings.LongPress);
                        }
                    }
                    break;
            }
            return false;
        }


        protected override void PickButtonAction(InputWait wait, Timing timing)
        {
            var handler = GetHandler(wait.index);
            var grasp = HandHolder.GetHand(wait.index).Grasp;
            switch (wait.button)
            {
                case EVRButtonId.k_EButton_SteamVR_Touchpad:
                    if (timing == Timing.Full)
                    {
                        if (handler.IsBusy)
                        {
                            handler.UpdateTracker();
                            var chara = handler.GetChara;
                            var partName = handler.GetTrackPartName();
                            if (!grasp.OnTouchpadResetActive(partName, chara))
                            {
                                grasp.OnTouchpadSyncStart(partName, chara);
                            }
                        }
                        else
                        {
                            HandHolder.GetHand(wait.index).Grasp.OnTouchpadSyncStop();
                        }
                    }
                    break;
                case EVRButtonId.k_EButton_SteamVR_Trigger:
                    if (grasp.IsBusy)
                    {
                        grasp.OnTriggerPress(temporarily: timing == Timing.Full);
                    }
                    break;
            }
        }

        protected override void PickDirectionAction(InputWait wait, Timing timing)
        {
            var adv = IsADV;
            switch (wait.direction)
            {
                case TrackpadDirection.Up:
                case TrackpadDirection.Down:
                    break;
                case TrackpadDirection.Left:
                case TrackpadDirection.Right:
                    if (adv)
                    {
                        if (!IsADVChoice)
                        {
                            if (timing == Timing.Full)
                            {
                                SetAutoADV();
                            }
                            else
                            {
                                VR.Input.Mouse.VerticalScroll(-1);
                            }
                        }
                        else
                        {
                            EnterState(adv);
                        }
                    }
                    else
                    {
                        if (timing == Timing.Full && ClickLastButton())
                        {
                            return;
                        }

                        if (wait.direction == TrackpadDirection.Left)
                        {
                            EnterState(adv);
                        }
                        else
                        {
                            LeaveState(adv);
                        }
                    }
                    break;
            }
        }

        private void SetAutoADV()
        {
            advScene.Scenario.isAuto = !advScene.Scenario.isAuto;
        }
        private void ScrollButtons(bool increase, bool adv)
        {
            var state = GetState();
#if KKS
            if (state == State.Listen || state == State.Topic)
            {

            }
            else
#endif
            {
                var buttons = GetRelevantButtons(state, adv);
                var length = buttons.Length;
                if (length == 0)
                {
                    return;
                }
                var selectedBtn = GetSelectedButton(buttons, adv);
                var index = increase ? 1 : -1;
                if (selectedBtn != null)
                {
                    index += Array.IndexOf(buttons, selectedBtn);
                }
                else
                {
                    index = 0;
                }
                if (index == length)
                {
                    index = 0;
                }
                else if (index < 0)
                {
                    index = length - 1;
                }
                MarkButton(buttons[index], adv);
            }
        }

        private bool ClickLastButton()
        {
            if (_lastSelectedButton != null && _lastSelectedButton.enabled)
            {
                _lastSelectedButton.onClick.Invoke();
                return true;
            }
            return false;
        }
        private void LeaveState(bool adv)
        {
            var state = GetState();
#if KKS
            if (state == State.Listen)
            {
                ReplyTopic(false);
            }
            else if (state == State.Topic)
            {
                RandomTopic();
            }
            else
#endif
            {
                var buttons = GetRelevantButtons(state, adv);
                var button = GetSelectedButton(buttons, adv);
                if (adv)
                {
                    button.onClick.Invoke();
                }
                else if (state != State.None)
                {
                    buttons = GetRelevantButtons(State.None, adv);
                    buttons[(int)state].onClick.Invoke();
                }
            }
        }
        private Button GetSelectedButton(Button[] buttons, bool adv)
        {
            foreach (var button in buttons)
            {
                // Adv buttons are huge so they often intersect with mouse cursor and catch focus unintentionally.
                if (button.name.EndsWith("+", StringComparison.Ordinal)
                    || (adv && button.currentSelectionState == Selectable.SelectionState.Highlighted))
                {
                    button.name = button.name.TrimEnd('+');
                    button.DoStateTransition(Selectable.SelectionState.Normal, false);
                    return button;
                }
            }
            return null;
        }
        private Button[] GetRelevantButtons(State state, bool adv)
        {
            return adv ? GetADVChoices() : state == State.None ? GetMainButtons() : GetCurrentContents(state);
        }
        private void MarkButton(Button button, bool adv)
        {
            // We modify button name to not lose track in case the player
            // manually highlights the button with the controller.
            button.DoStateTransition(adv ? Selectable.SelectionState.Highlighted : Selectable.SelectionState.Pressed, false);
            button.name += "+";
        }
        private Button[] GetMainButtons()
        {
#if KK

            return [talkScene.buttonTalk, talkScene.buttonListen, talkScene.buttonEvent];
#else
            var length = talkScene.buttonInfos.Length;
            var buttons = new Button[length];
            for (int i = 0; i < length; i++)
            {
                buttons[i] = talkScene.buttonInfos[i].button;
            }
            return buttons;
#endif
            //return new Button[] { talkScene.buttonInfos. talkScene.buttonTalk, talkScene.buttonListen, talkScene.buttonEvent };
        }

        private Button[] GetADVChoices()
        {
#if KK

            return Game.Instance.actScene.advScene.scenario.choices.GetComponentsInChildren<Button>()
#else
            return ActionScene.instance.advScene.scenario.choices.GetComponentsInChildren<Button>()
#endif
                .Where(b => b.isActiveAndEnabled)
                .ToArray();
        }
#if KK

        // KKS has it by default
        public void ShuffleTemper(SaveData.Heroine heroine)
        {
            var temper = heroine.m_TalkTemper;
            var bias = 1f - Mathf.Clamp01(0.3f - heroine.favor * 0.001f - heroine.intimacy * 0.001f - (heroine.isGirlfriend ? 0.1f : 0f));
            var part = bias * 0.5f;
            for (int i = 0; i < temper.Length; i++)
            {
                temper[i] = GetBiasedByte(bias, part);
            }
        }
        private byte GetBiasedByte(float bias, float part)
        {
            var value = Random.value;
            if (value > bias) return 2;
            if (value < part) return 1;
            return 0;
        }
#else
        private int RandomTopic()
        {
            return Random.Range(0, talkScene.topics.Count);
        }
        private void ReplyTopic(bool correct)
        {
            if (correct)
            {
                var properReply = talkScene.listenTopic.Topic;
                for (var i = 0; i < talkScene.topics.Count; i++)
                {
                    if (talkScene.topics[i].No == properReply)
                    {
                        talkScene.selectTopic = i;
                        return;
                    }
                }
            }
            talkScene.selectTopic = talkScene.topics.Count - 1;
        }
#endif
        private void EnterState(bool adv)
        {
            var state = GetState();
#if KKS
            if (state == State.Listen)
            {
                ReplyTopic(true);
            }
            else if (state == State.Topic)
            {
                RandomTopic();
            }
            else
#endif
            {
                var buttons = GetRelevantButtons(state, adv);
                var button = GetSelectedButton(buttons, adv);

                if (button == null)
                {
                    //VRPlugin.Logger.LogDebug($"EnterState:State - {state}:NoButton");
                    if (!adv)
                    {
                        if (state == State.None)
                        {
                            ClickLastCategory();
                            return;
                        }
                        else if (_lastSelectedButton != null)
                        {
                            var lastSelectedButtonIndex = Array.IndexOf(buttons, _lastSelectedButton);
                            if (lastSelectedButtonIndex > -1)
                            {
                                MarkButton(buttons[lastSelectedButtonIndex], adv);
                                return;
                            }
                        }
                    }
                    MarkButton(buttons[Random.Range(0, buttons.Length)], adv);
                    return;
                }
                //VRPlugin.Logger.LogDebug($"EnterState:State - {state}:Button - {button.name}");

                if (!adv)
                {
                    if (state == State.None)
                        _lastSelectedCategory = button;
                    else
                        _lastSelectedButton = button;

                    //if (state == State.Talk && Random.value < 0.5f) ShuffleTemper(talkScene.targetHeroine);
                }
                button.onClick.Invoke();
            }
        }
        private void ClickLastCategory()
        {
            if (_lastSelectedCategory == null)
            {
#if KK
                _lastSelectedCategory = talkScene.buttonTalk;
#else
                _lastSelectedCategory = talkScene.buttonInfos[0].button;
#endif
            }
            _lastSelectedCategory.onClick.Invoke();
        }
        private Button[] GetCurrentContents(State state)
        {
#if KK
            return state == State.Talk ?
                talkScene.buttonTalkContents
                :
                talkScene.buttonEventContents
                .Where(b => b.isActiveAndEnabled)
                .ToArray();
#else
            return state switch
            {
                State.Talk => talkScene.buttonTalkContents,
                State.Event => talkScene.buttonEventContents
                .Where(b => b.isActiveAndEnabled)
                .ToArray(),
                State.R18 => talkScene.buttonR18Contents
                .Where(b => b.isActiveAndEnabled)
                .ToArray(),
                _ => null
            };
#endif
        }
        private State GetState()
        {
#if KK
            if (talkScene != null && talkScene.objTalkContentsRoot.activeSelf)
            {
                return State.Talk;
            }
            else if (talkScene != null && talkScene.objEventContentsRoot.activeSelf)
            {
                return State.Event;
            }
            else
            {
                return State.None;
            }
#else
            return (State)talkScene.selectButton;
#endif
        }
    }
}
