using BepInEx;
using KK_VR.Features;
using KK_VR.Fixes;
using KK_VR.Interpreters;
using ADV.Commands.Object;
using Illusion.Game;
using IllusionUtility.GetUtility;
using SceneAssist;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using VRGIN.Core;
using static HandCtrl;
using static RootMotion.FinalIK.RagdollUtility;
using KK_VR.Interactors;
using VRGIN.Controls;
using ADV.Commands.Base;
using KK_VR.Controls;
using RootMotion.FinalIK;
using KK_VR.Handlers;
using KK_VR.Grasp;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;

namespace KK_VR.Holders
{
    // We adapt animated aibu items as controller models. To see why we do this in SUCH a roundabout way
    // grab default disabled ones in HScene and scroll through their animation layers,
    // their orientations are outright horrible for our purposes.
    internal class HandHolder : Holder
    {
        // There currently a bug that doesn't let every second chosen 'Finger hand" to scale.
        // Initially asset has component that does exactly this (EliminateScale), but we remove it during initialization.
        // Yet once parented every !second! time, 'Finger hand' freezes it's own local scale at Vec.one;
        // At that moment no components 'EliminateScale' are present in runtime, no clue what can it be.
        private static readonly List<HandHolder> _instances = [];
        //private static readonly Dictionary<int, AibuItem> _loadedAssetsList = [];
        private readonly List<ItemType> _itemList = [];
        //private ItemType _activeItem;
        //private Transform _controller;
        //private Transform _anchor;
        //private Rigidbody _rigidBody;
        // private AudioSource _audioSource;
        private Transform _controller;
        //private readonly Vector3[] _prevPositions = new Vector3[20];
        //private readonly Quaternion[] _prevRotations = new Quaternion[20];
        //private readonly float[] _frameCoefs = new float[19];
        //private readonly float _avgCoef = 1f / 20f;
        //private int _currentStep;
        //private bool _lag;
        private ItemLag _itemLag;
        private bool _parent;
        internal bool IsParent => _parent;
        private HandNoise _handNoise;
        internal HandNoise Noise => _handNoise;
        internal Controller Controller { get; private set; }
        internal int Index { get; private set; }
        //internal static Material Material { get; private set; }
        //internal AudioSource AudioSource => _audioSource;
        internal ItemHandler Handler => _handler;
        internal GraspController Grasp { get; private set; }
        //internal Transform Anchor => _anchor;
        internal GameplayTool Tool { get; private set; }
        internal static List<HandHolder> GetHands() => _instances;
        private readonly int[] _itemIDs = [0, 2, 5, 7, 9, 11];
        internal static HandHolder GetHand(int index) => _instances[index];
        internal void Init(int index)
        {
            _instances.Add(this);
            Index = index;
            Controller = index == 0 ? VR.Mode.Left : VR.Mode.Right;
            Tool = Controller.GetComponent<GameplayTool>();
            _controller = Controller.transform;
            if (_loadedAssetsList.Count == 0)
            {
                LoadAssets();
                HandNoise.Init();
            }
            SetItems(index);
            Grasp = new GraspController(this);
            _handNoise = new HandNoise(gameObject.AddComponent<AudioSource>());
        }

        internal static void UpdateHandlers<T>()
            where T : ItemHandler
        {
            foreach (var inst in _instances)
            {
                inst.RemoveHandler();
                inst.AddHandler<T>();
            }
        }
        protected private void AddHandler<T>() 
            where T : ItemHandler
        {
            _handler = gameObject.AddComponent<T>();
            _handler.Init(this);
        }
        protected private void RemoveHandler()
        {
            if (_handler != null)
            {
                UnityEngine.Component.Destroy(_handler);
            }
        }
        internal static void DestroyHandlers()
        {
            foreach (var inst in _instances)
            {
                inst.RemoveHandler();
            }
        }

        private void SetItems(int index)
        {
            _anchor = transform;
            _anchor.SetParent(VR.Manager.transform, false);
            _offset = new GameObject("offset").transform;
            _offset.SetParent(_anchor, false);
            _rigidBody = gameObject.AddComponent<Rigidbody>();
            _rigidBody.useGravity = false;
            _rigidBody.freezeRotation = true;
            VRBoop.AddDB(gameObject.AddComponent<DynamicBoneCollider>());
            VRBoop.AddDB(_offset.gameObject.AddComponent<DynamicBoneCollider>());
            


            for (var i = 0; i <
#if KK
                4;
#else
                6; 
#endif
                i++)
            {
                InitItem(i, index);
            }

            _activeItem = _itemList[0];
            ActivateItem();
            Controller.Model.gameObject.SetActive(false);
        }
        private void InitItem(int i, int index)
        {
            var item = new ItemType(
                i,
                _asset: _loadedAssetsList[_itemIDs[i] + index]
                );
            _itemList.Add(item);
        }

        private void SetPhysMat(PhysicMaterial material)
        {
            material.staticFriction = 1f;
            material.dynamicFriction = 1f;
            material.bounciness = 0f;
        }
        private void SetColliders(int index)
        {
            /*
             * Material - static + dynamic friction = 1f
             * 
             * 
             * Hand - child (0,0.01f,0), Capsule - dir = 2, height - 0.1, radius = 0.025
             * 
             * Massager -  parent - Capsule - dir = 2, height = 0.15f, radius = 0.025f
             *             child - (0,0,0.115f), Capsule - dir = 2, height = 0.05f, radius = 0.032f
             * 
             * vibe -  child - (0,0,0.1f), Capsule - dir = 2, height = 0.3, radius = 0.025
             */

            foreach (var collider in gameObject.GetComponents<Collider>())
            {
                UnityEngine.Component.Destroy(collider);
            }
            foreach (var collider in _offset.GetComponents<Collider>())
            {
                UnityEngine.Component.Destroy(collider);
            }

            /*
             * 1 
             *      db1
             *      db.m_Direction = DynamicBoneCollider.Direction.Z;
             *      db.m_Height = 0.1f;
                    db.m_Radius = 0.025f;
                    db.m_Center = new Vector3(0f, 0.01f, 0f);
                    db2
                    disabled
               2
                    9
                        db1
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
             *          db.m_Height = 0.1f;
                        db.m_Radius = 0.01f;
                        db.m_Center = new Vector3(-0.015f, 0.01f, 0.025f);
            *           db2
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           db.m_Height = 0.04f;
            *           db.m_Radius = 0.025f;
            *           db.m_Center = new Vector3(0f, 0.01f, 0f);
            *       4
            *           db1
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           db.m_Height = 0.04f;
            *           db.m_Radius = 0.025f;
            *           db.m_Center = new Vector3(0f, 0.005f, 0f);
            *       
            *           db2
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           db.m_Height = 0.08f;
            *           db.m_Radius = 0.01f;
            *           db.m_Center = new Vector3(-0.015f, 0.01f, 0.035f);
            *           localRotation = Quaternion.Euler(15, 0, 0);
            *           
            *       6
            *           db1
            *           db.m_Center = new Vector3(0f, 0.005f, 0f);
            *           db.m_Radius = 0.025f;
            *           db.m_Height = 0.04f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *       
            *           db2
            *           db.m_Center = new Vector3(-0.01f, 0.02f, 0.03f);
            *           db.m_Radius = 0.013f;
            *           db.m_Height = 0.07f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           localRotation = Quaternion.Euler(45, 0, 0);
            *           
            *       1
            *           db1
            *           db.m_Center = new Vector3(0f, 0.005f, 0f);
            *           db.m_Radius = 0.025f;
            *           db.m_Height = 0.04f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *       
            *           db2
            *           db.m_Center = new Vector3(-0.018f, 0.02f, 0.03f);
            *           db.m_Radius = 0.01f;
            *           db.m_Height = 0.06f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           localRotation = Quaternion.Euler(45, 0, 0);
            *           
            *       3
            *           db1
            *           db.m_Center = new Vector3(0f, 0.005f, 0f);
            *           db.m_Radius = 0.025f;
            *           db.m_Height = 0.04f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *       
            *           db2
            *           db.m_Center = new Vector3(-0.018f, 0.015f, 0.02f);
            *           db.m_Radius = 0.01f;
            *           db.m_Height = 0.05f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           localRotation = Quaternion.Euler(60, 0, 0);
            * 3
            *           db1
            *           db.m_Center = new Vector3(0f, 0f, 0.115f);
            *           db.m_Radius = 0.032f;
            *           db.m_Height = 0.05f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *       
            *           db2
            *           db.m_Radius = 0.025f;
            *           db.m_Height = 0.1f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            * 4
            *           db1
            *           db.m_Center = new Vector3(0f, 0f, 0.1f);
            *           db.m_Radius = 0.018f;
            *           db.m_Height = 0.28f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *       
            *           db2
            *           db.m_Center = new Vector3(0f, -0.0325f, 0.1100f);
            *           db.m_Radius = 0.012f;
            *           db.m_Height = 0.03f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           localRotation = Quaternion.Euler(-30, 0, 0);
            *           
            * 5
            *           db1
            *           db.m_Center = new Vector3(0f, 0.0075, 0.1f);
            *           db.m_Radius = 0.02f;
            *           db.m_Height = 0.18f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           localRotation = Quaternion.Euler(7, 0, 0);
            *       
            *           db2
            *           db.m_Center = new Vector3(0f, -0.0350, 0.03f);
            *           db.m_Radius = 0.02f;
            *           db.m_Height = 0.06f;
            *           db.m_Direction = DynamicBoneCollider.Direction.X;
            *           
            * 6
            *           db1
            *           db.m_Center = new Vector3(0f, 0,f 0f);
            *           db.m_Radius = 0.015f;
            *           db.m_Height = 0.04f;
            *           db.m_Direction = DynamicBoneCollider.Direction.Z;
            *           db2
            *           disabled
                        
             * 
             * 
             * 
             * 
             */


            if (index < 2)
            {
                //First collider is a main collision shape that gets disabled when necessary.
                // Hands

                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.025f;
                capsule.height = 0.1f;
                capsule.center = new Vector3(0f, 0.01f, 0f);
                SetPhysMat(capsule.material);

                // A bit bigger copy-trigger.
                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.035f;
                capsule.height = 0.11f;
                capsule.center = new Vector3(0f, 0.01f, 0f);
                capsule.isTrigger = true;
            }
            else if (index == 2)
            {
                // Massager

                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.032f;
                capsule.height = 0.05f;
                capsule.center = new Vector3(0f, 0f, 0.115f);
                SetPhysMat(capsule.material);

                // A bit bigger copy-trigger.
                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.038f;
                capsule.height = 0.06f;
                capsule.center = new Vector3(0f, 0f, 0.115f);
                capsule.isTrigger = true;

                // Extra capsule for handle
                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.025f;
                capsule.height = 0.1f;
                SetPhysMat(capsule.material);

            }
            else if (index == 3)
            {
                // Vibrator

                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.02f;
                capsule.height = 0.28f;
                capsule.center = new Vector3(0f, 0f, 0.1f);
                SetPhysMat(capsule.material);

                // A bit bigger copy-trigger.
                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.025f;
                capsule.height = 0.32f;
                capsule.center = new Vector3(0f, 0f, 0.1f);
                capsule.isTrigger = true;
            }
            else if (index == 4)
            {
                // Dildo
                //_offset.localPosition = new Vector3(0f, -0.01f, 0.1f);
                //_offset.localRotation = Quaternion.Euler(5f, 0f, 0f);
                //if (!_offset.TryGetComponent<CapsuleCollider>(out var childCollider))
                //{
                //    childCollider = _offset.gameObject.AddComponent<CapsuleCollider>();
                //}
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.02f;
                capsule.height = 0.2f;
                capsule.center = new Vector3(0f, -0.01f, 0.1f);
                SetPhysMat(capsule.material);

                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.024f;
                capsule.height = 0.22f;
                capsule.center = new Vector3(0f, -0.01f, 0.1f);
                capsule.isTrigger = true;
            }
            else if (index == 5)
            {
                // Rotor

                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.015f;
                capsule.height = 0.04f;
                SetPhysMat(capsule.material);

                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = 0.02f;
                capsule.height = 0.06f;
                capsule.isTrigger = true;
            }
        }
        private void UpdateDynamicBoneColliders()
        {
            var infos = _activeItem.animParam.dbcInfo[Array.IndexOf(_activeItem.animParam.layers, _activeItem.layer)];
            for (var i = 0; i < 2; i++)
            {
                var info = infos[i];
                var db = (i == 0 ? transform : _offset).GetComponent<DynamicBoneCollider>();
                if (info != null)
                {
                    db.enabled = true;
                    db.m_Center = info.center;
                    db.m_Radius = info.radius;
                    db.m_Height = info.height;
                    db.m_Direction = (DynamicBoneCollider.Direction)info.direction;
                    if (i == 1)
                    {
                        _offset.localRotation = info.localRot;
                    }
                }
                else
                {
                    db.m_Radius = 0f;
                    db.m_Height = 0f;
                    db.enabled = false;
                }
            }
        }

        //public static void SetHandColor(ChaControl chara)
        //{
        //    // Different something (material, shader?) so the colors wont match from the get go.
        //    var color = chara.fileBody.skinMainColor;
        //    for (var i = 0; i < 4; i++)
        //    {
        //        aibuItemList[i].SetHandColor(color);
        //    }
        //}
        private void ActivateItem()
        {
            _activeOffsetPos = _activeItem.animParam.positionOffset;
            _activeOffsetRot = _activeItem.animParam.rotationOffset;
            _anchor.SetPositionAndRotation(_controller.TransformPoint(_activeOffsetPos), _controller.rotation);
            _activeItem.rootPoint.localScale = Fixes.Util.Divide(Vector3.Scale(Vector3.one, _activeItem.rootPoint.localScale), _activeItem.rootPoint.lossyScale);
            _activeItem.rootPoint.gameObject.SetActive(true);
            SetStartLayer();
            SetColliders(_activeItem.animParam.index);
            // Assign this one on basis of player's character scale?
            // No clue where ChaFile hides the height.
        }
        private void DeactivateItem()
        {
            _activeItem.rootPoint.gameObject.SetActive(false);
            //_activeItem.rootPoint.SetParent(VR.Manager.transform, false);
            StopSE();
        }
        public void SetRotation(float x, float y, float z)
        {
            _activeOffsetRot = Quaternion.Euler(x, y, z);
        }
        private void LateUpdate()
        {
            if (_itemLag != null)
            {
                _itemLag.SetPositionAndRotation(_controller.TransformPoint(_activeOffsetPos), _controller.rotation);
            }
            if (_activeItem != null)
            {
                _activeItem.rootPoint.rotation = _anchor.rotation * _activeOffsetRot * Quaternion.Inverse(_activeItem.movingPoint.rotation) * _activeItem.rootPoint.rotation;
                _activeItem.rootPoint.position += _anchor.position - _activeItem.movingPoint.position;
                //_activeItem.rootPoint.SetPositionAndRotation(
                //    _activeItem.rootPoint.position + (_anchor.position - _activeItem.movingPoint.position),
                //    _anchor.rotation * _activeItem.rotationOffset * Quaternion.Inverse(_activeItem.movingPoint.rotation) * _activeItem.rootPoint.rotation
                //    );
            }
        }

        /*
         * Material - static + dynamic friction = 1f
         * 
         * 
         * Hand - child (0,0.01f,0), Capsule - dir = 2, height - 0.1, radius = 0.025
         * 
         * Massager -  parent - Capsule - dir = 2, height = 0.15f, radius = 0.025f
         *             child - (0,0,0.115f), Capsule - dir = 2, height = 0.05f, radius = 0.032f
         * 
         * vibe -  child - (0,0,0.1f), Capsule - dir = 2, height = 0.3, radius = 0.025
         */


        private void FixedUpdate()
        {
            if (_itemLag == null)
            {
                // Debug.
                //_anchor.SetPositionAndRotation(_controller.TransformPoint(_activeItem.positionOffset), _controller.rotation);

                _rigidBody.MoveRotation(_controller.rotation);
                _rigidBody.MovePosition(_controller.TransformPoint(_activeOffsetPos));
            }
        }

        // Due to scarcity of hotkeys, we'll go with increase only.
        internal void ChangeItem()
        {
            DeactivateItem();

            _activeItem = _itemList[(_itemList.IndexOf(_activeItem) + 1) % _itemList.Count];
            ActivateItem();
        }
        private void PlaySE()
        {
            var aibuItem = _activeItem.aibuItem;
            if (aibuItem.pathSEAsset.IsNullOrEmpty()) return;

            if (aibuItem.transformSound == null)
            {
                var se = new Utils.Sound.Setting
#if KK
                {
                    type = Manager.Sound.Type.GameSE3D,
                    assetBundleName = aibuItem.pathSEAsset,
                    assetName = aibuItem.nameSEFile,
                };
#else
                ();
                se.loader.type = Manager.Sound.Type.GameSE3D;
                se.loader.bundle = aibuItem.pathSEAsset;
                se.loader.asset = aibuItem.nameSEFile;
#endif
                aibuItem.transformSound = Utils.Sound.Play(se).transform;
                aibuItem.transformSound.GetComponent<AudioSource>().loop = true;
                aibuItem.transformSound.SetParent(_activeItem.movingPoint, false);
            }
            else
            {
                aibuItem.transformSound.GetComponent<AudioSource>().Play();
            }
        }
        private void StopSE()
        {
            if (_activeItem.aibuItem.transformSound != null)
            {
                _activeItem.aibuItem.transformSound.GetComponent<AudioSource>().Stop();
            }
        }
        public void SetStartLayer()
        {
            _activeItem.aibuItem.anm.SetLayerWeight(_activeItem.layer, 0f);
            _activeItem.aibuItem.anm.SetLayerWeight(_activeItem.animParam.startLayer, 1f);
            _activeItem.layer = _activeItem.animParam.startLayer;
            UpdateDynamicBoneColliders();
        }
        public void ChangeLayer(bool increase, bool skipTransition = false)
        {
            //TestLayer(increase, skipTransition);

            if (_activeItem.animParam.layers == null) return;
            StopSE();
            var anm = _activeItem.aibuItem.anm;
            var oldLayer = _activeItem.layer;

            var oldIndex = Array.IndexOf(_activeItem.animParam.layers, oldLayer);
            var newIndex = increase ? (oldIndex + 1) % _activeItem.animParam.layers.Length : oldIndex <= 0 ? _activeItem.animParam.layers.Length - 1 : oldIndex - 1;
            //VRPlugin.Logger.LogDebug($"oldIndex:{oldIndex}:newIndex:{newIndex}");
            var newLayer = _activeItem.animParam.layers[newIndex];

            if (skipTransition)
            {
                anm.SetLayerWeight(newLayer, 1f);
                anm.SetLayerWeight(oldLayer, 0f);
                _activeItem.layer = newLayer;
                UpdateDynamicBoneColliders();
            }
            else
            {
                StartCoroutine(ChangeLayerCo(anm, oldLayer, newLayer));
            }

            if (newLayer != 0 && _activeItem.aibuItem.pathSEAsset != null)
            {
                PlaySE();
            }
        }

        private IEnumerator ChangeLayerCo(Animator anm, int oldLayer, int newLayer)
        {
            var timer = 0f;
            var stop = false;
            while (!stop)
            {
                timer += Time.deltaTime * 2f;
                if (timer > 1f)
                {
                    timer = 1f;
                    stop = true;
                }
                anm.SetLayerWeight(newLayer, timer);
                anm.SetLayerWeight(oldLayer, 1f - timer);
                yield return null;
            }
            _activeItem.layer = newLayer;
            UpdateDynamicBoneColliders();
        }
        /// <summary>
        /// Sets current item to an empty one and returns it's anchor.
        /// </summary>
        internal Transform GetEmptyAnchor()
        {
            DeactivateItem();
            _activeItem = _itemList[_itemList.Count - 1];


            // TEST TEST TEST
            //_rigidBody.isKinematic = true;


            ActivateItem();
            // Util.CreatePrimitive(PrimitiveType.Sphere, new Vector3(0.05f, 0.05f, 0.05f), _anchor, Color.cyan, 0.5f);
            return _anchor;
        }



        internal Transform OnGraspHold()
        {
            // We adjust position after release of rigidBody, as it most likely had some velocity on it.
            // Can't arbitrary move controller with this SteamVR version, kinda given tbh.
            if (_parent)
            {
                _parent = false;
                AddLag(20);
            }
            else
            {
                Shackle(20);
            }
            return _anchor;
        }
        internal void Shackle(int amount)
        {
            // We compensate release of rigidBody's velocity by teleporting controller (target point of rigidBody).
            var pos = _anchor.position;
            _rigidBody.isKinematic = true;
            _anchor.position = pos;
            _activeOffsetPos = _controller.InverseTransformPoint(_anchor.position);
            AddLag(amount);
        }
        internal void OnGraspRelease()
        {
            foreach (var inst in _instances)
            {
                if (inst.FindChild())
                {
                    inst.OnBecomingParent();
                    if (inst == this) return;
                }
            }
            Unshackle();
        }
        internal void Unshackle()
        {
            RemoveLag();
            _rigidBody.isKinematic = false;
            _activeOffsetPos = _activeItem.animParam.positionOffset;
        }
        internal void AddLag(int number)
        {
            _itemLag = new ItemLag(_anchor, KoikatuInterpreter.ScaleWithFps(number));
        }
        internal void RemoveLag()
        {
            _itemLag = null;
        }
        private void OnBecomingParent()
        {
            _parent = true;
            AddLag(10);
            _rigidBody.isKinematic = true;
        }
        private bool FindChild()
        {
            foreach (Transform child in _anchor.transform)
            {
                if (child.name.StartsWith("ik_", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private readonly List<string> _colliderParentListStartsWith =
            [
            "cf_j_middle02_",
            "cf_j_index02_",
            "cf_j_ring02_",
            "cf_j_thumb02_",
            "cf_s_hand_",
        ];
        private readonly List<string> _colliderParentListEndsWith =
            [
            "_head_00",
            "J_vibe_02",
            "J_vibe_05",
        ];
        //private void AddDynamicBones()
        //{
        //    var gameObjectList = new List<GameObject>();
        //    for (var i = 0; i < _itemList.Count - 1; i++)
        //    {
        //        var transforms = _itemList[i].aibuItem.obj.GetComponentsInChildren<Transform>(includeInactive: true)
        //            .Where(t => _colliderParentListStartsWith.Any(c => t.name.StartsWith(c, StringComparison.Ordinal))
        //            || _colliderParentListEndsWith.Any(c => t.name.EndsWith(c, StringComparison.Ordinal)))
        //            .ToList();
        //        transforms?.ForEach(t => gameObjectList.Add(t.gameObject));
        //    }
        //    VRBoop.InitDB(gameObjectList);
        //}
        //public void UpdateSkinColor(ChaFileControl chaFile)
        //{
        //    var color = chaFile.custom.body.skinMainColor;
        //    foreach (var item in aibuItemList.Values)
        //    {
        //        item.SetHandColor(color);
        //    }
        //}

    }
}
