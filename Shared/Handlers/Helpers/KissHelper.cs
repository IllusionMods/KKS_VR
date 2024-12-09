using KK_VR.Interpreters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using Random = UnityEngine.Random;

namespace KK_VR.Handlers
{
    internal class KissHelper
    {
        private float _kissAttemptTimestamp;
        private float _kissAttemptChance;
        private Transform _eyes;
        private Transform _shoulders;

        internal KissHelper(Transform eyes, Transform shoulders)
        {
            _eyes = eyes;
            _shoulders = shoulders;
            var heroine = HSceneInterpreter.hFlag.lstHeroine[0];
            _kissAttemptChance = 0.1f + ((int)heroine.HExperience - 1) * 0.15f + (heroine.weakPoint == 0 ? 0.1f : 0f);
        }
        internal void AttemptProactiveKiss()
        {
            if (_kissAttemptTimestamp < Time.time)
            {
                VRPlugin.Logger.LogDebug($"AttemptProactiveKiss:chance - {_kissAttemptChance}");
                var headPos = VR.Camera.Head.position;
                if (Random.value < _kissAttemptChance
                    && !HSceneInterpreter.IsVoiceActive
                    && HSceneInterpreter.IsIdleOutside(HSceneInterpreter.hFlag.nowAnimStateName)
                    && Mathf.Abs(Mathf.DeltaAngle(_shoulders.eulerAngles.y, _eyes.eulerAngles.y)) < 30f
                    && Vector3.Distance(_eyes.position, headPos) < 0.55f
                    && Vector3.Angle(headPos - _eyes.position, _eyes.forward) < 30f)
                {
                    //_kissAttempt = true;
                    //_kissDistance = 0.4f;
                    HSceneInterpreter.LeanToKiss();
                    SetAttemptTimestamp(2f + Random.value * 2f);
                }
                else
                {
                    SetAttemptTimestamp();
                }
            }
        }
        private void SetAttemptTimestamp(float modifier = 1f)
        {
            _kissAttemptTimestamp = Time.time + (20f * modifier);
        }
    }
}
