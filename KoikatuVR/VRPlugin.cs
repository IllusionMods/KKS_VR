﻿using BepInEx;
using System;
using VRGIN.Helpers;
using VRGIN.Core;
using VRGIN.Native;
using System.Collections;
using UnityEngine;
using HarmonyLib;

namespace KoikatuVR
{
    [BepInPlugin(GUID: GUID, Name: "Main Game VR", Version: "0.11.0")]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    public class VRPlugin : BaseUnityPlugin
    {
        public const string GUID = "mosirnik.kk-main-game-vr";

        void Awake()
        {
            VRLog.Backend = new BepInExLoggerBackend(Logger);
            bool vrDeactivated = Environment.CommandLine.Contains("--novr");
            bool vrActivated = Environment.CommandLine.Contains("--vr");
            var settings = SettingsManager.Create(Config);

            bool enabled = vrActivated || (!vrDeactivated && SteamVRDetector.IsRunning);
            StartCoroutine(LoadDevice(enabled, settings));
        }

        private const string DeviceOpenVR = "OpenVR";
        private const string DeviceNone = "None";

        IEnumerator LoadDevice(bool vrMode, KoikatuSettings settings)
        {
            var newDevice = vrMode ? DeviceOpenVR : DeviceNone;

            if (UnityEngine.VR.VRSettings.loadedDeviceName != newDevice)
            {
                // 指定されたデバイスの読み込み.
                UnityEngine.VR.VRSettings.LoadDeviceByName(newDevice);
                // 次のフレームまで待つ.
                yield return null;
            }
            // VRモードを有効にする.
            UnityEngine.VR.VRSettings.enabled = vrMode;
            // 次のフレームまで待つ.
            yield return null;

            // デバイスの読み込みが完了するまで待つ.
            while (UnityEngine.VR.VRSettings.loadedDeviceName != newDevice || UnityEngine.VR.VRSettings.enabled != vrMode)
            {
                yield return null;
            }

            while (true)
            {
                var rect = WindowManager.GetClientRect();
                if (rect.Right - rect.Left > 0)
                {
                    break;
                }
                VRLog.Info("waiting for the window rect to be non-empty");
                yield return null;
            }
            VRLog.Info("window rect is not empty!");

            if (vrMode)
            {
                new Harmony(VRPlugin.GUID).PatchAll();
                // Boot VRManager!
                VRManager.Create<Interpreters.KoikatuInterpreter>(new KoikatuContext(settings));
                // VRGIN doesn't update the near clip plane until a first "main" camera is created, so we set it here.
                VR.Camera.gameObject.GetComponent<Camera>().nearClipPlane = VR.Context.NearClipPlane;
                VR.Manager.SetMode<KoikatuStandingMode>();
                VRFade.Create();
            }
        }
    }
}
