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
        private static readonly List<HandHolder> _instances = [];
        private readonly List<ItemType> _itemList = [];
        private Transform _controller;
        private ItemLag _itemLag;
        private bool _parent;
        internal bool IsParent => _parent;
        private HandNoise _handNoise;
        internal HandNoise Noise => _handNoise;
        internal Controller Controller { get; private set; }
        internal int Index { get; private set; }
        internal ItemHandler Handler => _handler;
        internal GraspController Grasp { get; private set; }
        internal GameplayTool Tool { get; private set; }

        /// <summary>
        /// Clutch for unruly colliders in roaming.
        /// </summary>
        internal static void SetKinematic(bool state)
        {
            foreach (var inst in _instances)
            {
                inst._rigidBody.isKinematic = state;
            }
        }
        internal static List<HandHolder> GetHands => _instances;
        private readonly int[] _itemIDs = [0, 2, 5, 7, 9, 11];
        internal static HandHolder GetHand(int index) => _instances[index];
        internal void Init(int index)
        {
            _instances.Add(this);
            Index = index;
            Controller = index == 0 ? VR.Mode.Left : VR.Mode.Right;
            _controller = Controller.transform;
            Tool = Controller.GetComponent<GameplayTool>();
            if (_loadedAssetsList.Count == 0)
            {
                LoadAssets();
                HandNoise.Init();
            }
            SetItems(index);
            Grasp = new GraspController(this);
            _handNoise = new HandNoise(gameObject.AddComponent<AudioSource>());
        }

        internal static void OnBecomingBusy()
        {
            foreach (var inst in _instances)
            {
                inst.Tool.HideLaser();
            }
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
        private void AddHandler<T>() 
            where T : ItemHandler
        {
            _handler = gameObject.AddComponent<T>();
            _handler.Init(this);
        }
        private void RemoveHandler()
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

            foreach (var collider in gameObject.GetComponents<Collider>())
            {
                UnityEngine.Component.Destroy(collider);
            }
            foreach (var collider in _offset.GetComponents<Collider>())
            {
                UnityEngine.Component.Destroy(collider);
            }
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

        private void FixedUpdate()
        {
            if (_itemLag == null)
            {
                _rigidBody.MoveRotation(_controller.rotation);
                _rigidBody.MovePosition(_controller.TransformPoint(_activeOffsetPos));
            }
        }

        // Due to scarcity of hotkeys, we'll go with increase only.
        /// <summary>
        /// Scroll hand item.
        /// </summary>
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


        internal void OnLimbSyncStart()
        {
            DeactivateItem();
            AddLag(10);
        }

        internal void OnLimbSyncStop()
        {
            ActivateItem();
            RemoveLag();
        }


        internal void OnGraspHold()
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
        }
        internal void Shackle(int amount)
        {
            // We compensate the release of rigidBody's velocity by applying offset (target point of rigidBody).
            var pos = _anchor.position;
            _rigidBody.isKinematic = true;
            _anchor.position = pos;
            _activeOffsetPos = _controller.InverseTransformPoint(_anchor.position);
            AddLag(amount);
        }
        internal void OnGraspRelease()
        {
            if (!_parent)
            {
                Unshackle();
            }
            else
            {
                // Next GraspRelease will unparent limb from controller.
                _parent = false;
            }
        }
        internal void Unshackle()
        {
            RemoveLag();
            _rigidBody.isKinematic = false;
            _activeOffsetPos = _activeItem.animParam.positionOffset;
        }
        internal void AddLag(int numberOfFrames)
        {
            _itemLag = new ItemLag(_anchor, KoikatuInterpreter.ScaleWithFps(numberOfFrames));
        }
        internal void RemoveLag()
        {
            _itemLag = null;
        }
        internal void OnBecomingParent()
        {
            _parent = true;
            AddLag(10);
            _rigidBody.isKinematic = true;
        }

        //private readonly List<string> _colliderParentListStartsWith =
        //    [
        //    "cf_j_middle02_",
        //    "cf_j_index02_",
        //    "cf_j_ring02_",
        //    "cf_j_thumb02_",
        //    "cf_s_hand_",
        //];
        //private readonly List<string> _colliderParentListEndsWith =
        //    [
        //    "_head_00",
        //    "J_vibe_02",
        //    "J_vibe_05",
        //];
    }
}
