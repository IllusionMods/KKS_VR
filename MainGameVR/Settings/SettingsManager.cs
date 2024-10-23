using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using KKAPI.Utilities;
using VRGIN.Core;
using UnityEngine;

namespace KKS_VR.Settings
{
    /// <summary>
    /// Manages configuration and keeps it up to date.
    /// 
    /// BepInEx wants us to store the config in a bunch of ConfigEntry objects,
    /// but VRGIN wants it stored inside a class inheriting VRSettings. So
    /// our plan is:
    /// 
    /// * We have both ConfigEntry objects and KoikatuSettings around.
    /// * The ConfigEntry objects are the master copy and the KoikatuSettings
    ///   object is a mirror.
    /// * SettingsManager is responsible for keeping KoikatuSettings up to date.
    /// * No other parts of code should modify KoikatuSettings. In fact, there
    ///   are code paths where VRGIN tries to modify it. We simply attempt
    ///   to avoid executing those code paths.
    /// </summary>
    public class SettingsManager
    {
        public const string SectionGeneral = "0. General";
        public const string SectionRoaming = "1. Roaming";
        public const string SectionCaress = "1. Caress";
        public const string SectionEventScenes = "1. Event scenes";

        public static ConfigEntry<bool> EnableBoop { get; private set; }

        /// <summary>
        /// Create config entries under the given ConfigFile. Also create a fresh
        /// KoikatuSettings object and arrange that it be synced with the config
        /// entries.
        /// </summary>
        /// <returns>The new KoikatuSettings object.</returns>
        public static KoikatuSettings Create(ConfigFile config)
        {
            var settings = new KoikatuSettings();

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
                    "How quickly the the view should rotate when doing so with the controllers.",
                    new AcceptableValueRange<float>(-4f, 4f),
                    new ConfigurationManagerAttributes { Order = -1 }));
            Tie(rotationMultiplier, v => settings.RotationMultiplier = v);

            //todo missing
            //var touchpadThreshold = config.Bind(sectionGeneral, "Touchpad direction threshold", 0.8f,
            //    new ConfigDescription(
            //        "Touchpad presses within this radius are considered center clicks rather than directional ones.",
            //        new AcceptableValueRange<float>(0f, 1f)));
            //Tie(touchpadThreshold, v => settings.TouchpadThreshold = v);

            var logLevel = config.Bind(SectionGeneral, "Log level", VRLog.LogMode.Info,
                new ConfigDescription(
                    "The minimum severity for a message to be logged.",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true }));
            Tie(logLevel, v => VRLog.Level = v);

            var rotationAngle = config.Bind(SectionGeneral, "Rotation angle", 45f,
                new ConfigDescription(
                    "Angle of rotation, in degrees",
                    new AcceptableValueRange<float>(0f, 180f)));
            Tie(rotationAngle, v => settings.RotationAngle = v);

            var privacyScreen = config.Bind(SectionGeneral, "Privacy screen", false,
                "Attempt to hide everything in the desktop mirror window");
            Tie(privacyScreen, v => settings.PrivacyScreen = v);

            var nearClipPlane = config.Bind(SectionGeneral, "Near clip plane", 0.002f,
                new ConfigDescription(
                    "Minimum distance from camera for an object to be shown (causes visual glitches on some maps when set too small)",
                    new AcceptableValueRange<float>(0.001f, 0.2f)));
            Tie(nearClipPlane, v => settings.NearClipPlane = v);

            var usingHeadPos = config.Bind(SectionRoaming, "Use head position", false,
                new ConfigDescription(
                    "Place the camera exactly at the protagonist's head (may cause motion sickness). If disabled, use a fixed height from the floor.",
                    null,
                    new ConfigurationManagerAttributes { Order = -1 }));
            Tie(usingHeadPos, v => settings.UsingHeadPos = v);

            var standingCameraPos = config.Bind(SectionRoaming, "Camera height", 1.5f,
                new ConfigDescription(
                    "Default camera height for when not using the head position.",
                    new AcceptableValueRange<float>(0.2f, 3f),
                    new ConfigurationManagerAttributes { Order = -2 }));
            Tie(standingCameraPos, v => settings.StandingCameraPos = v);

            var crouchingCameraPos = config.Bind(SectionRoaming, "Crouching camera height", 0.7f,
                new ConfigDescription(
                    "Crouching camera height for when not using the head position",
                    new AcceptableValueRange<float>(0.2f, 3f),
                    new ConfigurationManagerAttributes { Order = -2 }));
            Tie(crouchingCameraPos, v => settings.CrouchingCameraPos = v);

            var crouchByHMDPos = config.Bind(SectionRoaming, "Crouch by HMD position", true,
                new ConfigDescription(
                    "Crouch when the HMD position is below some threshold.",
                    null,
                    new ConfigurationManagerAttributes { Order = -3 }));
            Tie(crouchByHMDPos, v => settings.CrouchByHMDPos = v);

            var crouchThreshold = config.Bind(SectionRoaming, "Crouch height", 0.9f,
                new ConfigDescription(
                    "Trigger crouching when the camera is below this height",
                    new AcceptableValueRange<float>(0.05f, 3f),
                    new ConfigurationManagerAttributes { Order = -4 }));
            Tie(crouchThreshold, v => settings.CrouchThreshold = v);

            var standUpThreshold = config.Bind(SectionRoaming, "Stand up height", 1f,
                new ConfigDescription(
                    "End crouching when the camera is above this height",
                    new AcceptableValueRange<float>(0.05f, 3f),
                    new ConfigurationManagerAttributes { Order = -4 }));
            Tie(standUpThreshold, v => settings.StandUpThreshold = v);

            var teleportWithProtagonist = config.Bind(SectionRoaming, "Teleport with protagonist", true,
                "When teleporting, the protagonist also teleports");
            Tie(teleportWithProtagonist, v => settings.TeleportWithProtagonist = v);

            var optimizeHInsideRoaming = config.Bind(SectionRoaming, "Aggressive performance optimizations", true,
                "Improve framerate and reduce stutter in H and Talk scenes inside Roaming. May cause visual glitches.");
            Tie(optimizeHInsideRoaming, v => settings.OptimizeHInsideRoaming = v);

            var automaticTouching = config.Bind(SectionCaress, "Automatic touching", false,
                "Touching the female's body with controllers triggers reaction");
            Tie(automaticTouching, v => settings.AutomaticTouching = v);

            var automaticKissing = config.Bind(SectionCaress, "Automatic kissing", true,
                "Initiate kissing by moving your head");
            Tie(automaticKissing, v => settings.AutomaticKissing = v);

            var automaticLicking = config.Bind(SectionCaress, "Automatic licking", true,
                "Initiate licking by moving your head");
            Tie(automaticLicking, v => settings.AutomaticLicking = v);

            var automaticTouchingByHmd = config.Bind(SectionCaress, "Kiss body", true,
                "Touch the female's body by moving your head");
            Tie(automaticTouchingByHmd, v => settings.AutomaticTouchingByHmd = v);

            var firstPersonADV = config.Bind(SectionEventScenes, "First person", true,
                "Prefer first person view in event scenes");
            Tie(firstPersonADV, v => settings.FirstPersonADV = v);

            EnableBoop = config.Bind(SectionGeneral, "Enable Boop", true,
                "Adds colliders to the controllers so you can boop things.\nGame restart required for change to take effect.");

            KeySetsConfig keySetsConfig = null;

            void updateKeySets()
            {
                keySetsConfig.CurrentKeySets(out var keySets, out var hKeySets);
                settings.KeySets = keySets;
                settings.HKeySets = hKeySets;
            }

            keySetsConfig = new KeySetsConfig(config, updateKeySets);
            updateKeySets();

            POVConfig pOVConfig = new POVConfig(config, settings);

            // Fixed settings
            settings.ApplyEffects = false; // We manage effects ourselves.

            return settings;
        }

        private static void Tie<T>(ConfigEntry<T> entry, Action<T> set)
        {
            set(entry.Value);
            entry.SettingChanged += (_, _1) => set(entry.Value);
        }
    }

    internal class KeySetsConfig
    {
        private readonly KeySetConfig _main;
        private readonly KeySetConfig _main1;
        private readonly KeySetConfig _h;
        private readonly KeySetConfig _h1;

        private readonly ConfigEntry<bool> _useMain1;
        private readonly ConfigEntry<bool> _useH1;

        public KeySetsConfig(ConfigFile config, Action onUpdate)
        {
            const string sectionP = "2. Non-H button assignments (primary)";
            const string sectionS = "2. Non-H button assignments (secondary)";
            const string sectionHP = "3. H button assignments (primary)";
            const string sectionHS = "3. H button assignments (secondary)";

            _main = new KeySetConfig(config, onUpdate, sectionP, false, false);
            _main1 = new KeySetConfig(config, onUpdate, sectionS, false, true);
            _h = new KeySetConfig(config, onUpdate, sectionHP, true, false);
            _h1 = new KeySetConfig(config, onUpdate, sectionHS, true, true);

            _useMain1 = config.Bind(sectionS, "Use secondary assignments", false,
                new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            _useMain1.SettingChanged += (_, _1) => onUpdate();
            _useH1 = config.Bind(sectionHS, "Use secondary assignments", false,
                new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            _useH1.SettingChanged += (_, _1) => onUpdate();
        }

        public void CurrentKeySets(out List<KeySet> keySets, out List<KeySet> hKeySets)
        {
            keySets = new List<KeySet>();
            keySets.Add(_main.CurrentKeySet());
            if (_useMain1.Value) keySets.Add(_main1.CurrentKeySet());

            hKeySets = new List<KeySet>();
            hKeySets.Add(_h.CurrentKeySet());
            if (_useH1.Value) hKeySets.Add(_h1.CurrentKeySet());
        }
    }

    internal class KeySetConfig
    {
        private readonly ConfigEntry<AssignableFunction> _trigger;
        private readonly ConfigEntry<AssignableFunction> _grip;
        private readonly ConfigEntry<AssignableFunction> _up;
        private readonly ConfigEntry<AssignableFunction> _down;
        private readonly ConfigEntry<AssignableFunction> _right;
        private readonly ConfigEntry<AssignableFunction> _left;
        private readonly ConfigEntry<AssignableFunction> _center;

        public KeySetConfig(ConfigFile config, Action onUpdate, string section, bool isH, bool advanced)
        {
            var order = -1;

            ConfigEntry<AssignableFunction> create(string name, AssignableFunction def)
            {
                var entry = config.Bind(section, name, def, new ConfigDescription("", null,
                    new ConfigurationManagerAttributes { Order = order, IsAdvanced = advanced }));
                entry.SettingChanged += (_, _1) => onUpdate();
                order -= 1;
                return entry;
            }

            if (isH)
            {
                _trigger = create("Trigger", AssignableFunction.LBUTTON);
                _grip = create("Grip", AssignableFunction.GRAB);
                _up = create("Up", AssignableFunction.SCROLLUP);
                _down = create("Down", AssignableFunction.SCROLLDOWN);
                _left = create("Left", AssignableFunction.NONE);
                _right = create("Right", AssignableFunction.RBUTTON);
                _center = create("Center", AssignableFunction.MBUTTON);
            }
            else
            {
                _trigger = create("Trigger", AssignableFunction.WALK);
                _grip = create("Grip", AssignableFunction.GRAB);
                _up = create("Up", AssignableFunction.F3);
                _down = create("Down", AssignableFunction.F1);
                _left = create("Left", AssignableFunction.LROTATION);
                _right = create("Right", AssignableFunction.RROTATION);
                _center = create("Center", AssignableFunction.RBUTTON);
            }
        }

        public KeySet CurrentKeySet()
        {
            return new KeySet(
                _trigger.Value,
                _grip.Value,
                _up.Value,
                _down.Value,
                _right.Value,
                _left.Value,
                _center.Value);
        }
    }

   public class POVConfig
    {
        private const string sectionPOV = "4. POV between characters (Hand Tool in free H)";
        public static ConfigEntry<KeyboardShortcut> switchPOVModeKey { get; private set; }

        public POVConfig(ConfigFile config, KoikatuSettings settings)
        {
            var enablePOV = config.Bind(sectionPOV, "POV", true,
                "Switch POV between characters in free H scenes (only works in Hand Tool)");
            Tie(enablePOV, v => settings.EnablePOV = v);

            switchPOVModeKey = config.Bind(sectionPOV, "Switch POV Mode", new KeyboardShortcut(KeyCode.Y),
                new ConfigDescription(
                    "Use VR Trigger Button to switch POV between characters. " +
                    "Two Modes:\r\n" +
                    "1: POV follows character's head position with forward rotation once\r\n" +
                    "2: No POV",
                    null,
                    new ConfigurationManagerAttributes { Order = -1 }));
        }
        private static void Tie<T>(ConfigEntry<T> entry, Action<T> set)
        {
            set(entry.Value);
            entry.SettingChanged += (_, _1) => set(entry.Value);
        }
    }
}
