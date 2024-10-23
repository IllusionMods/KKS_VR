using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Helpers;
using KKS_VR.Settings;


public enum POV_MODE
{
    // Press Key (default: Y) to change POV Mode, configurable in BepInEX Plugin Settings (F1)
    EYE = 0,      // Mode1: Tracking Eye Position (Default)
    TELEPORT = 1, // Mode2: Teleport(Jump) to next character when trigger controller
}

namespace KKS_VR.Camera
{
    internal class POV : ProtectedBehaviour
    {
        private Controller _leftController, _rightController;

        private ChaControl _currentChara;

        private POV_MODE _povMode;

        private bool _active;

        private KoikatuSettings _settings;

        public void Initialize(Controller left, Controller right)
        {
            _leftController = left;
            _rightController = right;
        }

        public bool IsActive()
        {
            return _active;
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            // HScene Init
            _currentChara = null;

            _povMode = POV_MODE.EYE;

            _active = false;

            _settings = VR.Context.Settings as KoikatuSettings;
        }

        private Transform GetHead(ChaControl human)
        {
            return human.objHead.GetComponentsInParent<Transform>().First((Transform t) => t.name.StartsWith("c") && t.name.ToLower().Contains("j_head"));
        }
        private Transform GetEyePose(ChaControl human)
        {
            Transform transform = human.objHeadBone.transform.Descendants().FirstOrDefault((Transform t) => t.name.StartsWith("c") && t.name.ToLower().EndsWith("j_faceup_tz"));
            if (transform == null)
            {
                VRLog.Info("Creating eyes, {0}", human.name);
                transform = new GameObject("cf_j_faceup_tz").transform;
                transform.SetParent(GetHead(human), false);
                transform.transform.localPosition = new Vector3(0f, 0.07f, 0.05f);
            }
            return transform;
        }

        private void UpdateCurrentChara(List<ChaControl> targets)
        {
            // Rotate targets only when currentCharaIdx is found and it is not the last element already
            int currentCharaIdx = targets.IndexOf(_currentChara);
            if (currentCharaIdx > -1 && currentCharaIdx < targets.Count - 1)
            {
                // Rotate the list using LINQ
                var rotated = targets.Skip(currentCharaIdx + 1).Concat(targets.Take(currentCharaIdx + 1)).ToList();
                targets.Clear();
                targets.AddRange(rotated);
            }

            foreach (var target in targets)
            {
                if (target.sex == 1) // 1 is female, only choose male as target
                    continue;

                // Set Camera to Character Eye
                var targetEyePose = GetEyePose(target);
                var tvec = targetEyePose.position;
                var quat = Quaternion.Euler(0, targetEyePose.rotation.eulerAngles.y, 0);
                VRManager.Instance.Mode.MoveToPosition(tvec, quat, false);
                VRLog.Info("POV target found, {0}", target.name);
                _currentChara = target;
                return;
            }
        }

        private void SetPOV()
        {
            if (_currentChara == null) return;

            switch (_povMode)
            {
                case POV_MODE.EYE:
                    VRManager.Instance.Mode.MoveToPosition(GetEyePose(_currentChara).position, false);
                    break;
                case POV_MODE.TELEPORT:
                default:
                    break;
            }
        }

        protected override void OnUpdate()
        {
            if (_settings.EnablePOV == false) return;
            // Press Key (default: Y) to change POV Mode, configurable in BepInEX Plugin Settings (F1)
            if (POVConfig.switchPOVModeKey.Value.IsDown())
            {
                _povMode = (POV_MODE)(((int)_povMode + 1) % Enum.GetValues(typeof(POV_MODE)).Length);
                VRLog.Info("Switching to POV_MODE {0}", (int)_povMode);
            }

            var isControllerActive =
                (VR.Mode.Left.ToolIndex == 2 && VR.Mode.Left.ActiveTool.isActiveAndEnabled) ||
                (VR.Mode.Right.ToolIndex == 2 && VR.Mode.Right.ActiveTool.isActiveAndEnabled);
            var isTriggered =
                _leftController.Input.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger) ||
                _rightController.Input.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);

            // When left | right hand controller is Hand Tool and visible
            if (isControllerActive && isTriggered)
            {
                // Press VR Trigger button to set _active
                if (!_active)
                {
                    _active = true;
                }
                List<ChaControl> targets = GameObject.FindObjectsOfType<ChaControl>().ToList();
                UpdateCurrentChara(targets);
            }
            // When there is no Hand Tool, deactive
            else if (_active && VR.Mode.Left.ToolIndex != 2 && VR.Mode.Right.ToolIndex != 2)
            {
                _active = false;
            }

            // Only update POV if active
            if (_active)
            {
                SetPOV();
            }
        }
    }
}
