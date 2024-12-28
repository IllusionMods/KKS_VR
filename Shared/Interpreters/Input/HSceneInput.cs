using KK_VR.Features;
using KK_VR.Handlers;
using KK_VR.Holders;
using Valve.VR;
using static HandCtrl;
using static HFlag;
using static VRGIN.Controls.Controller;
using static KK_VR.Interpreters.HSceneInterpreter;
using VRGIN.Core;
using Random = UnityEngine.Random;
using UnityEngine;

namespace KK_VR.Interpreters
{
    internal class HSceneInput : SceneInput
    {
        private int _frameWait;
        private bool _manipulateSpeed;
        private TrackpadDirection _lastDirection;
        private HSceneInterpreter _interpreter;
        private PoV _pov;
        private MouthGuide _mouth;
        private readonly AibuColliderKind[] _lastAibuKind = new AibuColliderKind[2];
        internal HSceneInput(HSceneInterpreter interpreter)
        {
            _interpreter = interpreter;
            _pov = PoV.Instance;
            _mouth = MouthGuide.Instance;
        }
        private HSceneHandler GetHandler(int index) => (HSceneHandler)HandHolder.GetHand(index).Handler;

        internal override void HandleInput()
        {
            base.HandleInput();
            if (_manipulateSpeed) HandleSpeed();
        }

        private void HandleSpeed()
        {
            if (IntegrationSensibleH.active)
            {
                IntegrationSensibleH.StopAuto();
            }
            if (_lastDirection == TrackpadDirection.Up)
            {
                SpeedUp();
            }
            else
            {
                SlowDown();
            }
        }

        private void SpeedUp()
        {
            if (mode == EMode.aibu)
            {
                hFlag.SpeedUpClickAibu(Time.deltaTime, hFlag.speedMaxAibuBody, true);
            }
            else
            {
                if (hFlag.speedCalc < 1f)
                {
                    hFlag.SpeedUpClick(Time.deltaTime * 0.2f, 1f);
                    //hFlag.speedCalc += Time.deltaTime * 0.2f;
                    if (hFlag.speedCalc > 1f)
                    {
                        hFlag.speedCalc = 1f;
                    }
                }
                else
                {
                    AttemptFinish();
                }
            }
        }

        private void SlowDown()
        {
            if (mode == EMode.aibu)
            {
                hFlag.SpeedUpClickAibu(-Time.deltaTime, hFlag.speedMaxAibuBody, true);
            }
            else
            {
                if (hFlag.speedCalc > 0f)
                {
                    hFlag.SpeedUpClick(Time.deltaTime * -0.2f, 1f);

                    //hFlag.speedCalc -= Time.deltaTime * 0.2f;
                    if (hFlag.speedCalc < 0f)
                    {
                        hFlag.speedCalc = 0f;
                    }
                }
                else
                {
                    AttemptStop();
                }
            }
        }

        private void AttemptFinish()
        {
            // Grab SensH ceiling.
            if (hFlag.gaugeMale == 100f)
            {
                // There will be only one finish appropriate for the current mode/setting.
                ClickRandomButton();
                _manipulateSpeed = false;
            }
        }

        private void AttemptStop()
        {
            // Happens only when we recently pressed the button.
            _manipulateSpeed = false;
            Pull();
            if (IntegrationSensibleH.active)
            {
                IntegrationSensibleH.StopAuto();
            }
        }

        internal override void OnGripMove(int index, bool press)
        {
            if (press)
            {
                _pov.OnGripMove(press);
                _mouth.OnGripMove(press);
                // _hands[index].Grasp.OnGripRelease();
                AddInputState(InputState.Move);
                if (_mouth.IsActive)
                {
                    var hand = HandHolder.GetHand(index);
                    hand.Tool.LazyGripMove(KoikatuInterpreter.ScaleWithFps(15));
                    hand.Tool.AttachGripMove(_mouth.LookAt);
                }
            }
            else
            {
                // Check if another controller still gripMoves.
                if (!IsGripMove())
                {
                    _pov.OnGripMove(press);
                    _mouth.OnGripMove(press);
                    RemoveInputState(InputState.Move);
                    if (_mouth.IsActive)
                    {
                        _mouth.UpdateOrientationOffsets();
                    }
                }
            }
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
                            handler.UpdateTracker(tryToAvoid: PoV.Active ? PoV.Target : null);

                            // We attempt to reset active body part (held parts reset on press);
                            if (!grasp.OnTouchpadResetActive(handler.GetTrackPartName(), handler.GetChara))
                            {
                                // We update tracker to remove bias from PoV target we set beforehand.
                                handler.UpdateTracker();

                                // We attempt to impersonate, false if already impersonating/or setting.
                                var chara = handler.GetChara;
                                if (PoV.Active && PoV.Target == chara && grasp.OnTouchpadSyncStart(handler.GetTrackPartName(), chara))
                                {
                                    _pov.OnLimbSync(start: true);
                                }
                            }

                        }
                        else
                        {
                            if (HandHolder.GetHand(wait.index).Grasp.OnTouchpadSyncStop())
                            {
                                _pov.OnLimbSync(start: false);
                            }
                            else
                            {
                                _pov.TryEnable();
                            }
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
        protected override bool OnTrigger(int index, bool press)
        {
            // With present 'Wait' for 'Direction' (no buttons pressed) trigger simply finishes 'Wait' and prompts the action,
            // but if button is present, it in addition also offers alternative mode. Currently TouchpadPress only.
            var handler = GetHandler(index);
            var grasp = HandHolder.GetHand(index).Grasp;
            if (press)
            {
                _pressedButtons[index, 0] = true;
                if (IsInputState(InputState.Caress))
                {
                    if (IntegrationSensibleH.active)
                    {
                        IntegrationSensibleH.JudgeProc(_lastAibuKind[index]);
                    }
                    else
                    {
                        HSceneInterpreter.handCtrl.JudgeProc();
                    }
                }
                else if (IsInputState(InputState.Grasp))
                {
                    AddWait(index, EVRButtonId.k_EButton_SteamVR_Trigger, _settings.ShortPress);
                }
                else 
                {
                    if (_mouth.IsActive && !IsInputState(InputState.Move))
                    {
                        _mouth.OnTriggerPress();
                    }
                    else if (handler.IsBusy)
                    {
                        //Merge this with usual PickAction.
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
                    else if (IsWait) // && !IsTouchpadPress(index))
                    {
                        PickAction(Timing.Full);
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

        private TrackpadDirection SwapSides(TrackpadDirection direction)
        {
            return direction switch
            {
                TrackpadDirection.Left => TrackpadDirection.Right,
                TrackpadDirection.Right => TrackpadDirection.Left,
                _ => direction
            };

        }
        internal override bool OnDirectionDown(int index, TrackpadDirection direction)
        {
            var wait = 0f;
            var speed = false;
            var handler = GetHandler(index);
            var grasp = HandHolder.GetHand(index).Grasp;

            if (index == 0)
            {
                // We respect lefties now.
                direction = SwapSides(direction);
            }
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
                        if (handler.DoUndress(direction == TrackpadDirection.Down))
                        {

                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        if (HSceneInterpreter.IsHPointMove)
                        {
                            _interpreter.MoveCategory(direction == TrackpadDirection.Down);
                        }
                        else if (HSceneInterpreter.IsActionLoop)
                        {
                            if (HSceneInterpreter.mode == EMode.aibu)
                            {
                                if (HSceneInterpreter.IsHandActive)
                                {
                                    // Reaction if too long, speed meanwhile.
                                    wait = 3f;
                                    speed = true;
                                }
                                else
                                {
                                    // Reaction/Lean to kiss.
                                    wait = _settings.LongPress;
                                }
                            }
                            else
                            {
                                speed = true;
                            }
                        }
                        else
                        {
                            // ?? is this.
                            wait = 0.5f;
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
                        if (grasp.OnFreeHorizontalScroll(handler.GetTrackPartName(), handler.GetChara, direction == TrackpadDirection.Right))
                        {

                        }
                        else if (handler.IsAibuItemPresent(out var touch))
                        {
                            if (IsHandActive)
                            {
                                _interpreter.ToggleAibuHandVisibility(touch);
                            }
                            else
                            {
                                SetSelectKindTouch(touch);
                                VR.Input.Mouse.VerticalScroll(direction == TrackpadDirection.Right ? -1 : 1);
                            }
                        }
                    }
                    else
                    {
                        if (IsTriggerPress(index))
                        {
                            HandHolder.GetHand(index).ChangeLayer(direction == TrackpadDirection.Right);
                        }
                        else if (IsHPointMove)
                        {
                            if (direction == TrackpadDirection.Right)
                                wait = _settings.LongPress;
                            else
                                _interpreter.GetHPointMove.Return();
                        }
                        else if (IsActionLoop)
                        {
                            if (mode == EMode.aibu)
                            {
                                _interpreter.ScrollAibuAnim(direction == TrackpadDirection.Right);
                            }
                            else if (IntegrationSensibleH.active)
                            {
                                IntegrationSensibleH.ChangeLoop(_interpreter.GetCurrentLoop(direction == TrackpadDirection.Right));
                            }
                        }
                        else
                            wait = _settings.LongPress;
                    }
                    break;
            }
            _manipulateSpeed = speed;
            _lastDirection = direction;
            if (wait != 0f)
            {
                AddWait(index, direction, speed, wait);
                return true;
            }
            else
                return false;
        }

        internal override void OnDirectionUp(int index, TrackpadDirection direction)
        {
            if (IsWait)
            {
                PickAction(index, direction);
            }
            else if (_manipulateSpeed)
            {
                _manipulateSpeed = false;
                if (IntegrationSensibleH.active)
                {
                    IntegrationSensibleH.OnUserInput();
                }
            }
            HandHolder.GetHand(index).Grasp.OnScrollRelease();
        }

        protected override bool OnTouchpad(int index, bool press)
        {
            if (press)
            {
                _pressedButtons[index, 2] = true;

                if (IsInputState(InputState.Move))
                {
                    if (!_pov.OnTouchpad(true))
                    {
                        if (!IsTriggerPress(index))
                        {
                            // Reset to upright. 
                            AddWait(index, EVRButtonId.k_EButton_SteamVR_Touchpad, _settings.LongPress - 0.1f); // 0.7f
                        }
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
        protected override bool OnGrip(int index, bool press)
        {
            var handler = GetHandler(index);
            //VRPlugin.Logger.LogDebug($"OnGrip:{handler.IsBusy}");
            if (press)
            {
                _pressedButtons[index, 1] = true;
                if (HandHolder.GetHand(index).IsParent)
                {
                    HandHolder.GetHand(index).Grasp.OnGripRelease();
                }
                else if (handler.IsBusy)
                {
                    handler.UpdateTracker();
                    if (handler.IsAibuItemPresent(out var touch))
                    {
                        //AddInputState(InputState.Move);
                        AddInputState(InputState.Caress);
                        handler.StartMovingAibuItem(touch);
                        _lastAibuKind[index] = touch;
                        if (IntegrationSensibleH.active && HSceneInterpreter.handCtrl.GetUseAreaItemActive() != -1)
                        {
                            IntegrationSensibleH.ReleaseItem(touch);
                        }
                        HSceneInterpreter.EnableNip(touch);
                        if (_settings.HideHandOnUserInput != Settings.KoikatuSettings.HandType.None)
                        {
                            if (_settings.HideHandOnUserInput > Settings.KoikatuSettings.HandType.ControllerItem)
                            {
                                HSceneInterpreter.ShowAibuHand(touch, false);
                            }
                            if (_settings.HideHandOnUserInput != Settings.KoikatuSettings.HandType.CaressItem)
                            {
                                HandHolder.GetHand(index).SetItemRenderer(false);
                            }
                        }
                    }
                    else
                    {
                        // Shouldn't appear anymore, blacks are cleared properly.
                        if (!handler.InBlack)
                        {
                            // We grasped something, don't start GripMove.
                            //AddInputState(InputState.Move);
                            AddInputState(InputState.Grasp);                            
                            HandHolder.GetHand(index).Grasp.OnGripPress(handler.GetTrackPartName(), handler.GetChara);
                        }
                    }
                    return true;
                }
            }
            else
            {
                RemoveInputState(InputState.Move);
                RemoveInputState(InputState.Caress);
                if (IsInputState(InputState.Grasp))
                {
                    RemoveInputState(InputState.Grasp);
                    _pov.OnGraspEnd();
                }
                _pressedButtons[index, 1] = false;

                handler.StopMovingAibuItem();
                HandHolder.GetHand(index).Grasp.OnGripRelease();
            }
            return false;
        }
        private string GetButtonName(bool anal, bool noVoice)
        {
            string name;
            switch (mode)
            {
                case EMode.sonyu:
                    name = "Insert";
                    if (anal)
                    {
                        name += "Anal";
                    }
                    if (noVoice)
                    {
                        name += "_novoice";
                    }
                    break;
                default:
                    name = "";
                    break;
            }
            return name;
        }
        /// <summary>
        /// Empty string to click whatever is there(except houshi slow/fast), otherwise checks start of the string and clicks corresponding button.
        /// </summary>
        private void ClickRandomButton()
        {
            if (IntegrationSensibleH.active)
            {
                IntegrationSensibleH.ClickButton("");
            }
        }
        private bool IsFrameWait()
        {
            // Clutch to skip frames while changeing speed.
            if (_frameWait != 0)
            {
                //VRPlugin.Logger.LogDebug($"FrameWait");
                if (!CrossFader.InTransition)
                {
                    _frameWait--;
                }
                _manipulateSpeed = true;
                return true;
            }
            return false;
        }
        private void WaitFrame(int count)
        {
            _frameWait = count;
            _manipulateSpeed = true;
        }
        private void Pull()
        {
            if (!IsFrameWait() && PullHelper())
            {
                if (IntegrationSensibleH.active)
                {
                    IntegrationSensibleH.ClickButton("Pull");
                }
                else
                {
                    hFlag.click = ClickKind.pull;
                }
            }
        }
        private bool PullHelper()
        {
            var nowAnim = hFlag.nowAnimStateName;

            if (mode == EMode.sonyu)
            {
                if (IsIdleOutside(nowAnim) || IsAfterClimaxOutside(nowAnim))
                {
                    // When outside pull back to get condom on. Extra plugin disables auto condom on denial.
                    sprite.CondomClick();
                }
                else if (IsFinishLoop)
                {
                    hFlag.finish = FinishKind.outside;
                }
                else if (IsActionLoop)
                {
                    hFlag.click = ClickKind.modeChange;
                    WaitFrame(3);
                }
                else
                {
                    return true;
                }
            }
            else if (mode == EMode.houshi)
            {
                if (IsClimaxHoushiInside(nowAnim))
                {
                    hFlag.click = ClickKind.vomit;
                }
                else if (IsActionLoop)
                {
                    lstProc[(int)hFlag.mode].MotionChange(0);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
        private void Insert(bool noVoice, bool anal)
        {
            if (InsertHelper())
            {
                if (IntegrationSensibleH.active)
                {
                    IntegrationSensibleH.ClickButton(GetButtonName(anal, hFlag.isDenialvoiceWait || noVoice));
                }
                else if (mode == EMode.sonyu)
                {
                    // Houshi is done mostly by helper.
                    hFlag.click = anal ? noVoice ? ClickKind.insert_anal : ClickKind.insert_anal_voice : noVoice ? ClickKind.insert : ClickKind.insert_voice;
                }
            }
        }
        private bool InsertHelper()
        {
            var nowAnim = hFlag.nowAnimStateName;
            if (mode == EMode.sonyu)
            {
                if (IsInsertIdle(nowAnim) || IsAfterClimaxInside(nowAnim))
                {
                    // Sonyu start auto.
                    hFlag.click = ClickKind.modeChange;
                    if (IntegrationSensibleH.active)
                    {
                        IntegrationSensibleH.OnUserInput();
                    }
                }
                else// if (!hFlag.voiceWait)
                {
                    return true;
                }
            }
            else if (mode == EMode.houshi)
            {
                if (IsClimaxHoushiInside(nowAnim))
                {
                    hFlag.click = ClickKind.drink;
                }
                else if (IsIdleOutside(nowAnim))
                {
                    // Start houshi after pose change/long pause after finish.
                    hFlag.click = ClickKind.speedup;
                    if (IntegrationSensibleH.active)
                    {
                        IntegrationSensibleH.OnUserInput();
                    }
                }
                else if (IsAfterClimaxHoushiInside(nowAnim) || IsAfterClimaxOutside(nowAnim))
                {
                    // Restart houshi.
                    ClickRandomButton();
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
            return false;
        }
        protected override void PickDirectionAction(InputWait wait, Timing timing)
        {
            _manipulateSpeed = false;
            switch (wait.direction)
            {
                case TrackpadDirection.Up:
                    if (mode == EMode.aibu)
                    {
                        if (IsActionLoop)
                        {
                            switch (timing)
                            {
                                case Timing.Fraction:
                                    if (!IsHandActive && IsHandAttached)
                                    {
                                        PlayReaction();
                                    }
                                    break;
                                case Timing.Half:
                                    break;
                                case Timing.Full:
                                    break;
                            }
                        }
                        else // Non-action Aibu mode.
                        {
                            switch (timing)
                            {
                                case Timing.Fraction:
                                    if (Random.value < 0.5f)
                                    {
                                        PlayShort(lstFemale[0]);
                                    }
                                    break;
                                case Timing.Half:
                                    // Put in denial + voice.
                                    break;
                                case Timing.Full:
                                    //SetHand();
                                    break;
                            }
                        }
                    }
                    else // Non-Aibu mode.
                    {
                        switch (timing)
                        {
                            case Timing.Fraction:
                            case Timing.Half:
                                PlayReaction();
                                break;
                            case Timing.Full:
                                Insert(noVoice: IsTriggerPress(wait.index), anal: IsTouchpadPress(wait.index));
                                break;
                        }
                    }
                    break;
                case TrackpadDirection.Down:
                    if (mode == EMode.aibu)
                    {
                        if (IsActionLoop)
                        {
                            switch (timing)
                            {
                                case Timing.Fraction:
                                case Timing.Half:
                                    break;
                                case Timing.Full:

                                     LeanToKiss();

                                    break;
                            }
                        }
                        else // Non-action Aibu mode.
                        {
                            switch (timing)
                            {
                                case Timing.Fraction:
                                case Timing.Half:
                                    break;
                                case Timing.Full:
                                    LeanToKiss();
                                    break;
                            }
                        }
                    }
                    else // Non-Aibu mode.
                    {
                        switch (timing)
                        {
                            case Timing.Fraction:
                            case Timing.Half:
                                PlayReaction();
                                break;
                            case Timing.Full:
                                Pull();
                                break;
                        }
                    }
                    break;
                case TrackpadDirection.Right:
                    switch (timing)
                    {
                        case Timing.Fraction:
                        case Timing.Half:
                            PlayShort(lstFemale[0]);
                            break;
                        case Timing.Full:
                            if (!IsHPointMove)
                            {
                                hFlag.click = ClickKind.pointmove;
                            }
                            else
                            {
                                _pov.StartCoroutine(_interpreter.RandomHPointMove(startScene: false));
                            }
                            break;
                    }
                    break;
                case TrackpadDirection.Left:
                    switch (timing)
                    {
                        case Timing.Fraction:
                        case Timing.Half:
                            PlayShort(lstFemale[0]);
                            break;
                        case Timing.Full:
                            if (IntegrationSensibleH.active)
                            {
                                if (IsTriggerPress(wait.index))
                                {
                                    // Any animation goes.
                                    IntegrationSensibleH.ChangeAnimation(-1);
                                }
                                else
                                {
                                    // SameMode.
                                    IntegrationSensibleH.ChangeAnimation(3);
                                }
                            }
                            break;
                    }
                    break;
            }
        }

    }
}
