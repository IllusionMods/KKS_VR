using KK_VR.Settings;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;
using Object = UnityEngine.Object;

namespace KK_VR.Features
{
    /// <summary>
    /// Class that allows the user to hide contents of the desktop mirror screen.
    /// </summary>
    internal class PrivacyScreen
    {
        private static Canvas _blocker;

        public static void Initialize()
        {
            var settings = VR.Settings as KoikatuSettings;
            CreateOrDestroy(settings);
            settings.AddListener("PrivacyScreen", (_, _1) => CreateOrDestroy(settings));
        }

        private static void CreateOrDestroy(KoikatuSettings settings)
        {
            if (settings.PrivacyScreen)
                Create();
            else
                Destroy();
        }

        private static void Create()
        {
            if (_blocker != null) return;

            var blocker = new GameObject("PrivacyScreen").AddComponent<Canvas>();
            blocker.transform.SetParent(VR.Camera.transform, false);
            blocker.renderMode = RenderMode.ScreenSpaceOverlay;
            // We can't go all the way to 32767 because many of the game's canvases
            // try to go one above all other canvas' sorting orders, causing
            // an unwanted wrap-around behaviour.
            blocker.sortingOrder = 30000;
            var panel = new GameObject("Panel").AddComponent<Image>();
            panel.transform.SetParent(blocker.transform, false);
            panel.color = Color.black;
            panel.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
            _blocker = blocker;
        }

        private static void Destroy()
        {
            if (_blocker != null)
            {
                Object.Destroy(_blocker.gameObject);
                _blocker = null;
            }
        }

        public static bool IsOwnedCanvas(Canvas other)
        {
            return _blocker != null && _blocker == other;
        }
    }
}
