using UnityEngine;
using VRGIN.Core;
using StrayTech;
using KK_VR.Settings;
using KK_VR.Features;
using KK_VR.Camera;
using Manager;
using VRGIN.Controls;
using Valve.VR;
using KK_VR.Handlers;
using static VRGIN.Controls.Controller;
using System.Collections.Generic;
using KK_VR.Controls;
using ADV.Commands.Object;
using WindowsInput.Native;
using KK_VR.Holders;
using UnityEngine.SceneManagement;

namespace KK_VR.Interpreters
{
    internal class ActionSceneInterpreter : SceneInterpreter
    {
        // Roaming is in a sorry state, waiting for the VRIK to rework it.
        // But VRIK for this requires custom animations,
        // which I yet to figure out how to retarget.

        internal static ActionScene actionScene;

        internal static Transform FakeCamera;
        private GameObject _map;
        private GameObject _cameraSystem;
        private bool _resetCamera;
        private float _originAngle;


        internal override void OnStart()
        {
#if KK
            actionScene = Game.Instance.actScene;
#else
            actionScene = ActionScene.instance;
#endif
            HandHolder.SetKinematic(true);

            _resetCamera = true;
            //ResetCamera();
            //ResetState();
            DisableCameraSystem();
            if (_settings.ShadowsOptimization == KoikatuSettings.ShadowType.Auto)
            {
                KoikatuInterpreter.TweakShadowSettings(KoikatuSettings.ShadowType.Average);
            }
            base.OnStart();
        }

        internal override void OnDisable()
        {
            VRLog.Info("ActionScene OnDisable");

            HandHolder.SetKinematic(false);
            //ResetState();
            EnableCameraSystem();
        }
        internal override void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            _resetCamera = true;
        }
        internal override void OnUpdate()
        {
            if (_resetCamera)
            {
                ResetCamera();
            }
        }
        //internal override void OnUpdate()
        //{
        //    var map = actionScene.Map.mapRoot?.gameObject;

        //    if (map != _map)
        //    {
        //        ResetCamera();

        //        VRLog.Info("! map changed.");

        //        //ResetState();
        //        _map = map;
        //        //_resetCamera = true;
        //    }
        //    //if (_resetCamera)
        //    //{
        //    //}
        //    base.OnUpdate();
        //}
        private void CreateFakeCamera()
        {
            if (FakeCamera == null)
            {
                FakeCamera = new GameObject("FakeCamera").transform;
                FakeCamera.SetParent(MonoBehaviourSingleton<CameraSystem>.Instance.CurrentCamera.transform, worldPositionStays: false);
            }
        }


        //private void ResetState()
        //{
        //    VRLog.Info("ActionScene ResetState");

        //    //_sceneInput.StandUp();
        //    //_sceneInput.StopWalking();
        //    //_resetCamera = false;
        //}

        private void ResetCamera()
        {

            if (actionScene.Player.chaCtrl != null 
                && actionScene.Player.chaCtrl.objTop != null 
                && actionScene.Player.chaCtrl.objTop.activeSelf)
            {
                _cameraSystem = MonoBehaviourSingleton<CameraSystem>.Instance.gameObject;

                // トイレなどでFPS視点になっている場合にTPS視点に戻す
                _cameraSystem.GetComponent<ActionGame.CameraStateDefinitionChange>().ModeChangeForce((ActionGame.CameraMode?)ActionGame.CameraMode.TPS, true);
                //scene.GetComponent<ActionScene>().isCursorLock = false;

                // カメラをプレイヤーの位置に移動
                ((ActionSceneInput)KoikatuInterpreter.SceneInput).ResetState();
                ((ActionSceneInput)KoikatuInterpreter.SceneInput).CameraToPlayer();

                // Something interferes rarely.
                actionScene.Player.chaCtrl.visibleAll = true;

                _resetCamera = false;
                VRLog.Info("ResetCamera succeeded");
            }
#if KKS
            // KKS swaps VFX all the time gotta keep up, KK doesn't seem like.
            VREffector.Refresh();
#endif

        }
        private void DisableCameraSystem()
        {
            //VRLog.Info("ActionScene HoldCamera");

            _cameraSystem = MonoBehaviourSingleton<CameraSystem>.Instance.gameObject;

            if (_cameraSystem != null)
            {
                _cameraSystem.SetActive(false);

                //VRLog.Info("succeeded");
            }
        }

        private void EnableCameraSystem()
        {
            //VRLog.Info("ActionScene ReleaseCamera");

            if (_cameraSystem != null)
            {
                _cameraSystem.SetActive(true);

                //VRLog.Info("succeeded");
            }
        }








    }
}
