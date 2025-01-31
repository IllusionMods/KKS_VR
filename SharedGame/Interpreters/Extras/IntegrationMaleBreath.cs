using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using static KK_VR.Fixes.Util;

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
                if (GetMethod(type, "GetPlayerPersonality", out var getPlayerPersonality))
                {
                    GetMaleBreathPersonality = AccessTools.MethodDelegate<Func<int>>(getPlayerPersonality);
                }

                if (GetMethod(type, "OnPov", out var onPov))
                {
                    OnPov = AccessTools.MethodDelegate<Action<bool, ChaControl>>(onPov);
                }
                _active = GetMaleBreathPersonality != null && OnPov != null;
            }
        }
    }
}
