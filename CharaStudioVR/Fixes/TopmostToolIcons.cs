using System;
using HarmonyLib;
using KKAPI.Utilities;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Core;


// Only single tool left, no icons needed.
namespace KK_VR.Fixes
{
    /// <summary>
    /// Fix custom tool icons not being on top of the black circle
    /// </summary>
    public class TopmostToolIcons
    {
        public static void Patch()
        {
            new Harmony("TopmostToolIconsHook").PatchAll(typeof(TopmostToolIcons));
        }

        private static Shader _guiShader;

        public static Shader GetGuiShader()
        {
            if (_guiShader == null)
            {
                var bundle = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("topmostguishader"));
                _guiShader = bundle.LoadAsset<Shader>("topmostgui");
                if (_guiShader == null) throw new ArgumentNullException(nameof(_guiShader));
                //_guiShader = new Material(guiShader);
                bundle.Unload(false);
            }

            return _guiShader;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Controller), "OnUpdate")]
        private static void ToolIconFixHook(Controller __instance)
        {
            var tools = __instance.Tools;
            var any = 0;

            foreach (var tool in tools)
            {
                var canvasRenderer = tool.Icon?.GetComponent<CanvasRenderer>();
                if (canvasRenderer == null) return;

                var orig = canvasRenderer.GetMaterial();
                if (orig == null || orig.shader == _guiShader) continue;

                any++;

                var copy = new Material(GetGuiShader());
                canvasRenderer.SetMaterial(copy, 0);
            }

            if (any == 0) return;

            Canvas.ForceUpdateCanvases();

            VRLog.Debug($"Replaced materials on {any} tool icon renderers");
        }
    }
}
