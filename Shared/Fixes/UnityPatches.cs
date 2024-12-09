using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

// Collection of patches to Unity.

namespace KK_VR.Fixes
{
    /// <summary>
    /// GraphicRaycaster.sortOrderPriority and GraphicRaycaster.renderOrderPriority
    /// respect the render/sort order of the canvas only when the render mode
    /// is set to ScreenSpaceOverlay. This causes incorrect UI item to receive
    /// clicks (#40).
    ///
    /// This class defines patches to ensure that these properties remain
    /// referenced after the canvas is modified to render to the VRGIN GUI
    /// camera.
    /// </summary>
    [HarmonyPatch(typeof(GraphicRaycaster))]
    internal class GraphicRaycasterPatches
    {
        [HarmonyPatch(nameof(GraphicRaycaster.sortOrderPriority), MethodType.Getter)]
        [HarmonyPostfix]
        private static void PostGetSortOrderPriority(GraphicRaycaster __instance, ref Canvas ___m_Canvas, ref int __result)
        {
            ___m_Canvas = ___m_Canvas ?? __instance.GetComponent<Canvas>();
            if (___m_Canvas.worldCamera == _vrGuiCamera) __result = ___m_Canvas.sortingOrder;
        }

        [HarmonyPatch(nameof(GraphicRaycaster.renderOrderPriority), MethodType.Getter)]
        [HarmonyPostfix]
        private static void PostGetRenderOrderPriority(GraphicRaycaster __instance, ref Canvas ___m_Canvas, ref int __result)
        {
            ___m_Canvas = ___m_Canvas ?? __instance.GetComponent<Canvas>();
            if (___m_Canvas.worldCamera == _vrGuiCamera) __result = ___m_Canvas.rootCanvas.renderOrder;
        }

        public static void Initialize()
        {
            _vrGuiCamera = GameObject.Find("VRGIN_GUICamera")?.GetComponent<UnityEngine.Camera>();
        }

        private static UnityEngine.Camera _vrGuiCamera;


    }
}
