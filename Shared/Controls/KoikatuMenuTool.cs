using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Controls;
using UnityEngine;
using static Illusion.Utils;
using VRGIN.Visuals;
using VRGIN.Core;

namespace KK_VR.Controls
{
    class KoikatuMenuTool
    {
        internal bool attached;
        internal static GUIQuad Gui { get; private set; }
        internal KoikatuMenuTool(int index)
        {
            if (!Gui && index == 1)
            {
                Gui = GUIQuad.Create();
                Gui.transform.parent = VR.Mode.Right.transform;
                Gui.transform.localScale = Vector3.one * 0.3f;
                Gui.transform.localPosition = new Vector3(0, 0.05f, -0.06f);
                Gui.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Gui.IsOwned = true;
                Gui.gameObject.SetActive(true);
                attached = true;
            }
        }
        internal void ToggleState()
        {
            Gui.gameObject.SetActive(!Gui.gameObject.activeSelf);
        }
        //internal void TakeGUI(GUIQuad quad)
        //{
        //    if (quad && !Gui && !quad.IsOwned)
        //    {
        //        Gui = quad;
        //        //Gui.transform.parent = transform;
        //        Gui.transform.SetParent(transform, worldPositionStays: true);

        //        quad.IsOwned = true;
        //    }
        //    VRLog.Debug($"TakeGui:{Gui}:{quad.IsOwned}");
        //}

        internal void AbandonGUI()
        {
            if (attached)
            {
                //timeAbandoned = Time.unscaledTime;
                Gui.IsOwned = false;
                Gui.transform.SetParent(VR.Camera.Origin, true);
                attached = false;
            }
        }
    }
}
