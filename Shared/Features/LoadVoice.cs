using BepInEx.Configuration;
using HarmonyLib;
using Illusion.Game;
using KK_VR.Features.Extras;
using KK_VR.Interpreters;
using Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace KK_VR.Features
{
    /// <summary>
    /// Plays native voice lines for any chara at any moment
    /// </summary>
    public static class LoadVoice
    {
        public enum VoiceType
        {
            Laugh,
            Short
        }
        private static Func<int> _maleBreathPersonality;
        private const string _path = "sound/data/pcm/c**/";
        private static string GetBundleH(int personalityId)
        {
#if KK
            return personalityId switch
            {
                30 => "14",
                31 => "15",
                32 => "16",
                33 => "17",
                34 or 35 or 36 or 37 => "20",
                38 => "50",
                _ => "00"
            };
#else
            return personalityId switch
            {
                40 or 41 or 42 or 43 => "71",
                _ => "01"
            };
#endif
        }
        private static string GetBundleNonH(int personalityId)
        {
            return personalityId switch
            {
                40 or 41 or 42 or 43 => "70",
                _ => "00"
            };
        }
            public static void Init()
        {
            var type = AccessTools.TypeByName("KK_MaleBreath.MaleBreath");
            if (type != null)
            {
                _maleBreathPersonality = AccessTools.MethodDelegate<Func<int>>(AccessTools.FirstMethod(type, m => m.Name.Equals("GetPlayerPersonality")));
            }
        }
        private static void Play(VoiceType type, ChaControl chara)//, bool setCooldown)
        {
            // Copy MaleBreath method here, prettier.
           //VRPlugin.Logger.LogDebug($"Voice:Play:{type}:{chara}");

            var voiceList = GetVoiceList(type);
            if (voiceList == null)
            {
                return;
            }
#if KK
            var hExp = Game.Instance.HeroineList
#else
            var hExp = Game.HeroineList
#endif
                .Where(h => h.chaCtrl == chara)
                .Select(h => h.HExperience)
                .FirstOrDefault();

            var personalityId = chara.fileParam.personality;
            if (chara.sex == 0 && _maleBreathPersonality != null)
            {
                personalityId = _maleBreathPersonality();
            }

            if (hExp == SaveData.Heroine.HExperienceKind.不慣れ)
            {
                // They often use the same asset.
                // Hook for this? 
                hExp = SaveData.Heroine.HExperienceKind.初めて;
            }
            var bundle = _path + voiceList[Random.Range(0, voiceList.Count)];

            // Replace personality id.
            bundle = bundle.Replace("**", (personalityId < 10 ? "0" : "") + personalityId.ToString());

            // Replace hExp if there is any.
            bundle = bundle.Replace("^", ((int)hExp).ToString());
            var index = bundle.LastIndexOf('/');

            // Extract Asset from the string at the end.
            var asset = bundle.Substring(index + 1);

            // Remove it from the string.
            bundle = bundle.Remove(index + 1);

            var h = bundle.EndsWith("h/", StringComparison.OrdinalIgnoreCase);
            bundle += GetBundle(personalityId, hVoice: h);

           //VRPlugin.Logger.LogDebug($"{bundle} + {asset}");
            var setting = new Utils.Voice.Setting
            {
                no = personalityId,
                assetBundleName = bundle,
                assetName = asset,
                pitch = chara.fileParam.voicePitch,
                voiceTrans = chara.dictRefObj[ChaReference.RefObjKey.a_n_mouth].transform,

            };
            //chara.ChangeMouthPtn(0, true);
#if KK
            chara.SetVoiceTransform(Utils.Voice.OnecePlayChara(setting));
#else
            chara.SetLipSync(Utils.Voice.OncePlayChara(setting));
#endif

            // We respect hScene voices.
            if (KoikatuInterpreter.CurrentScene == KoikatuInterpreter.SceneType.HScene)
            {
                for (var i = 0; i < HSceneInterpreter.lstFemale.Count; i++)
                {
                    if (HSceneInterpreter.lstFemale[i] == chara)
                    {
                        // Something of this is probably unnecessary, but figuring it out is a huge pain, given 'HVoiceCtrl' structure.
                        var voice = HSceneInterpreter.hVoice.nowVoices[i];
                        voice.state = HVoiceCtrl.VoiceKind.breathShort;
                        voice.notOverWrite = true;
                        voice.shortInfo.isPlay = true;
                        voice.link = new HVoiceCtrl.LinkInfo();
                        voice.shortInfo.pathAsset = bundle;
                        voice.shortInfo.nameFile = asset;
                        HSceneInterpreter.hVoice.linkUseBreathPtn[i] = null;
                        HSceneInterpreter.hVoice.linkUseVoicePtn[i] = null;
                        break;
                    }
                }
            }
        }
        public static void PlayVoice(VoiceType voiceType, ChaControl chara, bool voiceWait = true)
        {
            if (!voiceWait || chara.asVoice == null || !IsVoiceActive(chara))
            {
                Play(voiceType, chara);
            }
        }
        private static bool IsVoiceActive(ChaControl chara)
        {
            if (KoikatuInterpreter.CurrentScene == KoikatuInterpreter.SceneType.HScene)
            {
                for (var i = 0; i < HSceneInterpreter.lstFemale.Count; i++)
                {
                    if (HSceneInterpreter.lstFemale[i] == chara)
                    {
                        return HSceneInterpreter.hVoice.nowVoices[i].state != HVoiceCtrl.VoiceKind.breath || HSceneInterpreter.IsKissAnim;
                    }
                }
            }
            return chara.asVoice != null
                && !(chara.asVoice.name.StartsWith("h_ko", StringComparison.Ordinal)
                // Match "0**_0*" at the end for 'Short'. e.g. in "h_ko_27_00_006_04" - [006_04] = Match!
                || (Regex.IsMatch(chara.asVoice.name, @"0..\S0.$", RegexOptions.CultureInvariant)
                && _kissBreaths.Any(s => chara.asVoice.name.EndsWith(s, StringComparison.Ordinal))));
        }
        private static readonly List<string> _kissBreaths =
            [
            "013",
            "014",
            "015",
            "016",
            "017",
            "018",
            "019",
            "020"
            ];
        private static string GetBundle(int id, bool hVoice)
        {
#if KK
            return GetBundleH(id) + (hVoice ? "_00.unity3d" : ".unity3d");
#else
            return (hVoice ? GetBundleH(id) : GetBundleNonH(id)) + ".unity3d";
#endif
        }
        private static List<string> GetVoiceList(VoiceType type)
        {
            return type switch
            {
                VoiceType.Laugh => VoiceBundles.Laughs,
                VoiceType.Short => VoiceBundles.Shorts,
                _ => null
            };

        }
    }
}
