using KK_VR.Features;
using System;
using System.Collections;
using UnityEngine;
using VRGIN.Core;
using KK_VR.Interpreters;
using KK_VR.Handlers;

namespace KK_VR.Camera
{
    /// <summary>
    /// We fly towards adjusted positions. By flying rather then teleporting the sense of actual scene is created. No avoidance system (yet). 
    /// </summary>
    class VRMoverH : MonoBehaviour
    {
        public static VRMoverH Instance;
        //private Transform _chara;
        //private Transform _eyes;
        //private Transform _torso;
        //private Transform _kokan;
        //private List<Coroutine> _activeCoroutines = new List<Coroutine>();

        public void Initialize()
        {
            Instance = this;
            //var chara = HSceneInterpreter.lstFemale[0];
            //_chara = chara.transform;
            //_eyes = chara.objHeadBone.transform.Find("cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_tz");
            //_torso = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03");
            //_kokan = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_d_kokan/cf_j_kokan");
        }
        public void MoveToInH(Vector3 position, Quaternion rotation, bool actionChange, HFlag.EMode mode)
        {
            //VRPlugin.Logger.LogDebug("VRMoverH:MoveToInH");
            StopAllCoroutines();
            if (PoV.Active || (KoikatuInterpreter.Settings.AutoEnterPov && actionChange))
            {
                PoV.Instance.TryDisable(moveTo: false);
                if (mode != HFlag.EMode.aibu)
                {
                    StartCoroutine(FlyToPov());
                    return;
                }
            }
            StartCoroutine(FlyToPosition(position, rotation));
        }

        public void MakeUpright(Action method = null, params object[] args)
        {
            StartCoroutine(RotateToUpright(method, args));
        }

        private IEnumerator RotateToUpright(Action method = null, params object[] args)
        {
            // Wait for lag.
            yield return null;
            yield return new WaitUntil(() => Time.deltaTime < 0.05f);

            // Wait for IK to put transforms back.
            yield return new WaitForEndOfFrame();
            var origin = VR.Camera.Origin;
            if (origin.eulerAngles.x != 0f || origin.eulerAngles.z != 0f)
            {
                var head = VR.Camera.Head;
                var uprightRot = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
                var lerpMultiplier = KoikatuInterpreter.Settings.FlightSpeed * 90f / Quaternion.Angle(head.rotation, uprightRot);
                var lerp = 0f;
                var startRot = origin.rotation;
                Vector3 oldPos;
                while (lerp < 1f)
                {
                    var step = Mathf.SmoothStep(0f, 1f, lerp += Time.deltaTime * lerpMultiplier);
                    oldPos = head.position;
                    origin.rotation = Quaternion.Lerp(startRot, uprightRot, step); //Quaternion.RotateTowards(origin.rotation, uprightRot, Time.deltaTime * 120f);
                    origin.position += oldPos - head.position;
                    yield return new WaitForEndOfFrame();
                }
                //oldPos = head.position;
                //origin.rotation = uprightRot;
                //origin.position += oldPos - head.position;
            }
            method?.DynamicInvoke(args);
            VRPlugin.Logger.LogDebug($"VRMoverH:MakeUpright:Done");
        }
        private IEnumerator FlyToPov()
        {
            VRPlugin.Logger.LogDebug($"VRMoverH:FlyToPov");
            // We wait for the lag of position change.
            yield return null;
            yield return new WaitUntil(() => Time.deltaTime < 0.05f);
            //if (actionChange)
            //{
            //    yield return new WaitForEndOfFrame();
            //    var destination = _pov.GetDestination();
            //    if (destination != Vector3.zero)
            //    {
            //        var head = VR.Camera.Head;
            //        var origin = VR.Camera.Origin;
            //        var distance = Vector3.Distance(destination, head.position);
            //        var target = destination + _pov.GetRotation() * (Vector3.forward * 0.4f);
            //        var rotation = Quaternion.LookRotation(target - head.position);
            //        if (distance > 2f && Quaternion.Angle(origin.rotation, rotation) > 30f)
            //        {
            //            VRPlugin.Logger.LogDebug($"VRMoverH:FlyToPov:MovementOverride");
            //            var moveSpeed = 0.5f + distance * 0.5f * _settings.FlightSpeed;
            //            var halfDist = distance * 0.5f;
            //            while (true)
            //            {
            //                var angleDelta = Mathf.Clamp(Quaternion.Angle(origin.rotation, rotation) - 30f, 0f, 180f);
            //                if (angleDelta == 0f)
            //                {
            //                    break;
            //                }
            //                distance = Vector3.Distance(destination, head.position) - halfDist;
            //                var step = Time.deltaTime * moveSpeed;
            //                var moveTowards = Vector3.MoveTowards(head.position, destination, step);
            //                var rotSpeed = angleDelta / (distance / step);
            //                origin.rotation = Quaternion.RotateTowards(origin.rotation, rotation, 1f * rotSpeed);
            //                origin.position += moveTowards - head.position;
            //                yield return new WaitForEndOfFrame();
            //            }
            //            while (true)
            //            {
            //                distance = Vector3.Distance(destination, head.position);
            //                var step = Time.deltaTime * moveSpeed;
            //                var angleDelta = Quaternion.Angle(origin.rotation, rotation);
            //                var moveTowards = Vector3.MoveTowards(head.position, destination, step);
            //                var rotSpeed = angleDelta / (distance / step);
            //                origin.rotation = Quaternion.RotateTowards(origin.rotation, rotation, 1f * rotSpeed);
            //                origin.position += moveTowards - head.position;
            //                yield return new WaitForEndOfFrame();
            //                if (distance < step)
            //                {
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}
            //VRPlugin.Logger.LogDebug($"VRMoverH:FlyToPov:Done");
            PoV.Instance.StartPov();
        }
        private IEnumerator FlyToPosition(Vector3 position, Quaternion rotation)
        {
            yield return null;
            yield return new WaitUntil(() => Time.deltaTime < 0.05f);
            yield return new WaitForEndOfFrame();
            var origin = VR.Camera.Origin;
            var head = VR.Camera.Head;
            MouthGuide.Instance.PauseInteractions = true;

            var chara = HSceneInterpreter.lstFemale[0];
            var eyes = chara.objHeadBone.transform.Find("cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_tz");
            //_torso = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03");
            var kokan = chara.objBodyBone.transform.Find("cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_d_kokan/cf_j_kokan");

            var eyeLevel = eyes.transform.position.y;

            if (eyeLevel - chara.transform.position.y > 1f)
            {
                //VRLog.Debug($"VRMoverH:FlyToPosition[height is high, resetting rotation]");
                // Prob upright position, some of them have weird rotations.
                rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                if (position.y < eyeLevel)
                {
                    //VRLog.Debug($"VRMoverH:FlyToPosition[height is low, meeting eye level]");
                    position.y = eyeLevel;
                }
                else
                {
                    position.y += 0.2f;
                    //VRLog.Debug($"VRMoverH:FlyToPosition[height is low, increasing a bit]");
                }

            }

            // distKokan = Vector3.Distance(position, kokan.position);
            //var distTorso = Vector3.Distance(position, _torso.position);
            //var distEyes = Vector3.Distance(position, eyes.position);
            var proximity = Mathf.Min(Vector3.Distance(position, eyes.position), Vector3.Distance(position, kokan.position));
            if (proximity > 0.4f)
            {
                // We are moving.. somewhere, maybe we'll get closer. Changing rotation dulls it more often then not.
                //VRLog.Debug($"VRMoverH:FlyToPosition[not close enough, moving forward for {proximity - 0.4f}]");
                position += rotation * Vector3.forward * (proximity - 0.4f);

            }
            var lerp = 0f;
            var lerpModifier = KoikatuInterpreter.Settings.FlightSpeed / Vector3.Distance(head.position, position);
            var startPos = head.position;
            var startRot = origin.rotation;
            while (lerp < 1f)
            {
                var step = Mathf.SmoothStep(0f, 1f, lerp += Time.deltaTime * lerpModifier);

                var pos = Vector3.Lerp(startPos, position, step);
                origin.rotation = Quaternion.Slerp(startRot, rotation, step);
                origin.position += pos - head.position;
                yield return new WaitForEndOfFrame();
            }
            MouthGuide.Instance.PauseInteractions = false;
        }
    }
}
