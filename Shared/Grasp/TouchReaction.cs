using KK_VR.IK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static KK_VR.Grasp.GraspController;
using KK.RootMotion.FinalIK;
using Random = UnityEngine.Random;
using KK_VR.Fixes;

namespace KK_VR.Grasp
{
    /// <summary>
    /// Component that implements custom HitReaction, with offsets only.
    /// </summary>
    internal class TouchReaction : MonoBehaviour
    {
        private class HitPoint
        {
            internal HitPoint(IKEffector effector, int _index, float _weight, bool useUp = true)
            {
                id = _index;
                _effector = effector;
                weight = _weight;
                _useUp = useUp;
            }
            internal readonly int id;
            private readonly IKEffector _effector;
            internal float weight;
            private bool _useUp;
            private AnimationCurve _curveForce;
            private AnimationCurve _curveUp;
            private Vector3 _vecForce;
            private Vector3 _vecUp;
            private Vector3 _offset;
            internal void Start(Vector3 vecForce, Vector3 vecUp, AnimationCurve curveForce, AnimationCurve curveUp)
            {
                _offset = Vector3.zero;
                _vecForce = vecForce;
                _vecUp = vecUp;
                _curveForce = curveForce;
                _curveUp = curveUp;
            }
            internal void Override(Vector3 vecForce, Vector3 vecUp, AnimationCurve curveForce, AnimationCurve curveUp)
            {
                curveForce.MoveKey(0, new Keyframe(0f, _offset.magnitude / (vecForce + _offset).magnitude));
                _vecForce = _offset + vecForce;
                _vecUp = vecUp;
                _curveForce = curveForce;
                _curveUp = curveUp;
            }
            internal void Move(float time)
            {
                if (weight == 0f) return;
                _offset = _curveForce.Evaluate(time) * _vecForce;
                if (_useUp)
                {
                    _offset += _curveUp.Evaluate(time) * _vecUp;
                }
                _effector.positionOffset += _offset * weight;
            }
        }
        private readonly Dictionary<List<HitPoint>, float[]> _currentReactions = [];
        private readonly List<List<HitPoint>> _reactionList = [];
        private IKEffector[] _effectors;
        private ChaControl chara;

        // Consequent reaction doesn't work well/look good, we'll keep to one at a time.
        internal bool IsBusy => _currentReactions.Count > 0;

        private void Awake()
        {
            chara = transform.GetComponentInParent<ChaControl>();
            var effectors = gameObject.GetComponent<FullBodyBipedIK>().solver.effectors;
            _effectors = effectors;
            _reactionList.AddRange(
                [
                    [// 0 Body
                    new HitPoint(effectors[0], 0, 0.15f),
                    new HitPoint(effectors[1], 1, 0.05f),
                    new HitPoint(effectors[2], 2, 0.05f),
                    new HitPoint(effectors[3], 3, 0f),
                    new HitPoint(effectors[4], 4, 0f),
                    new HitPoint(effectors[5], 5, -0.1f),
                    new HitPoint(effectors[6], 6, -0.1f),
                    new HitPoint(effectors[7], 7, 0.5f, false)
                    ],
                    [// 1 ShoulderL
                    new HitPoint(effectors[1], 1, 0.1f),
                    new HitPoint(effectors[0], 0, 0.05f),
                    new HitPoint(effectors[2], 2, -0.05f),
                    new HitPoint(effectors[3], 3, 0.05f),
                    new HitPoint(effectors[4], 4, -0.05f),
                    new HitPoint(effectors[5], 5, 0.05f)
                    ],
                    [ // 2 ShoulderR
                    new HitPoint(effectors[2], 2, 0.1f),
                    new HitPoint(effectors[0], 0, 0.05f),
                    new HitPoint(effectors[1], 1, -0.05f),
                    new HitPoint(effectors[3], 3, -0.05f),
                    new HitPoint(effectors[4], 4, 0.05f),
                    new HitPoint(effectors[6], 6, 0.05f)
                    ],
                    [ // 3 ThighL
                    new HitPoint(effectors[3], 3, 0.1f),
                    new HitPoint(effectors[0], 0, 0.05f),
                    new HitPoint(effectors[1], 1, 0f),
                    new HitPoint(effectors[2], 2, 0f),
                    new HitPoint(effectors[4], 4, 0.05f),
                    new HitPoint(effectors[7], 7, 0.1f)
                    ],
                    [ // 4 ThighR
                    new HitPoint(effectors[0], 0, 0.2f),
                    new HitPoint(effectors[3], 3, 0.05f),
                    new HitPoint(effectors[4], 4, 0.15f),
                    new HitPoint(effectors[8], 8, 0.1f)
                    ],
                    [ // 5 ArmL
                    new HitPoint(effectors[5], 5, 0.1f),
                    new HitPoint(effectors[0], 0, 0.05f),
                    new HitPoint(effectors[1], 1, 0.05f),
                    new HitPoint(effectors[2], 2, -0.05f),
                    new HitPoint(effectors[3], 3, 0.05f, false),
                    new HitPoint(effectors[4], 4, -0.05f, false)
                    ],
                    [// 6 ArmR
                    new HitPoint(effectors[6], 6, 0.1f),
                    new HitPoint(effectors[0], 0, 0.05f),
                    new HitPoint(effectors[1], 1, -0.05f),
                    new HitPoint(effectors[2], 2, 0.05f),
                    new HitPoint(effectors[3], 3, -0.05f, false),
                    new HitPoint(effectors[4], 4, 0.05f, false)
                    ],
                    [// 7 FootL
                    new HitPoint(effectors[1], 1, -0.05f),
                    new HitPoint(effectors[2], 2, -0.05f),
                    new HitPoint(effectors[3], 3, 0.05f),
                    new HitPoint(effectors[4], 4, 0.05f),
                    new HitPoint(effectors[0], 0, 0.1f),
                    ],
                    [// 8 FootR
                    new HitPoint(effectors[1], 7, -0.05f),
                    new HitPoint(effectors[2], 8, -0.05f),
                    new HitPoint(effectors[3], 3, 0.05f),
                    new HitPoint(effectors[4], 4, 0.05f),
                    new HitPoint(effectors[0], 0, 0.1f),
                    ],
                    [// 9 UpperBody
                    //new HitPoint(effectors[0], 0, 0.05f),
                    //new HitPoint(effectors[1], 1, 0.1f),
                    //new HitPoint(effectors[2], 2, 0.1f),

                    new HitPoint(effectors[0], 0, 0.05f),
                    new HitPoint(effectors[1], 1, 0.15f),
                    new HitPoint(effectors[2], 2, 0.15f),
                    new HitPoint(effectors[3], 3, 0.1f, false),
                    new HitPoint(effectors[4], 4, 0.1f, false),
                    new HitPoint(effectors[5], 5, 0.05f ),
                    new HitPoint(effectors[6], 6, 0f ),
                    new HitPoint(effectors[7], 7, 0.4f, false),
                    new HitPoint(effectors[8], 8, 0f, false)
                    ],
                    // Delayed reaction via AnimCurve ?

                    [// 10 LowerBody  // First pass done
                    new HitPoint(effectors[3], 3, 0.05f, false),
                    new HitPoint(effectors[4], 4, 0f, false),
                    new HitPoint(effectors[0], 0, 0.1f, false),
                    new HitPoint(effectors[1], 1, 0.05f, false),
                    new HitPoint(effectors[2], 2, -0.05f),
                    new HitPoint(effectors[5], 5, 0.05f),
                    new HitPoint(effectors[6], 6, 0.05f),
                    new HitPoint(effectors[7], 7, 0.05f, false),
                    new HitPoint(effectors[8], 8, 0f),
                    ],
                    [// 11 LegL
                    new HitPoint(effectors[0], 0, 0.05f),
                    new HitPoint(effectors[3], 3, 0.1f),
                    new HitPoint(effectors[7], 7, 0.2f)
                    ],
                    [// 12 LegR
                    new HitPoint(effectors[0], 0, 0.05f),
                    new HitPoint(effectors[3], 3, 0.1f),
                    new HitPoint(effectors[8], 8, 0.2f)
                    ],
                ]);
        }

        private Vector3 GetUpVec(int zeroIndexMasterId)
        {
            return zeroIndexMasterId switch
            {
                //0 => _effectors[0].bone.up,
                //1 or 2 => _effectors[0].bone.transform.rotation * Vector3.down,
                //3 or 4 => _effectors[0].bone.up,
                5 or 6 or 7 or 8 => (_effectors[zeroIndexMasterId - 4].bone.position - _effectors[zeroIndexMasterId].bone.position).normalized,
                _ => _effectors[0].bone.up
            };
        }
        private void HelpUpperBody(List<HitPoint> hitPoints)
        {

        }
        //private Vector3 GetRootUpVec(int zeroIndexMasterId)
        //{
        //    if (zeroIndexMasterId < 3)
        //    {
        //        // Shoulders
        //        return ((_effectors[3].bone.position + _effectors[4].bone.position) * 0.5f) - _effectors[0].bone.position;
        //    }
        //    else
        //    {
        //        // Thighs
        //        return ((_effectors[1].bone.position + _effectors[2].bone.position) * 0.5f) - _effectors[0].bone.position;
        //    }
        //}
        internal void React(int id, Vector3 direction)
        {
            // Direction doesn't have Y axis.
           //VRPlugin.Logger.LogInfo($"TouchReaction:{id}:{direction}");
            var list = _reactionList[id];
            var duration = 0f;
            var upVec = GetUpVec(list[0].id);
            direction = HelpVector(list[0].id, direction, upVec);
            if (id == 9)
            {
                // Perhaps there is an easier way to find signed angle in this situation, can't think of it tho.
#if KK
                var left = Util.SignedAngle(direction, _effectors[0].bone.forward, _effectors[0].bone.up) > 0f;
#else
                var left = Vector3.SignedAngle(direction, _effectors[0].bone.forward, _effectors[0].bone.up) > 0f;
#endif
                //var left = Mathf.DeltaAngle(0f, (Quaternion.Inverse(Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f, 0f)) * _effectors[0].bone.rotation).eulerAngles.y) < 0f;
               //VRPlugin.Logger.LogInfo($"Left = {left}");

                list[5].weight = left ? 0f : 0.05f;
                list[6].weight = left ? 0.05f : 0f;
                list[7].weight = left ? 0f : Random.Range(0.3f, 0.5f);
                list[8].weight = left ? Random.Range(0.3f, 0.5f) : 0f;

                
            }
            if (!_currentReactions.ContainsKey(list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Start(direction, upVec, GetForceCurve(out var forceDuration), GetUpCurve(forceDuration));
                    duration = Mathf.Max(duration, forceDuration);
                }
                _currentReactions.Add(_reactionList[id], [0f, duration]);
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Override(direction, upVec, GetForceCurve(out var forceDuration), GetUpCurve(forceDuration));
                    duration = Mathf.Max(duration, forceDuration);
                }
                _currentReactions[list][0] = 0f;
                _currentReactions[list][1] = duration;
            }
        }

        // Zero index is a master id from which reaction got triggered.
        private Vector3 HelpVector(int index, Vector3 direction, Vector3 upVec)
        {
            if (index == 5 || index == 6)
            {
                var hand = _effectors[index].bone;
                var shoulder = _effectors[index - 4].bone;
                if ((index == 5 && hand.InverseTransformPoint(shoulder.position).y > 0.2f) || (index == 6 && hand.InverseTransformPoint(shoulder.position).y < -0.2))
                {
                    return Quaternion.Euler(0f, 180f, 0f) * direction * 0.5f + upVec * Random.Range(0.6f, 1f);
                }
                else
                {
                    var vec = _effectors[0].bone.position - _effectors[index].bone.position;
                    vec.y = 0f;
                    if (Vector3.Angle(vec, direction) < 45f)
                    {
                        return Quaternion.Euler(0f, Random.Range(45f, 90f) * (Random.value > 0.5f ? 1 : -1), 0f) * direction;
                    }
                }
            }
            return direction;
        }

        private void LateUpdate()
        {
            if (_currentReactions.Count > 0)
            {
                foreach (var reaction in _currentReactions)
                {
                    if (reaction.Value[0] > reaction.Value[1])
                    {
                        _currentReactions.Remove(reaction.Key);
                        if (_currentReactions.Count == 0)
                        {
                            GraspHelper.Instance.OnTouchReactionStop(chara);
                        }
                        return;
                    }
                    else
                    {
                        reaction.Value[0] += Time.deltaTime;
                        foreach (var hitPoint in reaction.Key)
                        {
                            hitPoint.Move(reaction.Value[0]);
                        }
                    }
                }
            }
        }
        private AnimationCurve GetForceCurve(out float duration)
        {
            //return partName switch
            //{
            //PartName.Spine => 
            return new AnimationCurve(
               new Keyframe(0f, 0F),
               new Keyframe(Random.Range(0.25f, 0.75f), Random.Range(0.75f, 1f)),
               new Keyframe(Random.Range(1.5f, 2f), Random.Range(0.75f, 1f)),
               new Keyframe(duration = Random.Range(2.25f, 3f), 0f));
            //};
        }
        private AnimationCurve GetUpCurve(float duration)
        {
            //return partName switch
            //{
            //PartName.Spine =>
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(Random.Range(0.25f, 1.5f), Random.Range(0.5f, 1f)),
                new Keyframe(Random.Range(1.5f, 2f), Random.Range(0.25f, 0.75f)),
                new Keyframe(duration, 0f));

            //};
        }
    }
}
