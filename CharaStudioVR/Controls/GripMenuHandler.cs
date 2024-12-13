using System.Linq;
using KK_VR.Settings;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Native;
using VRGIN.Visuals;

namespace KK_VR.Controls
{
    public class GripMenuHandler : ProtectedBehaviour
    {
        private Controller _Controller;
        //private ResizeHandler _ResizeHandler;
        private Vector3 _ScaleVector;
        private GUIQuad _Target;
        private LineRenderer Laser;
        private Vector2? mouseDownPosition;
        protected DeviceLegacyAdapter Device => _Controller.Input;

        private readonly EVRButtonId leftClickButton = EVRButtonId.k_EButton_Axis1;
        private readonly EVRButtonId wheelAxis = EVRButtonId.k_EButton_Axis0;
        private readonly float wheelDeadzone = 0.001f;
        private float wheelTime = 0f;

        private bool IsResizing
        {
            get
            {
                //if ((bool)_ResizeHandler) return _ResizeHandler.IsDragging;
                return false;
            }
        }

        public bool LaserVisible
        {
            get
            {
                if ((bool)Laser) return Laser.gameObject.activeSelf;
                return false;
            }
            set
            {
                if ((bool)Laser)
                {
                    Laser.gameObject.SetActive(value);
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
        }

        public bool IsPressing { get; private set; }

        protected override void OnStart()
        {
            base.OnStart();
            _Controller = gameObject.GetComponentInChildren<Controller>(true);
            _ScaleVector = new Vector2(VRGUI.Width / (float)Screen.width, VRGUI.Height / (float)Screen.height);
            InitLaser();
        }

        private void InitLaser()
        {
            Laser = new GameObject().AddComponent<LineRenderer>();
            Laser.transform.SetParent(transform, false);
            Laser.material = new Material(VR.Context.Materials.Sprite);
            Laser.material.renderQueue += 5000;
            Laser.startColor = Laser.endColor = Color.cyan;
            Laser.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            Laser.transform.position += Laser.transform.forward * 0.07f * VR.Context.Settings.IPDScale;
            Laser.positionCount = 2;
            Laser.useWorldSpace = true;
            Laser.startWidth = Laser.endWidth = 0.002f * VR.Context.Settings.IPDScale;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!VR.Camera.gameObject.activeInHierarchy) return;
            if (LaserVisible)
            {
                if (IsResizing)
                {
                    Laser.SetPosition(0, Laser.transform.position);
                    Laser.SetPosition(1, Laser.transform.position);
                }
                else
                {
                    UpdateLaser();
                }
            }
            else if (_Controller.CanAcquireFocus())
            {
                CheckForNearMenu();
            }

            CheckInput();
        }

        private void OnDisable()
        {
            LaserVisible = false;
        }

        protected void CheckInput()
        {
            IsPressing = false;
            if (LaserVisible && (bool)_Target && !IsResizing)
            {
                if (StudioSettings.EnableHairTriggerClick.Value && Device.GetHairTriggerDown())
                {
                    MouseOperations.MouseEvent(WindowsInterop.MouseEventFlags.LeftDown);
                    MouseOperations.MouseEvent(WindowsInterop.MouseEventFlags.LeftUp);
                }

                if (Device.GetPressDown(leftClickButton))
                {
                    IsPressing = true;
                    MouseOperations.MouseEvent(WindowsInterop.MouseEventFlags.LeftDown);
                    mouseDownPosition = Vector2.Scale(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y), _ScaleVector);
                }

                if (Device.GetPress(leftClickButton))
                    IsPressing = true;

                if (Device.GetPressUp(leftClickButton))
                {
                    IsPressing = true;
                    MouseOperations.MouseEvent(WindowsInterop.MouseEventFlags.LeftUp);
                    mouseDownPosition = null;
                }

                if (Device.GetTouch(wheelAxis))
                {
                    var axis = Device.GetAxis(wheelAxis);
                    if (Mathf.Abs(axis.y) > wheelDeadzone)
                    {
                        wheelTime += Time.deltaTime;
                        if (wheelTime > StudioSettings.WheelRepeatTime.Value)
                        {
                            wheelTime = 0f;
                            if (axis.y > wheelDeadzone)
                                MouseWheelEvent(120);
                            else if (axis.y < -wheelDeadzone)
                                MouseWheelEvent(-120);
                        }
                    }
                    else
                    {
                        wheelTime = 0f;
                    }
                }
            }
        }

        public static void MouseWheelEvent(int amount)
        {
            var cursorPosition = MouseOperations.GetCursorPosition();
            WindowsInterop.mouse_event(0x0800, cursorPosition.X, cursorPosition.Y, amount, 0);
        }

        private void CheckForNearMenu()
        {
            _Target = GUIQuadRegistry.Quads.FirstOrDefault(IsLaserable);
            if ((bool)_Target) LaserVisible = true;
        }

        private bool IsLaserable(GUIQuad quad)
        {
            if (IsWithinRange(quad)) return Raycast(quad, out var _);
            return false;
        }

        private float GetRange(GUIQuad quad)
        {
            var scaledRange = StudioSettings.MaxLaserRange.Value * VR.Context.Settings.IPDScale;
            return Mathf.Clamp(quad.transform.localScale.magnitude * scaledRange, scaledRange, scaledRange * 5f);
        }

        private bool IsWithinRange(GUIQuad quad)
        {
            if (quad.transform.parent == transform) return false;
            var lhs = -quad.transform.forward;
            var position = Laser.transform.position;
            var forward = Laser.transform.forward;
            var num = (0f - quad.transform.InverseTransformPoint(position).z) * quad.transform.localScale.magnitude;
            if (num > 0f && num < GetRange(quad)) return Vector3.Dot(lhs, forward) < 0f;
            return false;
        }

        private bool Raycast(GUIQuad quad, out RaycastHit hit)
        {
            var position = Laser.transform.position;
            var forward = Laser.transform.forward;
            var component = quad.GetComponent<Collider>();
            if ((bool)component)
            {
                var ray = new Ray(position, forward);
                return component.Raycast(ray, out hit, GetRange(quad));
            }

            hit = default;
            return false;
        }

        private void UpdateLaser()
        {
            Laser.SetPosition(0, Laser.transform.position);
            Laser.SetPosition(1, Laser.transform.position + Laser.transform.forward);
            if ((bool)_Target && _Target.gameObject.activeInHierarchy)
            {
                if (IsWithinRange(_Target) && Raycast(_Target, out var hit))
                {
                    Laser.SetPosition(1, hit.point);
                    if (!IsOtherWorkingOn(_Target))
                    {
                        var b = new Vector2(hit.textureCoord.x * VRGUI.Width, (1f - hit.textureCoord.y) * VRGUI.Height);
                        if (!mouseDownPosition.HasValue || Vector2.Distance(mouseDownPosition.Value, b) > 30f)
                        {
                            MouseOperations.SetClientCursorPosition((int)b.x, (int)b.y);
                            mouseDownPosition = null;
                        }
                    }
                }
                else
                {
                    LaserVisible = false;
                }
            }
            else
            {
                LaserVisible = false;
            }
        }

        private bool IsOtherWorkingOn(GUIQuad target)
        {
            return false;
        }

        private class ResizeHandler : ProtectedBehaviour
        {
            private GUIQuad _Gui;
            private Vector3? _OffsetFromCenter;
            private Vector3? _StartLeft;
            private Vector3? _StartPosition;
            private Vector3? _StartRight;
            private Quaternion? _StartRotation;
            private Quaternion _StartRotationController;
            private Vector3? _StartScale;
            public bool IsDragging { get; private set; }

            protected override void OnStart()
            {
                base.OnStart();
                _Gui = GetComponent<GUIQuad>();
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();
                IsDragging = GetDevice(VR.Mode.Left).GetPress(EVRButtonId.k_EButton_Axis1) && GetDevice(VR.Mode.Right).GetPress(EVRButtonId.k_EButton_Axis1);
                if (IsDragging)
                {
                    if (!_StartScale.HasValue) Initialize();
                    var position = VR.Mode.Left.transform.position;
                    var position2 = VR.Mode.Right.transform.position;
                    var num = Vector3.Distance(position, position2);
                    var num2 = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
                    var vector = position2 - position;
                    var vector2 = position + vector * 0.5f;
                    var quaternion = Quaternion.Inverse(VR.Camera.SteamCam.origin.rotation);
                    var averageRotation = GetAverageRotation();
                    var quaternion2 = quaternion * averageRotation * Quaternion.Inverse(quaternion * _StartRotationController);
                    _Gui.transform.localScale = num / num2 * _StartScale.Value;
                    _Gui.transform.localRotation = quaternion2 * _StartRotation.Value;
                    _Gui.transform.position = vector2 + averageRotation * Quaternion.Inverse(_StartRotationController) * _OffsetFromCenter.Value;
                }
                else
                {
                    _StartScale = null;
                }
            }

            private Quaternion GetAverageRotation()
            {
                var position = VR.Mode.Left.transform.position;
                var normalized = (VR.Mode.Right.transform.position - position).normalized;
                var vector = Vector3.Lerp(VR.Mode.Left.transform.forward, VR.Mode.Right.transform.forward, 0.5f);
                return Quaternion.LookRotation(Vector3.Cross(normalized, vector).normalized, vector);
            }

            private void Initialize()
            {
                _StartLeft = VR.Mode.Left.transform.position;
                _StartRight = VR.Mode.Right.transform.position;
                _StartScale = _Gui.transform.localScale;
                _StartRotation = _Gui.transform.localRotation;
                _StartPosition = _Gui.transform.position;
                _StartRotationController = GetAverageRotation();
                Vector3.Distance(_StartLeft.Value, _StartRight.Value);
                var vector = _StartRight.Value - _StartLeft.Value;
                var vector2 = _StartLeft.Value + vector * 0.5f;
                _OffsetFromCenter = transform.position - vector2;
            }

            private DeviceLegacyAdapter GetDevice(Controller controller)
            {
                return controller.Input;
            }
        }
    }
}
