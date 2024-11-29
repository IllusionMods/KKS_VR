using System;
using System.Collections.Generic;
using Studio;
using UnityEngine;
using VRGIN.Core;

namespace KK_VR.Util
{
    public class MoveableGUIObject : MonoBehaviour
    {
        public List<Action<MonoBehaviour>> onMoveLister = new List<Action<MonoBehaviour>>();
        public List<Action<MonoBehaviour>> onReleasedLister = new List<Action<MonoBehaviour>>();
        public GuideObject guideObject;
        public GuideScale guideScale;
        public ObjectCtrlInfo objectCtrlInfo;
        public Vector3 oldPos;
        public Vector3 oldRot;
        public Vector3 oldScale;
        public bool isMoveObj;
        private Renderer renderer;
        public Renderer visibleReference;

        private void Start()
        {
            renderer = GetComponent<Renderer>();
        }

        public void OnMoveStart()
        {
            if (guideObject != null)
            {
                oldPos = guideObject.changeAmount.pos;
                oldRot = guideObject.changeAmount.rot;
                oldScale = guideObject.changeAmount.scale;
            }
        }

        public void OnMoved()
        {
            foreach (var item in onMoveLister)
                try
                {
                    item(this);
                }
                catch (Exception e)
                {
                    VRLog.Error(e);
                }
        }

        public void OnReleased()
        {
            if (guideObject != null)
            {
                if (guideScale == null)
                {
                    if (guideObject.enablePos)
                    {
                        var equalsInfo = new GuideCommand.EqualsInfo
                        {
                            dicKey = guideObject.dicKey,
                            oldValue = oldPos,
                            newValue = guideObject.changeAmount.pos
                        };
                        Singleton<UndoRedoManager>.Instance.Push(new GuideCommand.MoveEqualsCommand(new GuideCommand.EqualsInfo[1] { equalsInfo }));
                    }

                    if (guideObject.enableRot)
                    {
                        var equalsInfo2 = new GuideCommand.EqualsInfo
                        {
                            dicKey = guideObject.dicKey,
                            oldValue = oldRot,
                            newValue = guideObject.changeAmount.rot
                        };
                        Singleton<UndoRedoManager>.Instance.Push(new GuideCommand.RotationEqualsCommand(new GuideCommand.EqualsInfo[1] { equalsInfo2 }));
                    }
                }
                else if (guideObject.enableScale)
                {
                    var changeAmountInfo = new GuideCommand.EqualsInfo[1]
                    {
                        new GuideCommand.EqualsInfo
                        {
                            dicKey = guideObject.dicKey,
                            oldValue = oldScale,
                            newValue = guideObject.changeAmount.scale
                        }
                    };
                    Singleton<UndoRedoManager>.Instance.Push(new GuideCommand.ScaleEqualsCommand(changeAmountInfo));
                }
            }

            foreach (var item in onReleasedLister)
                try
                {
                    item(this);
                }
                catch (Exception e)
                {
                    VRLog.Error(e);
                }
        }

        private void Update()
        {
            if (isMoveObj) transform.localScale = Vector3.one * 0.1f * Studio.Studio.optionSystem.manipulateSize;
            if (guideScale != null) transform.localScale = Vector3.one * 0.05f * Studio.Studio.optionSystem.manipulateSize;
            if (visibleReference != null && renderer != null) renderer.gameObject.layer = visibleReference.gameObject.layer;
        }
    }
}
