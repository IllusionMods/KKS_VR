using System;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_VR.Util
{
    internal class MaterialHelper
    {
        private static Shader _colorZOrderShader;

        public static Shader GetColorZOrderShader()
        {
            if (_colorZOrderShader == null)
            {
                try
                {
                    var bundle = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("ColorZOrderShader"));
                    _colorZOrderShader = bundle.LoadAsset<Shader>("ColorZOrder");
                    bundle.Unload(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return null;
                }
            }

            return _colorZOrderShader;
        }
    }
}
