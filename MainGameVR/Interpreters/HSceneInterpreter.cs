using UnityEngine;
using VRGIN.Core;

namespace KKS_VR.Interpreters
{
    internal class HSceneInterpreter : SceneInterpreter
    {
        private bool _active;
        private HSceneProc _proc;
        private Caress.VRMouth _vrMouth;

        private Color _currentBackgroundColor;
        private bool _currentShowMap;

        public override void OnStart()
        {
            _currentBackgroundColor = Manager.Config.HData.BackColor;
            _currentShowMap = Manager.Config.HData.Map;
            UpdateCameraState();
        }

        public override void OnDisable()
        {
            Deactivate();
        }

        public override void OnUpdate()
        {
            if (_currentShowMap != Manager.Config.HData.Map || _currentBackgroundColor != Manager.Config.HData.BackColor)
            {
                UpdateCameraState();
            }

            if (_active && (!_proc || !_proc.enabled))
            {
                // The HProc scene is over, but there may be one more coming.
                Deactivate();
            }

            if (!_active &&
                Manager.Scene.GetRootComponent<HSceneProc>("HProc") is HSceneProc proc &&
                proc.enabled)
            {
                _vrMouth = VR.Camera.gameObject.AddComponent<Caress.VRMouth>();
                AddControllerComponent<Caress.CaressController>();
                _proc = proc;
                _active = true;
            }
        }

        private void Deactivate()
        {
            if (_active)
            {
                VR.Camera.SteamCam.camera.clearFlags = CameraClearFlags.Skybox;
                Object.Destroy(_vrMouth);
                DestroyControllerComponent<Caress.CaressController>();
                _proc = null;
                _active = false;
            }
        }

        private void UpdateCameraState()
        {
            if (!Manager.Config.HData.Map)
            {
                VR.Camera.SteamCam.camera.backgroundColor = Manager.Config.HData.BackColor;
                VR.Camera.SteamCam.camera.clearFlags = CameraClearFlags.SolidColor;
            }
            else
            {
                VR.Camera.SteamCam.camera.clearFlags = CameraClearFlags.Skybox;
            }
            _currentBackgroundColor = Manager.Config.HData.BackColor;
            _currentShowMap = Manager.Config.HData.Map;
        }
    }
}
