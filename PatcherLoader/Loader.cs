using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using Mono.Cecil;
using BepInEx;
using BepInEx.Logging;
using System.Diagnostics;
using System.Linq;

namespace PatcherLoader
{
    /// <summary>
    /// This "patcher" doesn't patch any .NET assemblies. It just loads and
    /// executes a native DLL, which applies some patches to in-memory native
    /// code.
    ///
    /// This needs to be implemented as a preloading-time patcher rather than
    /// a regular plugin because the latter doesn't run early enough in the
    /// startup sequence of the game.
    /// </summary>
    public static class Loader
    {
        public static IEnumerable<string> TargetDLLs => GetDLLs();

        public static void Patch(AssemblyDefinition assembly)
        {
        }

        private readonly static ManualLogSource logger
            = Logger.CreateLogSource("KK_MainGameVR_Patcher");

        private delegate void SetupAll();

        private static IEnumerable<string> GetDLLs()
        {
            var processName = Paths.ProcessName;
            if ((processName != "Koikatu" && processName != "Koikatsu Party")
                || (!Environment.CommandLine.Contains("--vr") 
                && !Process.GetProcesses().Where(p => p != null && p.ProcessName == "vrcompositor").Any()))
            {
                yield break;
            }

            var patcherPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "native_patcher.dll");

            logger.LogInfo($"Loading {patcherPath}");

            var hModule = NativeMethods.LoadLibraryW(patcherPath);
            if (hModule == IntPtr.Zero)
            {
                logger.LogError($"Failed to load native DLL. Error code: {Marshal.GetLastWin32Error()}");
                yield break;
            }
            var funPtr = NativeMethods.GetProcAddress(hModule, "setup_all");
            if (funPtr == IntPtr.Zero)
            {
                logger.LogError($"GetProcAddress failed: {Marshal.GetLastWin32Error()}");
                yield break;
            }
            var setupAll = (SetupAll)Marshal.GetDelegateForFunctionPointer(funPtr, typeof(SetupAll));
            setupAll();

            yield break;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryW(string name);

        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    }
}
