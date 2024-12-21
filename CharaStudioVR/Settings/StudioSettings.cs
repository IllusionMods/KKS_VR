using System;
using BepInEx.Configuration;
using KKAPI.Utilities;
using VRGIN.Core;

namespace KK_VR.Settings
{
    public static class StudioSettings
    {
        private const string SectionGeneral = "General";
        private const string SectionStudioTool = "Studio Tool";

        public static ConfigEntry<float> NearClipPlane { get; private set; }
        public static ConfigEntry<bool> LockRotXZ { get; private set; }
        public static ConfigEntry<float> MaxVoiceDistance { get; private set; }
        public static ConfigEntry<float> MinVoiceDistance { get; private set; }
        public static ConfigEntry<float> GrabMovementMult { get; private set; }
        public static ConfigEntry<float> MaxLaserRange { get; private set; }
        public static ConfigEntry<bool> EnableBoop { get; private set; }
        public static ConfigEntry<float> WheelRepeatTime { get; private set; }
        public static ConfigEntry<bool> EnableHairTriggerClick { get; private set; }

        public static VRSettings Create(ConfigFile config)
        {
            var settings = new VRSettings();

            var ipdScale = config.Bind(SectionGeneral, "IPD Scale", 1f,
                new ConfigDescription(
                    "Scale of the camera. The higher, the more gigantic the player is.",
                    new AcceptableValueRange<float>(0.25f, 4f)));
            Tie(ipdScale, v => settings.IPDScale = v);

            var rumble = config.Bind(SectionGeneral, "Rumble", true,
                "Whether or not rumble is activated.");
            Tie(rumble, v => settings.Rumble = v);

            var rotationMultiplier = config.Bind(SectionGeneral, "Rotation multiplier", 1f,
                new ConfigDescription(
                    "How quickly the the view should rotate when doing so with the controllers (only applies to WarpTool).",
                    new AcceptableValueRange<float>(-4f, 4f),
                    new ConfigurationManagerAttributes { Order = -1 }));
            Tie(rotationMultiplier, v => settings.RotationMultiplier = v);

            var logLevel = config.Bind(SectionGeneral, "Log level", VRLog.LogMode.Info,
                new ConfigDescription(
                    "The minimum severity for a message to be logged.",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true }));
            Tie(logLevel, v => VRLog.Level = v);

            NearClipPlane = config.Bind(SectionGeneral, "Near clip plane", 0.002f,
                new ConfigDescription(
                    "Minimum distance from camera for an object to be shown (causes visual glitches on some maps when set too small).",
                    new AcceptableValueRange<float>(0.001f, 0.2f)));

            MaxVoiceDistance = config.Bind(SectionGeneral, "Max Voice distance", 300f,
                new ConfigDescription(
                    "Max Voice distance (in unit. 300 = 30m in real (HS2 uses 10 unit = 1m scale).",
                    new AcceptableValueRange<float>(100f, 600f)));

            MinVoiceDistance = config.Bind(SectionGeneral, "Min Voice distance", 7f,
                new ConfigDescription(
                    "Min Voice distance (in unit. 7 = 70 cm in real (HS2 uses 10 unit = 1m scale).",
                    new AcceptableValueRange<float>(1f, 70f)));

            GrabMovementMult = config.Bind(SectionStudioTool, "Grab Movement Multiplier", 1.5f,
                new ConfigDescription(
                    "Adjust how fast you can drag the camera around (only applies to the studio tool).",
                    new AcceptableValueRange<float>(0.5f, 10f)));

            MaxLaserRange = config.Bind(SectionStudioTool, "Laser Range", 0.3f,
                new ConfigDescription(
                    "The maximum range of the UI cursor laser.",
                    new AcceptableValueRange<float>(0.1f, 1f)));

            EnableBoop = config.Bind(SectionGeneral, "Enable Boop", true,
                "Adds colliders to the controllers so you can boop things.\nGame restart required for change to take effect.");

            WheelRepeatTime = config.Bind(SectionGeneral, "WheelRepeatTime", 0.3f,
                new ConfigDescription(
                    "Defines how often the wheel input repeats when scrolling.",
                    new AcceptableValueRange<float>(0.01f, 0.5f)));

            EnableHairTriggerClick = config.Bind(SectionStudioTool, "Enable Hair Trigger Click", false,
                "Allow left clicking the UI without fully depressing the trigger.");

            return settings;
        }

        private static void Tie<T>(ConfigEntry<T> entry, Action<T> set)
        {
            set(entry.Value);
            entry.SettingChanged += (_, _1) => set(entry.Value);
        }
    }
}
