using System;
using System.Collections;
using KK_VR.Util;
using Studio;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace KK_VR.Controls
{
    public class IKTool : MonoBehaviour
    {
        public static IKTool instance;

        private readonly float markerOverlayAlpha = 0.4f;
        private readonly float DEFAULT_SCALE_POS_XYZ_DIST = Mathf.Sqrt(0.1875f);

        private GameObject handle;

        public static IKTool Create(GameObject container)
        {
            if (instance != null) return instance;
            instance = container.AddComponent<IKTool>();
            return instance;
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneWasLoaded;
        }

        private void Start()
        {
            StartWatch();
        }

        private IEnumerator InstallMoveableObjectCo()
        {
            var studio = Singleton<Studio.Studio>.Instance;
            if (studio == null) yield break;

            while (true)
            {
                yield return new WaitForSeconds(1f);
                try
                {
                    foreach (var item in studio.dicObjectCtrl)
                    {
                        if (item.Value == null) continue;
                        var value = item.Value;
                        if (!(value.guideObject != null) || !(value.guideObject.gameObject != null)) continue;
                        MakeObjectMoveable(value.guideObject, true, true);
                        if (!(value is OCIChar)) continue;
                        var oCIChar = value as OCIChar;
                        foreach (var item2 in oCIChar.listIKTarget) MakeObjectMoveable(item2.guideObject, true);
                        foreach (var listBone in oCIChar.listBones) MakeObjectMoveable(listBone.guideObject, true);
                    }
                }
                catch (Exception value2)
                {
                    Console.WriteLine(value2);
                }
            }
        }

        private void OnSceneWasLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            StartWatch();
        }

        private void StartWatch()
        {
            StopAllCoroutines();
            StartCoroutine(InstallMoveableObjectCo());

            if (handle == null)
            {
                handle = new GameObject("handle");
                handle.transform.parent = gameObject.transform;
            }
        }

        private void MakeObjectMoveable(GuideObject guideObject, bool replaceMaterial = false, bool installToCenter = false)
        {
            if (!(guideObject.transformTarget == null))
            {
                if (installToCenter)
                {
                    InstallGripMoveMarker(guideObject.gameObject, OnObjectMove, guideObject, replaceMaterial, installToCenter);
                }
                else
                {
                    var target = guideObject.gameObject.transform.Find("Sphere").gameObject;
                    InstallGripMoveMarker(target, OnObjectMove, guideObject, replaceMaterial, installToCenter);
                }

                if (guideObject.enableScale) InstallScaleMoveMarker(guideObject);
            }
        }

        private bool InstallGripMoveMarker(GameObject target, Action<MonoBehaviour> moveHandler, GuideObject guideObject, bool replaceMaterial, bool installToCenter)
        {
            if (target.transform.Find("_gripmovemarker") == null)
            {
                Renderer visibleReference = null;
                GameObject gameObject;
                if (installToCenter)
                {
                    gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    gameObject.name = "_gripmovemarker";
                    gameObject.layer = LayerMask.NameToLayer("Studio/Col");
                    var component = gameObject.GetComponent<Renderer>();
                    var material = new Material(MaterialHelper.GetColorZOrderShader());
                    material.color = new Color(0f, 1f, 0f, 0.3f);
                    material.SetFloat("_AlphaRatio", 0.5f);
                    material.renderQueue = 3800;
                    component.material = material;
                    component.shadowCastingMode = ShadowCastingMode.Off;
                    component.receiveShadows = false;
                    var transform = target.transform.Find("move/XYZ");
                    if (transform != null) visibleReference = transform.gameObject.GetComponent<Renderer>();
                }
                else
                {
                    gameObject = new GameObject("_gripmovemarker");
                    if (replaceMaterial)
                    {
                        var component2 = target.GetComponent<Renderer>();
                        if (component2 != null)
                        {
                            var material2 = new Material(MaterialHelper.GetColorZOrderShader());
                            material2.CopyPropertiesFromMaterial(component2.material);
                            material2.SetFloat("_AlphaRatio", 0.5f);
                            material2.renderQueue = 3800;
                            component2.material = material2;
                            component2.shadowCastingMode = ShadowCastingMode.Off;
                            component2.receiveShadows = false;
                            visibleReference = component2;
                        }
                    }
                }

                var sphereCollider = gameObject.AddComponent<SphereCollider>();
                var obj = gameObject.transform;
                obj.transform.parent = target.transform;
                obj.transform.localPosition = Vector3.zero;
                obj.transform.rotation = guideObject.transformTarget.rotation;
                obj.transform.localScale = Vector3.one;
                sphereCollider.isTrigger = true;
                var moveableGUIObject = gameObject.AddComponent<MoveableGUIObject>();
                moveableGUIObject.guideObject = guideObject;
                moveableGUIObject.onMoveLister.Add(moveHandler);
                moveableGUIObject.visibleReference = visibleReference;
                if (installToCenter) moveableGUIObject.isMoveObj = true;
                return true;
            }

            return false;
        }

        private bool InstallScaleMoveMarker(GuideObject guideObject)
        {
            var transform = guideObject.gameObject.transform.Find("scale");
            if (transform.transform.Find("X/_gripmovemarker_scale") == null)
            {
                var array = new string[4] { "XYZ", "X", "Y", "Z" };
                foreach (var n in array)
                {
                    var transform2 = transform.Find(n);
                    var component = transform2.gameObject.GetComponent<GuideScale>();
                    if (component != null)
                    {
                        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        obj.name = "_gripmovemarker_scale";
                        obj.layer = LayerMask.NameToLayer("Studio/Col");
                        var component2 = obj.GetComponent<Renderer>();
                        var material = new Material(MaterialHelper.GetColorZOrderShader());
                        material.color = new Color(0f, 1f, 1f, 0.3f);
                        material.SetFloat("_AlphaRatio", 0.5f);
                        material.renderQueue = 3800;
                        component2.material = material;
                        component2.shadowCastingMode = ShadowCastingMode.Off;
                        component2.receiveShadows = false;
                        Renderer renderer = null;
                        renderer = transform2.gameObject.GetComponent<Renderer>();
                        var sphereCollider = obj.AddComponent<SphereCollider>();
                        var obj2 = obj.transform;
                        obj2.transform.parent = transform2;
                        obj2.transform.localPosition = CalcScaleHandleDefaultPos(component);
                        obj2.transform.rotation = guideObject.transformTarget.rotation;
                        obj2.transform.localScale = Vector3.one;
                        sphereCollider.isTrigger = true;
                        var moveableGUIObject = obj.AddComponent<MoveableGUIObject>();
                        moveableGUIObject.guideObject = guideObject;
                        moveableGUIObject.guideScale = component;
                        moveableGUIObject.onMoveLister.Add(OnScaleMove);
                        moveableGUIObject.onReleasedLister.Add(OnScaleReleased);
                        moveableGUIObject.visibleReference = renderer;
                    }
                }

                return true;
            }

            return false;
        }

        private void OnObjectMove(MonoBehaviour marker)
        {
            DoOnMove(marker, marker.transform.parent);
        }

        private void OnObjectCubeMoveNoRotation(MonoBehaviour marker)
        {
            DoOnMove(marker, marker.transform.parent.parent, false);
        }

        private void OnObjectRotationNoMove(MonoBehaviour marker)
        {
            DoOnMove(marker, marker.transform.parent, true, false);
        }

        private void OnRawObjectMove(MonoBehaviour marker)
        {
            DoOnMove(marker, marker.transform.parent);
        }

        private void DoOnMove(MonoBehaviour marker, Transform target, bool rotation = true, bool pos = true)
        {
            var component = marker.GetComponent<MoveableGUIObject>();
            var parent = marker.transform.parent;
            var guideObject = component.guideObject;
            pos &= guideObject.enablePos;
            rotation &= guideObject.enableRot;
            if (pos)
            {
                target.position += marker.transform.position - parent.transform.position;
                guideObject.transformTarget.transform.position = target.position;
                guideObject.changeAmount.pos = guideObject.transformTarget.localPosition;
            }

            marker.transform.localPosition = Vector3.zero;
            if (rotation)
            {
                guideObject.transformTarget.rotation = marker.transform.rotation;
                guideObject.changeAmount.rot = guideObject.transformTarget.localEulerAngles;
            }
        }

        private void OnScaleMove(MonoBehaviour marker)
        {
            var component = marker.GetComponent<MoveableGUIObject>();
            var guideObject = component.guideObject;
            var guideScale = component.guideScale;
            if (!guideObject.enableScale || !component.guideScale) return;
            var magnitude = marker.transform.localPosition.magnitude;
            if (magnitude > 0f)
            {
                var num = magnitude / 0.25f;
                var oldScale = component.oldScale;
                switch (guideScale.axis)
                {
                    case GuideScale.ScaleAxis.XYZ:
                        oldScale *= num;
                        break;
                    case GuideScale.ScaleAxis.X:
                        oldScale.x *= num;
                        break;
                    case GuideScale.ScaleAxis.Y:
                        oldScale.y *= num;
                        break;
                    case GuideScale.ScaleAxis.Z:
                        oldScale.z *= num;
                        break;
                }

                oldScale.x = Mathf.Max(oldScale.x, 0.01f);
                oldScale.y = Mathf.Max(oldScale.y, 0.01f);
                oldScale.z = Mathf.Max(oldScale.z, 0.01f);
                guideObject.changeAmount.scale = oldScale;
            }
        }

        private void OnScaleReleased(MonoBehaviour marker)
        {
            var component = marker.GetComponent<MoveableGUIObject>();
            var guideObject = component.guideObject;
            var guideScale = component.guideScale;
            if (guideObject.enableScale && (bool)guideScale)
                marker.transform.localPosition = CalcScaleHandleDefaultPos(guideScale);
        }

        private Vector3 CalcScaleHandleDefaultPos(GuideScale guideScale)
        {
            return guideScale.axis switch
            {
                GuideScale.ScaleAxis.XYZ => new Vector3(0.25f, 0.25f, 0.25f) * 0.25f / DEFAULT_SCALE_POS_XYZ_DIST,
                GuideScale.ScaleAxis.X => new Vector3(0.25f, 0f, 0f),
                GuideScale.ScaleAxis.Y => new Vector3(0f, 0.25f, 0f),
                GuideScale.ScaleAxis.Z => new Vector3(0f, 0f, 0.25f),
                _ => Vector3.zero
            };
        }

        private Material CreateForceDrawMaterial()
        {
            try
            {
                var material = new Material(MaterialHelper.GetColorZOrderShader());
                var red = Color.red;
                red.a = markerOverlayAlpha;
                material.SetColor("_Color", red);
                return material;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}
