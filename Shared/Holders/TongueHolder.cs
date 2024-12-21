//using KK_VR.Features;
//using KK_VR.Fixes;
//using Studio;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using UnityEngine;
//using VRGIN.Controls;
//using VRGIN.Core;
//using Random = UnityEngine.Random;

//namespace KK_VR.Holders
//{
//    internal class TongueHolder : Holder
//    {
//        private void SetItems(GameObject gameObject)
//        {
//            _anchor = gameObject.transform;
//            //_anchor.SetParent(VR.Manager.transform, false);
//            _rigidBody = _anchor.gameObject.AddComponent<Rigidbody>();
//            _rigidBody.useGravity = false;
//            _rigidBody.freezeRotation = true;
//            //_audioSource = _anchor.gameObject.AddComponent<AudioSource>();


//            //_activeItem = new ItemType(
//            //    _loadedAssetsList[4], _defaultAnimParamList[4]
//            //    );
//            //SetColliders();
//            //AddDynamicBones();
//            ActivateItem();
//        }
//        private void ActivateItem()
//        {
//            _activeItem.rootPoint.SetParent(_anchor, false);
//            _activeItem.rootPoint.gameObject.SetActive(true);
//            _anchor.SetParent(VR.Camera.Head, false);
//            _anchor.localPosition = _activeItem.animParam.positionOffset;
//            _anchor.localScale = Util.Divide(Vector3.Scale(Vector3.one, _anchor.localScale), _anchor.lossyScale);
//            //_anchor.SetPositionAndRotation(VR.Camera.Head.TransformPoint(_activeItem.positionOffset), VR.Camera.Head.rotation);
//            //_activeItem.rootPoint.localScale = Util.Divide(Vector3.Scale(Vector3.one, _activeItem.rootPoint.localScale), _activeItem.rootPoint.lossyScale);
//            //SetStartLayer();
//        }
//        private void DeactivateItem()
//        {
//            _activeItem.rootPoint.gameObject.SetActive(false);
//            //_activeItem.rootPoint.SetParent(VR.Manager.transform, false);
//            //StopSE();
//        }

//        //private void AddDynamicBones()
//        //{
//        //    var gameObjectList = _activeItem.aibuItem.obj.GetComponentsInChildren<Transform>(includeInactive: true)
//        //        .Where(t => t.name.Equals("cf_j_tang_04", StringComparison.Ordinal))
//        //        .Select(t => t.gameObject);

//        //    VRBoop.InitDB(gameObjectList);
//        //}

//        private bool _lick;
//        private float _timestamp;
//        private int _newLayer;
//        private float _layerWeight;
//        private Animator _anm;
//        private bool _active;
//        private float _intensity;
//        private Quaternion _rotTarget = Quaternion.identity;
//        private Quaternion _rotOffset = Quaternion.identity;
//        /// <summary>
//        /// Can be in -1 to 1 range for retracted/extended states.
//        /// </summary>
//        private float _state;

//        private readonly Vector3 _posOffset = new(0f, 0f, 0.05f);

//        internal void StartLick()
//        {
//            _lick = true;
//            _timestamp = Time.time + 3f + 6f * Random.value;
//            _newLayer = _activeItem.animParam.layers[Random.Range(0, _activeItem.animParam.layers.Length)];
//        }
//        private void Enable()
//        {

//        }
//        //private void Lick()
//        //{
//        //    if (_state < 1f)
//        //    {
//        //        _state += Time.deltaTime;
//        //        _activeItem.positionOffset = _posOffset * _state;
//        //    }
//        //    else if (_intensity > 0f)
//        //    {
//        //        if (_rotOffset == Quaternion.identity)
//        //        {

//        //            _rotOffset = Quaternion.Inverse(_rotOffset) * Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(-30f, 30f), 0f);
//        //            _rotTarget = Quaternion.Inverse(_rotTarget) * 

//        //        }
//        //    }
//        //}

//        //private void DoLick()
//        //{
//        //    if (_active)
//        //    {
//        //        switch (_state)
//        //        {
//        //            case State.Disabled:
//        //        }

//        //    }


//        //    _anm.SetLayerWeight(_newLayer, _layerWeight);
//        //    _anm.SetLayerWeight(_activeItem.layer, timer);



//        //    if (_timestamp < Time.time)
//        //    {
//        //        _newLayer = _activeItem.availableLayers[Random.Range(0, _activeItem.availableLayers.Length)];
//        //        //_newLayer = _newLayer == newLayer ? 
//        //        //    _activeItem.availableLayers[(Array.IndexOf(_activeItem.availableLayers, _newLayer) + 1) % _activeItem.availableLayers.Length] 
//        //        //    : newLayer;
//        //        _timestamp = Time.time + 3f + 6f * Random.value;
//        //    }

//        //}



//        //public static void TestLayer(bool increase, bool skipTransition = false)
//        //{
//        //    var item = controllersDic[0][0];

//        //    var anm = item.aibuItem.anm;
//        //    var oldLayer = item.layer;
//        //    var oldIndex = Array.IndexOf(item.availableLayers, oldLayer);
//        //    var newIndex = increase ? (oldIndex + 1) % item.availableLayers.Length : oldIndex <= 0 ? item.availableLayers.Length - 1 : oldIndex - 1;
//        //    var newLayer = item.availableLayers[newIndex];

//        //    //var newRotationOffset = newLayer == 13 || newLayer == 15 ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;// Quaternion.Euler(-90f, 0f, 0f);

//        //    if (skipTransition)
//        //    {
//        //        anm.SetLayerWeight(newLayer, 1f);
//        //        anm.SetLayerWeight(oldLayer, 0f);
//        //        item.layer = newLayer;
//        //    }
//        //    else
//        //    {
//        //        KoikatuInterpreter.Instance.StartCoroutine(ChangeTongueCo(item, anm, oldLayer, newLayer));
//        //    }
//        //   //VRPlugin.Logger.LogDebug($"TestLayer:{newLayer}");

//        //}
//        //private static IEnumerator ChangeTongueCo(ItemType item, Animator anm, int oldLayer, int newLayer)
//        //{
//        //    var timer = 0f;
//        //    var stop = false;
//        //    //var initRotOffset = item.rotationOffset;
//        //    while (!stop)
//        //    {
//        //        timer += Time.deltaTime * 2f;
//        //        if (timer > 1f)
//        //        {
//        //            timer = 1f;
//        //            stop = true;
//        //        }
//        //        //item.rotationOffset = Quaternion.Lerp(initRotOffset, newRotationOffset, timer);
//        //        anm.SetLayerWeight(newLayer, timer);
//        //        anm.SetLayerWeight(oldLayer, 1f - timer);
//        //        yield return null;
//        //    }
//        //    item.layer = newLayer;
//        //}
//    }
//}
