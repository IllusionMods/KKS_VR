using System;
using System.Collections.Generic;
using System.Linq;
using VRGIN.Core;
using UnityEngine;
using Manager;
using ADV;
using Random = UnityEngine.Random;
using RootMotion.FinalIK;
using static HandCtrl;
using static KK_VR.Interpreters.SceneExtras;
using KK_VR.Handlers;
using KK_VR.Camera;
using KK_VR.Holders;
using KK_VR.Grasp;
using KK_VR.Settings;

namespace KK_VR.Interpreters
{
    internal class TalkSceneInterp : SceneInterpreter
    {
        internal static float talkDistance = 0.55f;
        internal static float height;
        internal static TalkScene talkScene;
        internal static ADVScene advScene;
        internal static bool afterH;
        private static HitReaction _hitReaction;
        private readonly static List<int> lstIKEffectLateUpdate = [];
        private static bool _lateHitReaction;

        private bool _talkSceneStart;

        /// <summary>
        /// Init phase, waiting to setup everything.
        /// </summary>
        internal bool IsStart => _start;
        private bool _start;
        private bool? _sittingPose;

        private Transform _eyes;

        private readonly List<string> _mapsWithoutPlayer =
            [
#if KK
            "MyRoom",
#else
            "HotelMyroom",
            "MinsyukuMyroom",
            "GasyukuMyroom",
#endif
            ];
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
            return _eyes.TransformPoint(0f, GameSettings.PositionOffsetY.Value, GameSettings.PositionOffsetZ.Value);
        }
        public TalkSceneInterp(MonoBehaviour behaviour)
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

            if (GameSettings.ShadowSetting.Value == GameSettings.ShadowType.Auto)
            {
                GameSettings.UpdateShadowSetting(GameSettings.ShadowType.Close);
            }
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
            SceneExtras.ReturnDirLight();
#endif
            if (GraspHelper.Instance != null)
            {
                Component.Destroy(GraspHelper.Instance);
            }
        }

        internal override void OnUpdate()
        {
#if KK
            if (talkScene == null && (advScene == null || !advScene.isActiveAndEnabled))
#else
            if ((talkScene == null || !talkScene._isPaly) && (advScene == null || !advScene.isActiveAndEnabled))
#endif
            {
                KoikGameInterp.EndScene(KoikGameInterp.SceneType.TalkScene);
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
                PlacePlayer(chara.transform.position, chara.transform.rotation);
                var charas = advScene.scenario.characters.GetComponentsInChildren<ChaControl>().Distinct();
                AddTalkColliders(charas);
                AddHColliders(charas);
                HitReactionInitialize(charas);

                // This one might be too much tbh. IK stuff is heavy all around and adv scene pops up all the time.
                //GraspController.Init(charas);
            }
        }

        private void HitReactionInitialize(IEnumerable<ChaControl> charas)
        {
            if (_hitReaction == null)
            {
                // ADV scene is turned off quite often, so we can't utilized its native component.
                _hitReaction = VRGIN.Helpers.UnityHelper.CopyComponent(advScene.GetComponent<HitReaction>(), Game.instance.gameObject);
            }
            ControllerTracker.Initialize(charas);
        }

        // TalkScenes have clones, reflect it on roam mode.
        internal void SynchronizeClothes(ChaControl chara)
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
            Features.LoadGameVoice.PlayVoice(Random.value < 0.4f ? Features.LoadGameVoice.VoiceType.Laugh : Features.LoadGameVoice.VoiceType.Short, chara);
        }

        /// <summary>
        /// We wait for the TalkScene to load up to a certain point and grab/add what we want, adjust charas/camera.
        /// </summary>
        private void AdjustTalkScene()
        {
            _start = false;
            // TODO Add collision detection? should be easy.
            // RayCast chara.forward / -chara.forward for ~0.15f after rotation adjustment,
            // if we catch something, move chara.forward.
            // KKS has good map colliders, KK semi-good after sky removal, there will be edge cases.

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
            }.Distinct();

            foreach (var npc in actionScene.npcList)
            {
                if (npc.heroine.Name != talkScene.targetHeroine.Name)
                {
                    if (npc.mapNo == actionScene.Map.no)
                    {
                        npc.SetActive(true);

                        // Adding reactions/manipulations to everyone present is near pointless and VERY expensive (FBBIK).
                        //charas.Add(chara);
                    }
                    npc.Pause(false);
                    npc.charaData.SetRoot(npc.gameObject);
                    //VRPlugin.Logger.LogDebug($"TalkScene:ExtraNPC:{npc.name}:{npc.motion.state}");
                }
            }
#if KK
            // KKS handles rotation itself.
            origin.rotation = chara.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
#endif
            origin.position += new Vector3(headPos.x, head.position.y, headPos.z) - head.position;

            PlacePlayer(headPos, heroine.transform.rotation);
            AddHColliders(charas);
            HitReactionInitialize(charas);

            // NetFramework 3.5 doesn't have HasFlag() method, flag enums are still neatly represented in BepInEx though.
            if ((KoikSettings.IKEnable.Value & KoikSettings.IKManipulationState.TalkScene) != 0)
            {
                GraspController.Init(charas);
            }

            // KKS has fixed dir light during Talk/ADV by default.
            // Reported that KKS has poor default orientation. 
            RepositionDirLight(chara);
        }

        internal static void GraspReInit()
        {
            if (GraspHelper.Instance != null || talkScene == null || talkScene.targetHeroine == null || talkScene.targetHeroine.chaCtrl == null ||
#if KK
                Game.Instance == null || Game.Instance.actScene == null || Game.Instance.actScene.Player == null || Game.Instance.actScene.Player.chaCtrl == null
#elif KKS
                ActionScene.instance == null || ActionScene.instance.Player == null || ActionScene.instance.Player.chaCtrl == null
#endif
                )
            {
                return;
            }

            IEnumerable<ChaControl> charas =
            [
                talkScene.targetHeroine.chaCtrl,
#if KK
                Game.Instance.actScene.Player.chaCtrl
#elif KKS
                ActionScene.instance.Player.chaCtrl
#endif
            ];

            GraspController.Init(charas.Distinct());
        }

        private void PlacePlayer(Vector3 floor, Quaternion rotation)
        {

            var player = GetPlayer();
            if (_mapsWithoutPlayer.Contains(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            {
                return;
            }
            if (player.chaCtrl == null || player.chaCtrl.objTop == null)
            {
#if DEBUG
                VRPlugin.Logger.LogInfo($"Talk/Adv:PlacePlayer:No male chara to place");
#endif
                return;
            }
            foreach (Transform child in advScene.Scenario.Characters)
            {
                if (child.name.StartsWith("chaM_", StringComparison.Ordinal))
                {
#if DEBUG
                    VRPlugin.Logger.LogInfo($"Talk/Adv:PlacePlayer:Scene has extra male chara, impersonating");
#endif
                    CameraMover.Instance.Impersonate(child.GetComponent<ChaControl>());
                    return;
                }
            }

            var head = VR.Camera.Head;
            player.rotation = rotation * Quaternion.Euler(0f, 180f, 0f);
            var eyePos = GetEyesPosition();
            var headPos = head.position;
            headPos.y = floor.y + (eyePos.y - player.position.y) + GameSettings.PositionOffsetY.Value;


            VR.Camera.Origin.position += headPos - head.position;
            var vec = player.position - eyePos;
            player.position = head.position + vec;

            player.SetActive(true);


            // An option to keep the head behind vr camera, allowing it to remain visible
            // so we don't see the shadow of a headless body.
            //if (KoikGame.settings.ForceShowMaleHeadInAdv)
            //{
            //    VRMale.ForceShowHead = true;
            //    position += player.transform.forward * -0.15f;
            //}
        }

    }
}
