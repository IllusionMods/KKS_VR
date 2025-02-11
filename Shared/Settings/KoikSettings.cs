using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using KK_VR.Features;
using KK_VR.Grasp;
using KK_VR.Holders;
using KKAPI.Utilities;
using UnityEngine;
using VRGIN.Core;
using static System.Collections.Specialized.BitVector32;
using static Illusion.Utils;
using static UnityEngine.UI.ScrollRect;

namespace KK_VR.Settings
{
    public abstract class KoikSettings
    {
        public const string SectionGeneral = "0. General";
        public const string SectionRoaming = "1. Roaming";
        public const string SectionEventScenes = "2. Event scenes";
        public const string SectionH = "3. H Scene";
        public const string SectionPov = "4. Impersonation (PoV)";
        public const string SectionIK = "5. Inverse Kinematics (IK)";
        public const string SectionGripMove = "6. GripMove";
        public const string SectionPerformance = "7. Performance";

        public enum Handedness
        {
            Left,
            Right
        }
        public enum PovMovementType
        {
            Disabled,
            Straight,
            Upright
        }
        public enum HeadEffector
        {
            Disabled,
            OnDemand,
            Always
        }
        public enum Genders
        {
            Disable,
            Boys,
            Girls,
            Both
        }
        public enum GripMoveStabilization
        {
            None,
            YawAndRotation,
            OnlyRotation,
        }
        public enum ShadowType
        {
            Disabled,
            Close,
            Average,
            Auto
        }
        #region General

        public static ConfigEntry<float> RotationAngle { get; private set; }
        public static ConfigEntry<bool> PrivacyScreen { get; private set; }
        public static ConfigEntry<float> NearClipPlane { get; private set; }
        public static ConfigEntry<float> PositionOffsetY { get; private set; }
        public static ConfigEntry<float> PositionOffsetZ { get; private set; }
        public static ConfigEntry<Handedness> MainHand { get; private set; }
        public static ConfigEntry<bool> EnableBoop { get; private set; }
        public static ConfigEntry<float> ShortPress { get; private set; }
        public static ConfigEntry<float> LongPress { get; private set; }
        public static ConfigEntry<bool> EnableSFX { get; private set; }
        public static ConfigEntry<ShadowType> ShadowSetting { get; private set; }
        public static ConfigEntry<Vector3> ModelRotation { get; private set; }
        public static ConfigEntry<Vector3> ModelPosition { get; private set; }

        #endregion


        #region Roaming
        public static ConfigEntry<bool> ContinuousRotation { get; private set; }

        #endregion


        #region Pov

        public static ConfigEntry<Genders> Pov { get; private set; }
        public static ConfigEntry<PovMovementType> FlyInPov { get; private set; }
        public static ConfigEntry<bool> HideHeadInPov { get; private set; }
        public static ConfigEntry<float> FlightSpeed { get; private set; }
        public static ConfigEntry<int> PovDeviationThreshold { get; private set; }
        public static ConfigEntry<bool> PovAutoEnter { get; private set; }

        #endregion


        #region H

        public static ConfigEntry<Genders> AutomaticTouching { get; private set; }
        public static ConfigEntry<float> TouchReaction { get; private set; }

        #endregion


        #region GripMove

        public static ConfigEntry<GripMoveStabilization> GripMoveStabilize { get; private set; }
        public static ConfigEntry<bool> GripMoveLimitRotation { get; private set; }
        public static ConfigEntry<int> GripMoveStabilizationAmount { get; private set; }
        public static ConfigEntry<bool> GripMoveEnableRotation { get; private set; }
        public static ConfigEntry<bool> GripMoveLocomotion { get; private set; }

        #endregion


        #region IK

        public static ConfigEntry<bool> IKMaintainRelativePosition { get; private set; }
        public static ConfigEntry<float> IKPushParent { get; private set; }

        public static ConfigEntry<bool> IKShowGuideObjects { get; private set; }
        public static ConfigEntry<HeadEffector> IKHeadEffector { get; private set; }
        public static ConfigEntry<float> IKDefaultBendConstraint { get; private set; }

        public static ConfigEntry<bool> IKReturnBodyPartAfterSync { get; private set; }

        #endregion


        #region Performance
        /// <summary>
        /// Very expensive, about ~10-20% extra gpu load.
        /// </summary>
        public static ConfigEntry<bool> FixMirrors { get; private set; }

        #endregion

        public virtual VRSettings Create(ConfigFile config)
        {
            var settings = new VRSettings();

            #region SectionGeneral


            // Seen some folks discovering this setting after quite a while, thus a smaller default for extra realism from the get go.
            // At 1.0 scale the characters are way too tiny.
            var ipdScale = config.Bind(SectionGeneral, "IPD Scale", 0.9f,
                new ConfigDescription(
                    "Scale of the camera. The lesser the bigger the world around appears.",
                    new AcceptableValueRange<float>(0.1f, 4f)));
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


            RotationAngle = config.Bind(SectionGeneral, "Rotation angle", 45f,
                new ConfigDescription(
                    "Angle of rotation, in degrees",
                    new AcceptableValueRange<float>(0f, 180f),
                    new ConfigurationManagerAttributes { Order = 10 }
                    ));


            PrivacyScreen = config.Bind(SectionGeneral, "Privacy screen", false,
                "Attempt to hide everything in the desktop mirror window");


            NearClipPlane = config.Bind(SectionGeneral, "Near clip plane", 0.002f,
                new ConfigDescription(
                    "Minimum distance from camera for an object to be shown (causes visual glitches on some maps when set too small)",
                    new AcceptableValueRange<float>(0.001f, 0.2f)));


            EnableBoop = config.Bind(SectionGeneral, "Enable Boop", true,
                "Add dynamic bone colliders to items that represent vr controllers.\nRequires game restart.");


            PositionOffsetY = config.Bind(SectionGeneral, "Camera offset-Y", 0.05f,
                new ConfigDescription(
                    "Camera offset from an attachment point. Applies whenever the camera assumes head orientation of a character.",
                    new AcceptableValueRange<float>(-1f, 1f)
                    ));


            PositionOffsetZ = config.Bind(SectionGeneral, "Camera offset-Z", 0.05f,
                new ConfigDescription(
                    "Camera offset from an attachment point. Applies whenever the camera assumes head orientation of a character.",
                    new AcceptableValueRange<float>(-1f, 1f)
                    ));


            ShortPress = config.Bind(SectionGeneral, "Short press", 0.35f,
                new ConfigDescription(
                    "",
                    new AcceptableValueRange<float>(0.1f, 0.5f),
                    new ConfigurationManagerAttributes { Order = -19 }
                    ));


            LongPress = config.Bind(SectionGeneral, "Long press", 0.7f,
                new ConfigDescription(
                    "",
                     new AcceptableValueRange<float>(0.5f, 1f),
                    new ConfigurationManagerAttributes { Order = -20 }
                    ));


            EnableSFX = config.Bind(SectionGeneral, "Enable sfx", true,
                new ConfigDescription(
                    "SFX for controller touch",
                    null,
                    new ConfigurationManagerAttributes { Order = -15 }
                    ));


            MainHand = config.Bind(SectionGeneral, "Main hand", Handedness.Right,
                new ConfigDescription(
                    "Default attachment side of the menu",
                    null,
                    new ConfigurationManagerAttributes { Order = -21 }
                    ));


            ShadowSetting = config.Bind(SectionGeneral, "Shadows", ShadowType.Auto,
                new ConfigDescription(
                    "Optimize shadows for preferred distance.",
                    null,
                    new ConfigurationManagerAttributes { Order = -10 }));


            ModelPosition = config.Bind(SectionGeneral, "Adjust model position", Vector3.zero,
                new ConfigDescription(
                    "",
                    null,
                    new ConfigurationManagerAttributes { Order = -25 }));


            ModelRotation = config.Bind(SectionGeneral, "Adjust model rotation", Vector3.zero,
                new ConfigDescription(
                    "",
                    null,
                    new ConfigurationManagerAttributes { Order = -30 }));


            EnableBoop = config.Bind(SectionGeneral, "Enable Boop", true,
                "Add dynamic bone colliders to items that represent vr controllers.\nRequires game restart.");


            #endregion

            #region Roaming


            ContinuousRotation = config.Bind(SectionRoaming, "Continuous rotation", true,
                "Rotate camera continuously instead of a snap turn. Influenced by the setting 'Rotation angle'.");


            #endregion

            #region H


            // This one can be a bit annoying currently as characters can overreact if unintentionally bullied by the controller in pov mode during animations.
            AutomaticTouching = config.Bind(SectionH, "Automatic touching",Genders.Both,
                "Touching body with controller triggers a reaction"
                );


            TouchReaction = config.Bind(SectionH, "Touch reaction", 0.2f,
                new ConfigDescription(
                    "Set probability of an alternative reaction to the touch.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = -10, ShowRangeAsPercent = false }
                    ));


            #endregion

            #region SectionPov


            Pov = config.Bind(SectionPov, "Enable", Genders.Boys,
                new ConfigDescription(
                    "The range of targets for impersonations.",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }
                    ));


            HideHeadInPov = config.Bind(SectionPov, "Hide head", true,
                "Hide the corresponding head when the camera is in it. " +
                "Can be used in combination with camera offset to have simultaneously visible head and PoV mode.(~0.11 Z-offset for that)");


            // No second mode after rework yet.
            FlyInPov = config.Bind(SectionPov, "Smooth transition", PovMovementType.Upright,
                new ConfigDescription(
                    "Apply camera's movements smoothly during impersonation.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }
                    ));


            PovDeviationThreshold = config.Bind(SectionPov, "Lazy", 15,
                new ConfigDescription(
                    "Introduces lazy impersonation when above 0. " +
                    "Follows camera in less invasive way for as long as the angle deviation is within limit.\n" +
                    "Changes take place after new impersonation.",
                    new AcceptableValueRange<int>(0, 60)
                    ));


            FlightSpeed = config.Bind(SectionPov, "Transition speed", 1f,
                new ConfigDescription(
                    "Speed of the smooth transition.",
                    new AcceptableValueRange<float>(0.1f, 3f)));

            // Didn't meet the expectations.
            //var directImpersonation = config.Bind(SectionPov, "DirectImpersonation", false, "");
            //Tie(directImpersonation, v => settings.DirectImpersonation = v);



            PovAutoEnter = config.Bind(SectionPov, "Auto impersonation", true,
                "Automatically impersonate on position change if appropriate according to the setting.");


            #endregion
            
            #region SectionIK


            IKShowGuideObjects = config.Bind(SectionIK, "Visual cue", false,
                new ConfigDescription(
                    "Show visual cue during IK manipulation that represent attachment point of a corresponding part of the body. " +
                    "The green hue signifies a possible attachment point.",
                    null,
                    new ConfigurationManagerAttributes { Order = 8 }
                    ));


            IKPushParent = config.Bind(SectionIK, "Push parent", 0.05f,
                new ConfigDescription(
                    "How well the limbs shall influence their parent joints.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = -10, ShowRangeAsPercent = false }
                    ));


            IKMaintainRelativePosition = config.Bind(SectionIK, "Maintain limb orientation", true,
                new ConfigDescription(
                    "The way ik handles arms. Use appropriate setting according to the taste/needs.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }
                    ));


            IKDefaultBendConstraint = config.Bind(SectionIK, "Bend constraint", 0.1f,
                new ConfigDescription(
                    "Bendability of the limbs. 0 for no limits, 1 for full limitation.",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Order = -9, ShowRangeAsPercent = false }
                    ));


            IKHeadEffector = config.Bind(SectionIK, "Head effector", HeadEffector.OnDemand,
                new ConfigDescription(
                    "Head effector is very finicky, will make it or break it. In case of latter can be fixed (more often then not) by manually adjusting the pose." +
                    "'OnDemand' setting will disable effector on soft/hard resets",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }
                    ));


            IKReturnBodyPartAfterSync = config.Bind(SectionIK, "Return after sync", true,
                new ConfigDescription(
                    "Return limb to the default state when sync stops.",
                    null,
                    new ConfigurationManagerAttributes { Order = -8 }
                    ));


            #endregion

            #region SectionGripMove


            GripMoveStabilizationAmount = config.Bind(SectionGripMove, "Stabilization amount", 10,
                new ConfigDescription(
                    "The bigger the number, the more 'average' rotation is",
                     new AcceptableValueRange<int>(5, 20),
                    new ConfigurationManagerAttributes { Order = 7 }
                    ));


            GripMoveStabilize = config.Bind(SectionGripMove, "Stabilization", GripMoveStabilization.OnlyRotation,
                new ConfigDescription(
                    "",
                    null,
                    new ConfigurationManagerAttributes { Order = 8 }
                    ));


            GripMoveLimitRotation = config.Bind(SectionGripMove, "Limit movement during rotation", false,
                new ConfigDescription(
                    "Disable the position adjustment during rotation.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }
                    ));


            GripMoveEnableRotation = config.Bind(SectionGripMove, "Enable rotation", false,
                new ConfigDescription(
                    "Enable rotation while pressing 'Touchpad'",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }
                    ));


            GripMoveLocomotion = config.Bind(SectionGripMove, "Locomotion", true,
                new ConfigDescription(
                    "",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }
                    ));


            #endregion

            #region SectionPerformance


            FixMirrors = config.Bind(SectionPerformance, "Fix mirrors", true,
                "Fix mirror reflections. Adds ~10-20% to gpu load when camera looks at the mirror.\n" +
                "Otherwise the reflection is of subpar quality, but performance doesn't suffer.");


            #endregion


            // We manage effects ourselves.
            settings.ApplyEffects = false;
            settings.AddListener("IPDScale", (_, _1) => UpdateIPD());

            return settings;
        }
        public static void UpdateOnStart()
        {
            NearClipPlane.SettingChanged += (sender, e) => UpdateNearClipPlane();
            ShadowSetting.SettingChanged += (sender, e) => UpdateShadowSetting();
            IKMaintainRelativePosition.SettingChanged += (sender, e) => UpdateIKMaintainRelativePosition();
            IKPushParent.SettingChanged += (sender, e) => UpdateIKPushParent();
            PrivacyScreen.SettingChanged += (sender, e) => Features.PrivacyScreen.UpdatePrivacyScreen();
            ModelPosition.SettingChanged += (sender, e) => HandHolder.UpdateOffsets();
            ModelRotation.SettingChanged += (sender, e) => HandHolder.UpdateOffsets();

            UpdateIPD();
            UpdateNearClipPlane();
            UpdateShadowSetting();
            HandHolder.UpdateOffsets();
            Features.PrivacyScreen.UpdatePrivacyScreen();
        }

        private void Tie<T>(ConfigEntry<T> entry, Action<T> set)
        {
            set(entry.Value);
            entry.SettingChanged += (_, _1) => set(entry.Value);
        }
        private static void UpdateNearClipPlane()
        {
            VR.Camera.gameObject.GetComponent<UnityEngine.Camera>().nearClipPlane = NearClipPlane.Value;
        }
        private static void UpdateIPD()
        {
            VRCamera.Instance.SteamCam.origin.localScale = Vector3.one * VR.Settings.IPDScale;
        }
        private static void UpdateIKMaintainRelativePosition()
        {
            if (GraspHelper.Instance != null)
            {
                GraspHelper.Instance.UpdateMaintainRelativePosition(IKMaintainRelativePosition.Value);
            }
        }
        private static void UpdateIKPushParent()
        {
            if (GraspHelper.Instance != null)
            {
                GraspHelper.Instance.UpdatePushParent(IKPushParent.Value);
            }
        }
        public static void UpdateShadowSetting(ShadowType shadowType = ShadowType.Disabled)
        {
            var enabled =
#if KKS
            Valve.VR.
#endif
            // SteamVR.enabled doesn't work in KKS.
            SteamVR.active;

            if (enabled)
            {
                if (shadowType == ShadowType.Disabled)
                {
                    shadowType = ShadowSetting.Value;
                }
                QualitySettings.shadowProjection = ShadowProjection.StableFit;
                QualitySettings.shadowCascades = 4;
                QualitySettings.shadowCascade2Split = 0.33f;
                QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadows = ShadowQuality.All;

                if (shadowType == ShadowType.Close)
                {
                    // Focus on proximity. Good while close, non-existent at mid range.
                    QualitySettings.shadowCascade4Split = new Vector3(0.025f, 0.085f, 0.25f);
                    QualitySettings.shadowDistance = 30;
                }
                else if (shadowType == ShadowType.Average)
                {
                    QualitySettings.shadowCascade4Split = new Vector3(0.06666667f, 0.2f, 0.4666667f);
                    QualitySettings.shadowDistance = 100;
                }
            }
        }
    }
}
