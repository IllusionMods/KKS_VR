using UnityEngine;
using VRGIN.Core;

namespace KKS_VR.Interpreters
{
    internal class HSceneInterpreter : SceneInterpreter
    {
        private bool _active;
        private HSceneProc _proc;
        private Caress.VRMouth _vrMouth;
        private Caress.CaressController _leftController;
        private Caress.CaressController _rightController;
        private Camera.POV _pov;

        private Color _currentBackgroundColor;
        private bool _currentShowMap;

        public override void OnStart()
        {
            _leftController = VR.Mode.Left.gameObject.AddComponent<Caress.CaressController>();
            _rightController = VR.Mode.Left.gameObject.AddComponent<Caress.CaressController>();
            _pov = VR.Camera.gameObject.AddComponent<Camera.POV>();
            _pov.Initialize(_leftController.getController(), _rightController.getController());
            _currentBackgroundColor = Manager.Config.HData.BackColor;
            _currentShowMap = Manager.Config.HData.Map;
            UpdateCameraState();
        }

        public override void OnDisable()
        {
            Deactivate();
            Object.Destroy(_pov);
            Object.Destroy(_leftController);
            Object.Destroy(_rightController);
        }

        public override void OnUpdate()
        {
            if (_currentShowMap != Manager.Config.HData.Map || _currentBackgroundColor != Manager.Config.HData.BackColor)
            {
                if (!_active || !_pov.IsActive())
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
