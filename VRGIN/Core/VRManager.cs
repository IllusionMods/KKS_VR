using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Controls.Speech;
using VRGIN.Modes;
using WindowsInput;

namespace VRGIN.Core
{
    /// <summary>
    /// Helper class that gives you easy access to all crucial objects.
    /// </summary>
    public static class VR
    {
        public static GameInterpreter Interpreter { get { return VRManager.Instance.Interpreter; } }
        public static VRCamera Camera { get { return VRCamera.Instance; } }
        public static VRGUI GUI { get { return VRGUI.Instance; } }
        public static IVRManagerContext Context { get { return VRManager.Instance.Context; } }
        public static ControlMode Mode { get { return VRManager.Instance.Mode; } }
        public static VRSettings Settings { get { return Context.Settings; } }
        public static Shortcuts Shortcuts { get { return Context.Settings.Shortcuts; } }
        public static VRManager Manager { get { return VRManager.Instance; } }
        public static IInputSimulator Input { get { return VRManager.Instance.Input; } }
        //public static SpeechManager Speech { get { return VRManager.Instance.Speech; } }
        public static HMDType HMD { get { return VRManager.Instance.HMD; } }
        public static bool Active { get; set; }
        public static bool Quitting => _Quitting || (_Quitting = VRManager.Instance.Interpreter.ApplicationIsQuitting);
            // Cache the result so that this remains true after the interpreter is destroyed.
        internal static bool _Quitting = false;
    }

    public enum HMDType
    {
        Oculus,
        Vive,
        Other
    }

    public class ModeInitializedEventArgs : EventArgs
    {
        public ControlMode Mode { get; private set; }

        public ModeInitializedEventArgs(ControlMode mode)
        {
            Mode = mode;
        }
    }

    public class VRManager : ProtectedBehaviour
    {
        private VRGUI _Gui;
        private bool _CameraLoaded = false;
        private bool _IsEnabledEffects = false;

        private static VRManager _Instance;
        public static VRManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    throw new InvalidOperationException("VR Manager has not been created yet!");
                }
                return _Instance;
            }
        }

        public IVRManagerContext Context { get; private set; }
        public GameInterpreter Interpreter { get; private set; }
        //public SpeechManager Speech { get; private set; }
        public HMDType HMD { get; private set; }

        public event EventHandler<ModeInitializedEventArgs> ModeInitialized = delegate { };
        private HashSet<Camera> _CheckedCameras = new HashSet<Camera>();

        /// <summary>
        /// Creates the manager with a context and an interpeter.
        /// </summary>
        /// <typeparam name="T">The interpreter that keeps track of actors and cameras, etc.</typeparam>
        /// <param name="context">The context of the game (materials, layers, settings...)</param>
        /// <returns></returns>
        public static VRManager Create<T>(IVRManagerContext context) where T : GameInterpreter
        {
            if (_Instance == null)
            {
                VR.Active = true;

                _Instance = new GameObject("VRGIN_Manager").AddComponent<VRManager>();
                _Instance.Context = context;
                _Instance.Interpreter = _Instance.gameObject.AddComponent<T>();
                // Makes sure that the GUI is instanciated
                _Instance._Gui = VRGUI.Instance;
                _Instance.Input = new InputSimulator();

                //if (VR.Settings.SpeechRecognition)
                //{
                //    _Instance.Speech = _Instance.gameObject.AddComponent<SpeechManager>();
                //}

                if (VR.Settings.ApplyEffects)
                {
                    _Instance.EnableEffects();
                }

                // Save settings so the XML is up-to-date
                VR.Settings.Save();
            }
            return _Instance;
        }

        /// <summary>
        /// Sets the mode the game works in.
        /// 
        /// A mode is required for the VR support to work. Refer to <see cref="SeatedMode"/> and <see cref="StandingMode"/> for
        /// example implementations. It is recommended to extend them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void SetMode<T>() where T : ControlMode
        {
            if (Mode == null || !(Mode is T))
            {
                ModeType = typeof(T);

                // Change!
                if (Mode != null)
                {
                    // Get on clean grounds
                    Mode.ControllersCreated -= OnControllersCreated;
                    DestroyImmediate(Mode);
                }

                Mode = VRCamera.Instance.gameObject.AddComponent<T>();
                Mode.ControllersCreated += OnControllersCreated;
            }
        }

        public ControlMode Mode
        {
            get;
            private set;
        }
        public IInputSimulator Input { get; set; }

        private static Type ModeType;

        protected override void OnAwake()
        {
            var trackingSystem = SteamVR.instance.hmd_TrackingSystemName;
            VRLog.Info("------------------------------------");
            VRLog.Info(" Booting VR [{0}]", trackingSystem);
            VRLog.Info("------------------------------------");
            HMD = trackingSystem == "oculus" ? HMDType.Oculus : trackingSystem == "lighthouse" ? HMDType.Vive : HMDType.Other;

            Application.targetFrameRate = 90;
            //Time.fixedDeltaTime = 1f / 90f;
            Application.runInBackground = true;

            GameObject.DontDestroyOnLoad(SteamVR_Render.instance.gameObject);
            GameObject.DontDestroyOnLoad(gameObject);
#if UNITY_4_5
            SteamVR_Render.instance.helpSeconds = 0;
#endif
        }
        protected override void OnStart()
        {
        }

        private void OnLevelWasLoaded(int level)
        {
            try
            {
                _CheckedCameras.Clear();
            }
            catch(Exception ex)
            {
                VRLog.Error(ex);
            }
            //StartCoroutine(Load());
        }

        // A scratch-pad buffer to keep OnUpdate allocation-free.
        private Camera[] _cameraBuffer = new Camera[0];
        protected override void OnUpdate()
        {
            int numCameras = Camera.allCamerasCount;
            if (_cameraBuffer.Length < numCameras)
            {
                _cameraBuffer = new Camera[numCameras];
            }
            Camera.GetAllCameras(_cameraBuffer);
            for(int i = 0; i < numCameras; i++)
            {
                var camera = _cameraBuffer[i];
                _cameraBuffer[i] = null;
                if (_CheckedCameras.Contains(camera))
                {
                    continue;
                }
                _CheckedCameras.Add(camera);
                var judgement = VR.Interpreter.JudgeCamera(camera);
                VRLog.Info("Detected new camera {0} Action: {1}", camera.name, judgement);
                switch (judgement)
                {
                    case CameraJudgement.MainCamera:
                        VR.Camera.Copy(camera, true);
                        if (_IsEnabledEffects) { ApplyEffects(); }
                        break;
                    case CameraJudgement.SubCamera:
                        VR.Camera.Copy(camera, false);
                        break;
                    case CameraJudgement.GUI:
                        VR.GUI.AddCamera(camera);
                        break;
                    case CameraJudgement.GUIAndCamera:
                        VR.Camera.Copy(camera, false, true);
                        VR.GUI.AddCamera(camera);
                        break;
                    case CameraJudgement.Ignore:
                        break;
                }
            }
        }

        private void OnControllersCreated(object sender, EventArgs e)
        {
            ModeInitialized(this, new ModeInitializedEventArgs(Mode));
        }

        private void OnDisable()
        {
            VR._Quitting = true;
        }

        public void EnableEffects()
        {
            _IsEnabledEffects = true;
            if (VR.Camera.Blueprint) { ApplyEffects(); }
        }

        public void DisableEffects()
        {
            _IsEnabledEffects = false;
        }

        public void ToggleEffects()
        {
            if (_IsEnabledEffects)
            {
                DisableEffects();
            }
            else
            {
                EnableEffects();
            }
        }

        private void ApplyEffects()
        {
            VR.Camera.CopyFX(VR.Camera.Blueprint);
        }
    }
}
