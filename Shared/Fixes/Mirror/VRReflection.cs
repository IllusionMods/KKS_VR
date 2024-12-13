// Gratefully copied and adapted from
// https://forum.unity.com/threads/reflection-rendering-wrong-in-openvr-htc-vive.398756/#post-2623563

using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace KK_VR.Fixes.Mirror
{
    // TODO borked
    [ExecuteInEditMode] // Make mirror live-update even when not in play mode
    public class VRReflection : MonoBehaviour
    {
        public bool m_DisablePixelLights = true;

        // If true, then a single RenderTexture will be used for ALL reflected cameras, with the size of that
        // texture defined by the m_SharedTextureSize field.  If false, then each reflected camera will have
        // it's own RenderTexture that is sized to match that camera.
        public bool m_UseSharedRenderTexture = false;
        public int m_SharedTextureSize = 256;

        public LayerMask m_ReflectLayers = -1;

        private class ReflectionData
        {
            public UnityEngine.Camera camera;
            public RenderTexture left;
            public RenderTexture right;
            public MaterialPropertyBlock propertyBlock;
        }

        private Dictionary<UnityEngine.Camera, ReflectionData> m_ReflectionCameras = new Dictionary<UnityEngine.Camera, ReflectionData>();

        private RenderTexture m_SharedReflectionTextureLeft = null;
        private RenderTexture m_SharedReflectionTextureRight = null;

        private static bool s_InsideRendering = false;

        private static int s_LeftTexturePropertyID;
        private static int s_RightTexturePropertyID;

        private void Awake()
        {
            s_LeftTexturePropertyID = Shader.PropertyToID("_LeftReflectionTex");
            s_RightTexturePropertyID = Shader.PropertyToID("_RightReflectionTex");
        }

        // This is called when it's known that the object will be rendered by some
        // camera. We render reflections and do other updates here.
        // Because the script executes in edit mode, reflections for the scene view
        // camera will just work!
        public void OnWillRenderObject()
        {
            var rend = GetComponent<Renderer>();
            if (!enabled || !rend || !rend.enabled)
                return;

            var cam = UnityEngine.Camera.current;
            if (!cam)
                return;

            // Safeguard from recursive reflections.
            if (s_InsideRendering)
                return;
            s_InsideRendering = true;

            ReflectionData reflectionData;
            CreateMirrorObjects(cam, out reflectionData);

            // Optionally disable pixel lights for reflection
            var oldPixelLightCount = QualitySettings.pixelLightCount;
            if (m_DisablePixelLights)
                QualitySettings.pixelLightCount = 0;

            // Invert culling as the reflection of the mirrors will reverse the winding order for everything rendered
            var oldInvertCulling = GL.invertCulling;
            GL.invertCulling = !oldInvertCulling;

            UpdateCameraModes(cam, reflectionData.camera);

            if (cam.stereoEnabled)
            {
                if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Left)
                {
                    var eyePos = cam.transform.TransformPoint(SteamVR.instance.eyes[0].pos);
                    var eyeRot = cam.transform.rotation * SteamVR.instance.eyes[0].rot;
                    var projectionMatrix = GetSteamVRProjectionMatrix(cam, EVREye.Eye_Left);
                    var target = m_UseSharedRenderTexture ? m_SharedReflectionTextureLeft : reflectionData.left;
                    reflectionData.propertyBlock.SetTexture(s_LeftTexturePropertyID, target);

                    RenderMirror(reflectionData.camera, target, eyePos, eyeRot, projectionMatrix);
                }

                if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Right)
                {
                    var eyePos = cam.transform.TransformPoint(SteamVR.instance.eyes[1].pos);
                    var eyeRot = cam.transform.rotation * SteamVR.instance.eyes[1].rot;
                    var projectionMatrix = GetSteamVRProjectionMatrix(cam, EVREye.Eye_Right);
                    var target = m_UseSharedRenderTexture ? m_SharedReflectionTextureRight : reflectionData.right;
                    reflectionData.propertyBlock.SetTexture(s_RightTexturePropertyID, target);

                    RenderMirror(reflectionData.camera, target, eyePos, eyeRot, projectionMatrix);
                }
            }
            else
            {
                var target = m_UseSharedRenderTexture ? m_SharedReflectionTextureLeft : reflectionData.left;
                reflectionData.propertyBlock.SetTexture(s_LeftTexturePropertyID, target);
                RenderMirror(reflectionData.camera, target, cam.transform.position, cam.transform.rotation, cam.projectionMatrix);
            }

            // Apply the property block containing the texture references to the renderer
            rend.SetPropertyBlock(reflectionData.propertyBlock);

            // Restore the original culling
            GL.invertCulling = oldInvertCulling;

            // Restore pixel light count
            if (m_DisablePixelLights)
                QualitySettings.pixelLightCount = oldPixelLightCount;

            s_InsideRendering = false;
        }

        private void RenderMirror(UnityEngine.Camera reflectionCamera, RenderTexture targetTexture, Vector3 camPosition, Quaternion camRotation, Matrix4x4 camProjectionMatrix)
        {
            // Copy camera position/rotation/projection data into the reflectionCamera
            reflectionCamera.ResetWorldToCameraMatrix();
            reflectionCamera.transform.position = camPosition;
            reflectionCamera.transform.rotation = camRotation;
            reflectionCamera.projectionMatrix = camProjectionMatrix;
            reflectionCamera.targetTexture = targetTexture;

            reflectionCamera.cullingMask = ~(1 << 4) & m_ReflectLayers.value; // never render water layer

            // find out the reflection plane: position and normal in world space
            var pos = transform.position;
            var normal = transform.up;

            // Reflect camera around reflection plane
            var worldSpaceClipPlane = Plane(pos, normal);
            reflectionCamera.worldToCameraMatrix *= CalculateReflectionMatrix(worldSpaceClipPlane);

            // Setup oblique projection matrix so that near plane is our reflection
            // plane. This way we clip everything behind it for free.
            var cameraSpaceClipPlane = CameraSpacePlane(reflectionCamera, pos, normal);
            reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(cameraSpaceClipPlane);

            // Set camera position and rotation (even though it will be ignored by the render pass because we
            // have explicitly set the worldToCameraMatrix). We do this because some render effects may rely 
            // on the position/rotation of the camera.
            reflectionCamera.transform.position = reflectionCamera.cameraToWorldMatrix.GetPosition();
            reflectionCamera.transform.rotation = reflectionCamera.cameraToWorldMatrix.GetRotation();

            reflectionCamera.Render();
        }

        // Cleanup all the objects we possibly have created
        private void OnDisable()
        {
            if (m_SharedReflectionTextureLeft)
            {
                DestroyImmediate(m_SharedReflectionTextureLeft);
                m_SharedReflectionTextureLeft = null;
            }

            if (m_SharedReflectionTextureRight)
            {
                DestroyImmediate(m_SharedReflectionTextureRight);
                m_SharedReflectionTextureRight = null;
            }

            foreach (var reflectionData in m_ReflectionCameras.Values)
            {
                DestroyImmediate(reflectionData.camera.gameObject);
                DestroyImmediate(reflectionData.left);
                if (reflectionData.right) DestroyImmediate(reflectionData.right);
            }

            m_ReflectionCameras.Clear();
        }


        private void UpdateCameraModes(UnityEngine.Camera src, UnityEngine.Camera dest)
        {
            if (dest == null)
                return;
            // set camera to clear the same way as current camera
            dest.clearFlags = src.clearFlags;
            dest.backgroundColor = src.backgroundColor;
            if (src.clearFlags == CameraClearFlags.Skybox)
            {
                var sky = src.GetComponent(typeof(Skybox)) as Skybox;
                var mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
                if (!sky || !sky.material)
                {
                    mysky.enabled = false;
                }
                else
                {
                    mysky.enabled = true;
                    mysky.material = sky.material;
                }
            }

            // update other values to match current camera.
            // even if we are supplying custom camera&projection matrices,
            // some of values are used elsewhere (e.g. skybox uses far plane)
            dest.farClipPlane = src.farClipPlane;
            dest.nearClipPlane = src.nearClipPlane;
            dest.orthographic = src.orthographic;
            dest.fieldOfView = src.fieldOfView;
            dest.aspect = src.aspect;
            dest.orthographicSize = src.orthographicSize;
        }

        // On-demand create any objects we need
        private void CreateMirrorObjects(UnityEngine.Camera currentCamera, out ReflectionData reflectionData)
        {
            if (!m_ReflectionCameras.TryGetValue(currentCamera, out reflectionData))
            {
                reflectionData = new ReflectionData();
                reflectionData.propertyBlock = new MaterialPropertyBlock();
                m_ReflectionCameras[currentCamera] = reflectionData;
            }

            // Camera for reflection
            if (!reflectionData.camera)
            {
                var go = new GameObject("Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(UnityEngine.Camera), typeof(Skybox), typeof(FlareLayer));
                reflectionData.camera = go.GetComponent<UnityEngine.Camera>();
                reflectionData.camera.enabled = false;
                go.hideFlags = HideFlags.HideAndDontSave;
            }

            if (m_UseSharedRenderTexture)
            {
                // Get rid of any per camera reflection textures
                if (reflectionData.left)
                {
                    DestroyImmediate(reflectionData.left);
                    reflectionData.left = null;
                }

                if (reflectionData.right)
                {
                    DestroyImmediate(reflectionData.right);
                    reflectionData.right = null;
                }

                // Create shared reflection textures of the desired size
                if (!m_SharedReflectionTextureLeft || m_SharedReflectionTextureLeft.width != m_SharedTextureSize)
                {
                    if (m_SharedReflectionTextureLeft)
                        DestroyImmediate(m_SharedReflectionTextureLeft);
                    m_SharedReflectionTextureLeft = new RenderTexture(m_SharedTextureSize, m_SharedTextureSize, 24);
                    m_SharedReflectionTextureLeft.name = "__MirrorReflectionLeft" + GetInstanceID();
                    m_SharedReflectionTextureLeft.hideFlags = HideFlags.DontSave;
                }

                if (!m_SharedReflectionTextureRight || m_SharedReflectionTextureRight.width != m_SharedTextureSize)
                {
                    if (m_SharedReflectionTextureRight)
                        DestroyImmediate(m_SharedReflectionTextureRight);
                    m_SharedReflectionTextureRight = new RenderTexture(m_SharedTextureSize, m_SharedTextureSize, 24);
                    m_SharedReflectionTextureRight.name = "__MirrorReflectionRight" + GetInstanceID();
                    m_SharedReflectionTextureRight.hideFlags = HideFlags.DontSave;
                }
            }
            else
            {
                // Get rid of any the shared reflection textures
                if (m_SharedReflectionTextureLeft)
                {
                    DestroyImmediate(m_SharedReflectionTextureLeft);
                    m_SharedReflectionTextureLeft = null;
                }

                if (m_SharedReflectionTextureRight)
                {
                    DestroyImmediate(m_SharedReflectionTextureRight);
                    m_SharedReflectionTextureRight = null;
                }

                // Reflection render texture
                if (!reflectionData.left || reflectionData.left.width != currentCamera.pixelWidth || reflectionData.left.height != currentCamera.pixelHeight)
                {
                    if (reflectionData.left)
                        DestroyImmediate(reflectionData.left);
                    reflectionData.left = new RenderTexture(currentCamera.pixelWidth, currentCamera.pixelHeight, 24);
                    reflectionData.left.name = "__MirrorReflectionLeft" + GetInstanceID() + " for " + currentCamera.GetInstanceID();
                    reflectionData.left.hideFlags = HideFlags.DontSave;
                    reflectionData.propertyBlock.SetTexture(s_LeftTexturePropertyID, reflectionData.left);
                }

                // If stereo is enabled, create right reflection texture
                if (currentCamera.stereoEnabled)
                    if (!reflectionData.right || reflectionData.right.width != currentCamera.pixelWidth || reflectionData.right.height != currentCamera.pixelHeight)
                    {
                        if (reflectionData.right)
                            DestroyImmediate(reflectionData.right);
                        reflectionData.right = new RenderTexture(currentCamera.pixelWidth, currentCamera.pixelHeight, 24);
                        reflectionData.right.name = "__MirrorReflectionRight" + GetInstanceID() + " for " + currentCamera.GetInstanceID();
                        reflectionData.right.hideFlags = HideFlags.DontSave;
                        reflectionData.propertyBlock.SetTexture(s_RightTexturePropertyID, reflectionData.right);
                    }
            }
        }

        // Given position/normal of the plane, calculate plane in world space.
        private Vector4 Plane(Vector3 pos, Vector3 normal)
        {
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(pos, normal));
        }

        // Given position/normal of the plane, calculates plane in camera space.
        private Vector4 CameraSpacePlane(UnityEngine.Camera cam, Vector3 pos, Vector3 normal)
        {
            var m = cam.worldToCameraMatrix;
            var cpos = m.MultiplyPoint(pos);
            var cnormal = m.MultiplyVector(normal).normalized;
            return Plane(cpos, cnormal);
        }

        // Calculates reflection matrix around the given plane
        private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            var reflectionMat = Matrix4x4.identity;

            reflectionMat.m00 = 1F - 2F * plane[0] * plane[0];
            reflectionMat.m01 = -2F * plane[0] * plane[1];
            reflectionMat.m02 = -2F * plane[0] * plane[2];
            reflectionMat.m03 = -2F * plane[3] * plane[0];

            reflectionMat.m10 = -2F * plane[1] * plane[0];
            reflectionMat.m11 = 1F - 2F * plane[1] * plane[1];
            reflectionMat.m12 = -2F * plane[1] * plane[2];
            reflectionMat.m13 = -2F * plane[3] * plane[1];

            reflectionMat.m20 = -2F * plane[2] * plane[0];
            reflectionMat.m21 = -2F * plane[2] * plane[1];
            reflectionMat.m22 = 1F - 2F * plane[2] * plane[2];
            reflectionMat.m23 = -2F * plane[3] * plane[2];

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;

            return reflectionMat;
        }

        public static Matrix4x4 GetSteamVRProjectionMatrix(UnityEngine.Camera cam, EVREye eye)
        {
            var proj = SteamVR.instance.hmd.GetProjectionMatrix(eye, cam.nearClipPlane, cam.farClipPlane);
            var m = new Matrix4x4();
            m.m00 = proj.m0;
            m.m01 = proj.m1;
            m.m02 = proj.m2;
            m.m03 = proj.m3;
            m.m10 = proj.m4;
            m.m11 = proj.m5;
            m.m12 = proj.m6;
            m.m13 = proj.m7;
            m.m20 = proj.m8;
            m.m21 = proj.m9;
            m.m22 = proj.m10;
            m.m23 = proj.m11;
            m.m30 = proj.m12;
            m.m31 = proj.m13;
            m.m32 = proj.m14;
            m.m33 = proj.m15;
            return m;
        }
    }
}
