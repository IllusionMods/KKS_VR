using System.Collections.Generic;
using System.Reflection;
using Studio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KK_VR.Controls
{
    public class VRItemObjMoveHelper : MonoBehaviour
    {
        public bool showGUI = true;
        public RectTransform menuRect;
        private static VRItemObjMoveHelper _instance;
        public bool keepY = true;
        public bool moveAlong;
        public Vector3 moveAlongBasePos;
        public Quaternion moveAlongBaseRot;
        private readonly ObjMoveHelper helper = new ObjMoveHelper();
        private Studio.Studio studio;
        private GameObject moveDummy;
        private Texture2D bgTex;
        private Button callButton;
        private Button callXZButton;

        private static readonly MethodInfo m_AddSelectNode =
            typeof(TreeNodeCtrl).GetMethod("AddSelectNode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

        public static VRItemObjMoveHelper Instance => _instance;

        public static void Install(GameObject container)
        {
            if (_instance == null) _instance = container.AddComponent<VRItemObjMoveHelper>();
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            studio = Singleton<Studio.Studio>.Instance;
            if (!(studio == null))
            {
                var objectListCanvas = studio.gameObject.transform.Find("Canvas Object List");
                _instance.Init(objectListCanvas);
                bgTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                bgTex.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f, 1f));
                bgTex.Apply();
            }
        }

        public static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            var vector = Vector2.Scale(transform.rect.size, transform.lossyScale);
            var result = new Rect(transform.position.x, Screen.height - transform.position.y, vector.x, vector.y);
            result.x -= transform.pivot.x * vector.x;
            result.y -= (1f - transform.pivot.y) * vector.y;
            return result;
        }

        private void Init(Transform objectListCanvas)
        {
            menuRect = objectListCanvas.Find("Image Bar/Scroll View").gameObject.GetComponent<RectTransform>();

            if (moveDummy == null)
            {
                moveDummy = new GameObject("MoveDummy");
                DontDestroyOnLoad(moveDummy);
                moveDummy.transform.parent = gameObject.transform;
            }

            var transform = objectListCanvas.Find("Image Bar/Button Duplicate");
            var transform2 = objectListCanvas.Find("Image Bar/Button Route");
            var transform3 = objectListCanvas.Find("Image Bar/Button Camera");
            if (transform != null)
            {
                var num = transform3.localPosition.x - transform2.localPosition.x;
                var sprite = Sprite.Create(UnityHelper.LoadImage("icon_call.png"), new Rect(0f, 0f, 32f, 32f), Vector2.zero);
                var sprite2 = Sprite.Create(UnityHelper.LoadImage("icon_call_xz.png"), new Rect(0f, 0f, 32f, 32f), Vector2.zero);
                if (callButton == null)
                {
                    var obj = Instantiate(transform.gameObject);
                    obj.name = "Button Call";
                    obj.transform.SetParent(transform.transform.parent);
                    obj.transform.localPosition = new Vector3(transform2.localPosition.x - num * 2f, transform2.localPosition.y, transform2.localPosition.z);
                    obj.transform.localScale = Vector3.one;

                    var component = obj.GetComponent<Button>();
                    component.spriteState = new SpriteState
                    {
                        disabledSprite = sprite,
                        highlightedSprite = sprite,
                        pressedSprite = sprite
                    };
                    component.onClick = new Button.ButtonClickedEvent();
                    component.onClick.AddListener(OnCallClick);
                    component.interactable = true;
                    callButton = component;

                    DestroyImmediate(obj.GetComponent<Image>());
                    var image = obj.AddComponent<Image>();
                    image.sprite = sprite;
                    image.type = Image.Type.Simple;
                    image.SetAllDirty();
                }

                if (callXZButton == null)
                {
                    var obj2 = Instantiate(transform.gameObject);
                    obj2.name = "Button Call YLock";
                    obj2.transform.SetParent(transform.transform.parent);
                    obj2.transform.localPosition = new Vector3(transform2.localPosition.x - num, transform2.localPosition.y, transform2.localPosition.z);
                    obj2.transform.localScale = Vector3.one;

                    var component2 = obj2.GetComponent<Button>();
                    component2.spriteState = new SpriteState
                    {
                        disabledSprite = sprite2,
                        highlightedSprite = sprite2,
                        pressedSprite = sprite2
                    };
                    component2.onClick = new Button.ButtonClickedEvent();
                    component2.onClick.AddListener(OnCallClickYLock);
                    component2.interactable = true;
                    callXZButton = component2;

                    DestroyImmediate(obj2.GetComponent<Image>());
                    var image2 = obj2.AddComponent<Image>();
                    image2.sprite = sprite2;
                    image2.type = Image.Type.Simple;
                    image2.SetAllDirty();
                }
            }

            VRLog.Info("VR ItemObjMoveHelper installed");
        }

        private void OnCallClick()
        {
            MoveAllCharaAndItemsHere(false);
        }

        private void OnCallClickYLock()
        {
            MoveAllCharaAndItemsHere(true);
        }

        public void CallCurrentObject(bool keepY = false)
        {
            if (studio.treeNodeCtrl.selectObjectCtrl != null && studio.treeNodeCtrl.selectObjectCtrl.Length != 0)
            {
                var objectCtrlInfo = studio.treeNodeCtrl.selectObjectCtrl[0];
                if (objectCtrlInfo != null) MoveObjectHere(objectCtrlInfo);
            }
        }

        public void MoveAllCharaAndItemsHere(bool keepY = false)
        {
            var newPos = VR.Camera.Head.TransformPoint(0f, 0f, 0.2f);
            var firstObject = helper.GetFirstObject();
            if (firstObject != null)
            {
                helper.moveAlongBasePos = firstObject.guideObject.transformTarget.position;
                helper.MoveAllCharaAndItemsHere(newPos, keepY);
                moveAlongBasePos = newPos;
            }
        }

        public void MoveObjectHere(ObjectCtrlInfo oci)
        {
            var newPos = VR.Camera.Head.TransformPoint(0f, 0f, 0.2f);
            helper.MoveObject(oci, newPos, keepY);
        }

        public void VRToggleObjectSelectOnCursor()
        {
            var instance = Singleton<Studio.Studio>.Instance;
            if (instance == null) return;

            var pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.mousePosition;

            var list = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, list);
            foreach (var item in list)
                if (item.gameObject != null && item.gameObject.transform.parent != null)
                {
                    var component = item.gameObject.transform.parent.gameObject.GetComponent<TreeNodeObject>();
                    if (component != null && instance.dicInfo.ContainsKey(component))
                    {
                        m_AddSelectNode.Invoke(instance.treeNodeCtrl, new object[2] { component, true });
                        break;
                    }
                }
        }
    }
}
