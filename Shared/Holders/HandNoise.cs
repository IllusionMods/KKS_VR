using BepInEx;
using KK_VR.Interpreters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using VRGIN.Core;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization.Formatters.Binary;
using KKAPI.Utilities;
using ADV.Commands.Base;
using static UnityEngine.UI.DefaultControls;
using static Manager.KeyInput.Pad;

namespace KK_VR.Holders
{
    /// <summary>
    /// Provides SFX for character/controller interactions
    /// </summary>
    internal class HandNoise
    {
        private readonly AudioSource _audioSource;
        internal bool IsPlaying => _audioSource.isPlaying;
        internal HandNoise(AudioSource audioSource)
        {
            _audioSource = audioSource;
        }
        internal static void Init()
        {
            PopulateDic();
        }
        internal void PlaySfx(float volume, Sfx sfx, Surface surface, Intensity intensity, bool overwrite)
        {
            if (!KoikatuInterpreter.Settings.EnableSFX) return;
            if (_audioSource.isPlaying)
            {
                if (!overwrite) return;
                _audioSource.Stop();
            }

            //VRPlugin.Logger.LogInfo($"AttemptToPlay:{sfx}:{surface}:{intensity}:{volume}");
            AdjustInput(sfx, ref surface, ref intensity);
            var audioClipList = sfxDic[sfx][(int)surface][(int)intensity];
            var count = audioClipList.Count;
            if (count != 0)
            {
                _audioSource.volume = Mathf.Clamp01(volume);
                _audioSource.pitch = 0.9f + UnityEngine.Random.value * 0.2f;
                _audioSource.clip = audioClipList[UnityEngine.Random.Range(0, count)];
                _audioSource.Play();
            }

        }
        private void AdjustInput(Sfx sfx, ref Surface surface, ref Intensity intensity)
        {
            // Because currently we have far from every category covered.
            if (intensity == Intensity.Wet)
            {
                surface = Surface.Skin;
            }
            else if (sfx == Sfx.Slap)
            {
                surface = Surface.Skin;
            }
            else if (sfx == Sfx.Traverse)
            {
                if (surface == Surface.Hair)
                {
                    intensity = Intensity.Soft;
                }
                else if (surface == Surface.Skin)
                {
                    intensity = Intensity.Soft;
                }
            }
            else if (sfx == Sfx.Undress)
            {

            }
        }

        // As of now categories are highly inconsistent, perhaps revamp of sorts?
        internal enum Sfx
        {
            Tap,
            Slap,
            Traverse,
            Undress,
        }
        internal enum Surface
        {
            Skin,
            Cloth,
            Hair
        }
        internal enum Intensity
        {
            Soft,
            Rough,
            Wet
        }


        //internal enum Intensity
        //{
        //    // Think about:
        //    //     Soft as something smallish and soft and on slower side of things, like boobs or ass.
        //    //     Rough as something flattish and big and at times intense, like tummy or thighs.
        //    //     Wet as.. I yet to mix something proper for it. WIP.
        //    Soft,
        //    Rough,
        //    Wet
        //}
        private static readonly Dictionary<Sfx, List<List<List<AudioClip>>>> sfxDic = [];
        private static void InitDic()
        {
            var sfxNames = Enum.GetNames(typeof(Sfx));
            var surfaceNames = Enum.GetNames(typeof(Surface));
            var intenseNames = Enum.GetNames(typeof(Intensity));

            for (var i = 0; i < sfxNames.Length; i++)
            {
                var key = (Sfx)i;
                sfxDic.Add(key, []);
                for (var j = 0; j < surfaceNames.Length; j++)
                {
                    sfxDic[key].Add([]);
                    for (var k = 0; k < intenseNames.Length; k++)
                    {
                        sfxDic[key][j].Add([]);
                    }
                }
            }
        }

        private static bool FindIndex(string[] array, string name, out int index)
        {
            index = Array.FindIndex(array, n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
            return index != -1;
        }
        private static void Populate()
        {
            var assembly = Assembly.GetAssembly(typeof(HandNoise));
            var resources = assembly.GetManifestResourceNames();
            var sfxNames = Enum.GetNames(typeof(Sfx));
            var surfaceNames = Enum.GetNames(typeof(Surface));
            var intenseNames = Enum.GetNames (typeof(Intensity));
            foreach (var resource in resources)
            {
                var stream = assembly.GetManifestResourceStream(resource);
                if (stream == null)
                {
                    VRPlugin.Logger.LogWarning($"Failed to get stream from resource\n{resource}");
                    continue;
                }

                var assetBundle =
#if KK
                    AssetBundle.LoadFromMemory(ResourceUtils.ReadAllBytes(stream));
#else
                    AssetBundle.LoadFromStream(stream);
#endif
                stream.Close();

                if (assetBundle == null)
                {
                    VRPlugin.Logger.LogWarning($"Failed to load embedded resource\n{resource}");
                    continue;
                }

                foreach (var asset in assetBundle.GetAllAssetNames())
                {
                    var words = asset.Trim().Split('/');
                    if (words.Length > 5
                        && words[0].Equals("assets", StringComparison.OrdinalIgnoreCase)
                        && words[1].Equals("sfx", StringComparison.OrdinalIgnoreCase))
                    {
                        if (FindIndex(sfxNames, words[2], out var sfxIndex)
                            && FindIndex(surfaceNames, words[3], out var surfaceIndex)
                            && FindIndex(intenseNames, words[4], out var intenseIndex))
                        {
                            var audioClip = assetBundle.LoadAsset<AudioClip>(words[5]);
                            if (audioClip != null)
                            {
#if DEBUG
                                VRPlugin.Logger.LogDebug($"AddEmbeddedAsset - {words[5]} : {asset}");
#endif
                                sfxDic[(Sfx)sfxIndex][surfaceIndex][intenseIndex].Add(audioClip);
                            }
                        }
                    }
                }
            }
        }
        private static void PopulateDic()
        {
            InitDic();
            Populate();
            return;
            //for (var i = 0; i < sfxDic.Count; i++)
            //{
            //    var key = (Sfx)i;
            //    for (var j = 0; j < sfxDic[key].Count; j++)
            //    {
            //        for (var k = 0; k < sfxDic[key][j].Count; k++)
            //        {
            //            //var directory = BepInEx.Utility.CombinePaths(
            //            //    [
            //            //        Paths.PluginPath,
            //            //        "SFX",
            //            //        key.ToString(),
            //            //        ((Surface)j).ToString(),
            //            //        ((Intensity)k).ToString()
            //            //    ]);
            //            //if (Directory.Exists(directory))
            //            //{

            //                var dirInfo = new DirectoryInfo(directory);
            //                var clipNames = new List<string>();
            //                //foreach (var file in dirInfo.GetFiles("*.wav"))
            //                //{
            //                //    clipNames.Add(file.Name);
            //                //}
            //                foreach (var file in dirInfo.GetFiles("*.ogg"))
            //                {
            //                    clipNames.Add(file.Name);
            //                }
            //                if (clipNames.Count == 0) continue;
            //                VRManager.Instance.StartCoroutine(LoadAudioFile(directory, clipNames, sfxDic[key][j][k]));
            //            //}
            //        }
            //    }
            //}
        }

        private static IEnumerator LoadAudioFile(string path, List<string> clipNames, List<AudioClip> destination)
        {
            foreach (var name in clipNames)
            {
                //UnityWebRequest audioFile;
                //if (name.EndsWith(".wav"))
                //{
                //    audioFile = UnityWebRequest.GetAudioClip(Path.Combine(path, name), AudioType.WAV);
                //}
                //else
                //{
#if KK

                var audioFile = UnityWebRequest.GetAudioClip(Path.Combine(path, name), AudioType.OGGVORBIS);
#else
                var audioFile = UnityWebRequestMultimedia.GetAudioClip(Path.Combine(path, name), AudioType.OGGVORBIS);
#endif
                //}
#if KK

                yield return audioFile.Send();
                if (audioFile.isError)
#else
                yield return audioFile.SendWebRequest();
                if (audioFile.isHttpError || audioFile.isNetworkError)
#endif
                {
                    VRPlugin.Logger.LogWarning($"{audioFile.error} - {Path.Combine(path, name)}");
                }
                else
                {
                    var clip = DownloadHandlerAudioClip.GetContent(audioFile);
                    clip.name = name;
                    destination.Add(clip);
                    //VRPlugin.Logger.LogDebug($"Loaded:SFX:{name}");
                }
            }
        }
    }
}
