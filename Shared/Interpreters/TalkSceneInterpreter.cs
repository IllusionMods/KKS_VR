using System;
using System.Collections.Generic;
using System.Linq;
using VRGIN.Core;
using UnityEngine;
using Manager;
using UnityEngine.UI;
using ADV;
using Random = UnityEngine.Random;
using RootMotion.FinalIK;
using KK_VR.Fixes;
using Valve.VR;
using static HandCtrl;
using static KK_VR.Interpreters.TalkSceneExtras;
using static VRGIN.Controls.Controller;
using KK_VR.Handlers;
using KK_VR.Features;
using KK_VR.Camera;
using KK_VR.Trackers;
using KK_VR.Holders;

namespace KK_VR.Interpreters
{
    class TalkSceneInterpreter : SceneInterpreter
    {
        public static float talkDistance = 0.55f;
        public static float height;
        public static TalkScene talkScene;
        public static ADVScene advScene;
        internal static bool afterH;
        private static HitReaction _hitReaction;
        private readonly static List<int> lstIKEffectLateUpdate = [];
        private static bool _lateHitReaction;

        private Button _lastSelectedCategory;
        private Button _lastSelectedButton;
        private bool _talkSceneStart;
        private bool _start;
        private bool? _sittingPose;

        private Transform _eyes;
        private ActionGame.Chara.Player GetPlayer()
        {
#if KK
            return Game.instance.actScene.Player;
#else
            return ActionScene.instance.Player;
#endif
        }

        private Vector3 GetEyesPosition()
        {
            if (_eyes == null)
            {
                _eyes = GetPlayer().chaCtrl.objHeadBone.transform.Find("cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_tz");
            }
            return _eyes.TransformPoint(0f, _settings.PositionOffsetY, _settings.PositionOffsetZ);
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
        public TalkSceneInterpreter(MonoBehaviour behaviour)
        {
#if KK
            if (behaviour != null)
            {
               //VRPlugin.Logger.LogDebug($"TalkScene:Start:Talk");
                talkScene = (TalkScene)behaviour;
                _talkSceneStart = true;
            }
            advScene = Game.Instance.actScene.advScene;
#else
            advScene = ActionScene.instance.AdvScene;
            talkScene = TalkScene.instance;
            if (talkScene._isPaly)
            {
                _talkSceneStart = true;
            }
#endif
            _start = true;
            SetHeight();
            HandHolder.UpdateHandlers<TalkSceneHandler>();
        }
        private void SetHeight()
        {
#if KK

            if (height == 0f && Game.Instance.actScene != null && Game.Instance.actScene.Player.chaCtrl != null)
            {
                var player = Game.Instance.actScene.Player.chaCtrl;
#else
                if (height == 0f && ActionScene.instance != null && ActionScene.instance.Player.chaCtrl != null)
            {
                var player = ActionScene.instance.Player.chaCtrl;
#endif
                height = player.objHeadBone.transform
                    .Find("cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_tz")
                    .position.y - player.transform.position.y;
            }
        }

        internal override void OnDisable()
        {
            HandHolder.DestroyHandlers();
#if KK
            TalkSceneExtras.ReturnDirLight();
#endif
            //TalkSceneExtras.ReturnDirLight();
            //HideMaleHead.ForceShowHead = false;
        }
        internal override void OnUpdate()
        {
#if KK
            if (talkScene == null && !advScene.isActiveAndEnabled)
#else
            if (!talkScene._isPaly && !advScene.isActiveAndEnabled)
#endif
            {
                KoikatuInterpreter.EndScene(KoikatuInterpreter.SceneType.TalkScene);
            }
            




            if (_start)
            {
                // We wait for a moment and grab/place everything we need during scene load.
#if KK      
                if (_talkSceneStart)
                {
                    if (_sittingPose == null && talkScene.targetHeroine != null)
                    {
                        _sittingPose = (talkScene.targetHeroine.chaCtrl.objHead.transform.position - talkScene.targetHeroine.transform.position).y < 1f;
                    }
                    if (talkScene.cameraMap.enabled)
                    {
                        AdjustTalkScene();
                    }
                }
                else
                {
                    if (advScene.Scenario.currentChara != null
                    && Manager.Scene.Instance.sceneFade._Color.a < 1f)
                    {
                        AdjustAdvScene();
                    }
                }

#else
                if (!_talkSceneStart && talkScene._isPaly)
                {
                    _talkSceneStart = true;
                }
                else if (_talkSceneStart && _sittingPose == null && talkScene.targetHeroine != null)
                {
                    _sittingPose = (talkScene.targetHeroine.chaCtrl.objHead.transform.position - talkScene.targetHeroine.transform.position).y < 1f;
                }
                else if (talkScene._isPaly)
                {
                    if (talkScene.cameraMap.enabled)
                    {
                        AdjustTalkScene();
                    }
                }
#endif
            }
            base.OnUpdate();
        }
        internal override void OnLateUpdate()
        {
            if (_lateHitReaction)
            {
                _lateHitReaction = false;
                _hitReaction.ReleaseEffector();
                _hitReaction.SetEffector(lstIKEffectLateUpdate);
                lstIKEffectLateUpdate.Clear();
            }
        }
#if KK
        internal void OverrideAdv(TalkScene instance)
        {
            talkScene = instance;
            _talkSceneStart = true;
        }
#endif
        internal void AdjustAdvScene()
        {
            _start = false;
            if (advScene.Scenario.currentChara != null && advScene.Scenario.currentChara.chaCtrl != null)
            {
                var chara = advScene.Scenario.currentChara.chaCtrl;
                {
                    PlacePlayer(chara.transform.position, chara.transform.rotation);
                    var charas = advScene.scenario.characters.GetComponentsInChildren<ChaControl>();
                    AddTalkColliders(charas);
                    AddHColliders(charas);
                    HitReactionInitialize(charas);
                }
            }
        }


        private void HitReactionInitialize(IEnumerable<ChaControl> charas)
        {
            if (_hitReaction == null)
            {
                // ADV scene is turned off quite often, so we can't utilized its native component.
                _hitReaction = (HitReaction)Util.CopyComponent(advScene.GetComponent<HitReaction>(), Game.instance.gameObject);
            }
            ControllerTracker.Initialize(charas);
        }
        // TalkScenes have clones, reflect it on roam mode.
        private void SynchronizeClothes(ChaControl chara)
        {
            // Broken in KKS.
#if KK

            var npc = Game.Instance.actScene.npcList
#else
            var npc = ActionScene.instance.npcList
#endif
                .Where(n => n.chaCtrl != null
                && n.chaCtrl.fileParam.personality == chara.fileParam.personality
                && n.chaCtrl.fileParam.fullname.Equals(chara.fileParam.fullname))
                .Select(n => n.chaCtrl)
                .FirstOrDefault();
            if (npc == null) return;
            var cloneState = chara.fileStatus.clothesState;
            var originalState = npc.fileStatus.clothesState;
            for (var i = 0; i < cloneState.Length; i++)
            {
                // Apparently there are some hooks to show/hide accessories depending on the state on 'ClothState' methods.
                //npc.SetClothesState(i, cloneState[i], next: false);
                originalState[i] = cloneState[i];
            }
        }
        private TalkSceneHandler GetHandler(int index) => (TalkSceneHandler)HandHolder.GetHand(index).Handler;
        public static void HitReactionPlay(AibuColliderKind aibuKind, ChaControl chara)
        {
           //VRPlugin.Logger.LogDebug($"TalkScene:Reaction:{aibuKind}:{chara}");
            var ik = chara.objAnim.GetComponent<FullBodyBipedIK>();
            if (_hitReaction.ik != ik)
            {
                _hitReaction.ik = ik;
            }
            var key = aibuKind - AibuColliderKind.reac_head;
            var index = Random.Range(0, dicNowReactions[key].lstParam.Count);
            var reactionParam = dicNowReactions[key].lstParam[index];
            var array = new Vector3[reactionParam.lstMinMax.Count];
            for (int i = 0; i < reactionParam.lstMinMax.Count; i++)
            {
                array[i] = new Vector3(Random.Range(reactionParam.lstMinMax[i].min.x, reactionParam.lstMinMax[i].max.x),
                    Random.Range(reactionParam.lstMinMax[i].min.y, reactionParam.lstMinMax[i].max.y),
                    Random.Range(reactionParam.lstMinMax[i].min.z, reactionParam.lstMinMax[i].max.z));
                array[i] = chara.transform.TransformDirection(array[i].normalized);
            }
            _hitReaction.weight = dicNowReactions[key].weight;
            _hitReaction.HitsEffector(reactionParam.id, array);
            lstIKEffectLateUpdate.AddRange(dicNowReactions[key].lstReleaseEffector);
            if (lstIKEffectLateUpdate.Count > 0)
            {
                _lateHitReaction = true;
            }
            Features.LoadVoice.PlayVoice(Random.value < 0.4f ? Features.LoadVoice.VoiceType.Laugh : Features.LoadVoice.VoiceType.Short, chara);
        }

        protected override bool OnTrigger(int index, bool press)
        {
            var handler = GetHandler(index);
            if (press)
            {
                _pressedButtons[index, 0] = true;
                if (handler.IsBusy)
                {
                    GetHandler(index).TriggerPress();
                }
            }
            else
            {
                _pressedButtons[index, 0] = false;
                PickAction(index, EVRButtonId.k_EButton_SteamVR_Trigger);
            }
            return false;
        }
        internal override bool OnDirectionDown(int index, TrackpadDirection direction)
        {
           VRPlugin.Logger.LogDebug($"TalkScene:OnDirectionDown[{direction}]");
            var adv = IsADV;
            var handler = GetHandler(index);
            switch (direction)
            {
                case TrackpadDirection.Up:
                case TrackpadDirection.Down:
                    if (handler.IsBusy)
                    {
                        if (handler.DoUndress(direction == TrackpadDirection.Down, out var chara))
                        {
                            SynchronizeClothes(chara);
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
                    if (IsTriggerPress(index))
                    {
                        HandHolder.GetHand(index).ChangeLayer(direction == TrackpadDirection.Right);
                    }
                    else
                    {
                        AddWait(index, direction, 1f);
                    }
                    break;
            }
            return false;
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
        /// <summary>
        /// We wait for the TalkScene to load up to a certain point and grab/add what we want, adjust charas/camera.
        /// </summary>
        private void AdjustTalkScene()
        {
            _start = false;
            // Basic TalkScene camera if near worthless.
            // Does nothing about clippings with map objects, provides the simplest possible position,
            // a chara.forward vector + lookAt rotation, and chara orientation is determined by the position of chara/camera on the roam map.
            // ADV camera on other hand is legit. ADV Mover though is a headache, would be nice to update it.

            // Here we reposition chara a bit forward if y-position on roam map was on lower side (probably sitting), adjust distance chara <-> camera, place player,
            // and bring back hidden/disabled charas(and correlating NPC components), so they do and go about their stuff (In KKS the "go" part doesn't always work). Bringing back can be prettier by transpiling init of
            // TalkScene(was it Action?), but i yet to figure out how to patch nested types, and that thingy has a fustercluck of them. On other hand the price is quite low,
            // we simply catch a glimpse of crossfading animation of all surrounding charas on fade end.

            //VRPlugin.Logger.LogDebug($"TalkScene:AdjustTalk");
            _talkSceneStart = false;
            talkScene.canvasBack.enabled = false;

            var head = VR.Camera.Head;
            var origin = VR.Camera.Origin;
            var heroine = talkScene.targetHeroine;
            var chara = talkScene.targetHeroine.chaCtrl;
            var headPos = head.position;

            VRPlugin.Logger.LogInfo($"TalkScene:AdjustTalk:sitting = {_sittingPose}");
            headPos.y = heroine.transform.position.y;
#if KK
            talkDistance = 0.4f + (talkScene.targetHeroine.isGirlfriend ? 0f : 0.1f) + (0.1f - talkScene.targetHeroine.intimacy * 0.001f);
#else
            talkDistance = 0.4f + (talkScene.targetHeroine.isGirlfriend ? 0f : 0.1f);
#endif
            var offset = _sittingPose == true || afterH ? 0.3f : 0f;
            afterH = false;

            heroine.transform.rotation = Quaternion.LookRotation(headPos - heroine.transform.position);
            var forwardVec = heroine.transform.forward;
            heroine.transform.position += forwardVec * offset;

            headPos = heroine.transform.position + (forwardVec * talkDistance);
#if KK
            var actionScene = Game.Instance.actScene;
#else
            var actionScene = ActionScene.instance;
#endif

            var charas = new List<ChaControl>()
            {
                chara,
                actionScene.Player.chaCtrl
            };

            foreach (var npc in actionScene.npcList)
            {
                if (npc.heroine.Name != talkScene.targetHeroine.Name)
                {
                    if (npc.mapNo == actionScene.Map.no)
                    {
                        npc.SetActive(true);
                        charas.Add(chara);
                    }
                    npc.Pause(false);
                    npc.charaData.SetRoot(npc.gameObject);
                    //VRPlugin.Logger.LogDebug($"TalkScene:ExtraNPC:{npc.name}:{npc.motion.state}");
                }
            }
#if KK
            origin.rotation = chara.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
#endif
            origin.position += new Vector3(headPos.x, head.position.y, headPos.z) - head.position;

            PlacePlayer(headPos, heroine.transform.rotation);
            AddHColliders(charas);
            HitReactionInitialize(charas);
#if KK
            // KKS has fixed dir light during Talk/ADV by default.
            RepositionDirLight(chara);
#endif
        }

        private void PlacePlayer(Vector3 floor, Quaternion rotation)
        {

            var player = GetPlayer();
            if (player.chaCtrl == null)
            {
                VRPlugin.Logger.LogDebug($"PlacePlayer:No chara to place");
                return;
            }

            if (player.chaCtrl.objTop.activeSelf)
            {
                VRCameraMover.Instance.Impersonate(player.chaCtrl);
                VRPlugin.Logger.LogDebug($"PlacePlayer:Already present, impersonating");
                return;
            }
            var head = VR.Camera.Head;
            var eyePos = GetEyesPosition();
            var headPos = head.position;
            headPos.y = floor.y + (eyePos.y - player.position.y);
            VR.Camera.Origin.position += headPos - head.position;
            var vec = player.position - eyePos;

            player.SetActive(true);
            player.rotation = rotation * Quaternion.Euler(0f, 180f, 0f);


            // An option to keep the head behind vr camera, allowing it to remain visible
            // so we don't see the shadow of a headless body.
            //if (KoikatuInterpreter.settings.ForceShowMaleHeadInAdv)
            //{
            //    VRMale.ForceShowHead = true;
            //    position += player.transform.forward * -0.15f;
            //}
            player.position = head.position + vec;
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
