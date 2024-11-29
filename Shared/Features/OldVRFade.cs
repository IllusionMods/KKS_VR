//using System;
//using System.Collections;
//using ActionGame;
//using UnityEngine;
//using Valve.VR;
//using VRGIN.Core;

//namespace KK_VR.Features
//{
//    /// <summary>
//    /// A VR fader that replaces the fader of the base game.
//    /// </summary>
//    internal class OldVRFade : ProtectedBehaviour
//    {
//        /// <summary>
//        /// Reference to the image used by the vanilla SceneFade object.
//        /// </summary>
//        private CanvasGroup _vanillaFade;

//        private readonly float _gridFadeTime = 1;
//        private readonly float _fadeAlphaThresholdHigh = 0.9999f;
//        private readonly float _fadeAlphaThresholdLow = 0.0001f;

//        private bool _isFading;

//        public static void Create()
//        {
//            VR.Camera.gameObject.AddComponent<OldVRFade>();
//        }

//        protected override void OnAwake()
//        {
//            _vanillaFade = Manager.Scene.sceneFadeCanvas?.canvasGroup ?? throw new ArgumentNullException(nameof(_vanillaFade), "sceneFadeCanvas or canvasGroup is null");
//        }

//        protected override void OnUpdate()
//        {
//            if (!_isFading && _vanillaFade && _vanillaFade.alpha > _fadeAlphaThresholdLow)
//            {
//                StartCoroutine(DeepFadeCo());
//            }
//        }

//        /// <summary>
//        /// A coroutine for entering "deep fade", where we cut to the compositor's grid and display some overlay.
//        /// Based on https://github.com/mosirnik/KK_MainGameVR/commit/12e435f1e9a70c7d7b5dd56de416d300a2836091
//        /// </summary>
//        private IEnumerator DeepFadeCo()
//        {
//            if (OpenVR.Overlay == null || _isFading)
//                yield break;

//            _isFading = true;

//            // Make the world outside of the game the same color as the loading screen instead of the headset default skybox
//            SetCompositorSkyboxOverride(GetFadeColor());

//            var compositor = OpenVR.Compositor;
//            if (compositor != null)
//            {
//                // Fade the game out so the ouside world is now seen instead of the laggy loading screen
//                compositor.FadeGrid(_gridFadeTime, true);

//                // It looks like we need to pause rendering here, otherwise the
//                // compositor will automatically put us back from the grid.
//                SteamVR_Render.pauseRendering = true;
//            }

//            // Wait for the game to fully fade in
//            while (_vanillaFade.alpha <= _fadeAlphaThresholdHigh)
//            {
//                if (!_vanillaFade || _vanillaFade.alpha < _fadeAlphaThresholdLow)
//                    goto endEarly;

//                yield return null;
//            }

//            // Wait for the game to start fading out
//            while (_vanillaFade.alpha > _fadeAlphaThresholdHigh)
//            {
//                yield return null;
//            }

//            // Wait for things to settle down
//            yield return null;
//            yield return null;

//        endEarly:

//            // Let the game be rendered again and fade into it
//            SteamVR_Render.pauseRendering = false;
//            if (compositor != null)
//            {
//                compositor.FadeGrid(_gridFadeTime, false);
//                yield return new WaitForSeconds(_gridFadeTime);
//            }

//            // Wait for the game to finish fading to make sure we are synchronized
//            while (_vanillaFade && _vanillaFade.alpha > _fadeAlphaThresholdLow)
//            {
//                yield return null;
//            }

//            SteamVR_Skybox.ClearOverride();

//            _isFading = false;
//        }

//        private static Color GetFadeColor()
//        {
//            try
//            {
//                var cycle = FindObjectOfType<Cycle>();
//                switch (cycle?.nowType)
//                {
//                    default:
//                    case Cycle.Type.WakeUp:
//                    case Cycle.Type.Morning:
//                    case Cycle.Type.Daytime:
//                        return new Color(0.44f, 0.78f, 1f);
//                    case Cycle.Type.Evening:
//                        return new Color(0.85f, 0.50f, 0.37f);
//                    case Cycle.Type.Night:
//                    case Cycle.Type.GotoMyHouse:
//                    case Cycle.Type.MyHouse:
//                        return new Color(0.12f, 0.2f, 0.5f);
//                }
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e);
//                return Color.white;
//            }
//        }

//        private static void SetCompositorSkyboxOverride(Color fadeColor)
//        {
//            var tex = new Texture2D(1, 1);
//            var color = fadeColor;
//            color.a = 1f;
//            tex.SetPixel(0, 0, color);
//            tex.Apply();
//            SteamVR_Skybox.SetOverride(tex, tex, tex, tex, tex, tex);
//            Destroy(tex);
//        }
//    }
//}
