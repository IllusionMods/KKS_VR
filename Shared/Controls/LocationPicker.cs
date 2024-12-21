using System;
using System.Collections;
using System.Linq;
using H;
using HarmonyLib;
using Illusion.Game;
using Manager;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;
using Utils = Illusion.Game.Utils;

namespace KK_VR.Controls
{
    /// <summary>
    /// A component to add to the controllers the ability to pick a new location in H scenes.
    /// </summary>
    internal class LocationPicker : MonoBehaviour
    {
        private Controller _controller;
        private LineRenderer _laser;
        private bool _laserEnabled;
        private Controller.Lock _lock; // may be null. Also, null if _selection is null.
        private HPointData _selection; // may be null
        private Animator _selectionAnim; // may be null. Also, null if _selection is null.

        internal static void AddComponents()
        {
            VR.Mode.Left.gameObject.AddComponent<LocationPicker>();
            VR.Mode.Right.gameObject.AddComponent<LocationPicker>();
        }

        internal static void DestroyComponents()
        {
#if KK
            var leftComponent = VR.Mode.Left.gameObject.GetComponent<LocationPicker>();
            if (leftComponent != null)
#elif KKS
            if (VR.Mode.Left.gameObject.TryGetComponent<LocationPicker>(out var leftComponent))
#endif
            {
                UnityEngine.Object.Destroy(leftComponent);
            }
#if KK
            var rightComponent = VR.Mode.Right.gameObject.GetComponent<LocationPicker>();
            if (rightComponent != null)
#elif KKS
            if (VR.Mode.Right.gameObject.TryGetComponent<LocationPicker>(out var rightComponent))
#endif
            {
                UnityEngine.Object.Destroy(rightComponent);
            }
        }
        private void Awake()
        {
            _controller = GetComponent<Controller>();
            AddLaser();
        }

        private void Update()
        {
            // TODO: somehow arrange that this component is only enabled during location selection?
#if KK
            if (Manager.Scene.Instance.NowSceneNames[0].Equals("HPointMove")
#elif KKS
            if (Scene.NowSceneNames[0] == "HPointMove"
#endif
                && (_lock != null || _controller.CanAcquireFocus()))
            {
                if (!_laserEnabled)
                {
                    _laserEnabled = true;
                    _laser.gameObject.SetActive(true);
                }

                UpdateSelection();
                HandleTrigger();
            }
            else if (_laserEnabled)
            {
                CleanupSelection();
                _laserEnabled = false;
                _laser.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update _selection, _selectionAnim and _lock depending on which
        /// location we are currently pointing at.
        /// Also triggers animation and plays sound.
        /// </summary>
        private void UpdateSelection()
        {
            var ray = new Ray(_laser.transform.position, _laser.transform.TransformDirection(Vector3.forward));
            var hit = Physics.RaycastAll(ray)
                .Where(h => h.collider.CompareTag("H/HPoint"))
                .OrderBy(h => h.distance)
                .FirstOrDefault();

            if (hit.collider != null)
            {
                var hPointData = hit.collider.transform.parent.GetComponent<HPointData>();
                if (hPointData != _selection) 
                    Select(hPointData);
            }
            else
            {
                Unselect();
            }
        }

        /// <summary>
        /// Initiate a location change if the trigger is pulled.
        /// </summary>
        private void HandleTrigger()
        {
            var device = _controller.Input;
            if (_lock == null || !device.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger)) return;
            var hPointMove = FindObjectOfType<HPointMove>();
            if (hPointMove == null)
            {
                VRLog.Warn("LocationPicker: failed to find HPointMove");
                return;
            }

            var trav = new Traverse(hPointMove);
            var selection = _selection;
            var actionSelect = trav.Field<Action<HPointData, int>>("actionSelect").Value;
            var category = trav.Field<int>("nowCategory").Value;

            StartCoroutine(ChangeLocation(() => actionSelect(selection, category)));
        }

        private static IEnumerator ChangeLocation(Action action)
        {
            yield return null;
            action();
#if KK
            Manager.Scene.Instance.UnLoad();
#else
            Scene.Unload();
#endif
        }

        private void Unselect()
        {
            if (_selectionAnim != null)
            {
                if (_selectionAnim.GetCurrentAnimatorStateInfo(0).IsName("upidle"))
                {
                    _selectionAnim.SetTrigger("down");
                }
                else
                {
                    _selectionAnim.Play("idle");
                }
            }
            CleanupSelection();
        }

        private void CleanupSelection()
        {
            _selection = null;
            _selectionAnim = null;
            _lock?.Release();
            _lock = null;
        }

        private void Select(HPointData point)
        {
            Unselect();
            if (point == null) return;
            _selection = point;

            var anim = point.GetComponentInChildren<Animator>();
            if (anim == null) return;
            _selectionAnim = anim;

            if (anim.GetCurrentAnimatorStateInfo(0).IsName("idle")) anim.SetTrigger("up");
            Utils.Sound.Play(SystemSE.sel);
            _controller.TryAcquireFocus(out _lock);
        }

        private void AddLaser()
        {
            var attachPosition = _controller.transform.Find("Laser");
            if (!attachPosition)
            {
                VRPlugin.Logger.LogWarning("Attach position not found for laser!");
                attachPosition = transform;
            }

            _laser = new GameObject("LocationPicker").AddComponent<LineRenderer>();

            _laser.transform.parent = _controller.transform;
            _laser.transform.SetPositionAndRotation(attachPosition.position, attachPosition.rotation);

            _laser.material = new Material(Shader.Find("Sprites/Default"));
            _laser.startColor = Color.cyan;
            _laser.endColor = new Color(1f, 1f, 1f, 0f);
            _laser.positionCount = 2;
            _laser.useWorldSpace = false;
            _laser.startWidth = _laser.endWidth = 0.002f;
            _laser.SetPosition(0, Vector3.zero);
            _laser.SetPosition(1, Vector3.forward * 5);
            _laser.gameObject.SetActive(false);
        }
    }
}
