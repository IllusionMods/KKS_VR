using UnityEngine;
using VRGIN.Core;
using System.Collections;
using UnityEngine.SceneManagement;
using KK_VR.Features;
using KK_VR.Camera;
using Studio;
using static KK_VR.Interpreters.KoikatuInterpreter;
using System.Runtime.Remoting;
using RootMotion;
using System.Collections.Generic;
using KK_VR.Handlers;
using VRGIN.Controls;
using KK_VR.Settings;
using KK_VR.Holders;

namespace KK_VR.Interpreters
{
    class KoikatuInterpreter : GameInterpreter
    {
        public enum SceneType
        {
            None,
            OtherScene,
            ActionScene,
            TalkScene,
            HScene
            //NightMenuScene,
            //CustomScene
        }
        public static SceneType CurrentScene => _currentScene;
        public static SceneInterpreter SceneInterpreter => _sceneInterpreter;
        public static KoikatuSettings Settings => _settings;

        private static SceneType _currentScene;
        private static SceneInterpreter _sceneInterpreter;
        private static KoikatuSettings _settings;

        private KK_VR.Fixes.Mirror.Manager _mirrorManager;
        private int _kkapiCanvasHackWait;
        private Canvas _kkSubtitlesCaption;
        private bool _hands;
        protected override void OnAwake()
        {
            _currentScene = SceneType.OtherScene;
            _sceneInterpreter = new OtherSceneInterpreter();
            SceneManager.sceneLoaded += OnSceneLoaded;
            _mirrorManager = new KK_VR.Fixes.Mirror.Manager();
            VR.Camera.gameObject.AddComponent<VREffector>();
            VR.Camera.gameObject.AddComponent<SmoothMover>();
            //VR.Manager.ModeInitialized += AddModels;
            _settings = VR.Context.Settings as KoikatuSettings;
            Features.LoadVoice.Init();
            IntegrationSensibleH.Init();
        }
        protected override void OnUpdate()
        {
            UpdateScene();
            _sceneInterpreter.OnUpdate();
        }
        protected override void OnLateUpdate()
        {
            //if (_kkSubtitlesCaption != null)
            //{
            //    FixupKkSubtitles();
            //}
            _sceneInterpreter.OnLateUpdate();
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //VRLog.Info($"OnSceneLoaded {scene.name}");
            if (!_hands && scene.name.Equals("Title"))
            {
                // Init too early will throw a wrench into VRGIN's init.
                CreateHands();
            }
#if KK
            // Change collision layers for action scene instead.
            var map = GameObject.Find("Map");
            if (map != null)
            {
                foreach (Transform child in map.transform)
                {
                    if (child.name.StartsWith("mo_koi_sky_", System.StringComparison.Ordinal))
                    {
                        if (child.GetComponent<Collider>() != null)
                            Destroy(child.GetComponent<Collider>());
                    }
                }
            }
            
            // KKS requires special care, it starts to show ?floor_collider?
            // Is it even broken in KKS? seems to work fine.
            foreach (var reflection in GameObject.FindObjectsOfType<MirrorReflection>())
            {
                _mirrorManager.Fix(reflection);
            }
#endif
        }

        private void CreateHands()
        {
            _hands = true;
            var left = new GameObject("handL")
            {
                layer = 10
            };
            left.AddComponent<HandHolder>().Init(0);

            var right = new GameObject("handR")
            {
                layer = 10
            };
            right.AddComponent<HandHolder>().Init(1);
        }

        // PR was merged long time ago in KK_Subtitles.
        ///// <summary>
        ///// Fix up scaling of subtitles added by KK_Subtitles. See
        ///// https://github.com/IllusionMods/KK_Plugins/pull/91 for details.
        ///// </summary>
        //private void FixupKkSubtitles()
        //{
        //    foreach (Transform child in _kkSubtitlesCaption.transform)
        //    {
        //        if (child.localScale != Vector3.one)
        //        {
        //            VRLog.Info($"Fixing up scale for {child}");
        //            child.localScale = Vector3.one;
        //        }
        //    }
        //}

        public override bool IsIgnoredCanvas(Canvas canvas)
        {
            if (PrivacyScreen.IsOwnedCanvas(canvas))
            {
                return true;
            }
            else if (canvas.name == "Canvas_BackGround")
            {
                BackgroundDisplayer.Instance.TakeCanvas(canvas);
                return true;
            }
            else if (canvas.name == "CvsMenuTree")
            {
                // Here, we attempt to avoid some unfortunate conflict with
                // KKAPI.
                //
                // In order to support plugin-defined subcategories in Maker,
                // KKAPI clones some UI elements out of CvsMenuTree when the
                // canvas is created, then uses them as templates for custom
                // UI items.
                //
                // At the same time, VRGIN attempts to claim the canvas by
                // setting its mode to ScreenSpaceCamera, which changes
                // localScale of the canvas by a factor of 100 or so. If this
                // happens between KKAPI's cloning out and cloning in, the
                // resulting UI items will have the wrong scale, 72x the correct
                // size to be precise.
                //
                // So our solution here is to hide the canvas from VRGIN for a
                // couple of frames. Crude but works.

                if (_kkapiCanvasHackWait == 0)
                {
                    _kkapiCanvasHackWait = 3;
                    return true;
                }
                else
                {
                    _kkapiCanvasHackWait -= 1;
                    return 0 < _kkapiCanvasHackWait;
                }
            }
            else if (canvas.name == "KK_Subtitles_Caption")
            {
                _kkSubtitlesCaption = canvas;
            }

            return false;
        }
        public static bool StartScene(SceneType type, MonoBehaviour behaviour = null)
        {
            if (_currentScene != type)
            {
               //VRPlugin.Logger.LogDebug($"Interpreter:Start:{type}");
                _currentScene = type;
                _sceneInterpreter.OnDisable();
                _sceneInterpreter = CreateSceneInterpreter(type, behaviour);
                _sceneInterpreter.OnStart();
                return true;
            }
            else
            {
               //VRPlugin.Logger.LogDebug($"Interpreter:AlreadyExists:{type}");
                return false;
            }
        }
        public static void EndScene(SceneType type)
        {
            if (_currentScene == type)
            {
               //VRPlugin.Logger.LogDebug($"Interpreter:EndScene:{type}");
                StartScene(SceneType.OtherScene);
            }
            else
            {
               //VRPlugin.Logger.LogDebug($"Interpreter:EndScene:WrongScene:Current = {CurrentScene}, required = {type}");
            }
        }

        // 前回とSceneが変わっていれば切り替え処理をする
        private void UpdateScene()
        {
            if (_currentScene < SceneType.TalkScene)
            {
                var sceneType = DetectScene();
                if (_currentScene != sceneType)
                {
                    EndScene(_currentScene);
                    StartScene(sceneType);
                }
            }
        }

        private SceneType DetectScene()
        {
#if KK
            if (Manager.Game.Instance.actScene != null)
            {
                if (Manager.Game.Instance.actScene.AdvScene.isActiveAndEnabled)
#else
            if (ActionScene.instance != null)
            {
                if (ActionScene.instance.AdvScene.isActiveAndEnabled)
#endif
                {
                    return SceneType.TalkScene;
                }
                //if (_scene.NowSceneNames.Contains("NightMenuScene"))
                //{
                //    return SceneType.NightMenuScene;
                //}
                return SceneType.ActionScene;
            }
            return SceneType.OtherScene;
        }

        //private bool SceneObjPresent(string name)
        //{
        //    if (_sceneObjCache != null && _sceneObjCache.name == name)
        //    {
        //        return true;
        //    }
        //    var obj = GameObject.Find(name);
        //    if (obj != null)
        //    {
        //        _sceneObjCache = obj;
        //        return true;
        //    }
        //    return false;
        //}

        private static SceneInterpreter CreateSceneInterpreter(SceneType type, MonoBehaviour behaviour)
        {
            return type switch
            {
                SceneType.ActionScene => new ActionSceneInterpreter(),
                //case SceneType.CustomScene:
                //    return new CustomSceneInterpreter();
                //case SceneType.NightMenuScene:
                //    return new NightMenuSceneInterpreter();
                SceneType.HScene => new HSceneInterpreter(behaviour),
                SceneType.TalkScene => new TalkSceneInterpreter(behaviour),
                _ => new OtherSceneInterpreter(),
            };
        }

        protected override CameraJudgement JudgeCameraInternal(UnityEngine.Camera camera)
        {
            if (camera.CompareTag("MainCamera"))
            {
                StartCoroutine(HandleMainCameraCo(camera));
            }
            return base.JudgeCameraInternal(camera);
        }

        /// <summary>
        /// A coroutine to be called when a new main camera is detected.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        private IEnumerator HandleMainCameraCo(UnityEngine.Camera camera)
        {
            // Unity might have messed with the camera transform for this frame,
            // so we wait for the next frame to get clean data.
            yield return null;

            if (camera.name == "ActionCamera" || camera.name == "FrontCamera")
            {
                VRLog.Info("Adding ActionCameraControl");
                camera.gameObject.AddComponent<Camera.ActionCameraControl>();
            }
            else if (camera.GetComponent<CameraControl_Ver2>() != null)
            {
                VRLog.Info("New main camera detected: moving to {0} {1}", camera.transform.position, camera.transform.eulerAngles);
                Camera.VRCameraMover.Instance.MoveTo(camera.transform.position, camera.transform.rotation);
                VRLog.Info("moved to {0} {1}", VR.Camera.Head.position, VR.Camera.Head.eulerAngles);
                VRLog.Info("Adding CameraControlControl");
                camera.gameObject.AddComponent<Camera.CameraControlControl>();
            }
            else
            {
                VRLog.Warn($"Unknown kind of main camera was added: {camera.name}");
            }
        }

        //public override bool ApplicationIsQuitting => Manager.Scene.isGameEnd;


        private const float _targetFps = 1f / 45f;
        // As my potater can't into 90fps, I limit it to half and use frame interpolation to compensate.
        // Thus all calculations are done with 45fps in mind.
        internal static int ScaleWithFps(int number)
        {
            return Mathf.FloorToInt(_targetFps / Time.deltaTime * number);
        }
    }
}
