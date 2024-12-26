using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using System.ComponentModel;
using VRGIN.Core;
using UnityEngine;
using KKAPI.Utilities;
using ADV.Commands.Object;
using KK_VR.Settings;

namespace KK_VR.Settings
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
        public const string SectionEventScenes = "2. Event scenes";
        public const string SectionH = "3. H Scene";
        public const string SectionPov = "4. Impersonation (PoV)";
        public const string SectionIK = "5. Inverse Kinematics (IK)";
        public const string SectionGripMove = "6. GripMove";
        public const string SectionPerformance = "7. Performance";

        public static ConfigEntry<bool> EnableBoop { get; private set; }
        /// <summary>
        /// Create config entries under the given ConfigFile. Also create a fresh
        /// KoikatuSettings object and arrange that it be synced with the config
        /// entries.
        /// </summary>
        /// <param name="config"></param>
        /// <returns>The new KoikatuSettings object.</returns>
        public static KoikatuSettings Create(ConfigFile config)
        {
            var settings = new KoikatuSettings();

            // Seen some folks discovering this setting after quite a while, thus a smaller default for extra realism from get go.
            // At 1.0 scale the characters are way too tiny.
            var ipdScale = config.Bind(SectionGeneral, "IPD Scale", 0.9f,
                new ConfigDescription(
                    "Scale of the camera. The lesser the bigger the world around appears.",
                    new AcceptableValueRange<float>(0.25f, 4f)));
            Tie(ipdScale, v => settings.IPDScale = v);

            // KKS SteamVR also has it built-in on trigger/grip press/release, should be possible to remove them in unity project.
            var rumble = config.Bind(SectionGeneral, "Haptic Feedback", true,
                "Whether or not haptic feedback is active.");
            Tie(rumble, v => settings.Rumble = v);

            var rotationMultiplier = config.Bind(SectionGeneral, "Rotation multiplier", 1f,
                new ConfigDescription(
                    "How quickly the the view should rotate when doing so with the controllers.",
                    new AcceptableValueRange<float>(-4f, 4f),
                    new ConfigurationManagerAttributes { Order = -1 }));
            Tie(rotationMultiplier, v => settings.RotationMultiplier = v);

            var touchpadThreshold = config.Bind(SectionGeneral, "Touchpad direction threshold", 0.8f,
                new ConfigDescription(
                    "Touchpad presses within this radius are considered center clicks rather than directional ones.",
                    new AcceptableValueRange<float>(0f, 1f)));
            Tie(touchpadThreshold, v => settings.TouchpadThreshold = v);

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

            // Pretty sure this got removed.
            //var useLegacyInputSimulator = config.Bind(SectionGeneral, "Use legacy input simulator", false,
            //    new ConfigDescription(
            //        "Simulate mouse and keyboard input by generating system-wide fake events",
            //        null,
            //        new ConfigurationManagerAttributes { IsAdvanced = true }));
            //Tie(useLegacyInputSimulator, v => settings.UseLegacyInputSimulator = v);

            var usingHeadPos = config.Bind(SectionRoaming, "Use head position", true,
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

            var crouchByHMDPos = config.Bind(SectionRoaming, "Crouch by camera position", true,
                new ConfigDescription(
                    "Crouch when the camera position is below threshold.",
                    null,
                    new ConfigurationManagerAttributes { Order = -3 }));
            Tie(crouchByHMDPos, v => settings.CrouchByCameraPos = v);


            // Too many settings + near worthless customization.
            //var crouchThreshold = config.Bind(SectionRoaming, "Crouch height", 0.9f,
            //    new ConfigDescription(
            //        "Trigger crouching when the camera is below this height",
            //        new AcceptableValueRange<float>(0.05f, 3f),
            //        new ConfigurationManagerAttributes { Order = -4 }));
            //Tie(crouchThreshold, v => settings.CrouchThreshold = v);

            //var standUpThreshold = config.Bind(SectionRoaming, "Stand up height", 1f,
            //    new ConfigDescription(
            //        "End crouching when the camera is above this height",
            //        new AcceptableValueRange<float>(0.05f, 3f),
            //        new ConfigurationManagerAttributes { Order = -4 }));
            //Tie(standUpThreshold, v => settings.StandUpThreshold = v);

            // With removed warp tool has no use.
            //var teleportWithProtagonist = config.Bind(SectionRoaming, "Teleport with protagonist", true,
            //    "When teleporting, the protagonist also teleports");
            //Tie(teleportWithProtagonist, v => settings.TeleportWithProtagonist = v);

            // Couldn't really see the difference tbh, but oh well.
            var optimizeHInsideRoaming = config.Bind(SectionPerformance, "Aggressive performance optimizations", true,
                "Improve framerate and reduce stutter in H and Talk scenes inside Roaming. May cause visual glitches.");
            Tie(optimizeHInsideRoaming, v => settings.OptimizeHInsideRoaming = v);

            // This one can be a bit annoying currently as characters can overreact if unintentionally bullied by the controller in pov mode during animations.
            var automaticTouching = config.Bind(SectionH, "Automatic touching", KoikatuSettings.SceneType.Both,
                "Touching body with controller triggers a reaction");
            Tie(automaticTouching, v => settings.AutomaticTouching = v);

            var assistedKissing = config.Bind(SectionH, "Assisted kissing", true,
                new ConfigDescription(
                    "Initiate kissing by moving your head to partner's head.\nGripMove required outside of caress.",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }));
            Tie(assistedKissing, v => settings.AssistedKissing = v);

            var assistedLicking = config.Bind(SectionH, "Assisted licking", true,
                new ConfigDescription(
                    "Initiate licking by moving your head to partner's point of interest.\nGripMove required outside of caress.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }));
            Tie(assistedLicking, v => settings.AssistedLicking = v);

            var followRotationDuringKiss = config.Bind(SectionH, "Assisted action rotation", true,
                new ConfigDescription(
                    "Apply rotation to the camera during the assisted kiss/lick.",
                    null,
                    new ConfigurationManagerAttributes { Order = 8 }));
            Tie(followRotationDuringKiss, v => settings.FollowRotationDuringKiss = v);

            var proximityKiss = config.Bind(SectionH, "Assisted kiss distance", 0.1f,
                new ConfigDescription(
                    "The distance between the camera and partner's head during the initial phase of assisted kiss.",
                    new AcceptableValueRange<float>(0.05f, 0.15f),
                    new ConfigurationManagerAttributes { Order = 7 }));
            Tie(proximityKiss, v => settings.ProximityDuringKiss = v);

            var imperfectRot = config.Bind(SectionH, "Imperfect rotation", false,
                new ConfigDescription(
                    "Allow poorly stabilized rotation after assisted kiss/lick. Purely for aesthetic reasons.",
                    null,
                    new ConfigurationManagerAttributes { Order = 6, IsAdvanced = true }));
            Tie(imperfectRot, v => settings.ImperfectRotation = v);

            // Disabled for now, as the tongue isn't implemented yet, and a stop gap measure seems half assed.
            //var automaticTouchingByHmd = config.Bind(SectionH, "Kiss body", true,
            //    "Touch body by moving your head");
            //Tie(automaticTouchingByHmd, v => settings.AutomaticTouchingByHeadset = v);

            // Removed in favor of talkscene/adv rework, it (attempts) takes  care of everything.
            //var firstPersonADV = config.Bind(SectionEventScenes, "First person", true,
            //    "Prefer first person view in event scenes");
            //Tie(firstPersonADV, v => settings.FirstPersonADV = v);

            // Synchronizing shenanigans during adv/talk scene between both games proved to be a major headache, removed for now.
            //var showMaleHeadInAdv = config.Bind(SectionEventScenes, "Show head in ADV", true,
            //    "");
            //Tie(showMaleHeadInAdv, v => settings.ForceShowMaleHeadInAdv = v);


            EnableBoop = config.Bind(SectionGeneral, "Enable Boop", true,
                "Add dynamic bone colliders to items that represent vr controllers.\nRequires game restart.");


            var enablePOV = config.Bind(SectionPov, "Enable", KoikatuSettings.Impersonation.Boys,
                new ConfigDescription(
                    "The range of targets for impersonations.",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }));
            Tie(enablePOV, v => settings.PoV = v);

            var HeadPosPoVY = config.Bind(SectionGeneral, "Camera offset-Y", 0.05f,
                new ConfigDescription(
                    "Camera offset from an attachment point. Applies whenever the camera assumes head orientation of a character.",
                    new AcceptableValueRange<float>(-1f, 1f)));
            Tie(HeadPosPoVY, v => settings.PositionOffsetY = v);

            var HeadPosPoVZ = config.Bind(SectionGeneral, "Camera offset-Z", 0.05f,
                new ConfigDescription(
                    "Camera offset from an attachment point. Applies whenever the camera assumes head orientation of a character.",
                    new AcceptableValueRange<float>(-1f, 1f)));
            Tie(HeadPosPoVZ, v => settings.PositionOffsetZ = v);

            var hideHeadInPOV = config.Bind(SectionPov, "Hide head", true,
                "Hide the corresponding head when the camera is in it. Can be used in combination with camera offset to have simultaneously visible head and PoV mode.(~0.11 Z-offset for that)");
            Tie(hideHeadInPOV, v => settings.HideHeadInPOV = v);

            // No second mode after rework yet.
            var flyInPov = config.Bind(SectionPov, "Smooth transition", KoikatuSettings.MovementTypeH.Upright,
                new ConfigDescription(
                    "Apply camera's movements smoothly during impersonation.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }));
            Tie(flyInPov, v => settings.FlyInPov = v);

            var autoEnter = config.Bind(SectionPov, "Auto impersonation", true,
                "Automatically impersonate on position change if appropriate according to the setting.");
            Tie(autoEnter, v => settings.AutoEnterPov = v);

            var rotationDeviationThreshold = config.Bind(SectionPov, "Lazy", 15,
                new ConfigDescription(
                    "Introduces lazy impersonation when above 0. Follows camera in less invasive way for as long as the angle deviation is within limit.\n" +
                    "Changes take place after new impersonation.",
                    new AcceptableValueRange<int>(0, 60)));
            Tie(rotationDeviationThreshold, v => settings.RotationDeviationThreshold = v);

            var flyInH = config.Bind(SectionH, "Smooth transition", true,
                "Apply camera's movements smoothly when camera supposed to teleport.");
            Tie(flyInH, v => settings.SmoothTransition = v);


            var flightSpeed = config.Bind(SectionH, "Transition speed", 1f,
                new ConfigDescription(
                    "Speed of the smooth transition.",
                    new AcceptableValueRange<float>(0.1f, 3f)));
            Tie(flightSpeed, v => settings.FlightSpeed = v);

            var continuousRot = config.Bind(SectionRoaming, "Continuous rotation", true,
                    "Rotate camera continuously instead of a snap turn. Influenced by the setting 'Rotation angle'.");
            Tie(continuousRot, v => settings.ContinuousRotation = v);

            // Didn't meet expectations.
            //var directImpersonation = config.Bind(SectionPov, "DirectImpersonation", false, "");
            //Tie(directImpersonation, v => settings.DirectImpersonation = v);

            var showGuideObjects = config.Bind(SectionIK, "Visual cue", true,
                new ConfigDescription(
                    "Show visual cue during IK manipulation that represent attachment point of a corresponding part of the body. " +
                    "The green hue signifies a possible attachment point.",
                    null,
                    new ConfigurationManagerAttributes { Order = 8 }));
            Tie(showGuideObjects, v => settings.ShowGuideObjects = v);


            //var showDebugIK = config.Bind(SectionIK, "DebugIK", false,
            //    new ConfigDescription(
            //        "Show actual IK targets. Requires new scene.\n" +
            //        "yellow - ik",
            //        null,
            //        new ConfigurationManagerAttributes { IsAdvanced = true }
            //        )
            //    );
            //Tie(showDebugIK, v => settings.IKShowDebug = v);

            var pushParent = config.Bind(SectionIK, "Push parent", 0.05f,
                new ConfigDescription(
                    "How well the limbs shall influence their parent joints.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = -10, ShowRangeAsPercent = false }));
            Tie(pushParent, v => settings.PushParent = v);

            var maintainLimbOrientation = config.Bind(SectionIK, "Maintain limb orientation", true,
                new ConfigDescription(
                    "The way ik handles arms. Use appropriate setting according to the taste/needs.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }));
            Tie(maintainLimbOrientation, v => settings.MaintainLimbOrientation = v);


            var hideAibuHandOnUserInput = config.Bind(SectionH, "Hide caress hand", KoikatuSettings.HandType.Both,
                    "Hide caress item model when assuming manual control over it.");
            Tie(hideAibuHandOnUserInput, v => settings.HideHandOnUserInput = v);


            var ikBendConstraint = config.Bind(SectionIK, "Bend constraint", 0.1f,
                new ConfigDescription(
                    "Bendability of the limbs. 0 for no limits, 1 for full limitation.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = -9, ShowRangeAsPercent = false }));
            Tie(ikBendConstraint, v => settings.IKDefaultBendConstraint = v);

            var ikHeadEffector = config.Bind(SectionIK, "Head effector", KoikatuSettings.HeadEffector.OnDemand,
                new ConfigDescription(
                    "Head effector is very finicky, will make it or break it. In case of latter can be fixed (more often then not) by manually adjusting the pose." +
                    "'OnDemand' setting will disable effector on soft/hard resets",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }));
            Tie(ikHeadEffector, v => settings.IKHeadEffector = v);

            var fixMirrors = config.Bind(SectionPerformance, "Fix mirrors", true,
                "Fix mirror reflections. Adds ~10-20% to gpu load when camera looks at a mirror.\n" +
                "Otherwise the reflection is of subpar quality.");
            Tie(fixMirrors, v => settings.FixMirrors = v);

            var touchReaction = config.Bind(SectionH, "Touch reaction", 0.2f,
                new ConfigDescription(
                    "Set probability of an alternative reaction to the touch.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = -10, ShowRangeAsPercent = false }));
            Tie(touchReaction, v => settings.TouchReaction = v);

            var shadowType = config.Bind(SectionGeneral, "Shadows", KoikatuSettings.ShadowType.Average,
                new ConfigDescription(
                    "Optimize shadows for preferred distance.",
                    null,
                    new ConfigurationManagerAttributes { Order = -10 }));
            Tie(shadowType, v => settings.ShadowsOptimization = v);

            var returnSyncBodyPart = config.Bind(SectionIK, "Return after sync", true,
                new ConfigDescription(
                    "Return limb to the default state when sync stops.",
                    null,
                    new ConfigurationManagerAttributes { Order = -8 }));
            Tie(returnSyncBodyPart, v => settings.ReturnBodyPartAfterSync = v);

            var stabilizeGripMove = config.Bind(SectionGripMove, "Stabilization", KoikatuSettings.GripMoveStabilization.OnlyRotation,
                new ConfigDescription(
                    "",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }));
            Tie(stabilizeGripMove, v => settings.GripMoveStabilize = v);

            var gripMoveStabilizationAmount = config.Bind(SectionGripMove, "Stabilization amount", 10,
                new ConfigDescription(
                    "The bigger the number, the more 'average' rotation is",
                     new AcceptableValueRange<int>(5, 20),
                    new ConfigurationManagerAttributes { Order = 9 }));
            Tie(gripMoveStabilizationAmount, v => settings.GripMoveStabilizationAmount = v);

            var gripMoveLimitRotation = config.Bind(SectionGripMove, "Limit movement", false,
                new ConfigDescription(
                    "Disable the position adjustment during rotation.",
                    null,
                    new ConfigurationManagerAttributes { Order = 0 }));
            Tie(gripMoveLimitRotation, v => settings.GripMoveLimitRotation = v);

            var shortPress = config.Bind(SectionGeneral, "Short press", 0.35f,
                new ConfigDescription(
                    "",
                     new AcceptableValueRange<float>(0.1f, 0.5f),
                    new ConfigurationManagerAttributes { Order = -19 }));
            Tie(shortPress, v => settings.ShortPress = v);

            var longPress = config.Bind(SectionGeneral, "Long press", 0.7f,
                new ConfigDescription(
                    "",
                     new AcceptableValueRange<float>(0.5f, 1f),
                    new ConfigurationManagerAttributes { Order = -20 }));
            Tie(longPress, v => settings.LongPress = v);

            //void updateKeySets()
            //{
            //    keySetsConfig.CurrentKeySets(out var keySets, out var hKeySets);
            //    settings.KeySets = keySets;
            //    settings.HKeySets = hKeySets;
            //}

            //keySetsConfig = new KeySetsConfig(config, updateKeySets);
            //updateKeySets();

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

    //class KeySetsConfig
    //{
    //    private readonly KeySetConfig _main;
    //    private readonly KeySetConfig _main1;
    //    private readonly KeySetConfig _h;
    //    private readonly KeySetConfig _h1;

    //    private readonly ConfigEntry<bool> _useMain1;
    //    private readonly ConfigEntry<bool> _useH1;

    //    public KeySetsConfig(ConfigFile config, Action onUpdate)
    //    {
    //        const string sectionP = "2. Non-H button assignments (primary)";
    //        const string sectionS = "2. Non-H button assignments (secondary)";
    //        const string sectionHP = "3. H button assignments (primary)";
    //        const string sectionHS = "3. H button assignments (secondary)";

    //        _main = new KeySetConfig(config, onUpdate, sectionP, isH: false, advanced: false);
    //        _main1 = new KeySetConfig(config, onUpdate, sectionS, isH: false, advanced: true);
    //        _h = new KeySetConfig(config, onUpdate, sectionHP, isH: true, advanced: false);
    //        _h1 = new KeySetConfig(config, onUpdate, sectionHS, isH: true, advanced: true);

    //        _useMain1 = config.Bind(sectionS, "Use secondary assignments", false,
    //            new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
    //        _useMain1.SettingChanged += (_, _1) => onUpdate();
    //        _useH1 = config.Bind(sectionHS, "Use secondary assignments", false,
    //            new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
    //        _useH1.SettingChanged += (_, _1) => onUpdate();
    //    }

    //    public void CurrentKeySets(out List<KeySet> keySets, out List<KeySet> hKeySets)
    //    {
    //        keySets = new List<KeySet>();
    //        keySets.Add(_main.CurrentKeySet());
    //        if (_useMain1.Value)
    //        {
    //            keySets.Add(_main1.CurrentKeySet());
    //        }

    //        hKeySets = new List<KeySet>();
    //        hKeySets.Add(_h.CurrentKeySet());
    //        if (_useH1.Value)
    //        {
    //            hKeySets.Add(_h1.CurrentKeySet());
    //        }
    //    }
    //}

    //class KeySetConfig
    //{
    //    private readonly ConfigEntry<AssignableFunction> _trigger;
    //    private readonly ConfigEntry<AssignableFunction> _grip;
    //    private readonly ConfigEntry<AssignableFunction> _up;
    //    private readonly ConfigEntry<AssignableFunction> _down;
    //    private readonly ConfigEntry<AssignableFunction> _right;
    //    private readonly ConfigEntry<AssignableFunction> _left;
    //    private readonly ConfigEntry<AssignableFunction> _center;

    //    public KeySetConfig(ConfigFile config, Action onUpdate, string section, bool isH, bool advanced)
    //    {
    //        int order = -1;
    //        ConfigEntry<AssignableFunction> create(string name, AssignableFunction def)
    //        {
    //            var entry = config.Bind(section, name, def, new ConfigDescription("", null,
    //                new ConfigurationManagerAttributes { Order = order, IsAdvanced = advanced }));
    //            entry.SettingChanged += (_, _1) => onUpdate();
    //            order -= 1;
    //            return entry;
    //        }
    //        if (isH)
    //        {
    //            _trigger = create("Trigger", AssignableFunction.LBUTTON);
    //            _grip = create("Grip", AssignableFunction.GRAB);
    //            _up = create("Up", AssignableFunction.SCROLLUP);
    //            _down = create("Down", AssignableFunction.SCROLLDOWN);
    //            _left = create("Left", AssignableFunction.NONE);
    //            _right = create("Right", AssignableFunction.RBUTTON);
    //            _center = create("Center", AssignableFunction.MBUTTON);
    //        }
    //        else
    //        {
    //            _trigger = create("Trigger", AssignableFunction.WALK);
    //            _grip = create("Grip", AssignableFunction.GRAB);
    //            _up = create("Up", AssignableFunction.F3);
    //            _down = create("Down", AssignableFunction.F1);
    //            _left = create("Left", AssignableFunction.LROTATION);
    //            _right = create("Right", AssignableFunction.RROTATION);
    //            _center = create("Center", AssignableFunction.RBUTTON);
    //        }
    //    }

    //    public KeySet CurrentKeySet()
    //    {
    //        return new KeySet(
    //            trigger: _trigger.Value,
    //            grip: _grip.Value,
    //            Up: _up.Value,
    //            Down: _down.Value,
    //            Right: _right.Value,
    //            Left: _left.Value,
    //            Center: _center.Value);
    //    }

    //}
}
