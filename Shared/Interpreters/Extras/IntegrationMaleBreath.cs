using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace KK_VR
{
    internal static class IntegrationMaleBreath
    {
        internal static bool IsActive => _active;
        private static bool _active;

        /// <summary>
        /// Provides whatever personality was set in the config for male voice, so we can use it for male reaction.
        /// </summary>
        internal static Func<int> GetMaleBreathPersonality;

        /// <summary>
        /// Synchronizes state of the plugin when PoV is used. 
        /// </summary>
        internal static Action<bool, ChaControl> OnPov;


        internal static void Init()
        {

            var type = AccessTools.TypeByName("KK_MaleBreath.MaleBreath");
            if (type != null)
            {
                GetMaleBreathPersonality = AccessTools.MethodDelegate<Func<int>>(AccessTools.FirstMethod(type, m => m.Name.Equals("GetPlayerPersonality")));
                OnPov = AccessTools.MethodDelegate<Action<bool, ChaControl>>(AccessTools.FirstMethod(type, m => m.Name.Equals("OnPov")));
            }
            _active = GetMaleBreathPersonality != null && OnPov != null;
        }
    }
}
