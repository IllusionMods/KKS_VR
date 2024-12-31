using KK_VR.Interpreters;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Core;
using Random = UnityEngine.Random;

namespace KK_VR.Camera
{
    /// <summary>
    /// Provides movement to a predefined poi of the provided character.
    /// </summary>
    internal class MoveToPoi
    {
        private struct PoIPatternInfo
        {
            internal string teleportTo;
            internal List<string> lookAt;
            internal float forwardMin;
            internal float forwardMax;
            internal float upMin;
            internal float upMax;
            internal float rightMin;
            internal float rightMax;
        }
        internal MoveToPoi(ChaControl chara, Action onFinish)
        {
            var dicValue = _poiDic.ElementAt(Random.Range(0, _poiDic.Count)).Value;

            var transforms = chara.transform.GetComponentsInChildren<Transform>(includeInactive: true);
            _teleportTo = transforms
                .Where(t => t.name.Equals(dicValue.teleportTo))
                .FirstOrDefault();

            if (_teleportTo == null)
            {
                throw new NullReferenceException($"{GetType().Name}:Bad dic, can't find the target.");
            }

            _onFinish = onFinish;
            var lookAtName = dicValue.lookAt[Random.Range(0, dicValue.lookAt.Count)];
            _lookAt = transforms
                .Where(t => t.name.Equals(lookAtName))
                .FirstOrDefault();

            _offset = new Vector3(
                Random.Range(dicValue.rightMin, dicValue.rightMax),
                Random.Range(dicValue.upMin, dicValue.upMax),
                Random.Range(dicValue.forwardMin, dicValue.forwardMax));

            _startRotation = VR.Camera.Origin.rotation;
            _startPosition = VR.Camera.Head.position;
            var offsetPos = _teleportTo.TransformPoint(_offset);
            _targetRotation = Quaternion.LookRotation(_lookAt.position - offsetPos);

            _lerpMultiplier = Mathf.Min(
                KoikatuInterpreter.Settings.FlightSpeed / Vector3.Distance(offsetPos, _startPosition),
                KoikatuInterpreter.Settings.FlightSpeed * 60f / Quaternion.Angle(_startRotation, _targetRotation));

        }
        private readonly Quaternion _startRotation;
        private readonly Quaternion _targetRotation;
        private readonly Vector3 _startPosition;

        private readonly Transform _teleportTo;
        private readonly Transform _lookAt;
        private readonly Vector3 _offset;
        private readonly float _lerpMultiplier;

        private readonly Action _onFinish;

        private float _lerp;

        internal void Move()
        {
            var step = Mathf.SmoothStep(0f, 1f, _lerp += Time.deltaTime * _lerpMultiplier);
            var offsetPos = _teleportTo.TransformPoint(_offset);
            var pos = Vector3.Slerp(_startPosition, offsetPos, step);
            VR.Camera.Origin.rotation = Quaternion.Slerp(_startRotation, _targetRotation, step);
            VR.Camera.Origin.position += pos - VR.Camera.Head.position;
            if (step == 1f)
            {
                _onFinish?.Invoke();
            }
        }

        //private readonly Dictionary<string, PoIPatternInfo> poiDicDev = new()
        //{

        //    {
        //        "NavelUpFront",  // Upfront
        //        new PoIPatternInfo {
        //            teleportTo = "cf_j_spine01",
        //            lookAt = [
        //                "cf_j_spine03",
        //                "cf_j_spine01",
        //                "cf_j_spine02"
        //            ],
        //            forwardMin = 0.05f,
        //            forwardMax = 0.15f,
        //            upMin = -0.1f,
        //            upMax = 0.1f,
        //            rightMin = -0.1f,
        //            rightMax = 0.1f
        //        }
        //    },
        //    {
        //        "NavelLeftSide",
        //        new PoIPatternInfo {
        //            teleportTo = "cf_j_spine01",
        //            lookAt = [
        //                "cf_j_spine03",
        //                "cf_j_spine01",
        //                "cf_j_spine02"
        //            ],
        //            forwardMin = -0.1f,
        //            forwardMax = 0.1f,
        //            upMin = -0.1f,
        //            upMax = 0.1f,
        //            rightMin = -0.15f,
        //            rightMax = -0.25f
        //        }
        //    },
        //    {
        //        "NavelRightSide",
        //        new PoIPatternInfo {
        //            teleportTo = "cf_j_spine01",
        //            lookAt = [
        //                "cf_j_spine03",
        //                "cf_j_spine01",
        //                "cf_j_spine02"
        //            ],
        //            forwardMin = -0.1f,
        //            forwardMax = 0.1f,
        //            upMin = -0.1f,
        //            upMax = 0.1f,
        //            rightMin = 0.15f,
        //            rightMax = 0.25f
        //        }
        //    }
        //};
        private readonly Dictionary<string, PoIPatternInfo> _poiDic = new()
        {
            {
                "FaceUpFront",  // Upfront
                new PoIPatternInfo {
                    teleportTo = "cf_J_FaceUp_tz",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_neck"
                    },
                    forwardMin = 0.15f,
                    forwardMax = 0.3f,
                    upMin = -0.05f,
                    upMax = 0.05f,
                    rightMin = -0.2f,
                    rightMax = 0.2f
                }
            },
            {
                "FaceLeftSide",
                new PoIPatternInfo {
                    teleportTo = "cf_J_FaceUp_tz",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_neck"
                    },
                    forwardMin = 0.1f,
                    forwardMax = 0.2f,
                    upMin = -0.05f,
                    upMax = 0.05f,
                    rightMin = -0.15f,
                    rightMax = -0.3f
                }
            },
            {
                "FaceRightSide",  // Right
                new PoIPatternInfo {
                    teleportTo = "cf_J_FaceUp_tz",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_neck"
                    },
                    forwardMin = 0.1f,
                    forwardMax = 0.2f,
                    upMin = -0.05f,
                    upMax = 0.05f,
                    rightMin = 0.15f,
                    rightMax = 0.3f
                }
            },
            {
                "NeckUpFront",  // Upfront
                new PoIPatternInfo {
                    teleportTo = "cf_j_neck",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_spine03"
                    },
                    forwardMin = 0.2f,
                    forwardMax = 0.3f,
                    upMin = -0.05f,
                    upMax = 0.05f,
                    rightMin = -0.2f,
                    rightMax = 0.2f
                }
            },
            {
                "NeckLeftSide",
                new PoIPatternInfo {
                    teleportTo = "cf_j_neck",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_spine03"
                    },
                    forwardMin = 0.1f,
                    forwardMax = 0.2f,
                    upMin = -0.05f,
                    upMax = 0.05f,
                    rightMin = -0.15f,
                    rightMax = -0.25f
                }
            },
            {
                "NeckRightSide",
                new PoIPatternInfo {
                    teleportTo = "cf_j_neck",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_spine03"
                    },
                    forwardMin = 0.1f,
                    forwardMax = 0.2f,
                    upMin = -0.05f,
                    upMax = 0.05f,
                    rightMin = 0.15f,
                    rightMax = 0.25f
                }
            },
            {
                "BreastUpFront",  // Upfront
                new PoIPatternInfo {
                    teleportTo = "cf_j_spine03",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_neck",
                        "cf_j_spine03"
                    },
                    forwardMin = 0.05f,
                    forwardMax = 0.15f,
                    upMin = -0.1f,
                    upMax = 0.1f,
                    rightMin = -0.1f,
                    rightMax = 0.1f
                }
            },
            {
                "BreastLeftSide",
                new PoIPatternInfo {
                    teleportTo = "cf_j_spine03",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_neck",
                        "cf_j_spine03"
                    },
                    forwardMin = -0.1f,
                    forwardMax = 0.1f,
                    upMin = -0.1f,
                    upMax = 0.1f,
                    rightMin = -0.15f,
                    rightMax = -0.25f
                }
            },
            {
                "BreastRightSide",
                new PoIPatternInfo {
                    teleportTo = "cf_j_spine03",
                    lookAt = new List<string> {
                        "cf_J_FaceUp_tz",
                        "cf_j_neck",
                        "cf_j_spine03"
                    },
                    forwardMin = -0.1f,
                    forwardMax = 0.1f,
                    upMin = -0.1f,
                    upMax = 0.1f,
                    rightMin = 0.15f,
                    rightMax = 0.25f
                }
            },
            {
                "NavelUpFront",  // Upfront
                new PoIPatternInfo {
                    teleportTo = "cf_j_spine01",
                    lookAt = new List<string> {
                        "cf_j_spine03",
                        "cf_j_spine01",
                        "cf_j_spine02"
                    },
                    forwardMin = 0.05f,
                    forwardMax = 0.15f,
                    upMin = -0.1f,
                    upMax = 0.1f,
                    rightMin = -0.1f,
                    rightMax = 0.1f
                }
            },
            {
                "NavelLeftSide",
                new PoIPatternInfo {
                    teleportTo = "cf_j_spine01",
                    lookAt = new List<string> {
                        "cf_j_spine03",
                        "cf_j_spine01",
                        "cf_j_spine02"
                    },
                    forwardMin = -0.1f,
                    forwardMax = 0.1f,
                    upMin = -0.1f,
                    upMax = 0.1f,
                    rightMin = -0.15f,
                    rightMax = -0.25f
                }
            },
            {
                "NavelRightSide",
                new PoIPatternInfo {
                    teleportTo = "cf_j_spine01",
                    lookAt = new List<string> {
                        "cf_j_spine03",
                        "cf_j_spine01",
                        "cf_j_spine02"
                    },
                    forwardMin = -0.1f,
                    forwardMax = 0.1f,
                    upMin = -0.1f,
                    upMax = 0.1f,
                    rightMin = 0.15f,
                    rightMax = 0.25f
                }
            }
        };
    }
}
