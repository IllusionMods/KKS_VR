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
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Controls.Handlers;
using KK_VR.Camera;

namespace KK_VR
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInIncompatibility("bero.crossfadervr")]
    public class VRPlugin : BaseUnityPlugin
    {
        public const string GUID = "kk.vr.game";
        public const string Name = "MainGameVR";
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            var settings = new GameSettings().Create(Config);

            if (Environment.CommandLine.Contains("--vr") || SteamVRDetector.IsRunning)
            {
                BepInExVrLogBackend.ApplyYourself();
                StartCoroutine(LoadDevice(settings));
            }
            CrossFader.Initialize(Config, enabled);
        }

        private const string DeviceOpenVR = "OpenVR";
        private IEnumerator LoadDevice(VRSettings settings)
        {
            //yield return new WaitUntil(() => Manager.Scene. initialized);
            //yield return new WaitUntil(() => Manager.Scene.initialized && Manager.Scene.LoadSceneName == "Title");
            
            if (UnityEngine.VR.VRSettings.loadedDeviceName != DeviceOpenVR)
            {
                UnityEngine.VR.VRSettings.LoadDeviceByName(DeviceOpenVR);
                yield return null;
            }
            UnityEngine.VR.VRSettings.enabled = true; 
            while (UnityEngine.VR.VRSettings.loadedDeviceName != DeviceOpenVR)
            {
                yield return null;
            }
            while (true)
            {
                var rect = VRGIN.Native.WindowManager.GetClientRect();
                if (rect.Right - rect.Left > 0)
                {
                    break;
                }
                //VRLog.Info("waiting for the window rect to be non-empty");
                yield return null;
            }

            new Harmony(GUID).PatchAll(typeof(VRPlugin).Assembly);
            VRManager.Create<Interpreters.KoikGameInterp>(new KoikContext(settings));

            VR.Manager.SetMode<GameStandingMode>();

            VRFade.Create();
            GraphicRaycasterPatches.Initialize();
            NativeMethods.DisableProcessWindowsGhosting();


            GameAPI.RegisterExtraBehaviour<InterpreterHooks>(GUID);

            Logger.LogInfo("Finished loading into VR mode!");
        }

        
        

        private static class NativeMethods
        {
            // It's been reported in #28 that the game window defocues when
            // the game is under heavy load. We disable window ghosting in
            // an attempt to counter this.
            [DllImport("user32.dll")]
            public static extern void DisableProcessWindowsGhosting();
        }
    }
}
