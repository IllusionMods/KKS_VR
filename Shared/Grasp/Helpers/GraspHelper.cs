using KK.RootMotion.FinalIK;
using KK_VR.Handlers;
using KK_VR.IK;
using KK_VR.Interpreters;
using KK_VR.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static KK_VR.Grasp.GraspController;
using KK_VR.Holders;
using System.Linq;
using static KK_VR.Fixes.Util;
using HarmonyLib;

namespace KK_VR.Grasp
{
    /// <summary>
    /// Singleton, helps Grasp with hooks/init/end. 
    /// Removes everything Grasp related OnDestroy()
    /// </summary>
    internal class GraspHelper : MonoBehaviour
    {
        internal static GraspHelper Instance => _instance;
        private static GraspHelper _instance;
        //private bool _transition;
        private bool _animChange;
        private bool _handChange;
        //private readonly List<OffsetPlay> _transitionList = [];
        private readonly Dictionary<ChaControl, string> _animChangeDic = [];
        private static Dictionary<ChaControl, List<BodyPart>> _bodyPartsDic;
        private static Dictionary<ChaControl, IKStuff> _auxDic = [];
        private readonly List<HandScroll> _handScrollList = [];
        internal BaseHold baseHold;

        private class IKStuff
        {
            internal KK.RootMotion.FinalIK.FullBodyBipedIK newFbik;
            internal RootMotion.FinalIK.FullBodyBipedIK oldFbik;
            internal LookAtController lookAt;
            internal TouchReaction reaction;
        }
        internal void Init(IEnumerable<ChaControl> charas, Dictionary<ChaControl, List<BodyPart>> bodyPartsDic)
        {
            _instance = this;
            _auxDic.Clear();
            _bodyPartsDic = bodyPartsDic;
            foreach (var chara in charas)
            {
                // Dude will have VRIK. Guess we'll need both and hot swap option for all of them. 
                // hot swap can't be pretty though, but if it happens on the time of pov exit, then it should be fine.

                // Adapting VRIK proves to be the pure pain. Perhaps will stick to the single mode only for each situation.
                // One being half animated/half controlled by VRIK, with some boundaries to keep player going nuts in intercourse/service.
                // Another one fully controlled by VRIK with custom advanced locomotion animController.
                // Hopefully AgiShark has all the proper animations for advanced locomotion, otherwise I've no clue how to retarget animation for our rig.
                // He doesn't. Gotta find someone who'd retarget them for us, otherwise working on VRIK is hardly worth it.
                
                AddChara(chara);
            }

            // By default component <Lookat_dan> aims dick before IK solver but we want it after.
            // The easiest solution seems to be to call it again as it isn't heavy.
            if (charas.Any())
            {
                var lookat_dan = FindObjectsOfType<Lookat_dan>()
                .Where(t => t.male != null);
                var type = typeof(Lookat_dan);

                if (GetMethod(type, "LateUpdate", out var method))
                {
                    foreach (var dan in lookat_dan)
                    {
                        var methodDelegate = AccessTools.MethodDelegate<Action>(method, dan);
                        _auxDic.Last().Value.newFbik.solver.OnPostUpdate += () => methodDelegate();
                    }
                }
            }
        }
        private void AddChara(ChaControl chara)
        {
            _auxDic.Add(chara, new IKStuff
            {
                newFbik = FBBIK.UpdateFBIK(chara),
                oldFbik = chara.objAnim.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>(),
                lookAt = LookAt.SetupLookAtIK(chara),
                reaction = chara.objAnim.AddComponent<TouchReaction>()
            });
            //AnimLoaderHelper.FindMissingBones(_auxDic[chara].oldFbik);
            var ik = _auxDic[chara].newFbik;
            var oldIK = _auxDic[chara].oldFbik;
            if (ik == null || oldIK == null) return;
            // MotionIK makes adjusts animations based on the body size, doesn't seem to be used outside of H.
            var withMotionIK = KoikGameInterp.CurrentScene == KoikGameInterp.SceneType.HScene;
            _bodyPartsDic.Add(chara,
            [

                new (
                    _name:       PartName.Spine,
                    _effector:   ik.solver.bodyEffector,
                    _afterIK:    ik.solver.bodyEffector.bone,
                    _beforeIK:   BeforeIK.CreateObj("spine", chara, ik.solver.bodyEffector.bone),
                    _chain:      ik.solver.chain[0]
                    ),

                new (
                    _name:       PartName.ShoulderL,
                    _effector:   ik.solver.leftShoulderEffector,
                    _afterIK:    ik.solver.leftShoulderEffector.bone,
                    _beforeIK:   BeforeIK.CreateObj("shoulderL", chara, ik.solver.leftShoulderEffector.bone)
                    ),

                new (
                    _name:       PartName.ShoulderR,
                    _effector:   ik.solver.rightShoulderEffector,
                    _afterIK:    ik.solver.rightShoulderEffector.bone,
                    _beforeIK:   BeforeIK.CreateObj("shoulderR", chara, ik.solver.rightShoulderEffector.bone)
                    ),

                new (
                    _name:       PartName.ThighL,
                    _effector:   ik.solver.leftThighEffector,
                    _afterIK:    ik.solver.leftThighEffector.bone,
                    _beforeIK:   BeforeIK.CreateObj("thighL", chara, ik.solver.leftThighEffector.bone)
                    ),

                new (
                    _name:       PartName.ThighR,
                    _effector:   ik.solver.rightThighEffector,
                    _afterIK:    ik.solver.rightThighEffector.bone,
                    _beforeIK:   BeforeIK.CreateObj("thighR", chara, ik.solver.rightThighEffector.bone)
                    ),

                new (
                    _name:       PartName.HandL,
                    _effector:   ik.solver.leftHandEffector,
                    _afterIK:    ik.solver.leftHandEffector.bone,
                    _beforeIK:   withMotionIK ? ik.solver.leftHandEffector.target : BeforeIK.CreateObj("handL", chara, ik.solver.leftHandEffector.bone),
                    _chain:      ik.solver.leftArmChain
                    ),

                new (
                    _name:       PartName.HandR,
                    _effector:   ik.solver.rightHandEffector,
                    _afterIK:    ik.solver.rightHandEffector.bone,
                    _beforeIK:   withMotionIK ? ik.solver.rightHandEffector.target : BeforeIK.CreateObj("handR", chara, ik.solver.rightHandEffector.bone),
                    _chain:      ik.solver.rightArmChain
                    ),

                new (
                    _name:       PartName.FootL,
                    _effector:   ik.solver.leftFootEffector,
                    _afterIK:    ik.solver.leftFootEffector.bone,
                    _beforeIK:   withMotionIK ? ik.solver.leftFootEffector.target : BeforeIK.CreateObj("footL", chara, ik.solver.leftFootEffector.bone),
                    _chain:      ik.solver.leftLegChain
                    ),

                new (
                    _name:       PartName.FootR,
                    _effector:   ik.solver.rightFootEffector,
                    _afterIK:    ik.solver.rightFootEffector.bone,
                    _beforeIK:   withMotionIK ? ik.solver.rightFootEffector.target : BeforeIK.CreateObj("footR", chara, ik.solver.rightFootEffector.bone),
                    _chain:      ik.solver.rightLegChain
                    ),

                new BodyPartHead(
                    _name:       PartName.Head,
                    _chara:      chara,
                    _afterIK:    ik.references.head,
                    _beforeIK:   BeforeIK.CreateObj("head", chara, ik.references.head)
                    ),
            ]);

            AddExtraColliders(chara);
            foreach (var bodyPart in _bodyPartsDic[chara])
            {
                bodyPart.anchor.SetParent(bodyPart.beforeIK, worldPositionStays: false);
                if (bodyPart is BodyPartHead head)
                {
                    head.headEffector.enabled = KoikSettings.IKHeadEffector.Value == KoikSettings.HeadEffector.Always;
                }
                bodyPart.guide.Init(bodyPart);

                //if (KoikGame.Settings.IKShowDebug)
                //{
                //    Fixes.Util.CreatePrimitive(PrimitiveType.Sphere, new Vector3(0.06f, 0.06f, 0.06f), bodyPart.anchor, Color.yellow, 0.5f);
                //    //Util.CreatePrimitive(PrimitiveType.Sphere, new Vector3(0.12f, 0.12f, 0.12f), bodyPart.afterIK, Color.yellow, 0.4f);
                //}
                if (bodyPart.IsLimb)
                {
                    FindColliders(bodyPart, chara);
                }
            }
            SetWorkingState(chara);

            // MonoBehavior will get sad if we won't let it get Start().
            StartCoroutine(InitCo(_bodyPartsDic[chara]));
        }

        private IEnumerator InitCo(IEnumerable<BodyPart> bodyParts)
        {
            yield return null;
            foreach (var bodyPart in bodyParts)
            {
                bodyPart.anchor.gameObject.SetActive(bodyPart.GetDefaultState());
            }
        }

        internal IKCaress StartIKCaress(HandCtrl.AibuColliderKind colliderKind, ChaControl chara, HandHolder hand)
        {
            var rough = hand.Anchor.gameObject.AddComponent<IKCaress>();
            rough.Init(_auxDic[chara].newFbik, colliderKind, _bodyPartsDic[chara], chara, hand.Anchor);
            return rough;
        }

        private void OnDestroy()
        {
            if (_bodyPartsDic != null && _bodyPartsDic.Count > 0)
            {
                foreach (var bodyPartList in _bodyPartsDic.Values)
                {
                    foreach (var bodyPart in bodyPartList)
                    {
                        if (bodyPart.anchor.parent != null && bodyPart.anchor.parent.name.StartsWith("ik_b4", StringComparison.OrdinalIgnoreCase))
                        {
                            Destroy(bodyPart.anchor.parent.gameObject);
                        }
                        else
                        {
                            Destroy(bodyPart.anchor.gameObject);
                        }
                        // Null check shouldn't be necessary.
                        if (bodyPart.guide != null)
                        {
                            Destroy(bodyPart.guide.gameObject);
                        }
                        if (bodyPart.goal != null)
                        {
                            Destroy(bodyPart.goal.gameObject);
                        }
                    }
                }
                foreach (var ik in _auxDic.Values)
                {
                    if (ik.newFbik != null)
                    {
                        Component.Destroy(ik.newFbik);
                    }
                    if (ik.lookAt != null)
                    {
                        Component.Destroy(ik.lookAt);
                    }
                    if (ik.reaction != null)
                    {
                        Component.Destroy(ik.reaction);
                    }
                }
                _bodyPartsDic.Clear();
            }
        }


        //private readonly Dictionary<string, float[]> _poseRelPos = new Dictionary<string, float[]>
        //{
        //    { "kha_f_00", [ 1f, 0f ] },
        //    { "kha_f_01", [ 0f, 0f ] },
        //    { "kha_f_02", [ 0f, 0f ] },
        //    { "kha_f_03", [ 1f, 1f ] },
        //    { "kha_f_04", [ 0f, 0f ] },
        //    { "kha_f_05", [ 0f, 0f ] },
        //    { "kha_f_06", [ 0f, 0f ] },
        //    { "kha_f_07", [ 0f, 1f ] },


        //    { "khs_f_00", [ 1f, 1f ] },
        //};

        private readonly List<string> _extraColliders =
        [
            "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L/cf_j_leg01_L/cf_j_leg03_L/cf_j_foot_L/cf_hit_leg02_L",
            "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R/cf_j_leg01_R/cf_j_leg03_R/cf_j_foot_R/cf_hit_leg02_R",
        ];

        private void AddFeetCollider(Transform bone)
        {
            // StopGap measure until mesh collider.
            var collider = bone.gameObject.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = bone.gameObject.AddComponent<CapsuleCollider>();
                collider.radius = 0.1f;
                collider.height = 0.5f;
                collider.direction = 2;
                bone.localPosition = new Vector3(bone.localPosition.x, 0f, 0.06f);
            }
        }
        private void AddExtraColliders(ChaControl chara)
        {
            foreach (var path in _extraColliders)
            {
                AddFeetCollider(chara.objBodyBone.transform.Find(path));
            }
        }
        private void FindColliders(BodyPart bodyPart, ChaControl chara)
        {
            foreach (var str in _limbColliders[bodyPart.name])
            {
                var target = chara.objBodyBone.transform.Find(str);
#if KK
                if (target != null)
                {
                    var col = target.GetComponent<Collider>();
                    if (col != null)
                    {
                        bodyPart.colliders.Add(col);
                    }
                }
#else
                if (target != null && target.TryGetComponent<Collider>(out var col))
                {
                    bodyPart.colliders.Add(col);
                }
#endif
            }
        }

        internal static void SetWorkingState(ChaControl chara)
        {
            // By default only limbs are used, the rest is limited to offset play by hitReaction.
            if (_bodyPartsDic != null && _bodyPartsDic.ContainsKey(chara))
            {
                foreach (var bodyPart in _bodyPartsDic[chara])
                {
                    if (bodyPart.effector != null)
                    {
                        bodyPart.effector.target = bodyPart.anchor;

                        if (bodyPart.IsLimb)
                        {
                            bodyPart.chain.bendConstraint.weight = bodyPart.IsState(State.Default) || bodyPart.goal.IsBusy ? 1f : KoikSettings.IKDefaultBendConstraint.Value;
                        }
                    }
                }
    
                    _auxDic[chara].oldFbik.enabled = false;
                AnimLoaderHelper.FixExtraAnim(chara, _bodyPartsDic[chara]);
            }
        }

        /// <summary>
        /// We put IKEffector.target to ~default state.
        /// MotionIK.Calc() requires original stuff, without it we won't get body size offsets or effector's supposed targets.
        /// </summary>
        internal static void SetDefaultState(ChaControl chara, string stateName)
        {
            //VRPlugin.Logger.LogDebug($"Helper:Grasp:SetDefaultState:{chara}");
            if (_bodyPartsDic != null && _bodyPartsDic.ContainsKey(chara))
            {
                if (stateName != null && chara.objTop.activeSelf && chara.visibleAll)
                {
                    _instance.StartAnimChange(chara, stateName);
                }
                foreach (var bodyPart in _bodyPartsDic[chara])
                {
                    if (bodyPart.effector != null)
                    {
                        bodyPart.effector.target = bodyPart.origTarget;
                        if (bodyPart.chain != null)
                        {
                            bodyPart.chain.bendConstraint.weight = 1f;
                        }
                    }
                }
            }

        }
        /// <summary>
        /// We hold anchors of currently modified Hand bodyParts while animation crossfades, and return back afterwards.
        /// </summary>
        private void StartAnimChange(ChaControl chara, string stateName)
        {
            for (var i = 5; i < 7; i++)
            {
                var bodyPart = _bodyPartsDic[chara][i]; 

                if (!_animChangeDic.ContainsKey(chara))
                {
                    _animChangeDic.Add(chara, stateName);
                    _animChange = true;
                }

                if (bodyPart.IsState(State.Active))
                {
#if DEBUG
                    VRPlugin.Logger.LogInfo($"Grasp:StartAnimChange:Active:{chara}:{stateName}");
#endif
                    bodyPart.guide.Follow(_bodyPartsDic[chara][i - 4].anchor, null); // anchor.parent = _bodyPartsDic[chara][(int)GetParent(bodyPart.name)].anchor;
                }
                else if (bodyPart.IsState(State.Default) && bodyPart.IsState(State.Attached))
                {
#if DEBUG
                    VRPlugin.Logger.LogInfo($"Grasp:StartAnimChange:DefaultAttached:{chara}:{stateName}");
#endif
                    bodyPart.guide.Sleep(instant: true);
                }
            }
        }

        internal void UpdateMaintainRelativePosition(bool active)
        {
            foreach (var bodyPartList in _bodyPartsDic.Values)
            {
                for (var i = 5; i < 7; i++)
                {
                    bodyPartList[i].effector.maintainRelativePositionWeight = active ? 1f : 0f;
                }
            }
        }

        internal void UpdatePushParent(float number)
        {
            foreach (var bodyPartList in _bodyPartsDic.Values)
            {
                for (var i = 5; i < 7; i++)
                {
                    bodyPartList[i].chain.push = number == 0f ? 0f : 1f;
                    bodyPartList[i].chain.pushParent = number;
                }
            }
        }

        private void DoAnimChange()
        {
#if DEBUG
            VRPlugin.Logger.LogDebug($"Grasp:DoAnimChange");
#endif
            foreach (var kv in _animChangeDic)
            {
                if (kv.Key.animBody.GetCurrentAnimatorStateInfo(0).IsName(kv.Value))
                {
                    OnAnimChangeEnd(kv.Key);
                    return;
                }
            }
        }
        private void OnAnimChangeEnd(ChaControl chara)
        {
#if DEBUG
            VRPlugin.Logger.LogInfo($"Grasp:OnAnimChangeEnd:{chara}");
#endif
            for (var i = 5; i < 7; i++)
            {
                var bodyPart = _bodyPartsDic[chara][i];
                if (bodyPart.IsState(State.Active))
                {
                    bodyPart.guide.Stay();
                }
                else
                {
                    FindAttachmentPoint(bodyPart, chara);
                }
            }
            _animChangeDic.Remove(chara);
            _animChange = _animChangeDic.Count != 0;
        }
        internal void FindAttachmentPoints()
        {
            foreach (var entry in _bodyPartsDic)
            {
                for (var i = 5; i < 7; i++)
                {
                    FindAttachmentPoint(entry.Value[i], entry.Key);
                }
            }
        }

        private void FindAttachmentPoint(BodyPart bodyPart, ChaControl chara)
        {
            if (!KoikSettings.IKAutoHandAttachment.Value) return;
#if DEBUG
            VRPlugin.Logger.LogDebug($"FindAttachmentPoint:{chara}:{bodyPart.name}");
#endif
            var colliders = Physics.OverlapSphere(bodyPart.origTarget.position, 0.05f);
            foreach (var collider in colliders)
            {
                if (Tracker.IsColliderPresent(collider, out var targetChara) && targetChara != chara)
                {
                    bodyPart.guide.Attach(collider.transform);
#if DEBUG
                    VRPlugin.Logger.LogDebug($"*Found:{collider.name}, dist = {Vector3.Distance(bodyPart.origTarget.position, collider.transform.position)}");
#endif
                    break;
                }
            }
        }

        internal void ScrollHand(PartName partName, ChaControl chara, bool increase)
        {
            _handChange = true;
            _handScrollList.Add(new HandScroll(partName, chara, increase));
        }

        internal void StopScroll()
        {
            _handChange = false;
            _handScrollList.Clear();
        }

        // Those are some shady animations where ik will be very wonky.
        private readonly List<string> _animationsNoIK =
            [
            "khs_f_61",
            ];

        internal void OnPoseChange()
        {
            StopAnimChange();
            foreach (var kv in _bodyPartsDic)
            {
                // Disable IK if animation is sloppy.                
                if (kv.Key.animBody.runtimeAnimatorController == null 
                    || _animationsNoIK.Contains(kv.Key.animBody.runtimeAnimatorController.name))
                {
                    _auxDic[kv.Key].newFbik.enabled = false;
                    continue;
                }
                else
                {
                    _auxDic[kv.Key].newFbik.enabled = true;
                }
                var baseDataEmpty = false;
                var count = kv.Value.Count;
                for (var i = 0; i < count; i++)
                {
                    var bodyPart = kv.Value[i];
                    bodyPart.guide.Sleep(instant: true);
                    if (bodyPart.IsLimb)
                    {
                        if (bodyPart.baseData.bone == null)
                        {
                            baseDataEmpty = true;
                        }
                        var component = bodyPart.beforeIK.GetComponent<NoPosBeforeIK>();
                        if (_auxDic[kv.Key].oldFbik.solver.effectors[i].rotationWeight == 0f)
                        {
                            //VRPlugin.Logger.LogWarning($"GraspHelper:RetargetEffectors:[{i}]");
                            //bodyPart.baseData.pos = kv.Key.objBodyBone.transform.InverseTransformDirection(bodyPart.baseData.transform.position - bodyPart.effector.bone.position);
                            //bodyPart.baseData.bone = bodyPart.effector.bone;
                            if (component == null)
                            {
                                bodyPart.beforeIK.gameObject.AddComponent<NoPosBeforeIK>().Init(bodyPart.effector.bone);
                            }
                        }
                        else if (component != null)
                        {
                            Component.Destroy(component);
                        }
                    }
                }
                if (baseDataEmpty)
                {
                    AnimLoaderHelper.FindMissingBones(kv.Key.objAnim.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>());
                }
            }
            // Works at 2 - 3 ticks for female like a clock, for male even 5 is barely stable.
            // No clue why.
            KoikGameInterp.RunAfterUpdate(FindAttachmentPoints, true, 5);
        }
        //internal void RetargetEffectors()
        //{
        //    // If default target doesn't provide rotation.
        //    foreach (var kv in _bodyPartsDic)
        //    {
        //        for (var i = 5; i < 9; i++)
        //        {
        //            var bodyPart = kv.Value[i];
        //            var component = bodyPart.beforeIK.GetComponent<NoPosBeforeIK>();
        //            if (_auxDic[kv.Key].oldFbik.solver.effectors[i].rotationWeight == 0f)
        //            {
        //                VRPlugin.Logger.LogWarning($"RetargetEffectors:[{i}]");
        //                //bodyPart.baseData.pos = kv.Key.objBodyBone.transform.InverseTransformDirection(bodyPart.baseData.transform.position - bodyPart.effector.bone.position);
        //                //bodyPart.baseData.bone = bodyPart.effector.bone;
        //                if (component == null)
        //                {
        //                    bodyPart.beforeIK.gameObject.AddComponent<NoPosBeforeIK>().Init(bodyPart.effector.bone);
        //                }
        //            }
        //            else if (component != null)
        //            {
        //                Component.Destroy(component);
        //            }
        //        }
        //    }

        //}

        internal bool IsGraspActive(ChaControl chara)
        {
            foreach (var bodyPart in _bodyPartsDic[chara])
            {
                if (!bodyPart.IsState(State.Default))
                {
                    // Don't invoke reaction if any of the bodyParts is manipulated. Looks ugly/disruptive.
                    return true;
                }
            }
            return false;
        }
        internal void TouchReaction(ChaControl chara, Vector3 handPosition, Tracker.Body body)
        {
            if (_auxDic.ContainsKey(chara) && !_auxDic[chara].reaction.IsBusy)
            {
                foreach (var bodyPart in _bodyPartsDic[chara])
                {
                    if (bodyPart.IsLimb)
                    {
                        ((BodyPartGuide)bodyPart.guide).StartRelativeRotation();
                    }
                }
                var index = ConvertToTouch(body);
                var vec = (GetClosestBone(chara, index).position - handPosition);
                vec.y = 0f;
                _auxDic[chara].reaction.React(index, vec.normalized);
                Features.LoadGameVoice.PlayVoice(Features.LoadGameVoice.VoiceType.Short, chara);
            }
        }
        private int ConvertToTouch(Tracker.Body part)
        {
            return part switch
            {
                Tracker.Body.LowerBody => 0,
                Tracker.Body.ArmL or Tracker.Body.MuneL => 1,
                Tracker.Body.ArmR  or Tracker.Body.MuneR => 2,
                Tracker.Body.ThighL => 3,
                Tracker.Body.ThighR => 4,
                Tracker.Body.HandL or Tracker.Body.ForearmL => 5,
                Tracker.Body.HandR or Tracker.Body.ForearmR => 6,
                Tracker.Body.FootL => 7,
                Tracker.Body.FootR => 8,
                Tracker.Body.UpperBody or Tracker.Body.Head => 9,
                Tracker.Body.Groin or Tracker.Body.Asoko => 10,
                Tracker.Body.LegL => 11,
                Tracker.Body.LegR => 12,
                _ => 0
            };
        }

        internal void CatchHitReaction(RootMotion.FinalIK.IKSolverFullBodyBiped solver, Vector3 offset, int index)
        {
            foreach (var value in _auxDic.Values)
            {
                if (value.oldFbik.solver == solver)
                {
                    value.newFbik.solver.effectors[index].positionOffset += offset;
                    return;
                }
            }
        }
        internal void OnTouchReactionStop(ChaControl chara)
        {
            foreach (var bodyPart in _bodyPartsDic[chara])
            {
                if (bodyPart.IsLimb)
                {
                    ((BodyPartGuide)bodyPart.guide).StopRelativeRotation();
                }
            }
            
        }
        private Transform GetClosestBone(ChaControl chara, int index)
        {
            // Normalize, not readable otherwise.
            return index switch
            {
                0 => _auxDic[chara].newFbik.solver.rootNode,   // chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_spine01"),
                1 or 2 or 9 => chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03"),
                3 or 4 or > 9 => chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02"),
                _ => _auxDic[chara].newFbik.solver.effectors[index].bone.transform
            };
        }
        private readonly List<PartName> _partNamesToHold =
            [
            PartName.HandL,
            PartName.HandR
            ];
        private void Update()
        {
            baseHold?.Execute();
            if (_animChange) DoAnimChange();
            if (_handChange) DoHandChange();
        }
        
        internal void StartBaseHold(BodyPart spine, Transform objAnim, Transform attachPoint)
        {
            baseHold = new BaseHold(spine, objAnim, attachPoint);
        }

        internal void StopBaseHold()
        {
            baseHold = null;
        }

        private void DoHandChange()
        {
            foreach (var scroll in _handScrollList)
            {
                scroll.Scroll();
            }
        }

        private void StopAnimChange()
        {
            _animChange = false;
            _animChangeDic.Clear();
        }
        private static readonly Dictionary<PartName, List<string>> _limbColliders = new()
        {
            {
                PartName.HandL, new List<string>()
                {
                    "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_L/cf_j_shoulder_L/" +
                    "cf_j_arm00_L/cf_j_forearm01_L/cf_d_forearm02_L/cf_s_forearm02_L/cf_hit_wrist_L",

                    "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_L/cf_j_shoulder_L/cf_j_arm00_L/cf_j_forearm01_L/cf_j_hand_L/com_hit_hand_L",
                }
            },
            {
                PartName.HandR, new List<string>()
                {
                    "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_R/cf_j_shoulder_R/" +
                    "cf_j_arm00_R/cf_j_forearm01_R/cf_d_forearm02_R/cf_s_forearm02_R/cf_hit_wrist_R",

                    "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_R/cf_j_shoulder_R/cf_j_arm00_R/cf_j_forearm01_R/cf_j_hand_R/com_hit_hand_R",
                }
            },
            {
                PartName.FootL, new List<string>()
                {
                    "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L/cf_j_leg01_L/cf_s_leg01_L/cf_hit_leg01_L/aibu_reaction_legL",
                    "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L/cf_j_leg01_L/cf_j_leg03_L/cf_j_foot_L/cf_hit_leg02_L",
                }
            },
            {
                PartName.FootR, new List<string>()
                {
                    "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R/cf_j_leg01_R/cf_s_leg01_R/cf_hit_leg01_R/aibu_reaction_legR",
                    "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R/cf_j_leg01_R/cf_j_leg03_R/cf_j_foot_R/cf_hit_leg02_R",
                }
            }
        };
        
    }
}
