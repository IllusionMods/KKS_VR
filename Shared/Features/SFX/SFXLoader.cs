using KK_VR.Interpreters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using VRGIN.Core;
using System.Reflection;
using KK_VR.Settings;
using ADV.Commands.Base;

namespace KK_VR.Features
{
    /// <summary>
    /// Provides SFX for character <-> controller interactions
    /// </summary>
    internal class SFXLoader
    {
        // Sfx contributions are greatly appreciated.
        // Mix them in Audacity or something.

        private readonly AudioSource _audioSource;
        internal bool IsPlaying => _audioSource.isPlaying;

        private static readonly Dictionary<Sfx, List<List<List<AudioClip>>>> sfxDic = [];
        internal SFXLoader(AudioSource audioSource)
        {
            _audioSource = audioSource;
        }
        internal static void Init()
        {
            InitDic();
            LoadEmbeddedResources();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>The length of an audio clip or 0 if none were found.</returns>
        internal float PlaySfx(float volume, Sfx sfx, Surface surface, Intensity intensity, bool overwrite)
        {
            if (!KoikSettings.SfxEnable.Value) return 0f;

            if (_audioSource.isPlaying)
            {
                if (!overwrite) return 0f;
                _audioSource.Stop();
            }
            AdjustInput(ref sfx, ref surface, ref intensity);
            var audioClipList = sfxDic[sfx][(int)surface][(int)intensity];
            var count = audioClipList.Count;
#if DEBUG
            VRPlugin.Logger.LogInfo($"{GetType().Name}.{MethodInfo.GetCurrentMethod().Name}:" +
                $"sfx[{sfx}] surface[{surface}] intensity[{intensity}] volume[{volume}] count[{count}]");
#endif
            if (count != 0)
            {
                _audioSource.volume = Mathf.Clamp01(volume * KoikSettings.SfxVolume.Value);
                _audioSource.pitch = 0.9f + UnityEngine.Random.value * 0.2f;
                _audioSource.clip = audioClipList[UnityEngine.Random.Range(0, count)];
                _audioSource.Play();
                return _audioSource.clip.length;
            }
            return 0f;
        }

        private void AdjustInput(ref Sfx sfx, ref Surface surface, ref Intensity intensity)
        {
            // Substitute Traverse Skin [Hollow –> Soft]
            // Substitute Traverse [Cloth Wet –> Skin Wet and/or Skin Soft]
            // Substitute Traverse Cloth [Hollow –> Hard]
            // Substitute [Dress Cloth Hard –> Undress Cloth Hard]

            if (sfx == Sfx.Traverse)
            {
                if (surface == Surface.Cloth)
                {
                    if (intensity == Intensity.Wet)
                    {
                        surface = Surface.Skin;
                        intensity = UnityEngine.Random.value < 0.5f ? Intensity.Wet : Intensity.Soft;
                    }
                    else if (intensity == Intensity.Hollow)
                    {
                        intensity = Intensity.Hard;
                    }
                }
                else if (surface == Surface.Skin && intensity == Intensity.Hollow)
                {
                    intensity = Intensity.Soft;
                }
            }
            else if (sfx == Sfx.Dress && surface == Surface.Cloth && intensity == Intensity.Hard)
            {
                sfx = Sfx.Undress;
            }
        }


        internal enum Sfx
        {
            Tap,
            Slap,
            Traverse,
            Dress,
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
            // Think about:
            //     Soft as something smallish and soft and on slower side of things. Skin[boobs, pelvis area], Cloth[Underwear, socks], Hair[medium, long]
            //     Hard as something flattish and big and at times intense. Skin[Limbs, rib cage, head], Cloth[Top, bottom], Hair[Short]
            //     Hollow. Skin[Tummy], Cloth[Shoes]
            //     Wet as something that makes squelch sounds. Skin[Vagina], Cloth[conditional panties?]
            Soft, 
            Hard,
            Hollow,
            Wet
        }

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

#if KK
        // .Net Framework 3.5 lacks this method.
        public static void CopyStream(Stream source, Stream destination, int bufferSize = 81920)
        {
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
            }
        }
#endif

        private static void LoadEmbeddedResources()
        {
            var assembly = Assembly.GetAssembly(typeof(SFXLoader));
            var resources = assembly.GetManifestResourceNames();

            var sfxNames = Enum.GetNames(typeof(Sfx));
            var surfaceNames = Enum.GetNames(typeof(Surface));
            var intenseNames = Enum.GetNames(typeof(Intensity));

            foreach (var resource in resources)
            {
#if DEBUG
                VRPlugin.Logger.LogDebug($"LoadEmbeddedResources:{resource}");
#endif
                if (!resource.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) 
                    && !resource.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) continue;

                // KKS_VR.SFX.Slap.Skin.Rough.sv_se_h_44.ogg -> Slap.Skin.Rough.sv_se_h_44.ogg
                var folders = resource.Remove(0, resource.IndexOf("SFX") + 4).Trim().Split('.');

                if (folders.Length > 4)
                {
                    // Approx estimated structure:
                    // Slap.Skin.Rough.sv_se_h_44.ogg

                    // Split into:
                    // Slap   Skin   Rough   sv_se_h_44   ogg

                    if (FindIndex(sfxNames, folders[0], out var sfxIndex)
                        && FindIndex(surfaceNames, folders[1], out var surfaceIndex)
                        && FindIndex(intenseNames, folders[2], out var intenseIndex))
                    {


                        // Temp path + file name + extension
                        var tempPath = System.IO.Path.Combine(
                            System.IO.Path.GetTempPath(),  
                            folders[folders.Length - 2] + "." + folders[folders.Length - 1]
                            );

                        using (var stream = assembly.GetManifestResourceStream(resource))
                        {
                            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite);
#if KK
                            CopyStream(stream, fileStream);
#else
                            stream.CopyTo(fileStream);
#endif
                            // Should be automatic
                            //stream.Close();
                            //fileStream.Close();
                        }

                        // Add to list on finished loading
                        var list = sfxDic[(Sfx)sfxIndex][surfaceIndex][intenseIndex];

                        // Some component's instance to run the coroutine
                        VRManager.Instance.StartCoroutine(
                            LoadAudioFile(
                                BepInEx.Utility.ConvertToWWWFormat(tempPath), 
                                folders[folders.Length -2], 
                                list)
                            );
                    }
                }
            }
        }

        private static IEnumerator LoadAudioFile(string path, string name, List<AudioClip> destination)
        {
            var audioType = path switch
            {
                var p when p.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) => AudioType.OGGVORBIS,
                var p when p.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) => AudioType.WAV,
                _ => AudioType.UNKNOWN,
            };
            if (audioType == AudioType.UNKNOWN)
            {
                VRPlugin.Logger.LogWarning($"Attempt to load an audio file with unsupported format. path[{path}] name[{name}]");
                yield break;
            }

#if KK
            var audioFile = UnityWebRequest.GetAudioClip(path, audioType);
            
            yield return audioFile.Send();
            if (audioFile.isError)
#else
            var audioFile = UnityWebRequestMultimedia.GetAudioClip(path, audioType);

            yield return audioFile.SendWebRequest();
            if (audioFile.isHttpError || audioFile.isNetworkError)
#endif
            {
                VRPlugin.Logger.LogWarning($"{audioFile.error} - {path}");
            }
            else
            {
                var clip = DownloadHandlerAudioClip.GetContent(audioFile);
                clip.name = name;
                destination.Add(clip);
#if DEBUG
                VRPlugin.Logger.LogDebug($"Loaded:SFX:{path}");
#endif
            }
        }
    }
}
