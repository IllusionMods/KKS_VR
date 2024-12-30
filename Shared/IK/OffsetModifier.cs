using UnityEngine;
using System.Collections;
using KK.RootMotion.FinalIK;
using RootMotion.FinalIK;

namespace KK_VR.IK
{
    // Simplified version from FinalIK scripts
    /// <summary>
    /// Base class for all FBBIK effector positionOffset modifiers. Works with animatePhysics, safe delegates, offset limits.
    /// </summary>
    public abstract class OffsetModifier : MonoBehaviour
    {

        ///// <summary>
        ///// Limiting effector position offsets
        ///// </summary>
        //[System.Serializable]
        //public class OffsetLimits
        //{
        //    public KK.RootMotion.FinalIK.FullBodyBipedEffector effector;
        //    /// <summary>
        //    /// Spring force, if zero then this is a hard limit, if not, offset can exceed the limit.
        //    /// </summary>
        //    public float spring = 0f;
        //    /// <summary>
        //    /// Axes to limit the offset on
        //    /// </summary>
        //    public bool x, y, z;
        //    /// <summary>
        //    /// Limits
        //    /// </summary>
        //    public float minX, maxX, minY, maxY, minZ, maxZ;

        //    // Apply the limit to the effector
        //    public void Apply(KK.RootMotion.FinalIK.IKEffector e, Quaternion rootRotation)
        //    {
        //        Vector3 offset = Quaternion.Inverse(rootRotation) * e.positionOffset;

        //        if (spring <= 0f)
        //        {
        //            // Hard limits
        //            if (x) offset.x = Mathf.Clamp(offset.x, minX, maxX);
        //            if (y) offset.y = Mathf.Clamp(offset.y, minY, maxY);
        //            if (z) offset.z = Mathf.Clamp(offset.z, minZ, maxZ);
        //        }
        //        else
        //        {
        //            // Soft limits
        //            if (x) offset.x = SpringAxis(offset.x, minX, maxX);
        //            if (y) offset.y = SpringAxis(offset.y, minY, maxY);
        //            if (z) offset.z = SpringAxis(offset.z, minZ, maxZ);
        //        }

        //        // Apply to the effector
        //        e.positionOffset = rootRotation * offset;
        //    }

        //    // Just math for limiting floats
        //    private float SpringAxis(float value, float min, float max)
        //    {
        //        if (value > min && value < max) return value;
        //        if (value < min) return Spring(value, min, true);
        //        return Spring(value, max, false);
        //    }

        //    // Spring math
        //    private float Spring(float value, float limit, bool negative)
        //    {
        //        float illegal = value - limit;
        //        float s = illegal * spring;

        //        if (negative) return value + Mathf.Clamp(-s, 0, -illegal);
        //        return value - Mathf.Clamp(s, 0, illegal);
        //    }
        //}

        protected private KK.RootMotion.FinalIK.FullBodyBipedIK _ik;

        // not using Time.deltaTime or Time.fixedDeltaTime here, because we don't know if animatePhysics is true or not on the character, so we have to keep track of time ourselves.
        //protected float deltaTime { get { return Time.time - lastTime; } }
        //protected float deltaTime { get { return Time.deltaTime; } }
        protected abstract void OnModifyOffset();

        //protected float lastTime;


        // The main function that checks for all conditions and calls OnModifyOffset if they are met
        protected void ModifyOffset()
        {
            if (gameObject.activeSelf)
            {
                OnModifyOffset();
            }
            //if (weight <= 0f) return;
            //if (ik == null) return;
            //weight = Mathf.Clamp01(weight);
            //if (deltaTime <= 0f) return;


            //lastTime = Time.time;
        }

        //protected void ApplyLimits(OffsetLimits[] limits)
        //{
        //    // Apply the OffsetLimits
        //    foreach (var limit in limits)
        //    {
        //        limit.Apply(ik.solver.GetEffector(limit.effector), transform.rotation);
        //    }
        //}

        // Remove the delegate when destroyed


        protected virtual void OnDestroy()
        {
            if (_ik != null) _ik.solver.OnPreUpdate -= ModifyOffset;
        }
    }

}
