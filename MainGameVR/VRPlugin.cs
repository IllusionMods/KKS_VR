using System;
using System.Collections;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKS_VR.Features;
using KKS_VR.Fixes;
using KKS_VR.Settings;
using Unity.XR.OpenVR;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;

namespace KKS_VR
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInIncompatibility("bero.crossfadervr")]
    public class VRPlugin : BaseUnityPlugin
    {
        public const string GUID = "KKS_MainGameVR";
        public const string Name = "KKS Main Game VR";
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            var vrActivated = Environment.CommandLine.Contains("--vr");
            if (vrActivated)
            {
                BepInExVrLogBackend.ApplyYourself();
                OpenVRHelperTempfixHook.Patch();

                var settings = SettingsManager.Create(Config);
                StartCoroutine(LoadDevice(settings));
            }

            AnimationCrossFader.Initialize(Config, vrActivated);
        }

        private IEnumerator LoadDevice(KoikatuSettings settings)
        {
            yield return new WaitUntil(() => Manager.Scene.initialized && Manager.Scene.LoadSceneName == "Title");

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
            TopmostToolIcons.Patch();

            VRManager.Create<Interpreters.KoikatuInterpreter>(new KoikatuContext(settings));

            // VRGIN doesn't update the near clip plane until a first "main" camera is created, so we set it here.
            UpdateNearClipPlane(settings);
            settings.AddListener("NearClipPlane", (_, _1) => UpdateNearClipPlane(settings));

            VR.Manager.SetMode<GameStandingMode>();

            VRFade.Create();
            PrivacyScreen.Initialize();
            GraphicRaycasterPatches.Initialize();
            
            // It's been reported in #28 that the game window defocues when
            // the game is under heavy load. We disable window ghosting in
            // an attempt to counter this.
            NativeMethods.DisableProcessWindowsGhosting();

            DontDestroyOnLoad(VRCamera.Instance.gameObject);

            // Probably unnecessary, but just to be safe
            VR.Mode.MoveToPosition(Vector3.zero, Quaternion.Euler(Vector3.zero), true);

            Logger.LogInfo("Finished loading into VR mode!");
        }

        private void UpdateNearClipPlane(KoikatuSettings settings)
        {
            VR.Camera.gameObject.GetComponent<UnityEngine.Camera>().nearClipPlane = settings.NearClipPlane;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern void DisableProcessWindowsGhosting();
        }
    }
}
