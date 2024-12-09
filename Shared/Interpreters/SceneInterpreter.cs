using VRGIN.Core;
using UnityEngine;
using VRGIN.Controls;
using Valve.VR;
using KK_VR.Settings;

namespace KK_VR.Interpreters
{
    abstract class SceneInterpreter
    {
        protected KoikatuSettings _settings = VR.Context.Settings as KoikatuSettings;
        public virtual void OnStart()
        {

        }
        public virtual void OnDisable()
        {

        }
        public virtual void OnUpdate()
        {

        }
        public virtual void OnLateUpdate()
        {

        }

        public virtual bool OnDirectionDown(int index, Controller.TrackpadDirection direction)
        {
            return false;
        }

        public virtual void OnDirectionUp(int index, Controller.TrackpadDirection direction)
        {

        }

        public virtual bool OnButtonDown(int index, EVRButtonId buttonId, Controller.TrackpadDirection direction)
        {
            return false;
        }

        public virtual void OnButtonUp(int index, EVRButtonId buttonId, Controller.TrackpadDirection direction)
        {

        }
        public enum Timing
        {
            Fraction,
            Half,
            Full
        }
        public virtual void OnGripMove(int index, bool active)
        {

        }

        public virtual bool IsGripMove()
        {
            return false;
        }
        //protected static void DestroyControllerComponent<T>()
        //    where T: Component
        //{
        //    var left = VR.Mode.Left.GetComponent<T>();
        //    if (left != null)
        //    {
        //        GameObject.Destroy(left);
        //    }
        //    var right = VR.Mode.Right.GetComponent<T>();
        //    if (right != null)
        //    {
        //        GameObject.Destroy(right);
        //    }
        //}

        public virtual bool IsTouchpadPress(int index)
        {
            return false;
        }
        public virtual bool IsGripPress(int index)
        {
            return false;
        }
        public virtual bool IsTriggerPress(int index)
        {
            return false;
        }
    }
}
