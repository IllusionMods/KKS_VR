using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using ADV;
using ADV.Commands.Object;
using HarmonyLib;
using KK_VR.Interpreters;
using KK_VR.Settings;
using Manager;
using UnityEngine;
using VRGIN.Core;

namespace KK_VR.Camera
{
    /// <summary>
    /// A class responsible for moving the VR camera.
    /// TODO probably has some bugs since it was copied mostly as it is from KK to KKS
    /// </summary>
    public class CameraMover
    {
        public static CameraMover Instance => _instance ??= new CameraMover();
        private static CameraMover _instance;

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

        public delegate void OnMoveAction();

        public event OnMoveAction OnMove;

        public CameraMover()
        {
            _lastPosition = Vector3.zero;
            _lastRotation = Quaternion.identity;
        }

        /// <summary>
        /// Move the camera to the specified pose.
        /// </summary>
        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            if (position.Equals(Vector3.zero))
            {
                return;
            }
//#if DEBUG
//               VRPlugin.Logger.LogDebug($"{GetType().Name}:MoveTo\n{new StackTrace(0)}");
//#endif
            _lastPosition = position;
            _lastRotation = rotation;

            // We don't want to respect head rotations when we deal with adv text.
            VR.Camera.Origin.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            VR.Camera.Origin.position += position - VR.Camera.Head.position;


            //// Trim out X (pitch) and Z (roll) to prevent player from being upside down and such
            //var trimmedRotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            //VR.Mode.MoveToPosition(position, trimmedRotation, keepHeight);

            //VR.Mode.MoveToPosition(position, rotation, ignoreHeight: keepHeight);
            OnMove?.Invoke();
        }

        /// <summary>
        /// Move the camera using some heuristics.
        ///
        /// The position and rotation arguments should represent the pose
        /// the camera would take in the 2D version of the game.
        /// </summary>
        public void MaybeMoveTo(Vector3 position, Quaternion rotation)
        {
            MoveWithHeuristics(position, rotation, false);
        }

        /// <summary>
        /// Similar to MaybeMoveTo, but also considers the ADV fade state.
        /// </summary>
        public void MaybeMoveADV(ADV.TextScenario textScenario, Vector3 position, Quaternion rotation)
        {
            var advFade = textScenario.advFade;// new Traverse(textScenario).Field<ADVFade>("advFade").Value;

            var closerPosition = AdjustAdvPosition(textScenario, position, rotation);
#if KKS
            AdjustBasedOnMap(ref closerPosition, ref rotation);
#endif
            MoveWithHeuristics(closerPosition, rotation, !advFade.IsEnd);
        }

        private Vector3 AdjustAdvPosition(TextScenario textScenario, Vector3 position, Quaternion rotation)
        {
            // Needed for zero checks later
            if (position.Equals(Vector3.zero)) return Vector3.zero;

            var characterTransforms = textScenario.commandController?.Characters
                .Where(x => x.Value?.transform != null)
                .Select(x => x.Value.transform.position);

            if (characterTransforms != null && characterTransforms.Count() > 0)
            {
                //var closerPosition = position + (rotation * Vector3.forward) * 1f;

                var averageV = new Vector3(characterTransforms.Sum(x => x.x), characterTransforms.Sum(x => x.y), characterTransforms.Sum(x => x.z)) / characterTransforms.Count();


                var positionNoY = position;
                positionNoY.y = 0;
                var averageNoY = averageV;
                averageNoY.y = 0;

                //if (Vector3.Angle(positionNoY, averageNoY) < 90)
                {
                    var closerPosition = Vector3.MoveTowards(positionNoY, averageNoY, Vector3.Distance(positionNoY, averageNoY) - TalkSceneInterp.talkDistance);

                    closerPosition.y = averageV.y + ActionCameraControl.GetPlayerHeight();

                    //VRLog.Warn("Adjusting position {0} -> {1} for rotation {2}", position, closerPosition, rotation.eulerAngles);

                    return closerPosition;
                }
            }

            return position;
        }

        /// <summary>
        /// This should be called every time a set of ADV commands has been executed.
        /// Moves the camera appropriately.
        /// </summary>
        public void HandleTextScenarioProgress(ADV.TextScenario textScenario)
        {
            var isFadingOut = IsFadingOut(textScenario.advFade);

            //VRLog.Debug("HandleTextScenarioProgress isFadingOut={0}", isFadingOut);
            
            // We catch it in TalkSceneInterp as we have visible male all the time.
            //if (_settings.FirstPersonADV &&
            //    FindMaleToImpersonate(out var male) &&
            //    male.objHead != null)
            //{
            //    VRLog.Debug("Maybe impersonating male");
            //    male.StartCoroutine(ImpersonateCo(isFadingOut, male.objHead.transform));
            //}
            if (ShouldApproachCharacter(textScenario, out var character))
            {
#if KK
                var distance = InCafe() ? 0.75f : TalkSceneInterp.talkDistance;
#elif KKS
                var distance = TalkSceneInterp.talkDistance;
#endif
                float height;
                Quaternion rotation;
#if KK
                if (Manager.Scene.Instance.NowSceneNames[0] == "H")
#elif KKS
                if (Manager.Scene.NowSceneNames[0] == "H")
#endif
                {
                    VRLog.Debug("Approaching character (H)");
                    // TODO: find a way to get a proper height.
                    height = character.transform.position.y + 1.3f;
                    rotation = character.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                }
                else
                {
                    VRLog.Debug("Approaching character (non-H)");
                    var originalTarget = ActionCameraControl.GetIdealTransformFor(textScenario.AdvCamera);
                    height = originalTarget.position.y;
                    rotation = originalTarget.rotation;
                }

                var cameraXZ = character.transform.position - rotation * (distance * Vector3.forward);
                MoveWithHeuristics(
                    new Vector3(cameraXZ.x, height, cameraXZ.z),
                    rotation,
                    isFadingOut);
            }
            else
            {
                var target = ActionCameraControl.GetIdealTransformFor(textScenario.AdvCamera);
                var targetPosition = target.position;
                var targetRotation = target.rotation;

                targetPosition = AdjustAdvPosition(textScenario, targetPosition, target.rotation);
#if KKS
                AdjustBasedOnMap(ref targetPosition, ref targetRotation);
#endif
                if (ActionCameraControl.HeadIsAwayFromPosition(targetPosition))
                    MoveWithHeuristics(targetPosition, targetRotation, isFadingOut);
            }
        }
#if KKS
        private void AdjustBasedOnMap(ref Vector3 targetPosition, ref Quaternion targetRotation)
        {
            var insideMyRoom = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GasyukuMyroom";
            if (insideMyRoom)
            {
                var middleOfRoom = new Vector3(1.3f, 1.6f, 1.2f);

                if (Vector3.Distance(targetPosition, middleOfRoom) > 10)
                {
                    targetPosition = middleOfRoom;
                    var middleOfRoomRotation = Quaternion.Euler(0, 180, 0);
                    targetRotation = middleOfRoomRotation;
                }
            }
        }
#endif


        private bool IsFadingOut(ADVFade fade)
        {
#if KK
            static bool IsFadingOutSub(ADVFade.Fade f) => f.initColor.a > 0.5f && !f.IsEnd;
            return IsFadingOutSub(fade.front) || IsFadingOutSub(fade.back);
#else
            // KKS doesn't have working advFade.
            return Scene.sceneFadeCanvas.isFading;
#endif

        }

        //private IEnumerator ImpersonateCo(bool isFadingOut, Transform head)
        //{
        //    // For reasons I don't understand, the male may not have a correct pose
        //    // until later in the update loop.
        //    yield return CoroutineUtils.WaitForEndOfFrame;
        //    MoveWithHeuristics(
        //        head.TransformPoint(0, 0.15f, 0.15f),
        //        head.rotation,
        //        false,
        //        isFadingOut);
        //}

        public void Impersonate(ChaControl chara)
        {
            if (chara != null && chara.objTop.activeSelf)
            {
                // Some inconsistent interference happens.
                chara.visibleAll = true;
                chara.fileStatus.visibleBodyAlways = true;

                var eyes = chara.objHeadBone.transform
                    .Find("cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_tz");
                var position = eyes.TransformPoint(GameSettings.PositionOffset.Value);
                MoveTo(position, eyes.rotation);
            }
        }
        private void MoveWithHeuristics(Vector3 position, Quaternion rotation, bool pretendFading)
        {
#if KK
            var fade = Manager.Scene.Instance.sceneFade;
            bool fadeOk = (fade._Fade == SimpleFade.Fade.Out) ^ fade.IsEnd;
            if (pretendFading || fadeOk || IsDestinationFar(position, rotation))
#elif KKS
            //var fade = Manager.Scene.sceneFadeCanvas;
            if (VRFade.IsFade || IsDestinationFar(position, rotation))  //(pretendFading || IsDestinationFar(position, rotation))

            // No clue what this condition should be about, in KKS it doesn't work (always true).
            // KK has no problem with it('s alternative).
            //var fadeOk = fade.isEnd; //(fade._Fade == SimpleFade.Fade.Out) ^ fade.IsEnd;
#endif
            {
#if DEBUG
                VRPlugin.Logger.LogDebug($"MoveWithHeuristics:Move:Pos = {position}");
#endif
                MoveTo(position, rotation);
            }
#if DEBUG
            else
            {
                VRPlugin.Logger.LogDebug($"MoveWithHeuristics:Stay:Pos = {position}");
            }
#endif
        }

        private bool IsDestinationFar(Vector3 position, Quaternion rotation)
        {
            var distance = (position - _lastPosition).magnitude;
            var deltaAngle = Mathf.Abs(Mathf.DeltaAngle(rotation.eulerAngles.y, _lastRotation.eulerAngles.y));
            return 1f < distance * 0.5f + deltaAngle / 90f;
        }

//        private static bool FindMaleToImpersonate(out ChaControl male)
//        {
//            male = null;

//            if (!Manager.Character.IsInstance()) return false;

//#if KK
//            var males = Manager.Character.Instance.dictEntryChara.Values
//#elif KKS
//            var males = Manager.Character.dictEntryChara.Values
//#endif
//                .Where(ch => ch.isActiveAndEnabled && ch.sex == 0 && ch.objTop?.activeSelf == true && ch.visibleAll)
//                .ToArray();
//            if (males.Length == 1)
//            {
//                male = males[0];
//                return true;
//            }
//            return false;
//        }

        private bool ShouldApproachCharacter(ADV.TextScenario textScenario, out ChaControl control)
        {
#if KK
            if ((Manager.Scene.Instance.NowSceneNames[0] == "H" || textScenario.BGParam.visible) 
#elif KKS
            if ((Manager.Scene.NowSceneNames[0] == "H" || textScenario.BGParam.visible)
#endif
                && textScenario.currentChara != null)
            {
                control = textScenario.currentChara.chaCtrl;
                return true;
            }
            control = null;
            return false;
        }

#if KK
        private bool InCafe()
        {
            return Manager.Game.IsInstance() 
                && Manager.Game.Instance.actScene.transform.Find("cafeChair");
        }
#endif
    }
}
