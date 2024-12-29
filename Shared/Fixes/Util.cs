using KK_VR.Handlers;
using KK_VR.Holders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KK_VR.Fixes
{
    static class Util
    {
        // Our version of C# doesn't have tuples, wtf.
        //public struct ValueTuple<T1, T2>
        //{
        //    public T1 Field1 { get; set; }
        //    public T2 Field2 { get; set; }
        //    public ValueTuple(T1 x1, T2 x2)
        //    {
        //        Field1 = x1;
        //        Field2 = x2;
        //    }

        //}

        //public struct ValueTuple<T1, T2, T3>
        //{
        //    public T1 Field1 { get; set; }
        //    public T2 Field2 { get; set; }
        //    public T3 Field3 { get; set; }
        //    public ValueTuple(T1 x1, T2 x2, T3 x3)
        //    {
        //        Field1 = x1;
        //        Field2 = x2;
        //        Field3 = x3;
        //    }

        //}

        //public class ValueTuple
        //{
        //    public static ValueTuple<T1, T2> Create<T1, T2>(T1 x1, T2 x2)
        //    {
        //        return new ValueTuple<T1, T2>(x1, x2);
        //    }
        //    public static ValueTuple<T1, T2, T3> Create<T1, T2, T3>(T1 x1, T2 x2, T3 x3)
        //    {
        //        return new ValueTuple<T1, T2, T3>(x1, x2, x3);
        //    }
        //}

        /// <summary>
        /// Remove a prefix from the given string.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string StripPrefix(string prefix, string str)
        {
            if (str.StartsWith(prefix))
            {
                return str.Substring(prefix.Length);
            }
            return null;
        }
        // Redundant. VRGIN.UnityHelper has it.
        //public static Component CopyComponent(Component original, GameObject destination)
        //{
        //    var type = original.GetType();
        //    var copy = destination.AddComponent(type);
        //    // Copied fields can be restricted with BindingFlags
        //    var fields = type.GetFields();
        //    foreach (var field in fields)
        //    {
        //        field.SetValue(copy, field.GetValue(original));
        //    }
        //    return copy;
        //}
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
    }
}
