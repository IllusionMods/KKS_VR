using KK_VR.Trackers;
using KK_VR.Grasp;
using NodeCanvas.Tasks.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_VR.Grasp
{
    /// <summary>
    /// Changes blend values of the character's hand
    /// </summary>
    internal class HandScroll
    {
        internal HandScroll(GraspController.PartName partName, ChaControl chara, bool increase)
        {
            _lr = partName == GraspController.PartName.HandL ? 0 : 1;
            _chara = chara;
            _increase = increase;
            _chara.SetEnableShapeHand(_lr, true);
            ReTarget();
        }
        private ChaControl _chara;
        private readonly int _lr;
        private readonly bool _increase;
        private float _blendValue;
        private bool _disable;

        private void ReTarget()
        {

            var array = _chara.fileStatus.shapeHandPtn;
           //VRPlugin.Logger.LogDebug($"HandScroll:ReTarget:{_chara.fileStatus.shapeHandBlendValue[_lr]}");
            //var newIndex = _increase ? (array[_lr, 1] + 1) % 24 : (array[_lr, 1] - 1) < 0 ? 23 : (array[_lr, 1] - 1);
            var newIndex = _increase ? (array[_lr, 1] + 1) % 24 : (array[_lr, 0] - 1) < 0 ? 23 : (array[_lr, 0] - 1);


            if (_increase)
            {
                //if (array[_lr, 1] == 0)
                //{
                //    // Was animated, skip default state and jump to the next.
                //   //VRPlugin.Logger.LogDebug($"ScrollHand:ExitAnimation:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}");
                //    _chara.SetShapeHandValue(_lr, array[_lr, 0], newIndex, 1f);
                //    _chara.SetEnableShapeHand(_lr, true);
                //}
                //else 
                if (_chara.fileStatus.shapeHandBlendValue[_lr] < 1f)
                {
                    _blendValue = _chara.fileStatus.shapeHandBlendValue[_lr];
                   //VRPlugin.Logger.LogDebug($"ScrollHand:PrematureBlendValue:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}:{_blendValue}");
                }
                else if (_disable)
                {
                    if (_increase)
                    {
                        _chara.SetShapeHandValue(_lr, 0, 0, 1f);
                    }
                    else
                    {
                        _chara.SetShapeHandValue(_lr, 0, 0, 1f);
                    }
                   //VRPlugin.Logger.LogDebug($"ScrollHand:Disable:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}");
                    _chara.SetEnableShapeHand(_lr, false);
                    _chara = null;
                }
                else
                {
                    _chara.SetShapeHandValue(_lr, array[_lr, 1], newIndex, 0f);
                    _blendValue = 0f;
                    if (newIndex == 0) _disable = true;
                   //VRPlugin.Logger.LogDebug($"ScrollHand:{_increase}:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}:blend = {_blendValue}");
                }
            }
            else
            {
                //if (array[_lr, 0] == 0)
                //{
                //   //VRPlugin.Logger.LogDebug($"ScrollHand:ExitAnimation:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}");
                //    _chara.SetShapeHandValue(_lr,  newIndex, array[_lr, 0], 1f);
                //    _chara.SetEnableShapeHand(_lr, true);
                //}
                //else 
                if (_chara.fileStatus.shapeHandBlendValue[_lr] > 0f)
                {
                    _blendValue = _chara.fileStatus.shapeHandBlendValue[_lr];
                   //VRPlugin.Logger.LogDebug($"ScrollHand:PrematureBlendValue:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}:{_blendValue}");
                }
                else if (_disable)
                {
                    if (_increase)
                    {
                        _chara.SetShapeHandValue(_lr, 0, 0, 1f);
                    }
                    else
                    {
                        _chara.SetShapeHandValue(_lr, 0, 0, 1f);
                    }
                   //VRPlugin.Logger.LogDebug($"ScrollHand:Disable:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}");
                    _chara.SetEnableShapeHand(_lr, false);
                    _chara = null;
                }
                else
                {
                    _chara.SetShapeHandValue(_lr, newIndex, array[_lr, 0], 1f);
                    _blendValue = 1f;
                    if (newIndex == 0) _disable = true;
                   //VRPlugin.Logger.LogDebug($"ScrollHand:{_increase}:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}:blend = {_blendValue}");
                }
            }


            //if ((_increase && array[_lr, 1] == 0) || (!_increase && array[_lr, 0] == 0))
            //{
            //   //VRPlugin.Logger.LogDebug($"ScrollHand:Enable:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}");
            //    _chara.SetShapeHandValue(_lr, array[_lr, 0], newIndex, 1f);
            //    _chara.SetEnableShapeHand(_lr, true);
            //}
            //else if (newIndex == 0)
            //{
            //    // Wants to be in default state. Set to animation instead.
            //   //VRPlugin.Logger.LogDebug($"ScrollHand:Enable:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}");
            //    _chara.SetShapeHandValue(_lr, array[_lr, 0], newIndex, 1f);
            //    _chara.SetEnableShapeHand(_lr, false);
            //}
            //else
            //{
            //   //VRPlugin.Logger.LogDebug($"ScrollHand:{_increase}:array - [{array[_lr, 0]}][{array[_lr, 1]}]:new - {newIndex}:blend = {_blendValue}");
            //    // If we are in middle of doing stuff.
            //    if (_chara.fileStatus.shapeHandBlendValue[_lr] <= 0f || _chara.fileStatus.shapeHandBlendValue[_lr] >= 1f)
            //    {
            //        _blendValue = _chara.fileStatus.shapeHandBlendValue[_lr];
            //    }
            //    else if (_increase)
            //    {
            //        _chara.SetShapeHandValue(_lr, array[_lr, 1], newIndex, 0f);
            //        _blendValue = 0f;
            //    }
            //    else
            //    {
            //        _chara.SetShapeHandValue(_lr, newIndex, array[_lr, 0], 1f);
            //        _blendValue = 1f;
            //    }

            //}
        }

        internal void Scroll()
        {
            if (_chara != null)
            {
                if (_increase)
                {
                    _chara.SetShapeHandBlend(_lr, _blendValue += Time.deltaTime * 2f);
                    if (_blendValue >= 1f)
                    {
                        ReTarget();
                    }
                }
                else
                {
                    _chara.SetShapeHandBlend(_lr, _blendValue -= Time.deltaTime * 2f);
                    if (_blendValue <= 0f)
                    {
                        ReTarget();
                    }
                }
            }
        }
    }
}
