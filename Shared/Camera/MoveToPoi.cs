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
    /// Provides movement to predefined pov of provided charachter.
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
        internal MoveToPoi(ChaControl chara)
        {
            var dicValue = _poiDic.ElementAt(Random.Range(0, _poiDic.Count)).Value;
            _teleportTo = chara.transform.GetComponentsInChildren<Transform>()
                .Where(t => t.name.Equals(dicValue.teleportTo))
                .FirstOrDefault();
            if (_teleportTo == null)
            {
                VRPlugin.Logger.LogWarning($"MoveTo:Init - Bad dic, can't find target.");
                throw new NullReferenceException();
            }
            _lookAt = chara.transform.GetComponentsInChildren<Transform>()
                .Where(t => t.name.Equals(dicValue.lookAt[Random.Range(0, dicValue.lookAt.Count)]))
                .FirstOrDefault();
            _offset = new Vector3(
                Random.Range(dicValue.rightMin, dicValue.rightMax),
                Random.Range(dicValue.upMin, dicValue.upMax),
                Random.Range(dicValue.forwardMin, dicValue.forwardMax));

            _startRotation = VR.Camera.Origin.rotation;
            _startPosition = VR.Camera.Head.position;
            _lerpMultiplier = Mathf.Min(
                KoikatuInterpreter.settings.FlightSpeed / Vector3.Distance(GetPosition(), _startPosition),
                KoikatuInterpreter.settings.FlightSpeed * 60f / Quaternion.Angle(_startRotation, GetRotation(GetPosition())));

        }
        private readonly Quaternion _startRotation;
        private readonly Vector3 _startPosition;

        private readonly Transform _teleportTo;
        private readonly Transform _lookAt;
        private readonly Vector3 _offset;
        private readonly float _lerpMultiplier;
        private float _lerp;

        /// <summary>
        /// Returns current lerp progress, 1f being the end goal.
        /// </summary>
        internal float Move()
        {
            var step = Mathf.SmoothStep(0f, 1f, _lerp += Time.deltaTime * _lerpMultiplier);
            var offsetPos = GetPosition();
            var pos = Vector3.Slerp(_startPosition, offsetPos, step);
            VR.Camera.Origin.rotation = Quaternion.Slerp(_startRotation, GetRotation(offsetPos), step);
            VR.Camera.Origin.position += pos - VR.Camera.Head.position;
            return step;
        }

        private Vector3 GetPosition() => _teleportTo.TransformPoint(_offset);
        private Quaternion GetRotation(Vector3 offsetPos) => Quaternion.LookRotation(_lookAt.position - offsetPos);

        ///// <summary>
        ///// Stub.
        ///// </summary>
        //private void NewLookAtPoI()
        //{
        //    if (_target == null)
        //        NextChara(keepChara: true);
        //    var chaControl = _target;
        //    var extraForBoobs = new Vector3();

        //    string poiIndex = poiDic.ElementAt(Random.Range(0, poiDic.Count)).Key;


        //    if (chaControl.sex != 1)
        //    {
        //        chaControl = FindObjectsOfType<ChaControl>()
        //            .Where(c => c.objTop.activeSelf && c.visibleAll && c.sex == 1) //!c.GetTopmostParent().name.Contains("ActionScene") && c.visibleAll)
        //            .FirstOrDefault();
        //    }

        //    string teleportTo = poiDic[poiIndex].teleportTo;
        //    if (teleportTo.Contains("cf_j_spine03"))
        //    {
        //        // Find median value between nipples and use it as an anchor point. Otherwise the big/small breast disrespect is upon us.
        //        var lNip = _target.objBodyBone.transform.Descendants()
        //            .Where(t => t.name.Contains("a_n_nip_L"))
        //            .Select(t => t.position)
        //            .FirstOrDefault();
        //        var rNip = _target.objBodyBone.transform.Descendants()
        //            .Where(t => t.name.Contains("a_n_nip_R"))
        //            .Select(t => t.position)
        //            .FirstOrDefault();
        //        extraForBoobs = (lNip + rNip) / 2f;
        //    }
        //    Transform teleportToPosition = chaControl.transform.Descendants()
        //        .Where(t => t.name.Contains(teleportTo))
        //        .FirstOrDefault();

        //    // Pick the object we will be looking at.
        //    string lookAtPoI = poiDic[poiIndex].lookAt.ElementAt(Random.Range(0, poiDic[poiIndex].lookAt.Count));
        //    Vector3 lookAtPosition = chaControl.transform.Descendants()
        //        .Where(t => t.name.Contains(lookAtPoI))
        //        .Select(t => t.position)
        //        .FirstOrDefault();

        //    var forward = Random.Range(poiDic[poiIndex].forwardMin, poiDic[poiIndex].forwardMax);
        //    var up = Random.Range(poiDic[poiIndex].upMin, poiDic[poiIndex].upMax);
        //    var right = Random.Range(poiDic[poiIndex].rightMin, poiDic[poiIndex].rightMax);

        //    Vector3 teleportVector = teleportTo.Contains("cf_j_spine03") ? extraForBoobs : teleportToPosition.position;
        //    teleportVector +=
        //        (teleportToPosition.forward * forward) +
        //        (teleportToPosition.up * up) +
        //        (teleportToPosition.right * right);

        //    VR.Camera.Origin.rotation = Quaternion.LookRotation(lookAtPosition - teleportVector);
        //    VR.Camera.Origin.position += teleportVector - VR.Camera.Head.position;
        //}
        private readonly Dictionary<string, PoIPatternInfo> poiDicDev = new()
        {

            {
                "NavelUpFront",  // Upfront
                new PoIPatternInfo {
                    teleportTo = "cf_j_spine01",
                    lookAt = [
                        "cf_j_spine03",
                        "cf_j_spine01",
                        "cf_j_spine02"
                    ],
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
                    lookAt = [
                        "cf_j_spine03",
                        "cf_j_spine01",
                        "cf_j_spine02"
                    ],
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
                    lookAt = [
                        "cf_j_spine03",
                        "cf_j_spine01",
                        "cf_j_spine02"
                    ],
                    forwardMin = -0.1f,
                    forwardMax = 0.1f,
                    upMin = -0.1f,
                    upMax = 0.1f,
                    rightMin = 0.15f,
                    rightMax = 0.25f
                }
            }
        };
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
