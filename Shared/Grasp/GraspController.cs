using KK_VR.Handlers;
using KK_VR.Holders;
using KK_VR.Interpreters;
using KK_VR.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace KK_VR.Grasp
{
    // Named Grasp so there is less confusion with GripMove. 
    // Each instance associated with hand controller.

    // Why new FinalIK (FBBIK and likes of it) ?
    // Pros. I've better mileage with it, it has HeadEffector that i can actually adapt to normal charas,
    // it has VRIK for player, it has animClip baker. Far beyond "enough of a reason" in my book.
    // Cons. Can't seem to make 'Reach' of IKEffector to work, not in game nor in editor. Old one has it working just fine.

    /// <summary>
    /// Manipulates IK of the character
    /// </summary>
    internal class GraspController
    {
        private readonly HandHolder _hand;
        private static GraspHelper _helper;
        private static readonly List<GraspController> _instances = [];
        private static readonly Dictionary<ChaControl, List<BodyPart>> _bodyPartsDic = [];

        private readonly Dictionary<ChaControl, List<Tracker.Body>> _blackListDic = [];
        private static readonly List<List<PartName>> _jointGroupList =
        [
            [PartName.ShoulderL, PartName.ShoulderR],
            [PartName.ThighL, PartName.ThighR]
        ];

        private ChaControl _heldChara;
        private ChaControl _syncedChara;

        // For Grip.
        private BendGoal _heldBendGoal;
        private readonly List<BodyPart> _heldBodyParts = [];
        // For Trigger conditional long press. 
        private readonly List<BodyPart> _tempHeldBodyParts = [];
        // For Touchpad.
        private readonly List<BodyPart> _syncedBodyParts = [];

        private static readonly List<Vector3> _limbPosOffsets =
        [
            new Vector3(-0.005f, 0.015f, -0.04f),
            new Vector3(0.005f, 0.015f, -0.04f),
            Vector3.zero,
            Vector3.zero
        ];
        private static readonly List<Quaternion> _limbRotOffsets =
        [
            Quaternion.Euler(0f, 90f, 0f),
            Quaternion.Euler(0f, -90f, 0f),
            Quaternion.identity,
            Quaternion.identity
        ];

        // Add held items too once implemented. All bodyParts have black list entries, dic is sufficient.
        internal bool IsBusy => _blackListDic.Count > 0 || _heldBendGoal != null || (_helper != null && _helper.baseHold != null);
        internal Dictionary<ChaControl, List<Tracker.Body>> GetBlacklistDic => _blackListDic;
        internal List<BodyPart> GetFullBodyPartList(ChaControl chara) => _bodyPartsDic[chara];
        internal enum State
        {
            Default = 1,      // Not influenced by player.
            Active = 2,       // Influenced by player. 
            Translation = 4,  // Returns to default position.
            Grasped = 8,      // Being held by player.
            Synced = 16,      // Follows player's controller.
            Attached = 32,    // Closely follows some object.
        }
        
        public enum PartName
        {
            Spine,
            ShoulderL,
            ShoulderR,
            ThighL,
            ThighR,
            HandL,
            HandR,
            FootL,
            FootR,
            Head,
            UpperBody,
            LowerBody,
            Everything
        }
        internal GraspController(HandHolder hand)
        {
            _hand = hand;
            _instances.Add(this);
        }
        internal static void Init(IEnumerable<ChaControl> charas)
        {
            _bodyPartsDic.Clear();
            foreach (var inst in _instances)
            {
                inst.HardReset();
            }
            if (_helper == null)
            {
                _helper = charas.First().gameObject.AddComponent<GraspHelper>();
                _helper.Init(charas, _bodyPartsDic);
#if DEBUG
                VRPlugin.Logger.LogInfo($"Grasp:Init");
#endif
            }
            else
            {
                VRPlugin.Logger.LogWarning($"Grasp:Init - wrong state, Grasp already exists");
            }
        }
        private void UpdateGrasp(BodyPart bodyPart, ChaControl chara)
        {
            _heldChara = chara;
            _heldBodyParts.Add(bodyPart);
        }
        private void UpdateGrasp(IEnumerable<BodyPart> bodyPart, ChaControl chara)
        {
            _heldChara = chara;
            _heldBodyParts.AddRange(bodyPart);
        }

        private void UpdateTempGrasp(BodyPart bodyPart)
        {
            _tempHeldBodyParts.Add(bodyPart);
        }
        private void UpdateSync(BodyPart bodyPart, ChaControl chara)
        {
            _syncedChara = chara;
            _syncedBodyParts.Add(bodyPart);
        }
        private void StopGrasp()
        {
            _heldBodyParts.Clear();
            if (_heldChara != null)
            {
                _blackListDic.Remove(_heldChara);
                _heldChara = null;
                _tempHeldBodyParts.Clear();

                UpdateBlackList();
            }
            _hand.OnGraspRelease();
        }
        private void StopTempGrasp()
        {
            _tempHeldBodyParts.Clear();
            UpdateBlackList();
        }
        private void StopSync(bool instant)
        {
            foreach (var bodyPart in _syncedBodyParts)
            {
                if (bodyPart.anchor.parent != null && bodyPart.anchor.parent.name.StartsWith("dispos", StringComparison.OrdinalIgnoreCase))
                {
                    // Check shouldn't be necessary tbh.
                    GameObject.Destroy(bodyPart.anchor.parent.gameObject);
                }
                bodyPart.anchor.SetParent(bodyPart.beforeIK, worldPositionStays: true);
                if (instant || KoikSettings.IKReturnBodyPartAfterSync.Value)
                {
                    bodyPart.guide.Sleep(instant);
                }
                else
                {
                    bodyPart.guide.Stay();
                }
                foreach (var collider in bodyPart.colliders)
                {
                    collider.enabled = true;
                }
            }
            _syncedBodyParts.Clear();
            _hand.OnLimbSyncStop();
            if (_syncedChara != null)
            {
                _syncedChara = null;
                UpdateBlackList();
            }
        }
        private PartName ConvertTrackerToIK(Tracker.Body part)
        {
            return part switch
            {
                Tracker.Body.ArmL => PartName.ShoulderL,
                Tracker.Body.ArmR => PartName.ShoulderR,
                Tracker.Body.MuneL or Tracker.Body.MuneR => PartName.UpperBody,
                Tracker.Body.LowerBody => PartName.Spine,
                Tracker.Body.LegL or Tracker.Body.FootL => PartName.FootL,
                Tracker.Body.LegR or Tracker.Body.FootR => PartName.FootR,
                Tracker.Body.ThighL => PartName.ThighL,
                Tracker.Body.ThighR => PartName.ThighR,
                Tracker.Body.HandL or Tracker.Body.ForearmL => PartName.HandL,
                Tracker.Body.HandR or Tracker.Body.ForearmR => PartName.HandR,
                Tracker.Body.Groin or Tracker.Body.Asoko => PartName.LowerBody,
                Tracker.Body.Head => PartName.Head,
                // actual UpperBody
                _ => PartName.Spine,
            };
        }

        private PartName GetChild(PartName parent)
        {
            // Shoulders/thighs found separately based on the distance.
            return parent switch
            {
                PartName.ThighL => PartName.FootL,
                PartName.ThighR => PartName.FootR,
                PartName.ShoulderL => PartName.HandL,
                PartName.ShoulderR => PartName.HandR,
                _ => parent
            };
        }

        private PartName FindJoints(List<BodyPart> lstBodyPart, Vector3 pos)
        {
            // Finds joint pair that was closer to the core and returns it as abnormal index for further processing.
            var list = new List<float>();
            foreach (var partNames in _jointGroupList)
            {
                // Avg distance to both joints
                list.Add(
                    (Vector3.Distance(lstBodyPart[(int)partNames[0]].effector.bone.position, pos)
                    + Vector3.Distance(lstBodyPart[(int)partNames[1]].effector.bone.position, pos))
                    * 0.5f);
            }
            // 0 - Shoulders, 1 - thighs
            return list[0] - 0.1f > list[1] ? PartName.LowerBody : PartName.UpperBody;
        }

        private List<PartName> FindJoint(List<BodyPart> lstBodyPart, List<PartName> partNames, Vector3 pos)
        {
            // Works with abnormal index, returns closer joint or both based on the distance.
            var a = Vector3.Distance(lstBodyPart[(int)partNames[0]].effector.bone.position, pos);
            var b = Vector3.Distance(lstBodyPart[(int)partNames[1]].effector.bone.position, pos);
            if ((a > b && a * 0.85f < b)
                || (a < b && a > b * 0.85f))
            {
                // Meaning they are approx equal.
                return partNames;
            }
            else
            {
                // Nope, they weren't.
                return [a < b ? partNames[0] : partNames[1]];
            }
        }

        /// <summary>
        /// Returns 1 .. 3 names that we should start interaction with.
        /// </summary>
        private List<BodyPart> GetTargetParts(List<BodyPart> lstBodyPart, PartName target, Vector3 pos)
        {
            // Finds PartName(s) that we should initially target. 

            var bodyPartList = new List<BodyPart>();
            if (target == PartName.Spine)
            {
                bodyPartList.Add(lstBodyPart[(int)target]);
                target = FindJoints(lstBodyPart, pos);
            }
            // abnormal index, i.e. pair of joints
            if (target > PartName.Head)
            {
                FindJoint(lstBodyPart, _jointGroupList[target == PartName.UpperBody ? 0 : 1], pos)
                    .ForEach(name => bodyPartList.Add(lstBodyPart[(int)name]));
            }
            else
            {
                bodyPartList.Add(lstBodyPart[(int)target]);
            }
            return bodyPartList;
        }
        /// <summary>
        /// Returns name of corresponding parent.
        /// </summary>
        private PartName GetParent(PartName childName)
        {
            return childName switch
            {
                PartName.Spine => PartName.Everything,
                PartName.Everything => childName,
                PartName.HandL => PartName.ShoulderL,
                PartName.HandR => PartName.ShoulderR,
                PartName.FootL => PartName.ThighL,
                PartName.FootR => PartName.ThighR,
                // For shoulders/thighs  
                _ => PartName.Spine
            };
        }
        internal bool OnTriggerPress(bool temporarily)
        {
            //VRPlugin.Logger.LogDebug($"OnTriggerPress");

            // We look for a BodyPart from which grasp has started (0 index in _heldBodyParts),
            // and attach it to the collider's gameObjects.

            if (_heldChara != null)
            {
                // First we look if it's a limb and it has tracking on something.
                // If there is no track, then expand limbs we are holding.
                var heldBodyParts = _heldBodyParts.Concat(_tempHeldBodyParts);
                var bodyPartsLimbs = heldBodyParts
                    .Where(b => b.IsLimb && b.guide.IsBusy);
                if (bodyPartsLimbs.Any())
                {
                    foreach (var bodyPart in bodyPartsLimbs)
                    {
                        bodyPart.guide.Attach(bodyPart.guide.GetTrackTransform);
                    }
                    ReleaseBodyParts(heldBodyParts);
                    StopGrasp();
                }
                else
                {
                    return ExtendGrasp(temporarily);
                }
            }
            else if (_syncedChara != null)
            {
                var bodyParts = _syncedBodyParts
                    .Where(b => b.guide.IsBusy);
                if (bodyParts.Any())
                {
                    foreach (var bodyPart in bodyParts)
                    {
                        bodyPart.guide.Attach(bodyPart.guide.GetTrackTransform);
                    }
                    ReleaseBodyParts(bodyParts);
                    StopGrasp();
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool ExtendGrasp(bool temporarily)
        {
            // Attempts to grasp BodyPart(s) higher in hierarchy or everything if already top.
            var bodyPartList = _bodyPartsDic[_heldChara];
            var closestToCore = _heldBodyParts
                .OrderBy(bodyPart => bodyPart.name)
                .First().name;
            var nearbyPart = GetChild(closestToCore);
            if (nearbyPart == closestToCore || !bodyPartList[(int)nearbyPart].IsState(State.Default))
            {
                nearbyPart = GetParent(closestToCore);
            }
            var attachPoint = bodyPartList[(int)closestToCore].anchor;
            if (nearbyPart != PartName.Everything)
            {
                if (temporarily)
                    UpdateTempGrasp(bodyPartList[(int)nearbyPart]);
                else
                {
                    UpdateGrasp(bodyPartList[(int)nearbyPart], _heldChara);
                }
                UpdateBlackList();
                GraspBodyPart(bodyPartList[(int)nearbyPart], attachPoint);
            }
            else
            {
                ReleaseBodyParts(bodyPartList);
                HoldChara();
            }
            return true;
        }
        private void HoldChara()
        {
            _helper.StartBaseHold(_bodyPartsDic[_heldChara][0], _heldChara.objAnim.transform, _hand.Anchor);
        }
        internal void OnTriggerRelease()
        {
            if (_tempHeldBodyParts.Count > 0)
            {
                ReleaseBodyParts(_tempHeldBodyParts);
                StopTempGrasp();
                UpdateBlackList();
            }
        }
        // Reset currently held body parts.
        internal bool OnTouchpadResetHeld()
        {
            if (_helper != null)
            {
                if (_heldBodyParts.Count > 0)
                {
                    ResetBodyParts(_heldBodyParts, false);
                    ResetBodyParts(_tempHeldBodyParts, false);
                    StopGrasp();
                    //_hand.Handler.ClearTracker();
                    return true;
                }
                else if (_heldBendGoal != null)
                { 
                    _heldBendGoal.Sleep();
                    ReleaseBendGoal();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private void ReleaseBendGoal()
        {
            _heldBendGoal = null;
            _hand.OnGraspRelease();
        }
        // Reset tracking by controller body part if not in default state.
        internal bool OnTouchpadResetActive(Tracker.Body trackerPart, ChaControl chara)
        {
            // We attempt to reset orientation if part was active.
            if (_helper != null && _bodyPartsDic.ContainsKey(chara))
            {
                var baseName = ConvertTrackerToIK(trackerPart);
                if (baseName != PartName.Spine)
                {
                    var bodyParts = GetTargetParts(_bodyPartsDic[chara], baseName, _hand.Anchor.position);
                    var result = false;
                    foreach (var bodyPart in bodyParts)
                    {
                        if (!bodyPart.IsState(State.Default) && !bodyPart.IsState(State.Translation))
                        {
                            bodyPart.guide.Sleep(false);
                            result = true;
                        }
                    }
                    //if (result)
                    //{
                    //    _hand.Handler.ClearTracker();
                    //}
                    return result;
                }
                else
                {
                    // If torso - reset whole chara.
                    return OnTouchpadResetEverything(chara);
                }
            }
            return false;            
        }
        internal bool OnTouchpadResetEverything(ChaControl chara)
        {
            if (_helper != null && _bodyPartsDic.ContainsKey(chara))
            {
                var result = false;
                foreach (var bodyPart in _bodyPartsDic[chara])
                {
                    if (bodyPart.IsState(State.Active) || bodyPart.IsState(State.Attached))
                    {
                        bodyPart.guide.Sleep(false);
                        result = true;
                    }
                }
                //_hand.Handler.ClearTracker();
                return result;
            }
            return false;
        }
        //internal bool OnMenuPress()
        //{
        //    if (_heldBodyParts.Count != 0)
        //    {

        //    }
        //    else
        //    {
        //        return false;
        //    }
        //    return true;
        //}
        internal void OnGripPress(Tracker.Body trackerPart, ChaControl chara)
        {
            if (_helper != null && _bodyPartsDic.ContainsKey(chara))
            {
                var anchor = _hand.Anchor;
                var bodyParts = GetTargetParts(_bodyPartsDic[chara], ConvertTrackerToIK(trackerPart), anchor.position);
                var firstBodyPart = bodyParts[0];

                // If limb is altered already and bend goal is very close, alter it instead.
                if (firstBodyPart.IsLimb && !firstBodyPart.IsState(State.Default) && firstBodyPart.goal.IsClose(anchor.position))
                {

                    // Going for colliders currently isn't optimal,
                    // maybe later if I'll manage to stuff in mesh collider in KK,
                    // then we can go for it. Having it KKS only isn't viable.

                    _heldBendGoal = firstBodyPart.goal;
                    firstBodyPart.goal.Follow(anchor);
                }
                else
                {
                    // Update blackList before actual grasp !!!
                    UpdateGrasp(bodyParts, chara);
                    UpdateBlackList();
                    foreach (var bodyPart in bodyParts)
                    {
                        GraspBodyPart(bodyPart, anchor);
                    }
                }

                //if (MouthGuide.Instance != null)
                //{
                //    MouthGuide.Instance.PauseInteractions = true;
                //}
                _hand.OnGraspHold();
            }
        }
        internal void OnGripRelease()
        {
            if (_helper != null)
            {
                if (_helper.baseHold != null)
                {
                    _helper.StopBaseHold();
                    StopGrasp();
                }
                else if (_heldBodyParts.Count > 0)
                {
                    ReleaseBodyParts(_heldBodyParts);
                    ReleaseBodyParts(_tempHeldBodyParts);
                    StopGrasp();
                }
                else if (_heldBendGoal != null)
                {
                    _heldBendGoal.Stay();
                    ReleaseBendGoal();
                }
            }
        }
        private bool AttemptToScrollBodyPart(bool increase)
        {
            // Only bodyParts directly from the tracker live at 0 index, i.e. firstly interacted with.

#if KK
            if (KoikGameInterp.IsParty) return false;
#endif

            if (_helper != null && _heldBodyParts.Count > 0 && (_heldBodyParts[0].name == PartName.HandL || _heldBodyParts[0].name == PartName.HandR))
            {
                _helper.ScrollHand(_heldBodyParts[0].name, _heldChara, increase);
                return true;
            }
            return false;
            
        }



        internal bool OnBusyHorizontalScroll(bool increase)
        {
            if (_helper.baseHold != null)
            {
                _helper.baseHold.StartBaseHoldScroll(2, increase);
            }
            else if (!AttemptToScrollBodyPart(increase))
            {
                return false;
            }
            return true;
        }
        internal bool OnFreeHorizontalScroll(Tracker.Body trackerPart, ChaControl chara, bool increase)
        {
#if KK
            if (KoikGameInterp.IsParty) return false;
#endif
            if (_helper != null && trackerPart == Tracker.Body.HandL || trackerPart == Tracker.Body.HandR)
            {
                _helper.ScrollHand((PartName)trackerPart, chara, increase);
                return true;
            }
            return false;
        }

        internal void OnScrollRelease()
        {
            if (_helper != null)
            {
                if (_helper.baseHold != null)
                {
                    _helper.baseHold.StopBaseHoldScroll();
                }
                else
                {
                    _helper.StopScroll();
                }
            }
        }

        internal bool OnVerticalScroll(bool increase)
        {
            if (_helper != null)
            {
                if (_helper.baseHold != null)
                {
                    _helper.baseHold.StartBaseHoldScroll(1, increase);
                }
                else if (_heldBodyParts.Count > 0)
                {
                    foreach (var bodyPart in _heldBodyParts)
                    {
                        bodyPart.visual.SetState(increase);
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        private void ReleaseBodyParts(IEnumerable<BodyPart> bodyPartsList)
        {
            foreach (var bodyPart in bodyPartsList)
            {
                // Attached bodyParts released one by one if they overstretch (not implemented), or by directly grabbing/resetting one.
                if (!bodyPart.IsState(State.Default) && !bodyPart.IsState(State.Attached))
                {
                    bodyPart.guide.Stay();
                }
            }
        }

        private void ResetBodyParts(IEnumerable<BodyPart> bodyPartList, bool instant)
        {
            foreach (var bodyPart in bodyPartList)
            {
                if (!bodyPart.IsState(State.Default))
                {
                    bodyPart.guide.Sleep(instant);
                }
            }
        }
        internal static void OnSpotPoseChange()
        {
            // If we are initiated. Everything attaches to charas, they gone - whole grasp too. First chara has master components. But all charas have extra weight on them.
            if (_helper != null)
            {
                _helper.OnPoseChange();
                foreach (var inst in _instances)
                {
                    inst.SoftReset();
                }
            }
        }
        private void SoftReset()
        {
            _hand.Handler.ClearTracker();
            _helper.StopBaseHold();
            _blackListDic.Clear();

            ResetBodyParts(_heldBodyParts, instant: true);
            _heldBodyParts.Clear();

            ResetBodyParts(_tempHeldBodyParts, instant: true);
            _tempHeldBodyParts.Clear();

            StopSync(instant: true);
            _syncedBodyParts.Clear();

            _heldChara = null;
            _syncedChara = null;
        }

        private void HardReset()
        {
            _blackListDic.Clear();
            _heldBodyParts.Clear();
            _tempHeldBodyParts.Clear();
            _syncedBodyParts.Clear();
            _heldChara = null;
            _syncedChara = null;
        }

        private void SyncBodyPart(BodyPart bodyPart, Transform attachPoint)
        {
            foreach (var collider in bodyPart.colliders)
            {
                collider.enabled = false;
            }
            bodyPart.anchor.SetParent(attachPoint, worldPositionStays: true);
            if (bodyPart.guide is BodyPartGuide bodyGuide)
            {
                bodyGuide.OnSyncStart();
            }
            bodyPart.chain.bendConstraint.weight = KoikSettings.IKDefaultBendConstraint.Value;
            bodyPart.goal.Sleep();
        }

        private void GraspBodyPart(BodyPart bodyPart, Transform attachPoint)
        {
            bodyPart.guide.Follow(attachPoint, _hand);
        }
        private bool IsLimb(PartName partName) => partName > PartName.ThighR && partName < PartName.UpperBody;
        internal bool OnTouchpadSyncStart(Tracker.Body trackerPart, ChaControl chara)
        {
            if (_helper != null)
            {
                var partName = ConvertTrackerToIK(trackerPart);
                if (IsLimb(partName))
                {
                    VRPlugin.Logger.LogDebug($"Grasp:OnTouchpadSyncStart:{trackerPart} -> {partName}");
                    var bodyPart = _bodyPartsDic[chara][(int)partName];

                    var limbIndex = (int)partName - 5;
                    var disposable = new GameObject("disposeOnSyncEnd").transform;
                    disposable.SetParent(_hand.Anchor, worldPositionStays: false);
                    disposable.localPosition = _limbPosOffsets[limbIndex];
                    disposable.localRotation = _limbRotOffsets[limbIndex];

                    SyncBodyPart(bodyPart, disposable);
                    //bodyPart.anchor.transform.localPosition = _limbPosOffsets[limbIndex];
                    //bodyPart.anchor.transform.localRotation = _limbRotOffsets[limbIndex];
                    //bodyPart.chain.pull = 0f;
                    UpdateSync(bodyPart, chara);
                    UpdateBlackList();
                    _hand.OnLimbSyncStart();
                    _hand.Handler.ClearTracker();
                    //_hand.Handler.ClearBlacks();
                    return true;
                }
            }
            return false;
        }

        internal bool OnTouchpadSyncStop()
        {
            if (_helper != null && _syncedBodyParts.Count != 0)
            {
                StopSync(instant: false);
                return true;
            }
            return false;
        }


        private void UpdateBlackList()
        {
            _blackListDic.Clear();
            SyncBlackList(_syncedBodyParts, _syncedChara);
            SyncBlackList(_heldBodyParts, _heldChara);
            SyncBlackList(_tempHeldBodyParts, _heldChara);
            _hand.Handler.ClearBlacks();
        }
        private void SyncBlackList(List<BodyPart> bodyPartList, ChaControl chara)
        {
            if (chara == null || bodyPartList.Count == 0) return;

            if (!_blackListDic.ContainsKey(chara))
            {
                _blackListDic.Add(chara, []);
            }
            foreach (var bodyPart in bodyPartList)
            {
                foreach (var entry in _blackListEntries[(int)bodyPart.name])
                {
                    if (!_blackListDic[chara].Contains(entry))
                        _blackListDic[chara].Add(entry);
                }
            }

        }


        // Parts that we blacklist and don't track (for that chara?). Tracker can flush active blacklisted tracks on demand. 
        private static readonly List<List<Tracker.Body>> _blackListEntries =
        [
            // 0
            // 'None' stands for complete ignore, chara will be skipped by that tracker.
            [Tracker.Body.None], 
            // 1
            [ Tracker.Body.HandL, Tracker.Body.ForearmL, Tracker.Body.ArmL,
                Tracker.Body.UpperBody, Tracker.Body.MuneL, Tracker.Body.MuneR ],
            // 2
            [ Tracker.Body.HandR, Tracker.Body.ForearmR, Tracker.Body.ArmR,
                Tracker.Body.UpperBody, Tracker.Body.MuneL, Tracker.Body.MuneR ],
            // 3
            [ Tracker.Body.LegL, Tracker.Body.ThighL, Tracker.Body.LowerBody,
                Tracker.Body.Asoko, Tracker.Body.Groin],
            // 4
            [ Tracker.Body.LegR, Tracker.Body.ThighR, Tracker.Body.LowerBody,
                Tracker.Body.Asoko, Tracker.Body.Groin],
            // 5 
            [Tracker.Body.HandL, Tracker.Body.ForearmL, Tracker.Body.ArmL],
            // 6
            [Tracker.Body.HandR, Tracker.Body.ForearmR, Tracker.Body.ArmR],
            // 7
            [Tracker.Body.LegL, Tracker.Body.FootL],
            // 8
            [Tracker.Body.LegR, Tracker.Body.FootR],
            // 9
            [Tracker.Body.None],
        ];
    }
}
