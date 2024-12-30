using UnityEngine.UI;
using UnityEngine;
using VRGIN.Helpers;
using System.Collections;
using Valve.VR;
using VRGIN.Core;
using ActionGame;
using Manager;
using KK_VR.Interpreters;

namespace KK_VR.Features
{
    /// <summary>
    /// A VR fader that replaces the fader of the base game.
    /// </summary>
    internal class VRFade : MonoBehaviour
    {
        /// <summary>
        /// Reference to the image used by the vanilla SceneFade object.
        /// </summary>
        //Graphic _vanillaGraphic;
        private static VRFade _instance;
        //internal static bool IsFade => _instance._alpha != 0f;
        internal static bool IsFade => _instance._alpha == 1f;
#if KK
        private Image _vanillaImage;
        private Slider _vanillaProgressBar;
#else
        // KKS doesn't have fade out by default,
        // and the fade is hidden so well couldn't even find it,
        // so we go with the fade delegates and custom fade.

        private LoadingIconJob _loadingIconJob;
        private SceneFadeCanvas _sceneFadeCanvas;
        private bool _isFade;
        private float _fadeTimestamp;
#endif
        private Material _fadeMaterial;
        private int _fadeMaterialColorID;
        private float _alpha;
        private bool _inDeepFade;
        private Color _fadeColor;

        

        const float DeepFadeAlphaThreshold = 0.9999f;

        public static void Create()
        {
            VR.Camera.gameObject.AddComponent<VRFade>();
        }
        private void Awake()
        {
            _instance = this;
#if KK
            _vanillaImage = Manager.Scene.Instance.sceneFade.image;
            _vanillaProgressBar = Manager.Scene.Instance.progressSlider;
            _vanillaImage.enabled = false;
#else
            _sceneFadeCanvas = Manager.Scene.sceneFadeCanvas;
            _loadingIconJob = Manager.Scene.loadingIconJob;
            Manager.Scene.sceneFadeCanvas.onStart += OnFadeIn;
            Manager.Scene.sceneFadeCanvas.onComplete += OnFadeOut;
#endif
            //_vanillaGraphic = _sceneFadeCanvas.fadeImage;
            _fadeMaterial = new Material(UnityHelper.GetShader("Custom/SteamVR_Fade"));
            _fadeMaterialColorID = Shader.PropertyToID("fadeColor");
        }
#if KKS
        private void OnFadeIn(FadeCanvas.Fade fade)
        {
             if (!_isFade)
             {
                _isFade = true;
                _fadeColor = GetFadeColor();

                _fadeTimestamp = Time.unscaledTime + 2f;
            }
        }
        private void OnFadeOut(FadeCanvas.Fade fade)
        {
            if (!_sceneFadeCanvas.isFading)
            {
                if (_inDeepFade)
                {
                    _isFade = false;
                    _alpha = 0f;
                }
                if (TalkSceneInterpreter.advScene != null && KoikatuInterpreter.SceneInterpreter is TalkSceneInterpreter talkInterpreter)
                {
                    talkInterpreter.AdjustAdvScene();
                }
            }
        }

        private void OnPostRender()
        {
            if (_isFade)
            {
                if (!_inDeepFade)
                {
                    if (Scene.IsFadeNow)
                    {
                        if (_alpha != 1f)
                        {
                            _alpha = Mathf.Clamp01(_alpha + Mathf.Min(Time.deltaTime, 0.05f));
                        }
                        else if (_fadeTimestamp < Time.unscaledTime)
                        {
                            StartCoroutine(DeepFadeCo());
                        }
                    }
                    else
                    {
                        _alpha = Mathf.Clamp01(_alpha - Mathf.Min(Time.deltaTime, 0.05f));
                        if (_alpha == 0f)
                        {
                            _isFade = false;
                        }
                    }
                    _fadeColor.a = Mathf.Clamp01(_alpha);
                    DrawQuad();
                }
                else
                {
                    // KKS doesn't have fade out by default it seems. We'll keep it that way on non-vr render.
                    if (Scene.IsFadeNow)
                    {
                        DrawQuad();
                    }
                }
            }
        }
#else
        private void OnPostRender()
        {
            if (_vanillaImage != null)
            {
                _fadeColor = _vanillaImage.color;
                _alpha = Mathf.Max(_alpha - 0.05f, _fadeColor.a); // Use at least 20 frames to fade out.
                _fadeColor.a = _alpha;
                if (_alpha > 0.0001f)
                {
                    DrawQuad();
                }

                if (DeepFadeAlphaThreshold < _alpha &&
                    _vanillaProgressBar.isActiveAndEnabled &&
                    !_inDeepFade)
                {
                    StartCoroutine(DeepFadeCo());
                }
            }
        }
#endif

        private void DrawQuad()
        {
            _fadeMaterial.SetColor(_fadeMaterialColorID, _fadeColor);
            _fadeMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.Vertex3(-1, -1, 0);
            GL.Vertex3(1, -1, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(-1, 1, 0);
            GL.End();
        }


        /// <summary>
        /// A coroutine for entering "deep fade", where we cut to the compositor's
        /// grid and display some overlay.
        /// </summary>
        private IEnumerator DeepFadeCo()
        {
            var overlay = OpenVR.Overlay;
            if (overlay == null)
            {
                yield break;
            }
            _inDeepFade = true;

            var gridFadeTime = 1f;
            var compositor = OpenVR.Compositor;
#if KK
            SetCompositorSkyboxOverride(_vanillaImage.color);
#else
            SetCompositorSkyboxOverride(GetFadeColor());
#endif
            if (compositor != null)
            {
                compositor.FadeGrid(gridFadeTime, true);
                // It looks like we need to pause rendering here, otherwise the
                // compositor will automatically put us back from the grid.
                SteamVR_Render.pauseRendering = true;
            }
            // Adding loading icon seems to be way harder then i'd like.

#if KK
            var loadingOverlay = new LoadingOverlay(overlay);
            while (DeepFadeAlphaThreshold < _vanillaImage.color.a)
            {
                loadingOverlay.Update();
                yield return null;
            }
            loadingOverlay.Destroy();
#else
            while (_alpha == 1f)
            {
                //loadingOverlay.Update();
                yield return null;
            }
#endif

            // Wait for things to settle down
            yield return null;
            yield return null;

            SteamVR_Render.pauseRendering = false;
            if (compositor != null)
            {
                compositor.FadeGrid(gridFadeTime, false);
                yield return new WaitForSeconds(gridFadeTime);
            }

            SteamVR_Skybox.ClearOverride();
            _inDeepFade = false;
        }
        private static void SetCompositorSkyboxOverride(Color fadeColor)
        {
            var tex = new Texture2D(1, 1);
            var color = fadeColor;
            color.a = 1f;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            SteamVR_Skybox.SetOverride(tex, tex, tex, tex, tex, tex);
            Destroy(tex);
        }
#if KKS
        private static Color GetFadeColor()
        {
            var cycle = FindObjectOfType<Cycle>();
            if (cycle == null)
            {
                //return Color.white;
                return new Color(0.44f, 0.78f, 1f);
            }
            return (cycle.nowType) switch
            {
                Cycle.Type.Evening => new Color(0.85f, 0.50f, 0.37f),
                // Find better color for the night.
                Cycle.Type.Night or Cycle.Type.GotoMyHouse or Cycle.Type.MyHouse => new Color(0.12f, 0.2f, 0.5f),
                _ => new Color(0.44f, 0.78f, 1f),
            };
        }
#endif
#if KK

        /// An object that manages an OpenVR overlay that shows a "Now Loading..." image
        /// and a progress bar. This needs to be an overlay rather than a GameObject so
        /// that its rendering continues while the game's framerate drops massively.
        class LoadingOverlay
        {
            readonly CVROverlay _overlay;
            readonly ulong _handle; // handle to our overlay
            readonly RenderTexture _texture; // texture to be displayed in the overlay
            readonly UnityEngine.Camera _camera; // camera for rendering to the texture
            readonly Canvas _canvas; // canvas to hold UI elements
            readonly Image _baseGameLoadingImage; // base game's "Now Loading" image
            readonly Image _loadingImage; // our "Now Loading" image
            readonly Slider _baseGameProgressBar; // base game's progress bar
            readonly Slider _progressBar; // our progress bar

            internal LoadingOverlay(CVROverlay overlay)
            {
                _overlay = overlay;

                _handle = OpenVR.k_ulOverlayHandleInvalid;
                var error = overlay.CreateOverlay(
                    VRPlugin.GUID + ".now_loading",
                    "Now Loading",
                    ref _handle);
                if (error != EVROverlayError.None)
                {
                    VRLog.Error("Cannot create overlay: {0}",
                        overlay.GetOverlayErrorNameFromEnum(error));
                    return;
                }

                _texture = new RenderTexture(272, 56, 24);

                _camera = new GameObject("VRLoadingOverlayCamera")
                    .AddComponent<UnityEngine.Camera>();
                DontDestroyOnLoad(_camera);
                _camera.targetTexture = _texture;
                _camera.cullingMask = VR.Context.UILayerMask;
                _camera.depth = 1;
                _camera.nearClipPlane = VR.Context.GuiNearClipPlane;
                _camera.farClipPlane = VR.Context.GuiFarClipPlane;
                _camera.backgroundColor = Color.clear;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.orthographic = true;
                _camera.useOcclusionCulling = false;

                var baseGameCanvas = Manager.Scene.Instance.sceneFade
                    .image.transform.parent.GetComponent<Canvas>();
                _canvas = GameObject.Instantiate(baseGameCanvas);
                DontDestroyOnLoad(_canvas);
                _canvas.name = "VRLoadingOverlayCanvas";
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.worldCamera = _camera;
                var scaler = _canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1;
                _canvas.gameObject.SetActive(true);

                _baseGameLoadingImage = baseGameCanvas.transform.Find("NowLoading").GetComponent<Image>();
                _loadingImage = _canvas.transform.Find("NowLoading").GetComponent<Image>();
                var imageTrans = _loadingImage.GetComponent<RectTransform>();
                imageTrans.anchorMin = imageTrans.anchorMax = Vector2.zero;
                imageTrans.offsetMin = new Vector2(24, 24);
                imageTrans.offsetMax = new Vector2(232, 56);

                _baseGameProgressBar = baseGameCanvas.transform.Find("Progress").GetComponent<Slider>();
                _progressBar = _canvas.transform.Find("Progress").GetComponent<Slider>();
                var barTrans = _progressBar.GetComponent<RectTransform>();
                barTrans.anchorMin = barTrans.anchorMax = Vector2.zero;
                barTrans.offsetMin = Vector2.zero;
                barTrans.offsetMax = new Vector2(272, 28);

                InitializeOverlay();
            }

            private void InitializeOverlay()
            {
                Check("SetWidth", _overlay.SetOverlayWidthInMeters(_handle, 0.3f));
                var vrcam = VR.Camera;
                var rot = Quaternion.Euler(0f, vrcam.transform.localRotation.eulerAngles.y, 0f);
                var pos = vrcam.transform.localPosition + rot * Vector3.forward * 3f;
                var offset = new SteamVR_Utils.RigidTransform(pos, rot);
                var t = offset.ToHmdMatrix34();
                Check("SetTransform",
                    _overlay.SetOverlayTransformAbsolute(
                        _handle,
                        SteamVR_Render.instance.trackingSpace,
                        ref t));

                var textureBounds1 = new VRTextureBounds_t
                {
                    uMin = 0f,
                    uMax = 1f,
                    // The image will be vertically flipped unless we set vMax < vMin.
                    // I don't know why.
                    vMin = 1f,
                    vMax = 0f,
                };
                Check("SetBounds", _overlay.SetOverlayTextureBounds(_handle, ref textureBounds1));

                _overlay.ShowOverlay(_handle);
            }

            internal void Update()
            {
                _loadingImage.gameObject.SetActive(_baseGameLoadingImage.gameObject.activeSelf);
                _loadingImage.color = _baseGameLoadingImage.color;
                _progressBar.gameObject.SetActive(_baseGameProgressBar.gameObject.activeSelf);
                _progressBar.value = _baseGameProgressBar.value;
                RedrawOverlay();
            }

            private void RedrawOverlay()
            {
                var tex = new Texture_t();
                tex.handle = _texture.GetNativeTexturePtr();
                tex.eType = SteamVR.instance.textureType;
                tex.eColorSpace = EColorSpace.Auto;
                Check("SetTexture", _overlay.SetOverlayTexture(_handle, ref tex));
            }

            internal void Destroy()
            {
                Check("Destroy", _overlay.DestroyOverlay(_handle));
                GameObject.Destroy(_camera.gameObject);
                GameObject.Destroy(_canvas.gameObject);
                GameObject.Destroy(_texture);
            }

            private void Check(string label, EVROverlayError error)
            {
                if (error != EVROverlayError.None)
                {
                    VRLog.Error($"Overlay {label}: {_overlay.GetOverlayErrorNameFromEnum(error)}");
                }
            }
        }
#endif
    }
}
