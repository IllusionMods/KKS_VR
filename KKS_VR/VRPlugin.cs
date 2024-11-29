using System;
using System.Collections;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using KK_VR.Features;
using KK_VR.Fixes;
using KK_VR.Interpreters;
using KK_VR.Settings;
using Unity.XR.OpenVR;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KK_VR
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInIncompatibility("bero.crossfadervr")]
    public class VRPlugin : BaseUnityPlugin
    {
        public const string GUID = "kks.vr.game";
        public const string Name = "KKS Main Game VR";
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            VRPlugin.Logger.LogDebug($"VRPlugin:Awake");

            var vrActivated = Environment.CommandLine.Contains("--vr");
            bool vrDeactivated = Environment.CommandLine.Contains("--novr");

            bool enabled = vrActivated || SteamVRDetector.IsRunning;
            var settings = SettingsManager.Create(Config);
            if (enabled)
            {
                BepInExVrLogBackend.ApplyYourself();


                // This thingy is single handedly responsible why KKS_VR was an absolute fuck. 
                //OpenVRHelperTempfixHook.Patch();

                StartCoroutine(LoadDevice(settings));
            }

            CrossFader.Initialize(Config, enabled);
        }

        private IEnumerator LoadDevice(KoikatuSettings settings)
        {
            yield return new WaitUntil(() => Manager.Scene.initialized);
            //yield return new WaitUntil(() => Manager.Scene.initialized && Manager.Scene.LoadSceneName == "Title");
            Logger.LogInfo("Loading OpenVR...");

            var ovrsettings = OpenVRSettings.GetSettings(true);
            ovrsettings.StereoRenderingMode = OpenVRSettings.StereoRenderingModes.MultiPass;
            ovrsettings.InitializationType = OpenVRSettings.InitializationTypes.Scene;
            ovrsettings.EditorAppKey = "kss.maingame.exe";
            var instance = SteamVR_Settings.instance;
            instance.autoEnableVR = true;
            instance.editorAppKey = "kss.maingame.exe";
            var openVRLoader = ScriptableObject.CreateInstance<OpenVRLoader>();
            if (!openVRLoader.Initialize())
            {
                Logger.LogInfo("Failed to Initialize OpenVR.");
                yield break;
            }

            if (!openVRLoader.Start())
            {
                Logger.LogInfo("Failed to Start OpenVR.");
                yield break;
            }

            Logger.LogInfo("Initializing SteamVR...");

            try
            {
                SteamVR_Behaviour.Initialize(false);
            }
            catch (Exception data)
            {
                Logger.LogError(data);
            }

            while (true)
            {
                var initializedState = SteamVR.initializedState;
                switch (initializedState)
                {
                    case SteamVR.InitializedStates.Initializing:
                        yield return new WaitForSeconds(0.1f);
                        continue;
                    case SteamVR.InitializedStates.InitializeSuccess:
                        break;
                    case SteamVR.InitializedStates.InitializeFailure:
                        Logger.LogInfo("Failed to initialize SteamVR.");
                        yield break;
                    default:
                        Logger.LogInfo($"Unknown SteamVR initializeState {initializedState}.");
                        yield break;
                }

                break;
            }

            Logger.LogInfo("Initializing the plugin...");

            new Harmony(GUID).PatchAll(typeof(VRPlugin).Assembly);
            //TopmostToolIcons.Patch();

            VRManager.Create<Interpreters.KoikatuInterpreter>(new KoikatuContext(settings));

            // VRGIN doesn't update the near clip plane until a first "main" camera is created, so we set it here.
            UpdateNearClipPlane(settings);
            UpdateIPD(settings);
            settings.AddListener("NearClipPlane", (_, _1) => UpdateNearClipPlane(settings));
            settings.AddListener("IPDScale", (_, _1) => UpdateIPD(settings));
            VR.Manager.SetMode<GameStandingMode>();


            VRFade.Create();
            PrivacyScreen.Initialize();
            GraphicRaycasterPatches.Initialize();

            // It's been reported in #28 that the game window defocues when
            // the game is under heavy load. We disable window ghosting in
            // an attempt to counter this.
            NativeMethods.DisableProcessWindowsGhosting();

            TweakShadowSettings();

            DontDestroyOnLoad(VRCamera.Instance.gameObject);

            // Probably unnecessary, but just to be safe
            VR.Mode.MoveToPosition(Vector3.zero, Quaternion.Euler(Vector3.zero), true);

            Logger.LogInfo("Finished loading into VR mode!");

            if (SettingsManager.EnableBoop.Value)
                VRBoop.Initialize();
            GameAPI.RegisterExtraBehaviour<InterpreterHooks>(GUID);
        }

        private void UpdateNearClipPlane(KoikatuSettings settings)
        {
            VR.Camera.gameObject.GetComponent<UnityEngine.Camera>().nearClipPlane = settings.NearClipPlane;
        }
        private void UpdateIPD(KoikatuSettings settings)
        {
            VRCamera.Instance.SteamCam.origin.localScale = Vector3.one * settings.IPDScale;
        }
        private static void TweakShadowSettings()
        {
            // Grab "KKS_BetterShadowQualitySettings.dll" from HongFire patch, and configure it to own taste.
            // Otherwise my take on "looks fine". Results should vary depending on the VR hardware btw.

            // Default shadows look too wobbly in VR.
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowCascade2Split = 0.33f;
            QualitySettings.shadowCascade4Split = new Vector4(0.025f, 0.085f, 0.25f);
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 20;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern void DisableProcessWindowsGhosting();
        }
    }
}
