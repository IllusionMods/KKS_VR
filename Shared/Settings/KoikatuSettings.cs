using KK_VR.Grasp;
using KK_VR.Interpreters;
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
    /// <summary>
    /// User settings. SettingsManager is responsible for updating this.
    /// </summary>
    public class KoikatuSettings : VRSettings
    {
        //public List<KeySet> KeySets
        //{
        //    get => _KeySets;
        //    set
        //    {
        //        _KeySets = value;
        //        TriggerPropertyChanged("KeySets");
        //    }
        //}
        //private List<KeySet> _KeySets = null;

        //public List<KeySet> HKeySets
        //{
        //    get => _HKeySets;
        //    set
        //    {
        //        _HKeySets = value;
        //        TriggerPropertyChanged("HKeySets");
        //    }
        //}
        //private List<KeySet> _HKeySets = null;

        public bool UsingHeadPos
        {
            get => _UsingHeadPos;
            set => _UsingHeadPos = value;
        }

        private bool _UsingHeadPos = false;

        public float StandingCameraPos
        {
            get => _StandingCameraPos;
            set => _StandingCameraPos = value;
        }

        private float _StandingCameraPos = 1.5f;

        public float CrouchingCameraPos
        {
            get => _CrouchingCameraPos;
            set => _CrouchingCameraPos = value;
        }
        private float _CrouchingCameraPos = 0.7f;

        public bool CrouchByCameraPos;

        //public float CrouchThreshold { get; set; }

        //public float StandUpThreshold { get; set; }

        public float RotationAngle { get; set; }

        public enum SceneType
        {
            Disabled,
            TalkScene,
            HScene,
            Both
        }
        public SceneType AutomaticTouching { get; set; }

        //public bool AutomaticTouchingByHeadset { get; set; }

        public bool AssistedKissing { get; set; }

        public bool AssistedLicking { get; set; }

        //public bool FirstPersonADV { get; set; }

        //public bool TeleportWithProtagonist { get; set; }

        public bool PrivacyScreen
        {
            get => _PrivacyScreen;
            set
            {
                _PrivacyScreen = value;
                TriggerPropertyChanged("PrivacyScreen");
            }
        }

        private bool _PrivacyScreen = false;

        public bool OptimizeHInsideRoaming { get; set; }

        public float NearClipPlane
        {
            get => _NearClipPlane;
            set
            {
                _NearClipPlane = value;
                TriggerPropertyChanged("NearClipPlane");
            }
        }

        private float _NearClipPlane;

        //public bool UseLegacyInputSimulator
        //{
        //    get { return _UseLegacyInputSimulator; }
        //    set { _UseLegacyInputSimulator = value; TriggerPropertyChanged("UseLegacyInputSimulator"); }
        //}
        //private bool _UseLegacyInputSimulator;

        public bool SmoothTransition { get; set; }
        public float FlightSpeed { get; set; }
        public Impersonation PoV { get; set; }
        public float PositionOffsetY { get; set; }
        public float PositionOffsetZ { get; set; }
        public bool HideHeadInPOV { get; set; }
        public MovementTypeH FlyInPov { get; set; }
        public bool AutoEnterPov { get; set; }
        public int RotationDeviationThreshold { get; set; }
        public float ProximityDuringKiss { get; set; }
        public bool ContinuousRotation { get; set; }
        //public HeadsetType HeadsetSpecifications { get; set; }
        //public bool ForceShowMaleHeadInAdv { get; set; }
        //public bool DirectImpersonation { get; set; }
        //public Color GuideObjectsColor { get; set; }
        public bool ShowGuideObjects { get; set; }
        public HandType HideHandOnUserInput { get; set; }
        public bool FollowRotationDuringKiss { get; set; }
        public HeadEffector IKHeadEffector { get; set; }
        //public bool IKShowDebug { get; set; }
        public float IKDefaultBendConstraint { get; set; }
        //public bool ImperfectRotation { get; set; }
        /// <summary>
        /// Very expensive, about ~10-20% extra gpu load.
        /// </summary>
        public bool FixMirrors { get; set; }
        public bool MaintainLimbOrientation 
        {
            get => _maintainLimbOrientation;
            set
            {
                _maintainLimbOrientation = value;
                if (GraspHelper.Instance != null)
                {
                    GraspHelper.Instance.ChangeMaintainRelativePosition(value);
                }
            }
        }
        private bool _maintainLimbOrientation;
        public float PushParent 
        {
            get => _pushParent;
            set
            {
                _pushParent = value;
                if (GraspHelper.Instance != null)
                {
                    GraspHelper.Instance.ChangeParentPush(value);
                }
            }
        }
        private float _pushParent;
        public ShadowType ShadowsOptimization
        {
            get => _shadowsOptimization;
            set
            {
                _shadowsOptimization = value;
                // Don't do it outside of VR.
                if (KoikatuInterpreter.SceneInterpreter != null)
                {
                    KoikatuInterpreter.TweakShadowSettings(value);
                }
            }
        }
        private ShadowType _shadowsOptimization { get; set; }
        public float TouchReaction { get; set; }
        public bool ReturnBodyPartAfterSync { get; set; }
        public GripMoveStabilization GripMoveStabilize { get; set; }
        public bool GripMoveLimitRotation {  get; set; }
        public int GripMoveStabilizationAmount { get; set; }
        public bool GripMoveEnableRotation { get; set; }

        public float ShortPress {  get; set; }
        public float LongPress { get; set; }
        public enum GripMoveStabilization
        {
            None,
            YawAndRotation,
            OnlyRotation,
        }

        public enum HeadEffector
        {
            Disabled,
            OnDemand,
            Always
        }
        public enum Impersonation
        {
            Disabled,
            Boys,
            Girls,
            Both
        }
        public enum ShadowType
        {
            Disabled,
            Close,
            Average,
            Auto
        }

        public enum MovementTypeH
        {
            Disabled,
            Straight,
            Upright
        }
        public enum HandType
        {
            None,
            ControllerItem,
            CaressItem,
            Both
        }
    }

    //public class KeySet
    //{
    //    public KeySet(
    //        AssignableFunction trigger,
    //        AssignableFunction grip,
    //        AssignableFunction Up,
    //        AssignableFunction Down,
    //        AssignableFunction Right,
    //        AssignableFunction Left,
    //        AssignableFunction Center)
    //    {
    //        this.Trigger = trigger;
    //        this.Grip = grip;
    //        this.Up = Up;
    //        this.Down = Down;
    //        this.Right = Right;
    //        this.Left = Left;
    //        this.Center = Center;
    //    }

    //    public AssignableFunction Trigger { get; set; }

    //    public AssignableFunction Grip { get; set; }

    //    public AssignableFunction Up { get; set; }

    //    public AssignableFunction Down { get; set; }

    //    public AssignableFunction Right { get; set; }

    //    public AssignableFunction Left { get; set; }

    //    public AssignableFunction Center { get; set; }
    //}
    //public enum AssignableFunction
    //{
    //    [Description("None")] NONE,
    //    [Description("Walk (Roam mode)")] WALK,
    //    [Description("Dash (Roam mode)")] DASH,
    //    [Description("Move protagonist to camera (Roam mode)")] PL2CAM,
    //    [Description("Crouch (Roam mode)")] CROUCH,
    //    [Description("Turn left")] LROTATION,
    //    [Description("Turn right")] RROTATION,
    //    [Description("Left mouse button")] LBUTTON,
    //    [Description("Right mouse button")] RBUTTON,
    //    [Description("Middle mouse button")] MBUTTON,
    //    [Description("Mouse wheel scroll up")] SCROLLUP,
    //    [Description("Mouse wheel scroll down")] SCROLLDOWN,
    //    [Description("Switch button assignments")] NEXT,
    //    [Description("Grab space to move")] GRAB,
    //    [Description("Keyboard Tab")] TAB,
    //    [Description("Keyboard Enter")] RETURN,
    //    [Description("Keyboard Esc")] ESCAPE,
    //    [Description("Keyboard Space")] SPACE,
    //    [Description("Keyboard Home")] HOME,
    //    [Description("Keyboard End")] END,
    //    [Description("Keyboard arrow left")] LEFT,
    //    [Description("Keyboard arrow up")] UP,
    //    [Description("Keyboard arrow right")] RIGHT,
    //    [Description("Keyboard arrow down")] DOWN,
    //    [Description("Keyboard Ins")] INSERT,
    //    [Description("Keyboard Del")] DELETE,
    //    [Description("Keyboard Page Up")] PRIOR,
    //    [Description("Keyboard Page Down")] KEYBOARD_PAGE_DOWN,
    //    [Description("Keyboard Backspace")] BACK,
    //    [Description("Keyboard Shift")] SHIFT,
    //    [Description("Keyboard Ctrl")] CONTROL,
    //    [Description("Keyboard Alt")] MENU,
    //    [Description("Keyboard Pause")] PAUSE,
    //    [Description("Keyboard F1")] F1,
    //    [Description("Keyboard F2")] F2,
    //    [Description("Keyboard F3")] F3,
    //    [Description("Keyboard F4")] F4,
    //    [Description("Keyboard F5")] F5,
    //    [Description("Keyboard F6")] F6,
    //    [Description("Keyboard F7")] F7,
    //    [Description("Keyboard F8")] F8,
    //    [Description("Keyboard F9")] F9,
    //    [Description("Keyboard F10")] F10,
    //    [Description("Keyboard F11")] F11,
    //    [Description("Keyboard F12")] F12,
    //    [Description("Keyboard A")] VK_A,
    //    [Description("Keyboard B")] VK_B,
    //    [Description("Keyboard C")] VK_C,
    //    [Description("Keyboard D")] VK_D,
    //    [Description("Keyboard E")] VK_E,
    //    [Description("Keyboard F")] VK_F,
    //    [Description("Keyboard G")] VK_G,
    //    [Description("Keyboard H")] VK_H,
    //    [Description("Keyboard I")] VK_I,
    //    [Description("Keyboard J")] VK_J,
    //    [Description("Keyboard K")] VK_K,
    //    [Description("Keyboard L")] VK_L,
    //    [Description("Keyboard M")] VK_M,
    //    [Description("Keyboard N")] VK_N,
    //    [Description("Keyboard O")] VK_O,
    //    [Description("Keyboard P")] VK_P,
    //    [Description("Keyboard Q")] VK_Q,
    //    [Description("Keyboard R")] VK_R,
    //    [Description("Keyboard S")] VK_S,
    //    [Description("Keyboard T")] VK_T,
    //    [Description("Keyboard U")] VK_U,
    //    [Description("Keyboard V")] VK_V,
    //    [Description("Keyboard W")] VK_W,
    //    [Description("Keyboard X")] VK_X,
    //    [Description("Keyboard Y")] VK_Y,
    //    [Description("Keyboard Z")] VK_Z,
    //    [Description("Keyboard 0")] VK_0,
    //    [Description("Keyboard 1")] VK_1,
    //    [Description("Keyboard 2")] VK_2,
    //    [Description("Keyboard 3")] VK_3,
    //    [Description("Keyboard 4")] VK_4,
    //    [Description("Keyboard 5")] VK_5,
    //    [Description("Keyboard 6")] VK_6,
    //    [Description("Keyboard 7")] VK_7,
    //    [Description("Keyboard 8")] VK_8,
    //    [Description("Keyboard 9")] VK_9,
    //    [Description("Keyboard Numpad 0")] NUMPAD0,
    //    [Description("Keyboard Numpad 1")] NUMPAD1,
    //    [Description("Keyboard Numpad 2")] NUMPAD2,
    //    [Description("Keyboard Numpad 3")] NUMPAD3,
    //    [Description("Keyboard Numpad 4")] NUMPAD4,
    //    [Description("Keyboard Numpad 5")] NUMPAD5,
    //    [Description("Keyboard Numpad 6")] NUMPAD6,
    //    [Description("Keyboard Numpad 7")] NUMPAD7,
    //    [Description("Keyboard Numpad 8")] NUMPAD8,
    //    [Description("Keyboard Numpad 9")] NUMPAD9,
    //}
}
