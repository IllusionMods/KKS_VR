using System.Collections;
using System.Collections.Generic;
using HarmonyLib;

namespace KK_VR.Caress
{
    public class CaressUtil
    {
        /// <summary>
        /// Send a synthetic click event to the hand controls.
        /// </summary>
        public static IEnumerator ClickCo()
        {
            var consumed = false;
            HandCtrlHooks.InjectMouseButtonDown(0, () => consumed = true);
            while (!consumed) yield return null;
            HandCtrlHooks.InjectMouseButtonUp(0);
        }
    }
}
