using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;
using VRGIN.U46.Visuals;
using VRGIN.Visuals;
using static SteamVR_Controller;

namespace VRGIN.Controls.Tools
{
    public class WarpTool : Tool
    {

        private enum WarpState
        {
            None,
            Rotating,
            Transforming,
            Grabbing
        }


        ArcRenderer ArcRenderer;
        PlayAreaVisualization _Visualization;
        private PlayArea _ProspectedPlayArea = new PlayArea();
        private const float SCALE_THRESHOLD = 0.05f;
        private const float TRANSLATE_THRESHOLD = 0.05f;

        /// <summary>
        /// Gets or sets what the user can do by touching the thumbpad
        /// </summary>
        private WarpState State = WarpState.None;

        private Vector3 _PrevPoint;
        private float? _TriggerDownTime = null;
        bool Showing = false;

        private List<Vector2> _Points = new List<Vector2>();

        private const float EXACT_IMPERSONATION_TIME = 1;
        private Controller.Lock _SelfLock = Controls.Controller.Lock.Invalid;
        private float _IPDOnStart;

        private GrabAction _Grab;

        public override Texture2D Image
        {
            get
            {
                return UnityHelper.LoadImage("icon_warp.png");
            }
        }


        protected override void OnAwake()
        {
            VRLog.Info("Awake!");
            ArcRenderer = new GameObject("Arc Renderer").AddComponent<ArcRenderer>();
            ArcRenderer.transform.SetParent(transform, false);
            ArcRenderer.gameObject.SetActive(false);

            // -- Create indicator

            _Visualization = PlayAreaVisualization.Create(_ProspectedPlayArea);
            DontDestroyOnLoad(_Visualization.gameObject);

            SetVisibility(false);
        }

        private void OnDestroy()
        {
            if (VR.Quitting)
            {
                return;
            }
            VRLog.Info("Destroy!");

            DestroyImmediate(_Visualization.gameObject);
        }

        protected override void OnStart()
        {
            VRLog.Info("Start!");

            base.OnStart();
            _IPDOnStart = VR.Settings.IPDScale;
            ResetPlayArea(_ProspectedPlayArea);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetVisibility(false);
            ResetPlayArea(_ProspectedPlayArea);
        }

        public void OnPlayAreaUpdated()
        {
            ResetPlayArea(_ProspectedPlayArea);
        }

        void SetVisibility(bool visible)
        {
            Showing = visible;

            if (visible)
            {
                ArcRenderer.Update();
                UpdateProspectedArea();
                _Visualization.UpdatePosition();
            }

            ArcRenderer.gameObject.SetActive(visible);
            _Visualization.gameObject.SetActive(visible);
        }

        private void ResetPlayArea(PlayArea area)
        {
            area.Position = VR.Camera.SteamCam.origin.position;
            area.Scale = VR.Settings.IPDScale;
            area.Rotation = VR.Camera.SteamCam.origin.rotation.eulerAngles.y;
        }

        protected override void OnDisable()
        {
            if (VR.Quitting)
            {
                return;
            }
            base.OnDisable();

            EnterState(WarpState.None);
            SetVisibility(false);
        }

        protected override void OnLateUpdate()
        {
            if (Showing)
            {
                UpdateProspectedArea();
            }
        }

        private void UpdateProspectedArea()
        {
            ArcRenderer.Offset = _ProspectedPlayArea.Height;
            ArcRenderer.Scale = VR.Settings.IPDScale;
            if (ArcRenderer.Target is Vector3 target)
            {
                _ProspectedPlayArea.Position = new Vector3(target.x, _ProspectedPlayArea.Position.y, target.z);
            }
        }

        private void CheckRotationalPress()
        {
            if (Controller.GetPressDown(ButtonMask.Touchpad))
            {
                var v = Controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
                _ProspectedPlayArea.Reset();
                if (v.x < -0.2f)
                {
                    _ProspectedPlayArea.Rotation -= 20f;
                }
                else if (v.x > 0.2f)
                {
                    _ProspectedPlayArea.Rotation += 20f;
                }
                _ProspectedPlayArea.Apply();
            }
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (State == WarpState.None)
            {
                var v = Controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
                if (v.magnitude < 0.5f)
                {
                    if (Controller.GetTouchDown(EVRButtonId.k_EButton_SteamVR_Touchpad) /*||Controller.GetTouch(EVRButtonId.k_EButton_SteamVR_Touchpad)*/)
                    {
                        EnterState(WarpState.Rotating);
                    }
                }
                else
                {
                    CheckRotationalPress();
                }

                if (Controller.GetPressDown(ButtonMask.Grip))
                {
                    EnterState(WarpState.Grabbing);
                }
            }
            if (State == WarpState.Grabbing)
            {
                switch (_Grab.HandleGrabbing())
                {
                    case GrabAction.Status.Continue:
                        break;
                    case GrabAction.Status.DoneQuick:
                        EnterState(WarpState.None);
                        Owner.StartRumble(new RumbleImpulse(800));
                        _ProspectedPlayArea.Height = 0;
                        _ProspectedPlayArea.Scale = _IPDOnStart;
                        break;
                    case GrabAction.Status.DoneSlow:
                        EnterState(WarpState.None);
                        ResetPlayArea(_ProspectedPlayArea);
                        break;
                }
            }


            if (State == WarpState.Rotating)
            {
                HandleRotation();
            }

            if (State == WarpState.Transforming)
            {
                if (Controller.GetPressUp(EVRButtonId.k_EButton_Axis0))
                {
                    // Warp!
                    _ProspectedPlayArea.Apply();

                    // The preview head has to move away
                    ArcRenderer.Update();

                    EnterState(WarpState.Rotating);
                }
            }

            if (State == WarpState.None)
            {
                if (Controller.GetHairTriggerDown())
                {
                    _TriggerDownTime = Time.unscaledTime;
                }
                if (_TriggerDownTime != null)
                {
                    if (Controller.GetHairTrigger() && (Time.unscaledTime - _TriggerDownTime) > EXACT_IMPERSONATION_TIME)
                    {
                        VRManager.Instance.Mode.Impersonate(VR.Interpreter.FindNextActorToImpersonate(),
                            ImpersonationMode.Exactly);
                        _TriggerDownTime = null;
                    }
                    if (VRManager.Instance.Interpreter.Actors.Any() && Controller.GetHairTriggerUp())
                    {
                        VRManager.Instance.Mode.Impersonate(VR.Interpreter.FindNextActorToImpersonate(),
                            ImpersonationMode.Approximately);
                    }
                }
            }
        }

        private void HandleRotation()
        {
            if (Showing)
            {
                _Points.Add(Controller.GetAxis(EVRButtonId.k_EButton_Axis0));

                if (_Points.Count > 2)
                {
                    DetectCircle();
                }
            }

            if (Controller.GetPressDown(EVRButtonId.k_EButton_Axis0))
            {
                EnterState(WarpState.Transforming);
            }

            if (Controller.GetTouchUp(EVRButtonId.k_EButton_Axis0))
            {
                EnterState(WarpState.None);
            }
        }

        private float NormalizeAngle(float angle)
        {
            return angle % 360f;
        }

        private void DetectCircle()
        {

            float? minDist = null;
            float? maxDist = null;
            float avgDist = 0;

            // evaulate points to determine center
            foreach (var point in _Points)
            {
                float dist = point.magnitude;
                minDist = Math.Max(minDist ?? dist, dist);
                maxDist = Math.Max(maxDist ?? dist, dist);
                avgDist += dist;
            }
            avgDist /= _Points.Count;

            if (maxDist - minDist < 0.2f && minDist > 0.2f)
            {
                float startAngle = Mathf.Atan2(_Points.First().y, _Points.First().x) * Mathf.Rad2Deg;
                float endAngle = Mathf.Atan2(_Points.Last().y, _Points.Last().x) * Mathf.Rad2Deg;
                float rot = (endAngle - startAngle);
                if (Mathf.Abs(rot) < 60)
                {
                    _ProspectedPlayArea.Rotation -= rot;
                    //Logger.Info("Detected circular movement. Total: {0}", _AdditionalRotation);
                }
                else
                {
                    VRLog.Info("Discarding too large rotation: {0}", rot);
                }
            }
            _Points.Clear();
        }

        private void EnterState(WarpState state)
        {
            // LEAVE state
            switch (State)
            {
                case WarpState.None:
                    _SelfLock = Owner.AcquireFocus(keepTool: true);
                    break;
                case WarpState.Rotating:

                    break;

                case WarpState.Grabbing:
                    _Grab.Destroy();
                    _Grab = null;
                    break;
            }


            // ENTER state
            switch (state)
            {
                case WarpState.None:
                    SetVisibility(false);
                    if (_SelfLock.IsValid)
                    {
                        _SelfLock.Release();
                    }
                    break;
                case WarpState.Rotating:
                    SetVisibility(true);
                    Reset();
                    break;
                case WarpState.Grabbing:
                    _Grab = new GrabAction(Owner, Controller, ButtonMask.Grip);
                    break;
            }

            State = state;
        }

        private void Reset()
        {
            _Points.Clear();
        }

        //public override List<HelpText> GetHelpTexts()
        //{
        //    return new List<HelpText>(new HelpText[] {
        //        HelpText.Create("Press to teleport", FindAttachPosition("trackpad"), new Vector3(0, 0.02f, 0.05f)),
        //        HelpText.Create("Circle to rotate", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0), new Vector3(0.015f, 0, 0)),
        //        HelpText.Create("press & move controller", FindAttachPosition("trackpad"), new Vector3(-0.05f, 0.02f, 0), new Vector3(-0.015f, 0, 0)),
        //        HelpText.Create("Warp into main char", FindAttachPosition("trigger"), new Vector3(0.06f, 0.04f, -0.05f)),
        //        HelpText.Create("reset area", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0.0f, -0.05f))
        //    });
        //}
    }
}
