using System;
using System.Runtime.InteropServices;
using NoraWallpaper.Native;

namespace NoraWallpaper.Core.WallpaperEngine;

public class DesktopInterop
{
    public static IntPtr GetWorkerW()
    {
        // 1. Find the Progman window (which manages the desktop)
        IntPtr progman = WindowsApi.FindWindow("Progman", null);

        // 2. Send a message to Progman to spawn a WorkerW behind the desktop icons
        UIntPtr result;
        WindowsApi.SendMessageTimeout(
            progman,
            WindowsApi.MSG_SPAWN_WORKER,
            UIntPtr.Zero,
            IntPtr.Zero,
            WindowsApi.SMTO_NORMAL,
            1000,
            out result);

        // 3. Find the new WorkerW
        IntPtr workerW = IntPtr.Zero;

        WindowsApi.EnumWindows(new WindowsApi.EnumWindowsProc((tophandle, topparamhandle) =>
        {
            IntPtr p = WindowsApi.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (p != IntPtr.Zero)
            {
                // The WorkerW we want is the sibling of the window that contains SHELLDLL_DefView
                workerW = WindowsApi.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
            }

            return true;
        }), IntPtr.Zero);

        return workerW;
    }

    public static void SetAsDesktopBackground(IntPtr hWnd)
    {
        IntPtr workerW = GetWorkerW();
        if (workerW != IntPtr.Zero)
        {
            // Update the window style to be a child window, removing borders etc.
            int style = WindowsApi.GetWindowLong(hWnd, WindowsApi.GWL_STYLE);
            style = (int)(style & ~(WindowsApi.WS_POPUP | WindowsApi.WS_CAPTION | WindowsApi.WS_THICKFRAME | WindowsApi.WS_BORDER) | WindowsApi.WS_CHILD | WindowsApi.WS_CLIPCHILDREN | WindowsApi.WS_CLIPSIBLINGS);
            WindowsApi.SetWindowLong(hWnd, WindowsApi.GWL_STYLE, style);

            // Set the parent of our window to the WorkerW
            WindowsApi.SetParent(hWnd, workerW);
        }
    }
}
