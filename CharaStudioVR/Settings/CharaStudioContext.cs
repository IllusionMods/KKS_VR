using System;
using System.Xml.Serialization;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Visuals;

namespace KK_VR.Settings
{
    [XmlRoot("Context")]
    public class CharaStudioContext : IVRManagerContext
    {
        private readonly DefaultMaterialPalette _Materials;
        private readonly VRSettings _Settings;
        [XmlIgnore] public IMaterialPalette Materials => _Materials;
        [XmlIgnore] public VRSettings Settings => _Settings;
        
        public bool ConfineMouse { get; set; }
        public bool EnforceDefaultGUIMaterials { get; set; }
        public bool GUIAlternativeSortingMode { get; set; }
        public float GuiFarClipPlane { get; set; }
        public string GuiLayer { get; set; }
        public float GuiNearClipPlane { get; set; }
        public int IgnoreMask { get; set; }
        public string InvisibleLayer { get; set; }
        public Color PrimaryColor { get; set; }
        public bool SimulateCursor { get; set; }
        public string UILayer { get; set; }
        public int UILayerMask { get; set; }
        public float UnitToMeter { get; set; }
        public float NearClipPlane { get; set; }
        public GUIType PreferredGUI { get; set; }
        public string Version { get; set; }
        public float MaxFarClipPlane { get; set; }
        public int GuiMaterialRenderQueue { get; set; }
        Type IVRManagerContext.VoiceCommandType { get; }
        public bool ForceIMGUIOnScreen { get; set; }

        public CharaStudioContext(VRSettings settings)
        {
            _Materials = new DefaultMaterialPalette();
            _Settings = settings;
            ConfineMouse = true;
            EnforceDefaultGUIMaterials = false;
            GUIAlternativeSortingMode = false;
            GuiLayer = "Default";
            GuiFarClipPlane = 1000f;
            GuiNearClipPlane = -1000f;
            IgnoreMask = 0;
            InvisibleLayer = "Ignore Raycast";
            PrimaryColor = Color.cyan;
            SimulateCursor = true;
            UILayer = "UI";
            UILayerMask = LayerMask.GetMask(UILayer);
            UnitToMeter = 1f;
            NearClipPlane = 0.001f;
            PreferredGUI = GUIType.uGUI;
        }
    }
}
