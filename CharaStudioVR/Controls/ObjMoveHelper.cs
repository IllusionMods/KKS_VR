using System.Collections.Generic;
using Studio;
using UnityEngine;

namespace KK_VR.Controls
{
    internal class ObjMoveHelper
    {
        public Vector3 moveAlongBasePos;

        public void SetBasePos(Vector3 basePos)
        {
            moveAlongBasePos = basePos;
        }

        public ObjectCtrlInfo GetFirstObject()
        {
            var instance = Singleton<Studio.Studio>.Instance;
            if (instance != null)
            {
                var selectObjectCtrl = instance.treeNodeCtrl.selectObjectCtrl;
                if (selectObjectCtrl != null && selectObjectCtrl.Length != 0)
                    return selectObjectCtrl[0];
            }

            return null;
        }

        public void MoveAllCharaAndItemsHere(Vector3 newPos, bool keepY = true)
        {
            var instance = Singleton<Studio.Studio>.Instance;
            if (instance == null) return;

            var vector = newPos - moveAlongBasePos;
            if (keepY) vector.y = 0f;
            
            var list = new List<GuideCommand.EqualsInfo>();
            var selectObjectCtrl = instance.treeNodeCtrl.selectObjectCtrl;
            for (var i = 0; i < selectObjectCtrl.Length; i++)
            {
                var guideObject = selectObjectCtrl[i].guideObject;
                if (guideObject != null)
                {
                    var localPosition = guideObject.transformTarget.localPosition;
                    guideObject.transformTarget.position = guideObject.transformTarget.position + vector;
                    guideObject.changeAmount.pos = guideObject.transformTarget.localPosition;
                    if (guideObject.enablePos)
                    {
                        var item = new GuideCommand.EqualsInfo
                        {
                            dicKey = guideObject.dicKey,
                            oldValue = localPosition,
                            newValue = guideObject.changeAmount.pos
                        };
                        list.Add(item);
                    }
                }

                Singleton<UndoRedoManager>.Instance.Push(new GuideCommand.MoveEqualsCommand(list.ToArray()));
            }
        }

        public void MoveObject(ObjectCtrlInfo oci, Vector3 newPos, bool keepY)
        {
            if (keepY)
                newPos.y = oci.guideObject.transformTarget.position.y;

            var guideObject = oci.guideObject;
            if (guideObject != null)
            {
                var localPosition = guideObject.transformTarget.localPosition;
                guideObject.transformTarget.position = newPos;
                guideObject.changeAmount.pos = guideObject.transformTarget.localPosition;
                if (guideObject.enablePos)
                {
                    var equalsInfo = new GuideCommand.EqualsInfo
                    {
                        dicKey = guideObject.dicKey,
                        oldValue = localPosition,
                        newValue = guideObject.changeAmount.pos
                    };
                    Singleton<UndoRedoManager>.Instance.Push(new GuideCommand.MoveEqualsCommand(new GuideCommand.EqualsInfo[1] { equalsInfo }));
                }
            }
        }
    }
}
