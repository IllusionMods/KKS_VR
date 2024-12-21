using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRGIN.Core;

namespace KK_VR.Interpreters
{
    internal class KKSCharaStudioInterpreter : GameInterpreter
    {
        private readonly List<KKSCharaStudioActor> _Actors = new List<KKSCharaStudioActor>();
        private GameObject CommonSpaceGo;
        public override IEnumerable<IActor> Actors => _Actors.Cast<IActor>();

        protected override void OnAwake()
        {
            base.OnAwake();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (!CommonSpaceGo) CommonSpaceGo = Manager.Scene.commonSpace;
            FixMenuCanvasLayers();
        }

        protected override void OnStart()
        {
            base.OnStart();
            FixMenuCanvasLayers();
        }

        public override UnityEngine.Camera FindCamera()
        {
            return null;
        }

        public override IActor FindNextActorToImpersonate()
        {
            var list = Actors.ToList();
            var actor = FindImpersonatedActor();
            if (actor == null) return list.FirstOrDefault();
            return list[(list.IndexOf(actor) + 1) % list.Count];
        }

        protected override void OnUpdate()
        {
            try
            {
                if ((bool)VR.Manager)
                {
                    RefreshActors();
                    UpdateMainCameraCullingMask();
                    SyncVRCameraSkybox();
                    FixMissingMode();
                }
            }
            catch (Exception e)
            {
                VRLog.Error(e);
            }
        }

        private void FixMissingMode()
        {
            if (VR.Mode == null)
            {
                VRLog.Error("VR.Mode is missing. Force reload as Standing Mode.");
                ForceResetAsStandingMode();
            }
        }

        private void SyncVRCameraSkybox()
        {
            if ((bool)VR.Camera) VR.Camera.SyncSkybox();
        }

        private void UpdateMainCameraCullingMask()
        {
            var component = VR.Camera.SteamCam.GetComponent<UnityEngine.Camera>();
            var list = new List<string>();
            var obj = new List<string> { "Studio/Col", "Studio/Select" };

            if (Singleton<Studio.Studio>.Instance.workInfo.visibleAxis)
            {
                if (Studio.Studio.optionSystem.selectedState == 0) list.Add("Studio/Col");
                list.Add("Studio/Select");
            }

            var mask = LayerMask.GetMask(list.ToArray());
            var mask2 = LayerMask.GetMask(obj.ToArray());
            component.cullingMask &= ~mask2;
            component.cullingMask |= mask;
        }

        private void RefreshActors()
        {
            _Actors.Clear();

            foreach (var value in Character.dictEntryChara.Values)
                if ((bool)value.objBodyBone)
                    AddActor(DefaultActorBehaviour<ChaControl>.Create<KKSCharaStudioActor>(value));
        }

        private void AddActor(KKSCharaStudioActor actor)
        {
            if (!actor.Eyes)
                actor.Head.Reinitialize();
            else
                _Actors.Add(actor);
        }

        public void ForceResetVRMode()
        {
            StartCoroutine(ForceResetVRModeCo());
        }

        private IEnumerator ForceResetVRModeCo()
        {
            VRPlugin.Logger.Log(LogLevel.Debug, "Check and reset to StandingMode if not.");
            
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            if (!VRManager.Instance.Mode)
            {
                VRPlugin.Logger.Log(LogLevel.Debug, "Mode is not StandingMode. Force reset as Standing Mode.");
                ForceResetAsStandingMode();
            }
            else
            {
                VRPlugin.Logger.Log(LogLevel.Debug, "Is Standing Mode. Skip to setting force.");
            }
        }

        public static void DisableDefaultAudioListener()
        {
            if (SingletonInitializer<Manager.Sound>.instance != null)
            {
                var componentInChildren = SingletonInitializer<Manager.Sound>.instance.transform.GetComponentInChildren<AudioListener>(true);
                if (componentInChildren != null) componentInChildren.enabled = false;
            }
        }

        public static void ForceResetAsStandingMode()
        {
            try
            {
                VR.Manager.SetMode<StudioStandingMode>();
                if ((bool)VR.Camera)
                {
                    var mainCmaera = Singleton<Studio.Studio>.Instance.cameraCtrl.mainCmaera;
                    VRPlugin.Logger.Log(LogLevel.Debug, $"Force replace blueprint camera with {mainCmaera}");
                    var camera = VR.Camera.SteamCam.camera;
                    var camera2 = mainCmaera;
                    camera.nearClipPlane = VR.Context.NearClipPlane;
                    camera.farClipPlane = Mathf.Max(camera2.farClipPlane, 10f);
                    camera.clearFlags = camera2.clearFlags == CameraClearFlags.Skybox ? CameraClearFlags.Skybox : CameraClearFlags.Color;
                    camera.renderingPath = camera2.renderingPath;
                    camera.clearStencilAfterLightingPass = camera2.clearStencilAfterLightingPass;
                    camera.depthTextureMode = camera2.depthTextureMode;
                    camera.layerCullDistances = camera2.layerCullDistances;
                    camera.layerCullSpherical = camera2.layerCullSpherical;
                    camera.useOcclusionCulling = camera2.useOcclusionCulling;
                    camera.allowHDR = camera2.allowHDR;
                    camera.backgroundColor = camera2.backgroundColor;

                    var component = camera2.GetComponent<Skybox>();
                    if (component != null)
                    {
                        var skybox = camera.gameObject.GetComponent<Skybox>();
                        if (skybox == null) skybox = skybox.gameObject.AddComponent<Skybox>();
                        skybox.material = component.material;
                    }

                    VR.Camera.CopyFX(camera2);
                    var component2 = VR.Camera.gameObject.GetComponent<AmplifyColorEffect>();
                    if ((bool)component2) component2.enabled = true;
                }
                else
                {
                    VRPlugin.Logger.Log(LogLevel.Debug, "VR.Camera is null");
                }
            }
            catch (Exception e)
            {
                VRLog.Error(e);
            }
        }

        public void FixMenuCanvasLayers()
        {
            foreach (var item in ((IDictionary)typeof(VRGUI).GetField("_Registry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(VRGUI.Instance)).Keys as
                ICollection<Canvas>)
                if (!item.transform.IsChildOf(VR.Camera.Origin.transform) && !IsIgnoredCanvas(item))
                {
                    var componentsInChildren = item.GetComponentsInChildren<Transform>(true);
                    for (var i = 0; i < componentsInChildren.Length; i++) componentsInChildren[i].gameObject.layer = LayerMask.NameToLayer(VR.Context.UILayer);
                }
        }

        public override bool IsIgnoredCanvas(Canvas canvas)
        {
            if ((bool)CommonSpaceGo && canvas.transform.IsChildOf(CommonSpaceGo.transform)) return true;
            return base.IsIgnoredCanvas(canvas);
        }
    }
}
