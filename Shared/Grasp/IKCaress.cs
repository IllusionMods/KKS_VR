using KK_VR.Handlers;
using KK_VR.IK;
using KK_VR.Interpreters;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HandCtrl;
using static KK_VR.Grasp.GraspController;

namespace KK_VR.Grasp
{
    /// <summary>
    /// Component that applies extra IK offsets during manual caress
    /// </summary>
    internal class IKCaress : OffsetManipulator
    {
        private bool _end;
        private float _startDistance;
        private Transform _poi;
        private Transform _anchor;
        private Vector3 _lastPos;

        private Transform _item;
        private int _itemId;

        internal void Init(KK.RootMotion.FinalIK.FullBodyBipedIK ik, AibuColliderKind colliderKind, List<BodyPart> bodyPartList, ChaControl chara, Transform anchor)
        {
            base.OnInit(ik);
            _itemId = (int)colliderKind - 2;
            _anchor = anchor;
            _lastPos = anchor.position;
            _item = HSceneInterpreter.handCtrl.useAreaItems[_itemId].obj.transform;

            var slaves = new List<int>();
            foreach (var masterIndex in GetMasterIndex(colliderKind))
            {
                Add(bodyPartList[masterIndex], 0.3f);
                slaves.AddRange(GetSlaveIndex(masterIndex));
                
            }
            foreach (var slaveIndex in slaves.Distinct())
            {
                var result = false;
                foreach (var link in _linkList)
                {
                    if (link.effector == bodyPartList[slaveIndex].effector)
                    {
                        result = true;
                        break;
                    }
                }
                if (!result)
                {
                    Add(bodyPartList[slaveIndex], 0.15f);
                }
            }
            //foreach (var test in _linkDic)
            //{
            //   //VRPlugin.Logger.LogInfo($"RoughCaress:{test.Key.name} - {test.Value.defaultWeight}");
            //}
            _poi = chara.objBodyBone.transform.Find(GetPoiName(colliderKind));
            _startDistance = Vector3.SqrMagnitude(_poi.position - anchor.position);
        }


        internal void Move()
        {
            var vec = (Vector2)_item.InverseTransformVector(_lastPos - _anchor.position);
            vec.y = 0f - vec.y;
            HSceneInterpreter.hFlag.xy[_itemId] += vec * 10f;
            _lastPos = _anchor.position;
        }
        private float _lerp;
        private Vector2 _midVec = new(0.5f, 0.5f);


        internal void End()
        {
            _end = true;
        }
        internal void Update()
        {
            if (_end)
            {
                var step = Mathf.SmoothStep(1f, 0f, _lerp += Time.deltaTime);
                foreach (var link in _linkList)
                {
                    link.weight = Mathf.Clamp01(link.weight * step);
                }
                HSceneInterpreter.hFlag.xy[_itemId] = (HSceneInterpreter.hFlag.xy[_itemId] - _midVec) * step + _midVec;
                if (step == 0f)
                {
                    Component.Destroy(this);
                }
            }
            else
            {
                var diff = (Vector3.SqrMagnitude(_poi.position - _anchor.position) - _startDistance) * 10f;
                foreach (var link in _linkList)
                {
                    link.weight = Mathf.Clamp01(link.defaultWeight + diff);
                }
                Move();
            }
        }
        private int[] GetMasterIndex(AibuColliderKind colliderKind)
        {
            return colliderKind switch
            {
                AibuColliderKind.muneL => [1],
                AibuColliderKind.muneR => [2],
                AibuColliderKind.kokan or AibuColliderKind.anal => [3, 4],
                AibuColliderKind.siriL => [3],
                AibuColliderKind.siriR => [4],
                _ => null
            };
        }
        private int[] GetSlaveIndex(int masterIndex)
        {
            return masterIndex switch
            {
                1 => [0, 2],
                2 => [0, 1],
                3 => [0, 4],
                4 => [0, 3],
                _ =>  null
            };
        }

        private string GetPoiName(AibuColliderKind colliderKind)
        {
            return colliderKind switch
            {
                AibuColliderKind.muneL => "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_bust00/cf_s_bust00_L/cf_d_bust01_L" +
                    "/cf_j_bust01_L/cf_d_bust02_L/cf_j_bust02_L/cf_d_bust03_L/cf_j_bust03_L/cf_s_bust03_L/k_f_mune03L_02",
                AibuColliderKind.muneR => "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_bust00/cf_s_bust00_R/cf_d_bust01_R" +
                    "/cf_j_bust01_R/cf_d_bust02_R/cf_j_bust02_R/cf_d_bust03_R/cf_j_bust03_R/cf_s_bust03_R/k_f_mune03R_02",
                AibuColliderKind.kokan => "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_d_kokan/cf_j_kokan",
                AibuColliderKind.anal => "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_d_ana/cf_j_ana",
                AibuColliderKind.siriL => "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_d_siri_L/cf_d_siri01_L/cf_j_siri_L",
                AibuColliderKind.siriR => "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_d_siri_R/cf_d_siri01_R/cf_j_siri_R",
                _ => null
            };

        }
    }
}
