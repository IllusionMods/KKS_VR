using BepInEx.Configuration;
using KK_VR.Grasp;
using KK_VR.Interpreters;
using KKAPI.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Helpers;
using static VRGIN.Visuals.GUIMonitor;

namespace KK_VR.Settings
{
    public class GameSettings : KoikSettings
    {
        public enum HandType
        {
            None,
            ControllerItem,
            CaressItem,
            Both
        }
        #region Roaming

        public static ConfigEntry<bool> UsingHeadPos { get; private set; }
        public static ConfigEntry<float> StandingCameraPos { get; private set; }
        public static ConfigEntry<float> CrouchingCameraPos { get; private set; }
        public static ConfigEntry<bool> CrouchByCameraPos { get; private set; }
        public static ConfigEntry<bool> OptimizeHInsideRoaming { get; private set; }

        #endregion

        #region H

        public static ConfigEntry<bool> AssistedKissing { get; private set; }
        public static ConfigEntry<bool> AssistedLicking { get; private set; }
        public static ConfigEntry<float> ProximityDuringKiss { get; private set; }
        public static ConfigEntry<bool> AssistedActionRotation { get; private set; }

        public static ConfigEntry<HandType> HideHandOnUserInput { get; private set; }
        public static ConfigEntry<bool> SmoothTransition { get; private set; }
        #endregion

        #region Pov


        #endregion


        public override VRSettings Create(ConfigFile config)
        {
            #region Roaming
            UsingHeadPos = config.Bind(SectionRoaming, "Use head position", true,
                new ConfigDescription(
                    "Place the camera exactly at the protagonist's head (may cause motion sickness). If disabled, use a fixed height from the floor.",
                    null,
                    new ConfigurationManagerAttributes { Order = -1 }
                    ));


            StandingCameraPos = config.Bind(SectionRoaming, "Camera height", 1.5f,
                new ConfigDescription(
                    "Default camera height for when not using the head position.",
                    new AcceptableValueRange<float>(0.2f, 3f),
                    new ConfigurationManagerAttributes { Order = -2 }
                    ));


            CrouchingCameraPos = config.Bind(SectionRoaming, "Crouching camera height", 0.7f,
                new ConfigDescription(
                    "Crouching camera height for when not using the head position",
                    new AcceptableValueRange<float>(0.2f, 3f),
                    new ConfigurationManagerAttributes { Order = -2 }
                    ));


            CrouchByCameraPos = config.Bind(SectionRoaming, "Crouch by camera position", true,
                new ConfigDescription(
                    "Crouch when the camera position is below threshold.",
                    null,
                    new ConfigurationManagerAttributes { Order = -3 }
                    ));


            // Couldn't really see the difference tbh, but oh well.
            OptimizeHInsideRoaming = config.Bind(SectionPerformance, "Aggressive performance optimizations", true,
                "Improve framerate and reduce stutter in H and Talk scenes inside Roaming. May cause visual glitches.");


            #endregion


            #region HScene

            AssistedKissing = config.Bind(SectionH, "Assisted kissing", true,
                new ConfigDescription(
                    "Initiate kissing by moving your head to partner's head.\nGripMove required outside of caress.",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }
                    ));


            AssistedLicking = config.Bind(SectionH, "Assisted licking", true,
                new ConfigDescription(
                    "Initiate licking by moving your head to partner's point of interest.\nGripMove required outside of caress.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }
                    ));


            AssistedActionRotation = config.Bind(SectionH, "Assisted action rotation", true,
                new ConfigDescription(
                    "Apply rotation to the camera during the assisted kiss/lick.",
                    null,
                    new ConfigurationManagerAttributes { Order = 8 }
                    ));


            ProximityDuringKiss = config.Bind(SectionH, "Assisted kiss distance", 0.1f,
                new ConfigDescription(
                    "The distance between the camera and partner's head during the initial phase of assisted kiss.",
                    new AcceptableValueRange<float>(0.05f, 0.15f),
                    new ConfigurationManagerAttributes { Order = 7 }
                    ));


            SmoothTransition = config.Bind(SectionH, "Smooth transition", true,
                "Apply camera's movements smoothly when camera supposed to teleport.");


            HideHandOnUserInput = config.Bind(SectionH, "Hide caress hand", HandType.Both,
                    "Hide caress item model when assuming manual control over it.");


            #endregion


            return base.Create(config);
        }

        //public float CrouchThreshold { get; set; }

        //public float StandUpThreshold { get; set; }

        //public float RotationAngle { get; set; }



        //public bool AutomaticTouchingByHeadset { get; set; }


        //public bool FirstPersonADV { get; set; }

        //public bool TeleportWithProtagonist { get; set; }

        //public bool PrivacyScreen
        //{
        //    get => _PrivacyScreen;
        //    set
        //    {
        //        _PrivacyScreen = value;
        //        TriggerPropertyChanged("PrivacyScreen");
        //    }
        //}

        //private bool _PrivacyScreen = false;


        //public float NearClipPlane
        //{
        //    get => _NearClipPlane;
        //    set
        //    {
        //        _NearClipPlane = value;
        //        TriggerPropertyChanged("NearClipPlane");
        //    }
        //}

        //private float _NearClipPlane;

        //public bool UseLegacyInputSimulator
        //{
        //    get { return _UseLegacyInputSimulator; }
        //    set { _UseLegacyInputSimulator = value; TriggerPropertyChanged("UseLegacyInputSimulator"); }
        //}
        //private bool _UseLegacyInputSimulator;

        //public Impersonation PoV { get; set; }
        //public float PositionOffsetY { get; set; }
        //public float PositionOffsetZ { get; set; }
        //public bool HideHeadInPOV { get; set; }
        //public MovementTypeH FlyInPov { get; set; }
        // public int PovDeviationThreshold { get; set; }
        //public bool ContinuousRotation { get; set; }
        //public HeadsetType HeadsetSpecifications { get; set; }
        //public bool ForceShowMaleHeadInAdv { get; set; }
        //public bool DirectImpersonation { get; set; }
        //public Color GuideObjectsColor { get; set; }
        //public bool ShowGuideObjects { get; set; }
        //public bool FollowRotationDuringKiss { get; set; }
        //public HeadEffector IKHeadEffector { get; set; }
        ////public bool IKShowDebug { get; set; }
        //public float IKDefaultBendConstraint { get; set; }
        ////public bool ImperfectRotation { get; set; }
        ///// <summary>
        ///// Very expensive, about ~10-20% extra gpu load.
        ///// </summary>
        //public bool FixMirrors { get; set; }
        //public bool MaintainLimbOrientation 
        //{
        //    get => _maintainLimbOrientation;
        //    set
        //    {
        //        _maintainLimbOrientation = value;
        //        if (GraspHelper.Instance != null)
        //        {
        //            GraspHelper.Instance.ChangeMaintainRelativePosition(value);
        //        }
        //    }
        //}
        //private bool _maintainLimbOrientation;
        //public float PushParent 
        //{
        //    get => _pushParent;
        //    set
        //    {
        //        _pushParent = value;
        //        if (GraspHelper.Instance != null)
        //        {
        //            GraspHelper.Instance.ChangeParentPush(value);
        //        }
        //    }
        //}
        //private float _pushParent;
        
        //{
        //    get => _shadowsOptimization;
        //    set
        //    {
        //        _shadowsOptimization = value;
        //        // Don't do it outside of VR.
        //        if (KoikGame.SceneInterpreter != null)
        //        {
        //            KoikGame.TweakShadowSettings(value);
        //        }
        //    }
        //}
        //private ShadowType _shadowsOptimization { get; set; }
        //public float TouchReaction { get; set; }
        //public bool ReturnBodyPartAfterSync { get; set; }
        //public GripMoveStabilization GripMoveStabilize { get; set; }
        //public bool GripMoveLimitRotation {  get; set; }
        //public int GripMoveStabilizationAmount { get; set; }
        //public bool GripMoveEnableRotation { get; set; }

        //public float ShortPress {  get; set; }
        //public float LongPress { get; set; }
        //public bool EnableSFX { get; set; }
        //public Handedness MainHand { get; set; }

        //public enum Handedness
        //{
        //    Left,
        //    Right
        //}
        //public enum MovementTypePov
        //{
        //    Disabled,
        //    Straight,
        //    Upright
        //}
    }
}
