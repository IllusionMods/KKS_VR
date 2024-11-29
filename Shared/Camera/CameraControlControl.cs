using VRGIN.Core;

namespace KK_VR.Camera
{
    /// <summary>
    /// CameraControlControl disables a CameraControl_Ver2 object and
    /// do its work instead. It's meant to be attached to a GameObject
    /// to which a CameraControl_Ver2 is already attached.
    ///
    /// The CameraControl_Ver2 implments a camera that can be rotated,
    /// zoomed and moved with a mouse, and is used in H scenes and in Maker.
    /// It's also responsible for "map masking", a feature that hides
    /// walls that blocks the view of the character.
    ///
    /// In VR, these features are not only unnecessary but are harmful,
    /// because the control features fight with Unity's attempt to
    /// set the camera position, and map masking causes random parts
    /// of walls to dissappear.
    ///
    /// For this reason we want to disable almost all of its
    /// functionalities, except that we want to keep its ability to unlock
    /// cursor. We also ensure that the controlled camera always has the same
    /// transform as the VR camera, so that (1) characters can correctly look
    /// at the camera, and (2) the directional light, which is a child of the
    /// main camera, has the right orientation.
    /// </summary>
    internal class CameraControlControl : ProtectedBehaviour
    {
        private CameraControl_Ver2 _control;

        protected override void OnStart()
        {
            _control = GetComponent<CameraControl_Ver2>();
            if (_control == null)
            {
                VRLog.Error("CameraControlControl: CameraControl_Ver2 was not found");
            }

            if (_control.enabled)
                _control.enabled = false;
            else
                VRLog.Warn("control is already disabled");
        }

        protected override void OnUpdate()
        {
            var head = VR.Camera.Head;
            transform.SetPositionAndRotation(head.position, head.rotation);
            // One of the default macros from GameObjectList enables the camera
            // control. We make sure that it remains desabled.
            if (_control != null)
            {
                _control.enabled = false;
            }
        }

        protected override void OnLateUpdate()
        {
            if (_control.isCursorLock && Singleton<GameCursor>.IsInstance())
            {
                Singleton<GameCursor>.Instance.SetCursorLock(false);
            }
        }
    }
}
