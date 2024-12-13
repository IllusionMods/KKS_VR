using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace KK_VR
{
    internal static class WindowTools
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("User32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        private const string GET_CLASS_NAME_MAGIC = "UnityWndClass";

        private static IntPtr WindowHandle = IntPtr.Zero;

        private static void GetWindowHandle()
        {
            var pid = Process.GetCurrentProcess().Id;
            EnumWindows((w, param) =>
            {
                if (w == IntPtr.Zero) return true;
                if (GetWindowThreadProcessId(w, out uint lpdwProcessId) == 0) return true;
                if (lpdwProcessId != pid) return true;
                var cn = new StringBuilder(256);
                if (GetClassName(w, cn, cn.Capacity) == 0) return true;
                if (cn.ToString() != GET_CLASS_NAME_MAGIC) return true;
                WindowHandle = w;
                return false;
            }, IntPtr.Zero);

            if (WindowHandle == IntPtr.Zero)
                Console.WriteLine("Could not find Unity window handle");
            else
                Console.WriteLine("Found Unity window handle ptr=" + WindowHandle);
        }

        public static void BringWindowToFront()
        {
            if (WindowHandle == IntPtr.Zero) GetWindowHandle();
            SetForegroundWindow(WindowHandle);
        }
    }
}
