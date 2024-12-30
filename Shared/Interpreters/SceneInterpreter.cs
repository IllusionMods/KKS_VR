using VRGIN.Core;
using UnityEngine;
using VRGIN.Controls;
using Valve.VR;
using KK_VR.Settings;
using System.Collections.Generic;
using System.Linq;
using KK_VR.Camera;
using KK_VR.Holders;
using UnityEngine.SceneManagement;

namespace KK_VR.Interpreters
{
    /// <summary>
    /// Scene based actions that don't correlate with the input
    /// </summary>
    abstract class SceneInterpreter
    {
        protected KoikatuSettings _settings = VR.Context.Settings as KoikatuSettings;
        internal virtual void OnStart()
        {
#if KKS
            // KKS swaps VFX all the time gotta keep up, KK doesn't seem like.
            VREffector.Refresh();
#endif
        }
        internal virtual void OnDisable()
        {

        }
        internal virtual void OnUpdate()
        {

        }
        internal virtual void OnLateUpdate()
        {

        }
        internal virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

        }

    }
}
