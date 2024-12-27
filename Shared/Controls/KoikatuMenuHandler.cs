using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using VRGIN.Controls;
using VRGIN.Visuals;
using System.Linq;
using VRGIN.Native;
using static VRGIN.Native.WindowsInterop;
using KK_VR.Holders;

namespace KK_VR.Controls
{
    internal class KoikatuMenuHandler
    {        /// <summary>
             /// Handler that is in charge of the menu interaction with controllers
             /// </summary>
        private readonly Transform _controller;
        private const int MOUSE_STABILIZER_THRESHOLD = 30; // pixels
                                                           //private Controller.Lock _LaserLock = Controller.Lock.Invalid;
        private LineRenderer Laser;
        private Vector2? _mouseDownPosition;
        private GUIQuad _quad;
        private Vector3 _scaleVector;
        private Buttons _pressedButtons;
        private Controller.TrackpadDirection _lastDirection;
        private float? _NextScrollTime; 
        internal bool LaserVisible
        {
            get
            {
                return Laser.gameObject.activeSelf;
            }
            set
            {
                // Toggle laser
                Laser.gameObject.SetActive(value);
                //VRPlugin.Logger.LogDebug($"LaserVisible = {value}");
                // Initialize start position
                if (value)
                {
                    Laser.SetPosition(0, Laser.transform.position);
                    Laser.SetPosition(1, Laser.transform.position);
                }
                else
                {
                    _mouseDownPosition = null;
                }
            }
        }

        internal void SetLastDirection(Controller.TrackpadDirection direction) => _lastDirection = direction;

        internal KoikatuMenuHandler(Controller controller)
        {
            //_controller = controller;
            _scaleVector = new Vector2((float)VRGUI.Width / Screen.width, (float)VRGUI.Height / Screen.height);
            _controller = controller.transform;
            var attachPosition = controller.FindAttachPosition("tip");

            if (!attachPosition)
            {
                VRLog.Error("Attach position not found for laser!");
                attachPosition = _controller;
            }
            Laser = new GameObject("Laser").AddComponent<LineRenderer>();
            Laser.transform.SetParent(_controller, worldPositionStays: false); // (attachPosition, false);
            //Laser.transform.SetParent(controller.transform, false); // (attachPosition, false);
            Laser.transform.SetPositionAndRotation(attachPosition.position, attachPosition.rotation);
            Laser.material = new Material(Shader.Find("Sprites/Default"));
            Laser.material.renderQueue += 5000;
            Laser.startColor = new Color(0f, 1f, 1f, 0f);
            Laser.endColor = Color.cyan;

            if (SteamVR.instance.hmd_TrackingSystemName == "lighthouse")
            {
                Laser.transform.localRotation = Quaternion.Euler(60, 0, 0);
                Laser.transform.position += Laser.transform.forward * 0.06f;
            }
            else
            {
#if KK
                Laser.transform.localRotation *= Quaternion.Euler(-10f, 0, 0);
#else
                Laser.transform.localRotation *= Quaternion.Euler(25f, 0, 0);
#endif
            }
            Laser.SetVertexCount(2);
            Laser.useWorldSpace = true;
            Laser.SetWidth(0.002f, 0.002f);
            LaserVisible = false;
        }

        enum Buttons
        {
            Left = 1,
            Right = 2,
            Middle = 4,
        }

        internal bool CheckMenu()
        {
            if (LaserVisible)
            {
                CheckInput();
                return UpdateLaser();
            }
            else
            {
                return CheckForNearMenu();
            }
        }

        internal void SetLaserVisibility(bool show)
        {
            LaserVisible = show;
        }

        internal void OnTrigger(bool press)
        {
            if (press)
            {
                VR.Input.Mouse.LeftButtonDown();
                _pressedButtons |= Buttons.Left;
                _mouseDownPosition = Vector2.Scale(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y), _scaleVector);
            }
            else
            {
                VR.Input.Mouse.LeftButtonUp();
                _pressedButtons &= ~Buttons.Left;
                _mouseDownPosition = null;
            }
        }
        internal void OnGrip(bool press)
        {
            if (press && !_quad.IsOwned)
            {
                _quad.transform.SetParent(_controller, worldPositionStays: true);
                _quad.IsOwned = true;
            }
            else
            {
                AbandonGUI();
            }
        }
        internal void OnTouchpad(bool press)
        {
            if (press)
            {
                if (_quad.IsOwned && (_pressedButtons & Buttons.Left) != 0)
                {
                    _quad.transform.SetParent(VR.Manager.transform, worldPositionStays: true);
                    _quad.IsOwned = false;
                }
                else
                {

                    VR.Input.Mouse.MiddleButtonDown();
                    _pressedButtons |= Buttons.Middle;
                }
            }
            else if ((_pressedButtons & Buttons.Middle) != 0)
            {
                VR.Input.Mouse.MiddleButtonUp();
                _pressedButtons &= ~Buttons.Middle;
            }
        }
        private void CheckInput()
        {
            switch (_lastDirection)
            {
                case Controller.TrackpadDirection.Up:
                    if (_quad.IsOwned)
                    {
                        if ((_pressedButtons & Buttons.Left) != 0)
                        {
                            MoveGui(Time.deltaTime);
                        }
                        else
                        {
                            ChangeGuiSize(Time.deltaTime);
                        }
                    }
                    else
                    {
                        Scroll(1);
                    }
                    break;
                case Controller.TrackpadDirection.Down:
                    if (_quad.IsOwned)
                    {
                        if ((_pressedButtons & Buttons.Left) != 0)
                        {
                            MoveGui(-Time.deltaTime);
                        }
                        else
                        {
                            ChangeGuiSize(-Time.deltaTime);
                        }
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
        }

        private void ChangeGuiSize(float number)
        {
            _quad.transform.localScale *= 1 + number;
        }

        private void MoveGui(float number)
        {
            _quad.transform.position += number * 0.6f * Laser.transform.forward;
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

        private bool CheckForNearMenu()
        {
            _quad = GUIQuadRegistry.Quads.FirstOrDefault(IsLaserable);
            if (_quad != null)
            {
                //VRPlugin.Logger.LogDebug($"CheckForNearMenu:Hit");
                return true;
            }
            //VRPlugin.Logger.LogDebug($"CheckForNearMenu:Miss");
            return false;
        }

        private bool IsLaserable(GUIQuad quad)
        {
            //return IsWithinRange(quad) && Raycast(quad, out _);
            return Raycast(quad, out _);
        }

        private float GetRange(GUIQuad quad)
        {
            return quad.transform.localScale.z;
        }
        private bool IsWithinRange(GUIQuad quad)
        {
            return (quad.transform.position - Laser.transform.position).magnitude < GetRange(quad);
        }

        private bool Raycast(GUIQuad quad, out RaycastHit hit)
        {
            return quad.GetComponent<Collider>().Raycast(new Ray(Laser.transform.position, Laser.transform.forward), out hit, GetRange(quad));
        }

        private bool UpdateLaser()
        {
            if (_quad && _quad.gameObject.activeInHierarchy
                && Raycast(_quad, out var hit))
            {
                Laser.SetPosition(0, Laser.transform.position);
                Laser.SetPosition(1, hit.point);

                var newPos = new Vector2(hit.textureCoord.x * VRGUI.Width, (1 - hit.textureCoord.y) * VRGUI.Height);
                //VRLog.Info("New Pos: {0}, textureCoord: {1}", newPos, hit.textureCoord);
                if (!_mouseDownPosition.HasValue || Vector2.Distance(_mouseDownPosition.Value, newPos) > MOUSE_STABILIZER_THRESHOLD)
                {
                    SetMousePosition(newPos);
                    _mouseDownPosition = null;
                }
                //VRPlugin.Logger.LogDebug($"UpdateLaser:On");
                //VRPlugin.Logger.LogDebug($"MenuHandler:UpdateLaser:Success");
                return true;
            }
            else
            {
                // May day, may day -- window is gone!

                //VRPlugin.Logger.LogDebug($"UpdateLaser:Off");
                LaserVisible = false;
                ClearPresses();
                return false;
            }
        }

        private void ClearPresses()
        {
            AbandonGUI();
            if ((_pressedButtons & Buttons.Left) != 0)
            {
                VR.Input.Mouse.LeftButtonUp();
            }
            if ((_pressedButtons & Buttons.Right) != 0)
            {
                VR.Input.Mouse.RightButtonUp();
            }
            if ((_pressedButtons & Buttons.Middle) != 0)
            {
                VR.Input.Mouse.MiddleButtonUp();
            }
            _pressedButtons = 0;
            _NextScrollTime = null;
        }

        private void AbandonGUI()
        {
            if (_quad && _quad.transform.parent == _controller.transform)
            {
                _quad.transform.SetParent(VR.Camera.Origin, true);
                _quad.IsOwned = false;
            }
        }

        


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


