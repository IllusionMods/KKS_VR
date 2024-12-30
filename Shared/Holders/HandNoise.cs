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
            for (var i = 0; i < Enum.GetNames(typeof(Sfx)).Length; i++)
            {
                var key = (Sfx)i;
                sfxDic.Add(key, []);
                for (var j = 0; j < Enum.GetNames(typeof(Surface)).Length; j++)
                {
                    sfxDic[key].Add([]);
                    for (var k = 0; k < Enum.GetNames(typeof(Intensity)).Length; k++)
                    {
                        sfxDic[key][j].Add([]);
                    }
                }
            }
        }
        private static void PopulateDic()
        {
            InitDic();
            for (var i = 0; i < sfxDic.Count; i++)
            {
                var key = (Sfx)i;
                for (var j = 0; j < sfxDic[key].Count; j++)
                {
                    for (var k = 0; k < sfxDic[key][j].Count; k++)
                    {
                        var directory = BepInEx.Utility.CombinePaths(
                            [
                                Paths.PluginPath,
                                "SFX",
                                key.ToString(),
                                ((Surface)j).ToString(),
                                ((Intensity)k).ToString()
                            ]);
                        if (Directory.Exists(directory))
                        {
                            var dirInfo = new DirectoryInfo(directory);
                            var clipNames = new List<string>();
                            //foreach (var file in dirInfo.GetFiles("*.wav"))
                            //{
                            //    clipNames.Add(file.Name);
                            //}
                            foreach (var file in dirInfo.GetFiles("*.ogg"))
                            {
                                clipNames.Add(file.Name);
                            }
                            if (clipNames.Count == 0) continue;
                            VRManager.Instance.StartCoroutine(LoadAudioFile(directory, clipNames, sfxDic[key][j][k]));
                        }
                    }
                }
            }
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
