using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KK_VR.Interpreters
{
    public class TransientHead : ProtectedBehaviour
    {
        private readonly List<Renderer> rendererList = new List<Renderer>();
        private bool hidden;
        private Transform root;
        private Renderer[] m_tongues;
        private ChaControl avatar;
        private Transform headTransform;
        private Transform eyesTransform;
        public Transform Eyes => eyesTransform;

        public bool Visible
        {
            get => !hidden;
            set
            {
                VRLog.Info(value ? "Show head" : "Hide head");
                SetVisibility(value);
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            avatar = GetComponent<ChaControl>();
            Reinitialize();
        }

        public void Reinitialize()
        {
            headTransform = GetHead(avatar);
            eyesTransform = GetEyes(avatar);
            root = avatar.objRoot.transform;
            var array = m_tongues = (from renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>()
                where renderer.name.ToLower().StartsWith("cm_o_tang") || renderer.name == "cf_o_tang"
                select renderer
                into tongue
                where tongue.enabled
                select tongue).ToArray();
        }

        public static Transform GetHead(ChaControl human)
        {
            return human.objHead.GetComponentsInParent<Transform>().First((Transform t) => t.name.StartsWith("c") && t.name.ToLower().Contains("j_head"));
        }

        public static Transform GetEyes(ChaControl human)
        {
            var transform = human.objHeadBone.transform.Descendants().FirstOrDefault((Transform t) => t.name.StartsWith("c") && t.name.ToLower().EndsWith("j_faceup_tz"));
            if (!transform)
            {
                VRLog.Info("Creating eyes");
                transform = new GameObject("cf_j_faceup_tz").transform;
                transform.SetParent(GetHead(human), false);
                transform.transform.localPosition = new Vector3(0f, 0.07f, 0.05f);
            }
            else
            {
                VRLog.Info("found eyes");
            }

            return transform;
        }

        private void SetVisibility(bool visible)
        {
            if (visible)
            {
                if (hidden)
                {
                    foreach (var renderer3 in rendererList)
                        if ((bool)renderer3)
                            renderer3.enabled = true;
                    var tongues = m_tongues;
                    foreach (var renderer2 in tongues)
                        if ((bool)renderer2)
                            renderer2.enabled = true;
                }
            }
            else if (!hidden)
            {
                var tongues = m_tongues = (from renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>()
                    where renderer.name.StartsWith("cm_o_tang") || renderer.name == "cf_o_tang"
                    select renderer
                    into tongue
                    where tongue.enabled
                    select tongue).ToArray();
                rendererList.Clear();

                foreach (var item in from renderer in headTransform.GetComponentsInChildren<Renderer>()
                    where renderer.enabled
                    select renderer)
                {
                    rendererList.Add(item);
                    item.enabled = false;
                }

                tongues = m_tongues;
                for (var i = 0; i < tongues.Length; i++)
                    tongues[i].enabled = false;
            }

            hidden = !visible;
        }
    }
}
