using HarmonyLib;
using KK_VR.Handlers;
using KK_VR.Holders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KK_VR.Fixes
{
    public static class Util
    {
        public static Vector3 Divide(Vector3 a, Vector3 b) => new(a.x / b.x, a.y / b.y, a.z / b.z);
        public static GameObject CreatePrimitive(PrimitiveType primitive, Vector3 size, Transform parent, Color color, float alpha, bool removeCollider = true)
        {
            return CreatePrimitive(primitive, size, parent, new Color(color.r, color.g, color.b, alpha), removeCollider);
        }
        public static GameObject CreatePrimitive(PrimitiveType primitive, Vector3 size, Transform parent, Color color, bool removeCollider = true)
        {
            var sphere = GameObject.CreatePrimitive(primitive);
            if (removeCollider)
            {
                GameObject.Destroy(sphere.GetComponent<Collider>());
            }
            var renderer = sphere.GetComponent<Renderer>();
            renderer.material = Holder.Material;
            renderer.material.color = color;
            if (parent != null)
                sphere.transform.SetParent(parent, false);
            sphere.transform.localScale = size;
            return sphere;
        }
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsignedAngle = Vector3.Angle(from, to);

            float cross_x = from.y * to.z - from.z * to.y;
            float cross_y = from.z * to.x - from.x * to.z;
            float cross_z = from.x * to.y - from.y * to.x;
            float sign = Mathf.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
            return unsignedAngle * sign;
        }
        /// <returns>True if found, false if not(null)</returns>
        internal static bool GetMethod(Type type, string methodName, out MethodInfo method)
        {
            method = AccessTools.FirstMethod(type, m => m.Name.Equals(methodName));
            return method != null;
        }
    }
}
