using UnityEngine;
using VRGIN.Core;
using HarmonyLib;
using System.Collections.Generic;
using KK_VR.Camera;
using KK_VR.Features;
using System;
using Manager;
using System.Linq;
using System.Collections;
using KK_VR.Interpreters;
using KK_VR.Caress;
using Random = UnityEngine.Random;
using static HFlag;
using static HandCtrl;
using static VRGIN.Controls.Controller;
using Valve.VR;
using KK_VR.Handlers;
using KK_VR.Controls;
using RootMotion.FinalIK;
using ADV.Commands.H;
using ADV;
using KK_VR.Fixes;
using System.Runtime.Serialization.Formatters;
using KK_VR.Trackers;
using KK_VR.Interactors;
using KK_VR.Patches;
using KK_VR.Grasp;
using KK_VR.Holders;
using System.Diagnostics;
using KK_VR.Settings;

namespace KK_VR.Interpreters
{
    internal class HSceneInterpreter : SceneInterpreter
    {
        
        private readonly PoV _pov;
        private HPointMove _hPointMove;

        private readonly static List<int> _lstIKEffectLateUpdate = [];
        private static bool _lateHitReaction;

        internal static HFlag hFlag;
        internal static HSprite sprite;
        internal static EMode mode;
        internal static HandCtrl handCtrl;
        internal static HandCtrl handCtrl1;
        internal static HAibu hAibu;
        internal static HVoiceCtrl hVoice;
        internal static List<HActionBase> lstProc;
        internal static List<ChaControl> lstFemale;
        internal static ChaControl male;
        private static int _backIdle;
        private static bool adjustDirLight;
        private readonly MouthGuide _mouth;
        private static HitReaction _hitReaction;

        internal static bool IsInsertIdle(string nowAnim) => nowAnim.EndsWith("InsertIdle", StringComparison.Ordinal);
        internal static bool IsIdleOutside(string nowAnim) => nowAnim.Equals("Idle");
        internal static bool IsAfterClimaxInside(string nowAnim) => nowAnim.EndsWith("IN_A", StringComparison.Ordinal);
        internal static bool IsAfterClimaxOutside(string nowAnim) => nowAnim.EndsWith("OUT_A", StringComparison.Ordinal);
        internal static bool IsClimaxHoushiInside(string nowAnim) => nowAnim.StartsWith("Oral", StringComparison.Ordinal);
        internal static bool IsAfterClimaxHoushiInside(string nowAnim) => nowAnim.Equals("Drink_A") || nowAnim.Equals("Vomit_A");
        internal static bool IsFinishLoop => hFlag.finish != FinishKind.none && IsOrgasmLoop;
        internal static bool IsWeakLoop => hFlag.nowAnimStateName.EndsWith("WLoop", StringComparison.Ordinal);
        internal static bool IsStrongLoop => hFlag.nowAnimStateName.EndsWith("SLoop", StringComparison.Ordinal);
        internal static bool IsOrgasmLoop => hFlag.nowAnimStateName.EndsWith("OLoop", StringComparison.Ordinal);
        internal static bool IsKissAnim => hFlag.nowAnimStateName.StartsWith("K_", StringComparison.Ordinal);
        internal static bool IsTouch => hFlag.nowAnimStateName.EndsWith("Touch", StringComparison.Ordinal);
        internal HPointMove GetHPointMove => _hPointMove == null ? _hPointMove = UnityEngine.Object.FindObjectOfType<HPointMove>() : _hPointMove;
        internal static int GetBackIdle => _backIdle;
#if KK
        internal static bool IsHPointMove => Scene.Instance.AddSceneName.Equals("HPointMove");
#else
        internal static bool IsHPointMove => Scene.AddSceneName.Equals("HPointMove");
#endif
        internal static bool IsVoiceActive => hVoice.nowVoices[0].state != HVoiceCtrl.VoiceKind.breath || IsKissAnim;
        internal static bool IsHandAttached => handCtrl.useItems[0] != null || handCtrl.useItems[1] != null;
        internal static bool IsHandActive => handCtrl.GetUseAreaItemActive() != -1;
        internal static bool IsActionLoop
        {
            get
            {
                return mode switch
                {
                    EMode.aibu => handCtrl.IsKissAction() || handCtrl.IsItemTouch(),
                    EMode.houshi or EMode.sonyu => hFlag.nowAnimStateName.EndsWith("Loop", StringComparison.Ordinal),
                    _ => false,
                };
            }
        }

        private static readonly List<string> _aibuAnims =
        [
            "Idle",     // 0
            "M_Touch",  // 1
            "A_Touch",  // 2
            "S_Touch",  // 3
            "K_Touch"   // 4
        ];

        private List<int> GetHPointCategoryList
        {
            get
            {
                var list = GetHPointMove.dicObj.Keys.ToList();
                list.Sort();
                return list;
            }
        }

        internal HSceneInterpreter(MonoBehaviour proc)
        {
            var traverse = Traverse.Create(proc);
            hFlag = traverse.Field("flags").GetValue<HFlag>();
            sprite = traverse.Field("sprite").GetValue<HSprite>();
            handCtrl = traverse.Field("hand").GetValue<HandCtrl>();
            handCtrl1 = traverse.Field("hand1").GetValue<HandCtrl>();
            lstProc = traverse.Field("lstProc").GetValue<List<HActionBase>>();
            hVoice = traverse.Field("voice").GetValue<HVoiceCtrl>();
            lstFemale = traverse.Field("lstFemale").GetValue<List<ChaControl>>();
            male = traverse.Field("male").GetValue<ChaControl>();
            hAibu = (HAibu)lstProc[0];

            CrossFader.HSceneHooks.SetFlag(hFlag);

            var charas = new List<ChaControl>() { male };
            charas.AddRange(lstFemale);
            var distinctCharas = charas.Distinct();
            VRBoop.RefreshDynamicBones(distinctCharas);

            SceneExtras.EnableDynamicBones(distinctCharas);
            SceneExtras.AddTalkColliders(distinctCharas);
            SceneExtras.AddHColliders(distinctCharas);
            GraspController.Init(distinctCharas);

            _mouth = MouthGuide.Create();
            _pov = PoV.Create();
            adjustDirLight = true;
            // Init after everything.
//#if KKS
//            MeshCollider.AddRascal(lstFemale[0]);
//#endif
            HitReactionInitialize(distinctCharas);
            LocationPicker.AddComponents();
#if KK
            // If disabled, camera won't know where to move.
            Manager.Config.EtcData.HInitCamera = true;
#else
            Manager.Config.HData.HInitCamera = true;
#endif
            if (_settings.ShadowsOptimization == KoikatuSettings.ShadowType.Auto)
            {
                KoikatuInterpreter.TweakShadowSettings(KoikatuSettings.ShadowType.Close);
            }
            if (KoikatuInterpreter.Settings.AutoEnterPov)
            {
                SmoothMover.Instance.MoveToPoV();
            }
        }


        internal override void OnDisable()
        {
            SmoothMover.Instance.MakeUpright();
            Component.Destroy(_pov);
            GameObject.Destroy(_mouth.gameObject);

            SceneExtras.ReturnDirLight();
            HandHolder.DestroyHandlers();
            LocationPicker.DestroyComponents();
            TalkSceneInterpreter.afterH = true;
#if KKS
            ObiCtrlFix.OnHSceneEnd();
#endif
            if (GraspHelper.Instance != null)
            {
                Component.Destroy(GraspHelper.Instance);
            }
        }

        internal override void OnUpdate()
        {
            // Exit through the title button in config doesn't trigger hook.
            if (hFlag == null) KoikatuInterpreter.EndScene(KoikatuInterpreter.SceneType.HScene);
            base.OnUpdate();
        }

        internal override void OnLateUpdate()
        {
            if (_lateHitReaction)
            {
                _lateHitReaction = false;
                _hitReaction.ReleaseEffector();
                _hitReaction.SetEffector(_lstIKEffectLateUpdate);
                _lstIKEffectLateUpdate.Clear();
            }
            if (adjustDirLight)
            {
                SceneExtras.RepositionDirLight(lstFemale[0]);
                adjustDirLight = false;
            }
        }
               
        internal static void EnableNip(AibuColliderKind colliderKind)
        {
            if (colliderKind == AibuColliderKind.muneL || colliderKind == AibuColliderKind.muneR)
            {
                var number = colliderKind == AibuColliderKind.muneL ? 0 : 1;
                handCtrl.female.DisableShapeNip(number, false);
                handCtrl.female.DisableShapeBodyID(number, ChaFileDefine.cf_ShapeMaskNipStand, false);
                //if (number == 1)
                //{
                //    handCtrl.female.DisableShapeBust(number, false);
                //}
            }
        }

        internal static void ShowAibuHand(AibuColliderKind colliderKind, bool show)
        {
            handCtrl.useAreaItems[(int)colliderKind - 2].objBody.GetComponent<Renderer>().enabled = show;
        }

        internal void ToggleAibuHandVisibility(AibuColliderKind colliderKind)
        {
            var renderer = handCtrl.useAreaItems[(int)colliderKind - 2].objBody.GetComponent<Renderer>();
            renderer.enabled = !renderer.enabled;
            EnableNip(colliderKind);
        }

        internal static bool PlayShort(ChaControl chara, bool voiceWait = true)
        {
            if (lstFemale.Contains(chara))
            {
                if (!voiceWait || !IsVoiceActive)
                {
                    hFlag.voice.playShorts[lstFemale.IndexOf(chara)] = Random.Range(0, 9);
                }
                return true;
            }
            else
            {
                Features.LoadVoice.PlayVoice(Features.LoadVoice.VoiceType.Short, chara, voiceWait);
            }
            return false;
        }

        internal IEnumerator RandomHPointMove(bool startScene)
        {
            if (startScene)
            {
                hFlag.click = ClickKind.pointmove;
                yield return new WaitUntil(() => IsHPointMove);
            }
            var hPoint = GetHPointMove;
            var key = hPoint.dicObj.ElementAt(Random.Range(0, hPoint.dicObj.Count)).Key;
            ChangeCategory(GetHPointCategoryList.IndexOf(key));
            yield return null;
            var dicList = hPoint.dicObj[hPoint.nowCategory];
            var hPointData = dicList[Random.Range(0, dicList.Count)].GetComponent<H.HPointData>();
            hPoint.actionSelect(hPointData, hPoint.nowCategory);
#if KK
            Singleton<Scene>.Instance.UnLoad();
#else
            Scene.Unload();
#endif
        }

        internal static void SetSelectKindTouch(AibuColliderKind colliderKind)
        {
            if (handCtrl != null) handCtrl.selectKindTouch = colliderKind;
        }

        private int GetCurrentBackIdleIndex()
        {
            var twoLetters = hFlag.nowAnimStateName.Remove(2);
            var anim = _aibuAnims
                .Where(anim => anim.StartsWith(twoLetters, StringComparison.Ordinal))
                .FirstOrDefault();
            var index = _aibuAnims.IndexOf(anim);
            _backIdle = index == 4 ? 0 : index;
            return index;
        }

        internal static void LeanToKiss()
        {
            HScenePatches.HoldKissLoop();
            if (IntegrationSensibleH.active)
            {
                IntegrationSensibleH.OnKissStart(AibuColliderKind.none);
            }
            SetPlay(_aibuAnims[4]);
        }

        internal void ScrollAibuAnim(bool increase)
        {
            var index = GetCurrentBackIdleIndex() + (increase ? 1 : -1);
            if (index > 3)
            {
                index = 1;
            }
            else if (index < 1)
            {
                index = 3;
            }
            _pov.StartCoroutine(PlayAnimOverTime(index));
        }

        internal static void PlayReaction()
        {
            var nowAnim = hFlag.nowAnimStateName;
            switch (mode)
            {
                case EMode.houshi:
                    if (IsActionLoop)
                    {
                        if (hFlag.nowAnimationInfo.kindHoushi == 1)
                        {
                            handCtrl.Reaction(AibuColliderKind.reac_head);
                        }
                        else if (hFlag.nowAnimationInfo.kindHoushi == 2)
                        {
                            handCtrl.Reaction(AibuColliderKind.reac_bodyup);
                        }
                        else
                        {
                            handCtrl.Reaction(AibuColliderKind.reac_armR);
                        }
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case EMode.sonyu:
                    if (IsAfterClimaxInside(nowAnim) || IsInsertIdle(nowAnim) || IsActionLoop)
                    {
                        handCtrl.Reaction(AibuColliderKind.reac_bodydown);
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                default:
                    var items = handCtrl.GetUseItemNumber();
                    var count = items.Count;
                    if (count != 0)
                    {
                        var item = items[Random.Range(0, count)];
                        handCtrl.Reaction(handCtrl.useItems[item].kindTouch < AibuColliderKind.kokan ? AibuColliderKind.reac_bodyup : AibuColliderKind.reac_bodydown);
                    }
                    break;
            }
        }

        private IEnumerator PlayAnimOverTime(int index)
        {
            PlayReaction();
            yield return new WaitForSeconds(0.25f);
            hAibu.backIdle = -1;
            HScenePatches.suppressSetIdle = true;
            SetPlay(_aibuAnims[index]);
        }

        internal static void SetPlay(string animation)
        {
            lstProc[(int)hFlag.mode].SetPlay(animation, true);
        }

        internal void MoveCategory(bool increase)
        {
            var list = GetHPointCategoryList;
            var index = list.IndexOf(GetHPointMove.nowCategory);
            if (increase)
            {
                if (index == list.Count - 1)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }
            }
            else
            {
                if (index == 0)
                {
                    index = list.Count - 1;
                }
                else
                {
                    index--;
                }
            }
            ChangeCategory(index);
        }

        private void ChangeCategory(int index)
        {
            var list = GetHPointCategoryList;
            GetHPointMove.SelectPointVisible(list[index], true);
            GetHPointMove.nowCategory = list[index];
        }

        internal int GetCurrentLoop(bool increase)
        {
            if (IsWeakLoop)
            {
                return increase ? 1 : 0;
            }
            if (IsStrongLoop)
            {
                return increase ? 2 : 0;
            }
            // OLoop
            return increase ? 2 : 1;
        }
        
        internal static void OnPoseChange(HSceneProc.AnimationListInfo anim)
        {
            mode = anim.mode switch
            {
                EMode.houshi or EMode.houshi3P or EMode.houshi3PMMF => EMode.houshi,
                EMode.sonyu or EMode.sonyu3P or EMode.sonyu3PMMF => EMode.sonyu,
                _ => anim.mode,
            };
            adjustDirLight = true;
            GraspController.OnSpotPoseChange();
            MouthGuide.OnPoseChange(anim.mode);
            if (male != null)
            {
                // KK has it by default? KKS definitely disables them for the male.
                SceneExtras.EnableDynamicBones(male);
            }
        }

        internal static void OnSpotChange()
        {
            adjustDirLight = true;
            GraspController.OnSpotPoseChange();
        }

        public void HitReactionInitialize(IEnumerable<ChaControl> charas)
        {
            if (_hitReaction == null)
            {
                _hitReaction = handCtrl1.hitReaction;
            }
            ControllerTracker.Initialize(charas);
            HandHolder.UpdateHandlers<HSceneHandler>();
        }

        public static void HitReactionPlay(AibuColliderKind aibuKind, ChaControl chara, bool voiceWait)
        {
            // This roundabout way is to allow player to touch anybody present, including himself, janitor,
            // and charas from kPlug (actually don't know if they have FullBodyBipedIK or not, because we need it).

            // TODO voice is a placeHolder, in h we have a good dic lying around with the proper ones.

           //VRPlugin.Logger.LogDebug($"HScene:Reaction:{aibuKind}:{chara}");
            _hitReaction.ik = chara.objAnim.GetComponent<FullBodyBipedIK>();

            var dic = handCtrl.dicNowReaction;
            if (dic.Count == 0)
            {
                dic = SceneExtras.dicNowReactions;
            }
            var key = aibuKind - AibuColliderKind.reac_head;
            var index = Random.Range(0, dic[key].lstParam.Count);
            var reactionParam = dic[key].lstParam[index];
            var array = new Vector3[reactionParam.lstMinMax.Count];
            for (int i = 0; i < reactionParam.lstMinMax.Count; i++)
            {
                array[i] = new Vector3(Random.Range(reactionParam.lstMinMax[i].min.x, reactionParam.lstMinMax[i].max.x),
                    Random.Range(reactionParam.lstMinMax[i].min.y, reactionParam.lstMinMax[i].max.y),
                    Random.Range(reactionParam.lstMinMax[i].min.z, reactionParam.lstMinMax[i].max.z));
                array[i] = chara.transform.TransformDirection(array[i].normalized);
            }
            _hitReaction.weight = dic[key].weight;
            _hitReaction.HitsEffector(reactionParam.id, array);
            _lateHitReaction = true;
            _lstIKEffectLateUpdate.AddRange(dic[key].lstReleaseEffector);

            PlayShort(chara, voiceWait);
        }

        //private bool SetHand()
        //{
        //   //VRPlugin.Logger.LogDebug($"Interpreter:HScene:SetHand");
        //    if (handCtrl.useItems[0] == null || handCtrl.useItems[1] == null)
        //    {
        //        var list = new List<int>();
        //        for (int i = 0; i < 6; i++)
        //        {
        //            if (handCtrl.useAreaItems[i] == null)
        //            {
        //                list.Add(i);
        //            }
        //        }
        //        list = list.OrderBy(a => Random.Range(0, 100)).ToList();
        //        var index = 0;
        //        foreach (var item in list)
        //        {
        //           //VRPlugin.Logger.LogDebug($"Interpreter:HScene:SetHand:Loop:{item}");
        //            var clothState = handCtrl.GetClothState((AibuColliderKind)(item + 2));
        //            //var layerInfo = handCtrl.dicAreaLayerInfos[item][handCtrl.areaItem[item]];
        //            var layerInfo = handCtrl.dicAreaLayerInfos[item][0];
        //            if (layerInfo.plays[clothState] == -1)
        //            {
        //                continue;
        //            }
        //            index = item;
        //            break;

        //        }
        //       //VRPlugin.Logger.LogDebug($"Interpreter:HScene:SetHand:Required:Choice - {index}");

        //        handCtrl.selectKindTouch = (AibuColliderKind)(index + 2);
        //        _pov.StartCoroutine(CaressUtil.ClickCo(() => handCtrl.selectKindTouch = AibuColliderKind.none));
        //        return false;
        //    }
        //    else
        //    {
        //       //VRPlugin.Logger.LogDebug($"Interpreter:HScene:SetHand:NotRequired");
        //        PlayReaction();
        //        return true;
        //    }
        //}
    }
}
