using UnityEngine;
using VRGIN.Core;

namespace KKS_VR.Interpreters
{
    internal class HSceneInterpreter : SceneInterpreter
    {
        private bool _active;
        private HSceneProc _proc;
        private Caress.VRMouth _vrMouth;

        public override void OnStart()
        {
        }

        public override void OnDisable()
        {
            Deactivate();
        }

        public override void OnUpdate()
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
    }
}
