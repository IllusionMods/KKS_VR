using KKAPI.Utilities;
using UnityEngine;
using VRGIN.Core;
using Object = UnityEngine.Object;

namespace KK_VR.Fixes.Mirror
{
    /// <summary>
    /// Mirrors in the base game look very weird in VR. This object
    /// replaces components and materials to fix this issue.
    /// </summary>
    internal class Manager
    {
        private Material _material;

        public void Fix(MirrorReflection refl)
        {
            if (refl.GetComponent<VRReflection>() != null) return;
            var mirror = refl.gameObject;
            Object.Destroy(refl);
            mirror.AddComponent<VRReflection>();
            mirror.GetComponent<Renderer>().material = Material();
        }

        private Material Material()
        {
            if (_material == null)
            {
                var bundle = ResourceUtils.GetEmbeddedResource("mirror-shader");
                if (bundle == null) VRLog.Error("Failed to load shader bundle");
                var shader = VRGIN.Helpers.UnityHelper.LoadFromAssetBundle<Shader>(bundle, "Assets/MirrorReflection.shader");
                if (shader == null) VRLog.Error("Failed to load shader");
                _material = new Material(shader);
            }

            return _material;
        }
    }
}
