using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static HandCtrl;
using static KK_VR.Holders.HandHolder;
using VRGIN.Core;
using IllusionUtility.GetUtility;
using KK_VR.Handlers;
using Illusion.Extensions;

namespace KK_VR.Holders
{
    /// <summary>
    /// Provides orientation of the held model
    /// </summary>
    internal abstract class Holder : MonoBehaviour
    {
        protected Rigidbody _rigidBody;
        protected ItemType _activeItem;

        // Can't arbitrary move VR controllers like in KK, have to do it with offsets.
        protected Vector3 _activeOffsetPos;
        protected Quaternion _activeOffsetRot;

        internal Transform Anchor => _anchor;
        protected Transform _anchor;
        protected Transform _offset;
        protected static readonly Dictionary<int, AibuItem> _loadedAssetsList = [];

        // Transparent thingy.
        internal static Material Material { get; private set; }
        protected ItemHandler _handler;
        protected readonly struct AnimParam
        {
            internal AnimParam(int _index, int[] _layers, List<ColliderInfo[]> _dbcInfo, int _startLayer, string _movePartName, Vector3 _posOffset, Quaternion _rotOffset)
            {
                index = _index;
                layers = _layers;
                dbcInfo = _dbcInfo;
                startLayer = _startLayer;
                movingPartName = _movePartName;
                positionOffset = _posOffset;
                rotationOffset = _rotOffset;
            }
            internal readonly int index;
            internal readonly int[] layers;
            internal readonly List<ColliderInfo[]> dbcInfo;
            internal readonly int startLayer;
            internal readonly string movingPartName;
            internal readonly Vector3 positionOffset;
            internal readonly Quaternion rotationOffset;
        }

        protected class ColliderInfo
        {
            internal ColliderInfo(Vector3 _center, float _radius, float _height, int _direction, Quaternion _localRot)
            {
                center = _center;
                radius = _radius;
                height = _height;
                direction = _direction;
                localRot = _localRot;
            }
            internal readonly Vector3 center;
            internal readonly float radius;
            internal readonly float height;
            internal readonly int direction;
            internal readonly Quaternion localRot;
        }

        protected private static readonly List<AnimParam> _defaultAnimParamList =
            [
            // Hand
            new AnimParam(
                _index: 0,
                _layers: [4, 7, 10],
                _dbcInfo: 
                [
                    [
                    new ColliderInfo(new Vector3(0f, 0.01f, 0f), 0.025f, 0.1f, 2, Quaternion.identity),
                    null,
                    ],
                    [
                    new ColliderInfo(new Vector3(0f, 0.01f, 0f), 0.025f, 0.1f, 2, Quaternion.identity),
                    null,
                    ],
                    [
                    new ColliderInfo(new Vector3(0f, 0.01f, 0f), 0.025f, 0.1f, 2, Quaternion.identity),
                    null,
                    ]
                ],
                _startLayer: 10,
                _movePartName: "cf_j_handroot_",
                _posOffset: new Vector3(0f, -0.02f, -0.07f),
                _rotOffset:  Quaternion.identity
                ),
            // Finger
            new AnimParam(
                _index: 1,
                _layers: [1, 3, 9, 4, 6],
                _dbcInfo:
                [
                    [ // 1
                    new ColliderInfo(new Vector3(0f, 0.005f, 0f), 0.025f, 0.04f, 2, Quaternion.identity),
                    new ColliderInfo(new Vector3(-0.018f, 0.02f, 0.03f), 0.01f, 0.06f, 2,  Quaternion.Euler(45, 0, 0)),
                    ],
                    [ // 3
                    new ColliderInfo(new Vector3(0f, 0.005f, 0f), 0.025f, 0.04f, 2, Quaternion.identity),
                    new ColliderInfo(new Vector3(-0.018f, 0.015f, 0.02f), 0.01f, 0.05f, 2, Quaternion.Euler(60, 0, 0)),
                    ],
                    [ // 9
                    new ColliderInfo(new Vector3(0f, 0.005f, 0f), 0.025f, 0.04f, 2, Quaternion.identity),
                    new ColliderInfo(new Vector3(-0.015f, 0.01f, 0.025f), 0.01f, 0.1f, 2, Quaternion.identity),
                    ],
                    [ // 4
                    new ColliderInfo(new Vector3(0f, 0.005f, 0f), 0.025f, 0.04f, 2, Quaternion.identity),
                    new ColliderInfo(new Vector3(-0.015f, 0.01f, 0.035f), 0.01f, 0.08f, 2, Quaternion.Euler(15, 0, 0)),
                    ],
                    [ // 6
                    new ColliderInfo(new Vector3(0f, 0.005f, 0f), 0.025f, 0.04f, 2, Quaternion.identity),
                    new ColliderInfo(new Vector3(-0.01f, 0.02f, 0.03f), 0.013f, 0.07f, 2, Quaternion.Euler(45, 0, 0))
                    ]
                ],
                _startLayer: 9,
                _movePartName: "cf_j_handroot_",
                _posOffset: new Vector3(0f, -0.02f, -0.07f),
                _rotOffset:  Quaternion.identity
                ),
            // Massager
            new AnimParam(
                _index: 2,
                _layers: [0, 1],
                _dbcInfo:
                [
                    [
                    new ColliderInfo(new Vector3(0f, 0f, 0.115f), 0.032f, 0.05f, 2, Quaternion.identity),
                    new ColliderInfo(Vector3.zero, 0.025f, 0.1f, 2, Quaternion.identity),
                    ],
                    [
                    new ColliderInfo(new Vector3(0f, 0f, 0.115f), 0.032f, 0.05f, 2, Quaternion.identity),
                    new ColliderInfo(Vector3.zero, 0.025f, 0.1f, 2, Quaternion.identity),
                    ]
                ],
                _startLayer: 0,
                _movePartName: "N_massajiki_",
                _posOffset: new Vector3(0f, 0f, -0.05f),
                _rotOffset: Quaternion.Euler(-90f, 180f, 0f)
                ),
            // Vibe
            new AnimParam(
                _index: 3,
                _layers: [0, 1],
                _dbcInfo:
                [
                    [
                    new ColliderInfo(new Vector3(0f, 0f, 0.1f), 0.02f, 0.28f, 2, Quaternion.identity),
                    new ColliderInfo(new Vector3(0f, -0.0325f, 0.1100f), 0.012f, 0.03f, 2, Quaternion.Euler(-30, 0, 0)),
                    ],
                    [
                    new ColliderInfo(new Vector3(0f, 0f, 0.1f), 0.02f, 0.28f, 2, Quaternion.identity),
                    new ColliderInfo(new Vector3(0f, -0.0325f, 0.1100f), 0.012f, 0.03f, 2, Quaternion.Euler(-30, 0, 0)),
                    ]
                ],
                _startLayer: 0,
                _movePartName: "N_vibe_Angle",
                _posOffset: new Vector3(0f, 0f, -0.1f),
                _rotOffset: Quaternion.Euler(-90f, 180f, 0f)
                ),

            // Dildo
            new AnimParam(
                _index: 4,
                _layers: [1, 3],
                _dbcInfo:
                [
                    [
                    new ColliderInfo(new Vector3(0f, -0.0350f, 0.03f), 0.02f, 0.06f, 0, Quaternion.identity),
                    new ColliderInfo(new Vector3(0f, 0.0075f, 0.1f), 0.02f, 0.18f, 2, Quaternion.Euler(7, 0, 0)),
                    ],
                    [
                    new ColliderInfo(new Vector3(0f, -0.0350f, 0.03f), 0.02f, 0.06f, 0, Quaternion.identity),
                    new ColliderInfo(new Vector3(0f, 0.0075f, 0.1f), 0.02f, 0.18f, 2, Quaternion.Euler(7, 0, 0)),
                    ]
                ],
                _startLayer: 1,
                _movePartName: "N_dildo_Angle",
                _posOffset: new Vector3(0f, 0f, -0.1f),
                _rotOffset: Quaternion.Euler(90f, 0f, 0f)
                ),

            // Rotor
            new AnimParam(
                _index: 5,
                _layers: [0, 1],
                _dbcInfo:
                [
                    [
                    new ColliderInfo(new Vector3(0f, 0f, 0f), 0.015f, 0.04f, 2, Quaternion.identity),
                    null,
                    ],
                    [
                    new ColliderInfo(new Vector3(0f, 0f, 0f), 0.015f, 0.04f, 2, Quaternion.identity),
                    null,
                    ]
                ],
                _startLayer: 0,
                _movePartName: "N_move_",
                _posOffset: new Vector3(0.005f, -0.01f, -0.025f),
                _rotOffset: Quaternion.Euler(90f, 0f, 0f)
                ),

            //new AnimParam
            //{
            //    // 6 - Tongue
            //    /*
            //     *  21, 16, 18, 19   - licking haphazardly
            //     *      7 - at lower angle 
            //     *      9 - at lower angle very slow
            //     *      10 - at higher angle
            //     *      12 - at higher angle very slow 
            //     *      
            //     *  13 - very high angle, flopping
            //     *  
            //     *  15 - very high angle, back forth
            //     *  1, 3, 4, 6,   - licking 
            //     *  
            //     *  1 - Lick
            //     *  2 - Lick stop.
            //     *  3 - Lick 
            //     *  4 - Lick
            //     *  5 - Lick stop.
            //     *  6 - Lick
            //     *  7 - Lick no pos move, angular same.
            //     *  8 - Lick no pos move, angular same stop.
            //     *  9 - Lick no pos move, slow angular
            //     *  10 - Lick no pos move, fast angular
            //     *  11 - Lick no pos move, fast(slow) angular, stop
            //     *  12 - Lick no pos move, slow angular
            //     *  13 - Doing "fish our of sea" movements
            //     *  14 - Doing "fish our of sea" movements stop
            //     *  15 - Tube-like back and forth
            //     *  16 - Intense lick
            //     *  17 - Intense lick stop
            //     *  18 - Intense lick
            //     *  19 - Extra intense lick
            //     *  20 - Extra intense lick stop
            //     *  21 - Lick
            //     */
            //    index = 6,
            //    availableLayers = [1, 7, 9, 10, 12, 13, 15, 16],
            //    movingPartName = "cf_j_tang_01", // cf_j_tang_01 / cf_j_tangangle
            //    //handlerParentName = "cf_j_tang_03",
            //    positionOffset = new Vector3(0f, -0.04f, 0.05f),
            //    rotationOffset = Quaternion.identity, // Quaternion.Euler(-90f, 0f, 0f)
            //},
            ];
        protected class ItemType
        {
            internal readonly AibuItem aibuItem;
            //internal readonly GameObject handlerParent;
            internal readonly Transform rootPoint;
            internal readonly Transform movingPoint;
            internal readonly AnimParam animParam;
            //internal readonly Quaternion rotationOffset;
            //internal readonly Vector3 positionOffset;
            internal int layer;
            //internal readonly int[] availableLayers;
            //internal readonly int startLayer;

            internal ItemType(int index, AibuItem _asset)
            {
                aibuItem = _asset;
                rootPoint = _asset.obj.transform;
                rootPoint.transform.SetParent(VR.Manager.transform, false);
                animParam = _defaultAnimParamList[index];
                movingPoint = rootPoint.GetComponentsInChildren<Transform>()
                    .Where(t => t.name.StartsWith(animParam.movingPartName, StringComparison.Ordinal))
                    .FirstOrDefault();
                //handlerParent = rootPoint.GetComponentsInChildren<Transform>()
                //    .Where(t => t.name.StartsWith(animParam.handlerParentName, StringComparison.Ordinal)
                //    || t.name.EndsWith(animParam.handlerParentName, StringComparison.Ordinal))
                //    .FirstOrDefault().gameObject;

            }
        }


        
        internal void SetCollisionState(bool state)
        {
            // Surprisingly we can't enable isKinematic during collision/intersection,
            // whole collision system goes haywire otherwise. 
            // So we disable first collider instead.
#if KK
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
#else
            if (gameObject.TryGetComponent<Collider>(out var collider))
#endif
            {
                collider.enabled = state;
            }
        }
#if KK
        protected void LoadAssets()
        {
            // Straight from HandCtrl.
            var textAsset = GlobalMethod.LoadAllListText("h/list/", "AibuItemObject", null);
            GlobalMethod.GetListString(textAsset, out var array);
            for (int i = 0; i < array.GetLength(0); i++)
            {
                int num = 0;
                int num2 = 0;

                int.TryParse(array[i, num++], out num2);

                if (!_loadedAssetsList.TryGetValue(num2, out var aibuItem))
                {
                    _loadedAssetsList.Add(num2, new AibuItem());
                    aibuItem = _loadedAssetsList[num2];
                }
                aibuItem.SetID(num2);


                var manifestName = array[i, num++];
                var text2 = array[i, num++];
                var assetName = array[i, num++];
                aibuItem.SetObj(CommonLib.LoadAsset<GameObject>(text2, assetName, true, manifestName));
                //this.flags.hashAssetBundle.Add(text2);
                var text3 = array[i, num++];
                var isSilhouetteChange = array[i, num++] == "1";
                var flag = array[i, num++] == "1";
                if (!text3.IsNullOrEmpty())
                {
                    aibuItem.objBody = aibuItem.obj.transform.FindLoop(text3);
                    if (aibuItem.objBody)
                    {
                        aibuItem.renderBody = aibuItem.objBody.GetComponent<SkinnedMeshRenderer>();
                        if (flag)
                        {
                            aibuItem.mHand = aibuItem.renderBody.material;

                        }
                    }
                }
                aibuItem.isSilhouetteChange = isSilhouetteChange;
                text3 = array[i, num++];
                if (!text3.IsNullOrEmpty())
                {
                    aibuItem.objSilhouette = aibuItem.obj.transform.FindLoop(text3);
                    if (aibuItem.objSilhouette)
                    {
                        aibuItem.renderSilhouette = aibuItem.objSilhouette.GetComponent<SkinnedMeshRenderer>();
                        aibuItem.mSilhouette = aibuItem.renderSilhouette.material;
                        aibuItem.renderSilhouette.enabled = false;
                        aibuItem.SetHandColor(new Color(0.960f, 0.887f, 0.864f, 1.000f));
                        if (Material == null)
                            Material = aibuItem.renderSilhouette.material;
                    }
                }
                int.TryParse(array[i, num++], out num2);
                aibuItem.SetIdObj(num2);
                int.TryParse(array[i, num++], out num2);
                aibuItem.SetIdUse(num2);
                if (aibuItem.obj)
                {
                    //EliminateScale[] componentsInChildren = aibuItem.obj.GetComponentsInChildren<EliminateScale>(true);
                    //if (componentsInChildren != null && componentsInChildren.Length != 0)
                    //{
                    //    componentsInChildren[componentsInChildren.Length - 1].LoadList(aibuItem.id);
                    //}
                    var components = aibuItem.obj.transform.GetComponentsInChildren<EliminateScale>(true);
                    foreach (var component in components)
                    {
                        //component.enabled = false;
                        UnityEngine.Component.Destroy(component);
                    }
                    aibuItem.SetAnm(aibuItem.obj.GetComponent<Animator>());
                    //aibuItem.obj.SetActive(false);
                    //aibuItem.obj.transform.SetParent(VR.Manager.transform, false);
                }
                aibuItem.pathSEAsset = array[i, num++];
                aibuItem.nameSEFile = array[i, num++];
                aibuItem.saveID = int.Parse(array[i, num++]);
                aibuItem.isVirgin = (array[i, num++] == "1");
                aibuItem.obj.SetActive(false);
            }
        }
#else
        // CopyPaste from the game.
        internal static readonly List<string> loadLists =
            [
            "h/list/00.unity3d",
            "h/list/01.unity3d",
            "h/list/30.unity3d",
            "h/list/31.unity3d",
            "h/list/60.unity3d",
            "h/list/70.unity3d",
            "h/list/71.unity3d",
            "h/list/81.unity3d",
            "h/list/91.unity3d",
            "h/list/bfwapartment1.unity3d",
            "h/list/hs2suite.unity3d",
            "h/list/mmbath.unity3d",
            "h/list/mmhouse01.unity3d",
            "h/list/poolmap.unity3d",
            "h/list/sakuraroom.unity3d",
            ];

        internal static bool BundleCheck(string path, string targetName)
        {
            bool result = false;
            string[] allAssetName = AssetBundleCheck.GetAllAssetName(path, false, "", false);
            for (int i = 0; i < allAssetName.Length; i++)
            {
                if (allAssetName[i].Compare(targetName, true))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        protected void LoadAssets()
        {
            // KKS HandCtrl.

            string text = "AibuItemObject";
            foreach (string text2 in loadLists)
            {
                if (AssetBundleCheck.IsFile(text2, text) && (AssetBundleCheck.IsSimulation || BundleCheck(text2, text)))
                {
                    AibuItemObjectData aibuItemObjectData = CommonLib.LoadAsset<AibuItemObjectData>(text2, text, true, "", false);
                    AssetBundleManager.UnloadAssetBundle(text2, true, null, false);
                    if (!(aibuItemObjectData == null))
                    {
                        foreach (AibuItemObjectData.Param param in aibuItemObjectData.param)
                        {
                            if (!_loadedAssetsList.TryGetValue(param.id, out var aibuItem))
                            {
                                _loadedAssetsList.Add(param.id, new AibuItem());
                                aibuItem = _loadedAssetsList[param.id];
                            }
                            aibuItem.SetID(param.id);
                            aibuItem.SetObj(CommonLib.LoadAsset<GameObject>(param.bundle, param.asset, true, param.manifest, true));
                            //this.flags.hashAssetBundle.Add(param.bundle);
                            if (!param.nomalObj.IsNullOrEmpty())
                            {
                                aibuItem.objBody = aibuItem.obj.transform.FindLoop(param.nomalObj);
                                if (aibuItem.objBody)
                                {
                                    aibuItem.renderBody = aibuItem.objBody.GetComponent<SkinnedMeshRenderer>();
                                    //aibuItem.meshRenderBody = aibuItem.objBody.GetComponent<MeshRenderer>();
                                    if (param.isMaterialGet)
                                    {
                                        if (aibuItem.renderBody != null)
                                        {
                                            aibuItem.mHand = aibuItem.renderBody.material;
                                        }
                                        //else if (aibuItem.meshRenderBody != null)
                                        //{
                                        //    aibuItem.mHand = aibuItem.meshRenderBody.material;
                                        //}
                                    }
                                }
                            }
                            aibuItem.isSilhouetteChange = param.isSilhouetteChange;
                            if (!param.objSilhouette.IsNullOrEmpty())
                            {
                                aibuItem.objSilhouette = aibuItem.obj.transform.FindLoop(param.objSilhouette);
                                if (aibuItem.objSilhouette)
                                {
                                    aibuItem.renderSilhouette = aibuItem.objSilhouette.GetComponent<SkinnedMeshRenderer>();
                                    //aibuItem.meshRenderSilhouette = aibuItem.objSilhouette.GetComponent<MeshRenderer>();
                                    if (aibuItem.renderSilhouette != null)
                                    {
                                        aibuItem.mSilhouette = aibuItem.renderSilhouette.material;
                                    }
                                    //else if (aibuItem.meshRenderSilhouette != null)
                                    //{
                                    //    aibuItem.mSilhouette = aibuItem.meshRenderSilhouette.material;
                                    //}
                                    aibuItem.renderSilhouette.enabled = false;
                                    aibuItem.SetHandColor(new Color(0.960f, 0.887f, 0.864f, 1.000f));
                                    if (!Material)
                                        Material = aibuItem.renderSilhouette.material;
                                }
                            }
                            aibuItem.SetIdObj(param.objKind);
                            aibuItem.SetIdUse(param.setKind);
                            if (aibuItem.obj != null)
                            {
                                //EliminateScale[] componentsInChildren = aibuItem.obj.GetComponentsInChildren<EliminateScale>(true);
                                //if (componentsInChildren != null && componentsInChildren.Length != 0)
                                //{
                                //    componentsInChildren[componentsInChildren.Length - 1].LoadList(aibuItem.id);
                                //}
                                foreach (var component in aibuItem.obj.transform.GetComponentsInChildren<EliminateScale>(true))
                                {
                                    UnityEngine.Component.Destroy(component);
                                }
                                aibuItem.SetAnm(aibuItem.obj.GetComponent<Animator>());
                                aibuItem.obj.SetActive(false);
                            }
                            aibuItem.pathSEAsset = param.bundleSE;
                            aibuItem.nameSEFile = param.nameSE;
                            aibuItem.saveID = param.saveID;
                            aibuItem.isVirgin = param.isVirgin;
                        }
                    }
                }
            }
        }
#endif

        //protected virtual void LateUpdate()
        //{
        //    _lastAnchorPos = _anchor.position;
        //}

        internal void SetItemRenderer(bool show)
        {
            if (_activeItem.aibuItem == null) return;
            _activeItem.aibuItem.objBody.GetComponent<Renderer>().enabled = show;
        }
    }
}
