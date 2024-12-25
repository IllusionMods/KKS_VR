using ADV.Commands.Base;
using IllusionUtility.GetUtility;
using KK_VR.Fixes;
using KK_VR.Handlers;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static HandCtrl;
using Random = UnityEngine.Random;

namespace KK_VR.Interpreters
{
    internal static class SceneExtras
    {
        private static Transform _dirLight;
        private static Transform _oldParent;
        internal static void RepositionDirLight(ChaControl chara)
        {
#if KK
            _dirLight = Component.FindObjectsOfType<Light>()
                .Where(g => g.name.Equals("Directional Light") && g.gameObject.active)
                .Select(g => g.transform)
                .FirstOrDefault();
#else
            _dirLight = Component.FindObjectsOfType<Light>()
                .Where(c => c.gameObject.activeSelf && c.name.Equals("Directional Light")
                && c.transform.parent != null && c.transform.parent.name.Contains("Camera"))
                .Select(c => c.transform)
                .FirstOrDefault();
#endif
            if (_dirLight != null)
            {
                _oldParent = _dirLight.parent;

                // We find look rotation from base of chara to the center of the scene (0,0,0).
                // Then we create rotation towards it from the chara for random degrees, and elevate it a bit.
                // And place our camera at chara's head position + Vector.forward with above rotation.
                // Consistent, doesn't defy logic too often, and is much better then camera directional light, that in vr makes one question own eyes.

                var lowHeight = (chara.objHeadBone.transform.position.y - chara.transform.position.y) < 0.5f;
                var yDeviation = Random.Range(15f, 45f);
                var xDeviation = Random.Range(15f, lowHeight ? 60f : 30f);
                var lookRot = Quaternion.LookRotation(new Vector3(0f, chara.transform.position.y, 0f) - chara.transform.position);
                _dirLight.parent = null;//  transform.SetParent(, worldPositionStays: true);
                _dirLight.position = chara.objHeadBone.transform.position + Quaternion.RotateTowards(chara.transform.rotation, lookRot, yDeviation)
                    * Quaternion.Euler(-xDeviation, 0f, 0f) * Vector3.forward;
                _dirLight.rotation = Quaternion.LookRotation((lowHeight ? chara.objBody : chara.objHeadBone).transform.position - _dirLight.position);
            }
        }
        internal static void ReturnDirLight()
        {
            if (_oldParent != null && _dirLight != null)
            {
                _dirLight.SetParent(_oldParent, false);
            }
        }
        internal static void AddTalkColliders(IEnumerable<ChaControl> charas)
        {
            AddColliders(charas, _talkColliders);
        }
        internal static void AddHColliders(IEnumerable<ChaControl> charas)
        {
            AddColliders(charas, _hColliders);
        }

        private static void AddColliders(IEnumerable<ChaControl> charas, string[,] colliders)
        {
            foreach (var chara in charas)
            {
                if (chara == null) continue;
                for (var i = 0; i < colliders.GetLength(0); i++)
                {
                    var target = chara.objBodyBone.transform.Find(colliders[i, 0]);
                    if (target != null && target.Find(colliders[i, 2]) == null)
                    {
                        var collider = CommonLib.LoadAsset<GameObject>(colliders[i, 1], colliders[i, 2], true);
                        collider.transform.SetParent(target, false);

                        // No clue about this.
                        AssetBundleManager.UnloadAssetBundle(colliders[i, 1], true);
                    }
                }
            }
        }

        internal static void EnableDynamicBones(IEnumerable<ChaControl> charas)
        {
            foreach (var chara in charas)
            {
                EnableDynamicBones(chara);
            }
        }
        internal static void EnableDynamicBones(ChaControl chara)
        {
            if (chara == null) return;

            foreach (var db in chara.objAnim.GetComponents<DynamicBone_Ver02>())
            {
                db.enabled = true;
            }

        }

        private static readonly string[,] _talkColliders =
        {
            {
                "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_L/cf_j_shoulder_L/cf_j_arm00_L/cf_j_forearm01_L/cf_j_hand_L",
                "communication/hit_00.unity3d",
                "com_hit_hand_L"
            },
            {
                "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_R/cf_j_shoulder_R/cf_j_arm00_R/cf_j_forearm01_R/cf_j_hand_R",
                "communication/hit_00.unity3d",
                "com_hit_hand_R"
            },
            {
                "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head",
                "communication/hit_00.unity3d",
                "com_hit_head"
            }
        };
        private static readonly string[,] _hColliders =
         {
            {
                "cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_hit_mouth"
            },
            {
                "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_d_kokan",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_hit_kokan",
            },
            {
                "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_d_ana",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_hit_ana",
            },
            {
                "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_hit_siri_L",
            },
            {
                "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_hit_siri_R",
            },
            //{
            //    "cf_j_waist02",
            //    "h/common/01.unity3d",
            //    "aibu_hit_block_koshi",
            //},
            //{
            //    "cf_d_bust00",
            //    "h/common/01.unity3d",
            //    "aibu_hit_block_mune",
            //},
            //{
            //    "cf_s_head",
            //    "h/common/01.unity3d",
            //    "aibu_hit_head",
            //},
            {
                "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L/cf_j_leg01_L/cf_s_leg01_L/cf_hit_leg01_L",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_reaction_legL",
            },
            {
                "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R/cf_j_leg01_R/cf_s_leg01_R/cf_hit_leg01_R",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_reaction_legR",
            },
            {
                "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L/cf_d_thigh02_L/cf_s_thigh02_L/cf_hit_thigh02_L",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_reaction_thighL",
            },
            {
                "cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R/cf_d_thigh02_R/cf_s_thigh02_R/cf_hit_thigh02_R",
#if KK
                "h/common/00_00.unity3d",
#else
                "h/common/01.unity3d",
#endif
                "aibu_reaction_thighR",
            }
        };

        //internal static void AddHColliders(IEnumerable<ChaControl> charas)
        //{
        //    var _strAssetFolderPath = "h/list/";
        //    var _file = "parent_object_base_female";
        //    var text = GlobalMethod.LoadAllListText(_strAssetFolderPath, _file, null);
        //    if (text == string.Empty) return;
        //    //string[,] array;
        //    GlobalMethod.GetListString(text, out var array);
        //    var length = array.GetLength(0);
        //    var length2 = array.GetLength(1);
        //    foreach (var chara in charas)
        //    {
        //        if (chara == null) continue;
        //        for (int i = 0; i < length; i++)
        //        {
        //            for (int j = 0; j < length2; j += 3)
        //            {
        //                var parentName = array[i, j];
        //                var assetName = array[i, j + 1];
        //                var colliderName = array[i, j + 2];
        //                if (parentName.IsNullOrEmpty() && assetName.IsNullOrEmpty() && colliderName.IsNullOrEmpty())
        //                {
        //                    break;
        //                }
        //                var parent = chara.objBodyBone.transform.FindLoop(parentName);
        //                if (parent.transform.Find(colliderName) != null)
        //                {
        //                    //VRPlugin.Logger.LogDebug($"Extras:Colliders:H:AlreadyHaveOne:{colliderName}");
        //                    continue;
        //                }
        //                //else
        //                //{
        //                //   //VRPlugin.Logger.LogDebug($"Extras:Colliders:H:Add:{colliderName}");
        //                //}
        //                var collider = CommonLib.LoadAsset<GameObject>(assetName, colliderName, true, string.Empty);
        //                AssetBundleManager.UnloadAssetBundle(assetName, true, null, false);
        //                var componentsInChildren = collider.GetComponentsInChildren<EliminateScale>(true);
        //                foreach (var eliminateScale in componentsInChildren)
        //                {
        //                    eliminateScale.chaCtrl = chara;
        //                }
        //                if (parent != null && collider != null)
        //                {
        //                    collider.transform.SetParent(parent.transform, false);
        //                }
        //                //if (!this.dicObject.ContainsKey(text4))
        //                //{
        //                //    this.dicObject.Add(text4, gameObject2);
        //                //}
        //                //else
        //                //{
        //                //    UnityEngine.Object.Destroy(this.dicObject[text4]);
        //                //    this.dicObject[text4] = gameObject2;
        //                //}
        //            }
        //        }
        //    }

        //}

        internal static Dictionary<int, ReactionInfo> dicNowReactions = new Dictionary<int, ReactionInfo>
        {
            {
                0, new ReactionInfo
                {
                    isPlay = true,
                    weight = 0.3f,
                    lstReleaseEffector = new List<int>(),
                    lstParam = new List<ReactionParam>
                    {
                        new ReactionParam
                        {
                            id = 0,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        },
                        new ReactionParam
                        {
                            id = 1,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        }
                    }
                }
            },
            {
                1, new ReactionInfo
                {
                    isPlay = true,
                    weight = 0.3f,
                    lstReleaseEffector = new List<int>(),
                    lstParam = new List<ReactionParam>
                    {
                        new ReactionParam
                        {
                            id = 0,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        },
                        new ReactionParam
                        {
                            id = 1,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        }
                    }
                }
            },
            {
                2, new ReactionInfo
                {
                    isPlay = true,
                    weight = 0.3f,
                    lstReleaseEffector = new List<int>(),
                    lstParam = new List<ReactionParam>
                    {
                        new ReactionParam
                        {
                            id = 2,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        },
                        new ReactionParam
                        {
                            id = 3,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        }
                    }
                }
            },
            {
                3, new ReactionInfo
                {
                    isPlay = true,
                    weight = 0.3f,
                    lstReleaseEffector = new List<int>
                    {
                        0
                    },
                    lstParam = new List<ReactionParam>
                    {
                        new ReactionParam
                        {
                            id = 4,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.zero,
                                    max = Vector3.up
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        },
                        new ReactionParam
                        {
                            id = 5,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.zero,
                                    max = Vector3.up
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        }
                    }
                }
            },
            {
                4, new ReactionInfo
                {
                    isPlay = true,
                    weight = 0.3f,
                    lstReleaseEffector = new List<int>
                    {
                        1
                    },
                    lstParam = new List<ReactionParam>
                    {
                        new ReactionParam
                        {
                            id = 6,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.zero,
                                    max = Vector3.up
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        },
                        new ReactionParam
                        {
                            id = 7,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.zero,
                                    max = Vector3.up
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        }
                    }
                }
            },
            {
                5, new ReactionInfo
                {
                    isPlay = true,
                    weight = 0.3f,
                    lstReleaseEffector = new List<int>(),
                    lstParam = new List<ReactionParam>
                    {
                        new ReactionParam
                        {
                            id = 8,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = new Vector3(-1f, 0f, -1f),
                                    max = new Vector3(1f, 0f, 1f),
                                },
                                new ReactionMinMax
                                {
                                    min = new Vector3(-1f, 0f, -1f),
                                    max = new Vector3(1f, 0f, 1f),
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        },
                        new ReactionParam
                        {
                            id = 9,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = new Vector3(-1f, 0f, -1f),
                                    max = new Vector3(1f, 0f, 1f),
                                },
                                new ReactionMinMax
                                {
                                    min = new Vector3(-1f, 0f, -1f),
                                    max = new Vector3(1f, 0f, 1f),
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        }
                    }
                }
            },
            {
                6, new ReactionInfo
                {
                    isPlay = true,
                    weight = 0.3f,
                    lstReleaseEffector = new List<int>(),
                    lstParam = new List<ReactionParam>
                    {
                        new ReactionParam
                        {
                            id = 10,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = new Vector3(-1f, 0f, -1f),
                                    max = new Vector3(1f, 0f, 1f),
                                },
                                new ReactionMinMax
                                {
                                    min = new Vector3(-1f, 0f, -1f),
                                    max = new Vector3(1f, 0f, 1f),
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        },
                        new ReactionParam
                        {
                            id = 11,
                            lstMinMax = new List<ReactionMinMax>
                            {
                                new ReactionMinMax
                                {
                                    min = new Vector3(-1f, 0f, -1f),
                                    max = new Vector3(1f, 0f, 1f),
                                },
                                new ReactionMinMax
                                {
                                    min = new Vector3(-1f, 0f, -1f),
                                    max = new Vector3(1f, 0f, 1f),
                                },
                                new ReactionMinMax
                                {
                                    min = Vector3.back,
                                    max = Vector3.forward
                                }
                            }
                        }
                    }
                }
            },
        };
    }
}
