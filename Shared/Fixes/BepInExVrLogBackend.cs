using System.Diagnostics;
using BepInEx.Logging;
using VRGIN.Core;

namespace KK_VR.Fixes
{
    public class BepInExVrLogBackend : ILoggerBackend
    {
        private static ManualLogSource _logger;

        public static void ApplyYourself()
        {
            _logger = BepInEx.Logging.Logger.CreateLogSource("VRLog");

            VRLog.Backend = new BepInExVrLogBackend();
        }

        public void Log(string text, object[] args, VRLog.LogMode severity)
        {
            var logText = args == null ? text : string.Format(text, args);
            _logger.Log(ConvertToBepinLogLevel(severity), Format(logText));
        }

        private static string Format(string text)
        {
            var frame = new StackTrace(4).GetFrame(0);
            return string.Format("[{0:0000}] [{2}.{3}] {1}", UnityEngine.Time.realtimeSinceStartup, text, frame.GetMethod().DeclaringType?.Name ?? "???", frame.GetMethod().Name);
        }

        private static LogLevel ConvertToBepinLogLevel(VRLog.LogMode severity)
        {
            return severity switch
            {
                VRLog.LogMode.Debug => LogLevel.Debug,
                VRLog.LogMode.Info => LogLevel.Info,
                VRLog.LogMode.Warning => LogLevel.Warning,
                VRLog.LogMode.Error => LogLevel.Error,
                _ => LogLevel.Message
            };
        }
    }
}
