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
                || (!Environment.CommandLine.Contains("--vr") && !IsVRCompositorRunning()))
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

        /// <summary>
        /// Checks whether SteamVR's compositor is running.
        ///
        /// Any exception thrown here would abort the whole BepInEx preloader
        /// and prevent the game from starting, so failures are ignored.
        /// Process.ProcessName throws InvalidOperationException for processes
        /// whose name can't be read (e.g. a process that exited after
        /// GetProcesses() returned, or one that is stuck while exiting).
        /// </summary>
        private static bool IsVRCompositorRunning()
        {
            try
            {
                var found = false;
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (!found && p.ProcessName == "vrcompositor")
                            found = true;
                    }
                    catch (Exception)
                    {
                        // Skip processes whose name can't be read
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }
                return found;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to check whether vrcompositor is running, assuming it is not: {ex.Message}");
                return false;
            }
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
