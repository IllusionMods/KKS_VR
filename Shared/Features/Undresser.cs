using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using HarmonyLib;

using static ChaFileDefine;
using KK_VR.Fixes;
using static KK_VR.Features.LoadVoice;
using KK_VR.Controls;
using KK_VR.Features;
using static KK_VR.Trackers.Tracker;

namespace KK_VR.Interactors
{
    /// <summary>
    /// Simplified/expanded version from https://github.com/mosirnik/KK_MainGameVR
    /// </summary>
    static class Undresser
    {
        public static bool IsBodyPartClothed(ChaControl chara, Body part)
        {
            var array = ConvertToSlot(part);
            if (array == null) return false;
            foreach (var item in array)
            {
                if (chara.IsClothes(item) && chara.fileStatus.clothesState[item] == 0) return true;
            }
            return false;
        }
        private static int[] ConvertToSlot(Body part)
        {
            return part switch
            {
                Body.MuneL or Body.MuneR => [0, 2],
                Body.UpperBody => [0],
                Body.LowerBody => [1, 5],
                Body.ArmL or Body.ArmR => [0, 4],
                Body.Groin or Body.Asoko => [1, 3, 5],
                Body.ThighL or Body.ThighR or Body.LegL or Body.LegR or Body.FootL or Body.FootR => [5, 6],
                _ => null,
            };
        }
        private static Body ConvertToUndress(Body body)
        {
            return body switch
            {
                Body.Head => Body.None,
                Body.HandR => Body.HandL,
                Body.ArmR => Body.ArmL,
                Body.MuneR => Body.MuneL,
                Body.Groin => Body.Asoko,
                Body.ThighR => Body.ThighL,
                Body.ForearmL => Body.ArmL,
                Body.ForearmR => Body.ArmL,
                Body.FootR => Body.FootL,
                _ => body
            };
        }
        public static bool Undress(Body part, ChaControl chara, bool decrease)
        {
            part = ConvertToUndress(part);
            //if (part == InteractionBodyPart.Crotch && IsWearingSkirt(female))
            //{
            //    //VRLog.Debug($"WearingSkirt");
            //    // Special case: if the character is wearing a skirt, allow
            //    // directly removing the underwear.
            //    targets = _skirtCrotchTargets;
            //}

            var targets = decrease ? UndressDic[part] : RedressDic[part];

            foreach (var target in targets)
            {
                var slot = target.slot;
                if (!chara.IsClothes(slot)
                    || (decrease && chara.fileStatus.clothesState[slot] > target.state)
                    || (!decrease && chara.fileStatus.clothesState[slot] <= target.state))
                {
                   //VRPlugin.Logger.LogDebug($"Undress:Skip[{part}]");
                    continue;
                }
                else
                {
                   //VRPlugin.Logger.LogDebug($"Undress:Valid:Part[{part}]:Slot[{slot}]:State[{target.state}]");
                }
                //if (slot > 6)
                //{
                //    // Target proper shoe slot.
                //    slot = chara.fileStatus.shoesType == 0 ? 7 : 8;
                //}
                if (slot == 3 || slot == 5 || slot == 6)
                {
                    if (decrease)
                    {
                        // Check for pants. If present override pantyhose/socks/panties with them.
                        if (chara.fileStatus.clothesState[1] < (slot == 3 ? 1 : 3) && chara.objClothes[1].GetComponent<DynamicBone>() == null)
                        {
                            chara.SetClothesStateNext(1);
                            return true;
                        }
                    }
                    else
                    {
                        if (slot != 3)
                        {
                            if (chara.fileStatus.clothesState[3] == 2)
                            {
                                // Is we decided to redress pantyhose/socks with panties hanging on the leg, remove them instead.
                                chara.SetClothesState(3, 3, false);
                                //chara.fileStatus.clothesState[3] = 3;
                            }
                            else if (slot == 5 && chara.fileStatus.clothesState[3] == 1)
                            {
                                // Or put them back on if only shifted and we redress pantyhose.
                                chara.SetClothesState(3, 0, false);
                                //chara.fileStatus.clothesState[3] = 0;
                            }
                        }
                        else
                        {
                            // Put panties on in one go.
                            chara.SetClothesState(3, 0, false);
                            return true;
                        }
                    }
                }
                if (decrease)
                {
                    chara.SetClothesStateNext(slot);
                    PlayVoice((VoiceType)UnityEngine.Random.Range(0, 2), chara);
                }
                else
                {
                    chara.SetClothesStatePrev(slot);
                }
                return true;
            }
            return false;
        }

        /*
         * KKS Slots
         * 0 - Top
         * 1 - Bottom
         * 2 - Bra
         * 3 - Panties
         * 4 - gloves
         * 5 - pantyhose
         * 6 - Stockings
         * 
         * 8 - Shoes
         */

        struct SlotState
        {
            public int slot;
            public int state;
        }
        private static readonly Dictionary<Body, List<SlotState>> UndressDic = new()
        {
            // Pairs of clothing slots and their states
            // We check each, if state is less or equal, jump to the next one, otherwise change state.
            {
                Body.Asoko, new List<SlotState>
                {
                    new() { slot = 1, state = 0 },
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 3, state = 0 },
                    //new SlotState { slot = 5, state = 1 },
                    new SlotState { slot = 3, state = 1 }
                }
            },
            {
                Body.LowerBody, new List<SlotState>
                {
                    new SlotState { slot = 1, state = 0 },
                    new SlotState { slot = 1, state = 1 }
                }
            },
            {
                Body.UpperBody, new List<SlotState>
                {
                    new SlotState { slot = 0, state = 0 },
                    new SlotState { slot = 0, state = 1 }
                }
            },
            {
                Body.ThighL, new List<SlotState>
                {
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 6, state = 0 },
                    new SlotState { slot = 1, state = 0 }
                }
            },
            {
                Body.LegL, new List<SlotState>
                {
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 6, state = 0 },
                    new SlotState { slot = 8, state = 0 },
                    new SlotState { slot = 5, state = 1 },
                    new SlotState { slot = 3, state = 2 }
                }
            },
            {
                Body.LegR, new List<SlotState>
                {
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 6, state = 0 },
                    new SlotState { slot = 8, state = 0 },
                    new SlotState { slot = 5, state = 1 },
                }
            },
            {
                Body.MuneL, new List<SlotState>
                {
                    new SlotState { slot = 0, state = 0 },
                    new SlotState { slot = 2, state = 0 },
                    new SlotState { slot = 0, state = 1 },
                    new SlotState { slot = 2, state = 1 },
                }
            },
            {
                Body.ArmL, new List<SlotState>
                {
                    new SlotState { slot = 0, state = 0 },
                    new SlotState { slot = 0, state = 1 },
                    new SlotState { slot = 4, state = 0 }
                }
            },
            {
                Body.HandL, new List<SlotState>
                {
                    new SlotState { slot = 4, state = 0 }
                }
            },
            {
                Body.FootL, 
                [
                    new SlotState { slot = 8, state = 0 },
                    new SlotState { slot = 6, state = 0 },
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 5, state = 1 }
                ]
            }

        };
        private static readonly Dictionary<Body, List<SlotState>> RedressDic = new()
        {
            // Pairs of clothing slots and their states
            // We check each, if state is less, jump to the next one, otherwise change state.
            {
                Body.Asoko, new List<SlotState>
                {
                    new SlotState { slot = 3, state = 0 },
                    new SlotState { slot = 5, state = 1 },
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 1, state = 0 }
                }
            },
            {
                Body.LowerBody, new List<SlotState>
                {
                    //new SlotState { slot = 3, state = 0 },
                    new SlotState { slot = 5, state = 1 },
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 1, state = 0 }
                }
            },
            {
                Body.UpperBody, new List<SlotState>
                {
                    new SlotState { slot = 2, state = 1 },
                    new SlotState { slot = 2, state = 0 },
                    new SlotState { slot = 0, state = 1 },
                    new SlotState { slot = 0, state = 0 },
                }
            },
            {
                Body.ThighL, new List<SlotState>
                {
                    new SlotState { slot = 5, state = 1 },
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 6, state = 0 }
                }
            },
            {
                Body.LegL, new List<SlotState>
                {
                    new SlotState { slot = 5, state = 1 },
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 6, state = 0 },
                    new SlotState { slot = 8, state = 0 }
                }
            },
            {
                Body.LegR, new List<SlotState>
                {
                    new SlotState { slot = 5, state = 1 },
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 6, state = 0 },
                    new SlotState { slot = 8, state = 0 }
                }
            },
            {
                Body.MuneL, new List<SlotState>
                {
                    new SlotState { slot = 2, state = 1 },
                    new SlotState { slot = 2, state = 0 },
                    new SlotState { slot = 0, state = 1 },
                    new SlotState { slot = 0, state = 0 }
                }
            },
            {
                Body.ArmL, new List<SlotState>
                {
                    new SlotState { slot = 4, state = 0 },
                    new SlotState { slot = 0, state = 1 },
                    new SlotState { slot = 0, state = 0 }
                }
            },
            {
                Body.HandL, new List<SlotState>
                {
                    new SlotState { slot = 4, state = 0 }
                }
            },
            {
                Body.FootL,
                [
                    new SlotState { slot = 5, state = 1 },
                    new SlotState { slot = 5, state = 0 },
                    new SlotState { slot = 6, state = 0 },
                    new SlotState { slot = 8, state = 0 },
                ]
            }
        };
    }
}
