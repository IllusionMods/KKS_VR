using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Native;
using VRGIN.Visuals;
using static VRGIN.Native.WindowsInterop;

namespace VRGIN.Controls.Handlers
{
    /// <summary>
    /// Handler that is in charge of the menu interaction with controllers
    /// </summary>
    public class MenuHandler : ProtectedBehaviour
    {
        private Controller _Controller;
        //const float RANGE = 0.25f;
        private const int MOUSE_STABILIZER_THRESHOLD = 30; // pixels
        private Controller.Lock _LaserLock = Controller.Lock.Invalid;
        private LineRenderer Laser;
        private Vector2? mouseDownPosition;
        private GUIQuad _Target;
        MenuHandler _Other;
        //ResizeHandler _ResizeHandler;
        private Vector3 _ScaleVector;
        private Buttons _PressedButtons;
        private Controller.TrackpadDirection _LastDirection;
        private float? _NextScrollTime;
        private List<HelpText> _HelpTexts = new List<HelpText>();
        private float? _AppButtonPressTime;

        enum Buttons
        {
            Left = 1,
            Right = 2,
            Middle = 4,
        }

        protected override void OnStart()
        {
            base.OnStart();
            VRLog.Info("Menu Handler started");
            _Controller = GetComponent<Controller>();
            _ScaleVector = new Vector2((float)VRGUI.Width / Screen.width, (float)VRGUI.Height / Screen.height);
            _Other = _Controller.Other.GetComponent<MenuHandler>();
        }

        private void OnRenderModelLoaded()
        {
            try
            {
                if(!_Controller) 
                    _Controller = GetComponent<Controller>();
                //var attachPosition = _Controller.FindAttachPosition("tip");
                var attachPosition = _Controller.transform;
                if (!attachPosition)
                {
                    VRLog.Error("Attach position not found for laser!");
                    attachPosition = transform;
                }
                Laser = new GameObject("Laser pointer").AddComponent<LineRenderer>();
                Laser.transform.SetParent(attachPosition, false);
                Laser.material = new Material(Shader.Find("Sprites/Default"));
                Laser.material.renderQueue += 5000;
                Laser.SetColors(Color.cyan, Color.cyan);

                if (SteamVR.instance.hmd_TrackingSystemName == "lighthouse")
                {
                    //Laser.transform.localRotation = Quaternion.Euler(60, 0, 0);
                    Laser.transform.position += Laser.transform.forward * 0.06f;
                }
                Laser.transform.localRotation *= Quaternion.Euler(30f, 0, 0);
                Laser.SetVertexCount(2);
                Laser.useWorldSpace = true;
                Laser.SetWidth(0.002f, 0.002f);
            }
            catch (Exception e)
            {
                VRLog.Error(e);
            }
        }

        /// <summary>
        /// Gets the attached controller input object.
        /// </summary>
        protected SteamVR_Controller.Device Device
        {
            get
            {
                return SteamVR_Controller.Input((int)_Controller.Tracking.index);
            }
        }

        protected override void OnUpdate()
        {
            if (LaserVisible)
            {
                //if (IsResizing)
                //{
                //    Laser.SetPosition(0, Laser.transform.position);
                //    Laser.SetPosition(1, Laser.transform.position);
                //}
                //else
                //{
                UpdateLaser();
                //}
                CheckInput();

            }
            else if (_Controller.CanAcquireFocus())
            {
                CheckForNearMenu();
            }

        }

        private void OnDisable()
        {
            if (_LaserLock.IsValid)
            {
                // Release to be sure
                _LaserLock.Release();
            }
        }

        //private void EnsureResizeHandler()
        //{
        //    if (!_ResizeHandler)
        //    {
        //        _ResizeHandler = _Target.GetComponent<ResizeHandler>();
        //        if (!_ResizeHandler)
        //        {
        //            _ResizeHandler = _Target.gameObject.AddComponent<ResizeHandler>();
        //        }
        //    }
        //}

        //private void EnsureNoResizeHandler()
        //{
        //    if (_ResizeHandler)
        //    {
        //        DestroyImmediate(_ResizeHandler);
        //    }
        //    _ResizeHandler = null;
        //}
        private bool _triggerPressed;
        protected void CheckInput()
        {
            if (LaserVisible && _Target)
            {
                //if (_Other.LaserVisible && _Other._Target == _Target)
                //{
                //    // No double input - this is handled by ResizeHandler
                //    EnsureResizeHandler();
                //}
                //else
                //{
                //    EnsureNoResizeHandler();
                //}

                if (Device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    _triggerPressed = true;
                    VR.Input.Mouse.LeftButtonDown();
                    _PressedButtons |= Buttons.Left;
                    mouseDownPosition = Vector2.Scale(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y), _ScaleVector);
                }
                else if (Device.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    _triggerPressed = false;
                    _PressedButtons &= ~Buttons.Left;
                    VR.Input.Mouse.LeftButtonUp();
                    mouseDownPosition = null;
                }
                var currentDirection = _Controller.GetTrackpadDirection();
                if (Device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
                {
                    if (_Target.IsOwned && _triggerPressed)
                    {
                        _Target.transform.SetParent(VR.Manager.transform, true);
                        _Target.IsOwned = false;
                    }
                    else
                    {
                        _LastDirection = currentDirection;
                        switch (_LastDirection)
                        {
                            case Controller.TrackpadDirection.Right:
                                VR.Input.Mouse.RightButtonDown();
                                _PressedButtons |= Buttons.Right;
                                break;
                            case Controller.TrackpadDirection.Center:
                                VR.Input.Mouse.MiddleButtonDown();
                                _PressedButtons |= Buttons.Middle;
                                break;
                            default:
                                break;
                        }
                    }
                    
                }
                else if (Device.GetPressUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
                {
                    switch (_LastDirection)
                    {
                        case Controller.TrackpadDirection.Right:
                            VR.Input.Mouse.RightButtonUp();
                            _PressedButtons &= ~Buttons.Right;
                            break;
                        case Controller.TrackpadDirection.Center:
                            VR.Input.Mouse.MiddleButtonUp();
                            _PressedButtons &= ~Buttons.Middle;
                            break;
                        default:
                            break;
                    }
                }
                switch (currentDirection)
                {
                    case Controller.TrackpadDirection.Up:
                        if (_Target.IsOwned)
                        {
                            if (_triggerPressed)
                                MoveGui(Time.deltaTime);
                            else
                                ChangeGuiSize(Time.deltaTime);
                        }
                        else
                        {
                            Scroll(1);
                        }
                        break;
                    case Controller.TrackpadDirection.Down:
                        if (_Target.IsOwned)
                        {
                            if (_triggerPressed)
                                MoveGui(-Time.deltaTime);
                            else
                                ChangeGuiSize(-Time.deltaTime);
                        }
                        else
                        {
                            Scroll(-1);
                        }
                        break;
                    default:
                        _NextScrollTime = null;
                        break;
                }

                if (Device.GetPressDown(EVRButtonId.k_EButton_Grip) && !_Target.IsOwned)
                {
                    _Target.transform.SetParent(_Controller.transform, worldPositionStays: true);
                    _Target.IsOwned = true;
                }
                else if (Device.GetPressUp(EVRButtonId.k_EButton_Grip))
                {
                    AbandonGUI();
                }
                //UpdateHelp();
            }
        }
        private void ChangeGuiSize(float number)
        {
            _Target.transform.localScale *= 1 + number;
        }
        private void MoveGui(float number)
        {
            _Target.transform.position += number * 0.6f * Laser.transform.forward;
        }
        private void Scroll(int amount)
        {
            if (_NextScrollTime == null)
            {
                _NextScrollTime = Time.unscaledTime + 0.5f;
            }
            else if (_NextScrollTime < Time.unscaledTime)
            {
                _NextScrollTime += 0.1f;
            }
            else
            {
                return;
            }
            VR.Input.Mouse.VerticalScroll(amount);
        }

        //private void UpdateHelp()
        //{
        //    if (_AppButtonPressTime is float pressTime)
        //    {
        //        if (pressTime + 0.5f < Time.unscaledTime && _HelpTexts.Count == 0)
        //        {
        //            ShowHelp();
        //        }
        //        else if (!Device.GetPress(EVRButtonId.k_EButton_ApplicationMenu))
        //        {
        //            _AppButtonPressTime = null;
        //            HideHelp();
        //        }
        //    }
        //    else
        //    {
        //        if (Device.GetPress(EVRButtonId.k_EButton_ApplicationMenu))
        //        {
        //            _AppButtonPressTime = Time.unscaledTime;
        //        }
        //    }
        //}

        //private void ShowHelp()
        //{
        //    var trigger = _Controller.FindAttachPosition("trigger");
        //    var trackpad = _Controller.FindAttachPosition("trackpad") ?? _Controller.FindAttachPosition("thumbstick");
        //    var grip = _Controller.FindAttachPosition("lgrip") ?? _Controller.FindAttachPosition("handgrip");
        //    _HelpTexts.Add(HelpText.Create("Click", trigger, new Vector3(0.06f, -0.04f, -0.05f)));
        //    _HelpTexts.Add(HelpText.Create("Right click", trackpad, new Vector3(0.05f, 0.04f, 0), new Vector3(0.01f, 0, 0)));
        //    _HelpTexts.Add(HelpText.Create("Middle click", trackpad, new Vector3(0, 0.06f, 0.02f)));
        //    _HelpTexts.Add(HelpText.Create("Scroll up", trackpad, new Vector3(0, 0.04f, 0.07f), new Vector3(0, 0, 0.01f)));
        //    _HelpTexts.Add(HelpText.Create("Scroll down", trackpad, new Vector3(0, 0.04f, -0.05f), new Vector3(0, 0, -0.01f)));
        //    _HelpTexts.Add(HelpText.Create("Grab screen", grip, new Vector3(-0.06f, 0, -0.05f)));
        //}

        //private void HideHelp()
        //{
        //    foreach (var help in _HelpTexts)
        //    {
        //        Destroy(help.gameObject);
        //    }
        //    _HelpTexts.Clear();
        //}

        //bool IsResizing
        //{
        //    get
        //    {
        //        return _ResizeHandler && _ResizeHandler.IsDragging;
        //    }
        //}
        void CheckForNearMenu()
        {
            _Target = GUIQuadRegistry.Quads.FirstOrDefault(IsLaserable);
            if (_Target)
            {
                LaserVisible = true;
            }
        }

        bool IsLaserable(GUIQuad quad)
        {
            return IsWithinRange(quad) && Raycast(quad, out _);
        }

        float GetRange(GUIQuad quad)
        {
            return quad.transform.localScale.z;
            //return Mathf.Clamp(quad.transform.localScale.magnitude * RANGE, RANGE, RANGE * 5) * VR.Settings.IPDScale;
        }
        bool IsWithinRange(GUIQuad quad)
        {
            if (!Laser) return false;

            return (quad.transform.position - Laser.transform.position).magnitude < GetRange(quad);
            //var normal = -quad.transform.forward;
            ////var otherPos = quad.transform.position;

            //var myPos = Laser.transform.position;
            //var laser = Laser.transform.forward;
            //var heightOverMenu = -quad.transform.InverseTransformPoint(myPos).z * quad.transform.localScale.magnitude;
            //return heightOverMenu > 0 && heightOverMenu < GetRange(quad)
            //    && Vector3.Dot(normal, laser) < 0; // They have to point the other way
        }

        bool Raycast(GUIQuad quad, out RaycastHit hit)
        {
            //var myPos = Laser.transform.position;
            //var laser = Laser.transform.forward;
            //var collider = quad.GetComponent<Collider>();
            //if (collider)
            //{
                //var ray = new Ray(myPos, laser);
                // So far so good. Now raycast!
                return quad.GetComponent<Collider>().Raycast(new Ray(Laser.transform.position, Laser.transform.forward), out hit, GetRange(quad));
            //}
            //else
            //{
            //    hit = new RaycastHit();
            //    return false;
            //}
        }

        void UpdateLaser()
        {
            if (_Target 
                && _Target.gameObject.activeInHierarchy
                && IsWithinRange(_Target) 
                && Raycast(_Target, out var hit))
            {
                Laser.SetPosition(0, Laser.transform.position);
                Laser.SetPosition(1, hit.point);
                if (!IsOtherWorkingOn(_Target))
                {
                    var newPos = new Vector2(hit.textureCoord.x * VRGUI.Width, (1 - hit.textureCoord.y) * VRGUI.Height);
                    //VRLog.Info("New Pos: {0}, textureCoord: {1}", newPos, hit.textureCoord);
                    if (!mouseDownPosition.HasValue || Vector2.Distance(mouseDownPosition.Value, newPos) > MOUSE_STABILIZER_THRESHOLD)
                    {
                        SetMousePosition(newPos);
                        mouseDownPosition = null;
                    }
                }
            }
            else
            {
                // May day, may day -- window is gone!
                LaserVisible = false;
                ClearPresses();
            }
        }

        private void ClearPresses()
        {
            AbandonGUI();
            //HideHelp();
            if ((_PressedButtons & Buttons.Left) != 0)
            {
                VR.Input.Mouse.LeftButtonUp();
            }
            if ((_PressedButtons & Buttons.Right) != 0)
            {
                VR.Input.Mouse.RightButtonUp();
            }
            if ((_PressedButtons & Buttons.Middle) != 0)
            {
                VR.Input.Mouse.MiddleButtonUp();
            }
            _PressedButtons = 0;
            _NextScrollTime = null;
            _AppButtonPressTime = null;
        }

        private void AbandonGUI()
        {
            if (_Target && _Target.transform.parent == _Controller.transform)
            {
                _Target.transform.SetParent(VR.Camera.Origin, true);
                _Target.IsOwned = false;
            }
        }

        private bool IsOtherWorkingOn(GUIQuad target)
        {
            return _Other && _Other.LaserVisible && _Other._Target == target && _Other.IsPressing;
        }

        public bool LaserVisible
        {
            get
            {
                return Laser && Laser.gameObject.activeSelf;
            }
            set
            {
                if (!Laser) return;

                if (value && !_LaserLock.IsValid)
                {
                    // Need to acquire focus!
                    _LaserLock = _Controller.AcquireFocus();
                    if (!_LaserLock.IsValid)
                    {
                        // Could not get focus, do nothing.
                        return;
                    }
                }
                else if (!value && _LaserLock.IsValid)
                {
                    // Need to release focus!
                    _LaserLock.Release();
                }

                // Toggle laser
                Laser.gameObject.SetActive(value);

                // Initialize start position
                if (value)
                {
                    Laser.SetPosition(0, Laser.transform.position);
                    Laser.SetPosition(1, Laser.transform.position);
                }
                else
                {
                    mouseDownPosition = null;
                }
            }
        }

        public bool IsPressing => _PressedButtons != 0;

        private static void SetMousePosition(Vector2 newPos)
        {
            int x = (int)Mathf.Round(newPos.x);
            int y = (int)Mathf.Round(newPos.y);
            var clientRect = WindowManager.GetClientRect();
            var virtualScreenRect = WindowManager.GetVirtualScreenRect();
            VR.Input.Mouse.MoveMouseToPositionOnVirtualDesktop(
                (clientRect.Left + x - virtualScreenRect.Left) * 65535.0 / (virtualScreenRect.Right - virtualScreenRect.Left),
                (clientRect.Top + y - virtualScreenRect.Top) * 65535.0 / (virtualScreenRect.Bottom - virtualScreenRect.Top));
        }

        //class ResizeHandler : ProtectedBehaviour
        //{
        //    GUIQuad _Gui;
        //    Vector3? _StartLeft;
        //    Vector3? _StartRight;
        //    Vector3? _StartScale;
        //    Quaternion? _StartRotation;
        //    Vector3? _StartPosition;
        //    Quaternion _StartRotationController;
        //    Vector3? _OffsetFromCenter;

        //    public bool IsDragging { get; private set; }
        //    protected override void OnStart()
        //    {
        //        base.OnStart();
        //        _Gui = GetComponent<GUIQuad>();
        //    }

        //    protected override void OnUpdate()
        //    {
        //        base.OnUpdate();
        //        IsDragging = GetDevice(VR.Mode.Left).GetPress(EVRButtonId.k_EButton_Grip) &&
        //               GetDevice(VR.Mode.Right).GetPress(EVRButtonId.k_EButton_Grip);

        //        if (IsDragging)
        //        {
        //            if (_StartScale == null)
        //            {
        //                Initialize();
        //            }
        //            var newLeft = VR.Mode.Left.transform.position;
        //            var newRight = VR.Mode.Right.transform.position;

        //            var distance = Vector3.Distance(newLeft, newRight);
        //            var originalDistance = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
        //            var newDirection = newRight - newLeft;
        //            var newCenter = newLeft + newDirection * 0.5f;

        //            // It would probably be easier than that but Quaternions have never been a strength of mine...
        //            var inverseOriginRot = Quaternion.Inverse(VR.Camera.SteamCam.origin.rotation);
        //            var avgRot = GetAverageRotation();
        //            var rotation = (inverseOriginRot * avgRot) * Quaternion.Inverse(inverseOriginRot * _StartRotationController);

        //            _Gui.transform.localScale = (distance / originalDistance) * _StartScale.Value;
        //            _Gui.transform.localRotation = rotation * _StartRotation.Value;
        //            _Gui.transform.position = newCenter + (avgRot * Quaternion.Inverse(_StartRotationController)) * _OffsetFromCenter.Value;

        //        }
        //        else
        //        {
        //            _StartScale = null;
        //        }
        //    }

        //    private Quaternion GetAverageRotation()
        //    {
        //        var leftPos = VR.Mode.Left.transform.position;
        //        var rightPos = VR.Mode.Right.transform.position;

        //        var right = (rightPos - leftPos).normalized;
        //        var up = Vector3.Lerp(VR.Mode.Left.transform.forward, VR.Mode.Right.transform.forward, 0.5f);
        //        var forward = Vector3.Cross(right, up).normalized;

        //        return Quaternion.LookRotation(forward, up);
        //    }
        //    private void Initialize()
        //    {
        //        _StartLeft = VR.Mode.Left.transform.position;
        //        _StartRight = VR.Mode.Right.transform.position;
        //        _StartScale = _Gui.transform.localScale;
        //        _StartRotation = _Gui.transform.localRotation;
        //        _StartPosition = _Gui.transform.position;
        //        _StartRotationController = GetAverageRotation();
                
        //        var originalDistance = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
        //        var originalDirection = _StartRight.Value - _StartLeft.Value;
        //        var originalCenter = _StartLeft.Value + originalDirection * 0.5f;
        //        _OffsetFromCenter = transform.position - originalCenter;
        //    }


        //    private SteamVR_Controller.Device GetDevice(Controller controller)
        //    {
        //        return SteamVR_Controller.Input((int)controller.Tracking.index);
        //    }
        //}
    }
}