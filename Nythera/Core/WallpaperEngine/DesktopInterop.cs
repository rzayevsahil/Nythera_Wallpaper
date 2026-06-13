using System;
using System.Runtime.InteropServices;
using Nythera.Native;

namespace Nythera.Core.WallpaperEngine;

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
        IntPtr shellWindow = IntPtr.Zero;

        WindowsApi.EnumWindows(new WindowsApi.EnumWindowsProc((tophandle, topparamhandle) =>
        {
            IntPtr p = WindowsApi.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (p != IntPtr.Zero)
            {
                shellWindow = tophandle;
                // The WorkerW we want is the sibling of the window that contains SHELLDLL_DefView
                workerW = WindowsApi.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
            }

            return true;
        }), IntPtr.Zero);

        // Fallback: If sibling search failed but we found the shellWindow, 
        // find any top-level WorkerW window that is not the shellWindow.
        if (workerW == IntPtr.Zero && shellWindow != IntPtr.Zero)
        {
            WindowsApi.EnumWindows(new WindowsApi.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                System.Text.StringBuilder className = new System.Text.StringBuilder(256);
                if (WindowsApi.GetClassName(tophandle, className, className.Capacity) > 0)
                {
                    if (className.ToString() == "WorkerW" && tophandle != shellWindow)
                    {
                        workerW = tophandle;
                        return false; // Stop enumerating
                    }
                }
                return true;
            }), IntPtr.Zero);
        }

        return workerW;
    }

    public static void SetAsDesktopBackground(IntPtr hWnd)
    {
        string debugFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "monitor_debug.txt");
        
        IntPtr workerW = GetWorkerW();
        System.IO.File.AppendAllText(debugFile, $"SetAsDesktopBackground called. hwnd={hWnd}, workerW={workerW}\n");
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

    public static void RestoreDesktop()
    {
        // Forcing a wallpaper refresh causes Windows to tear down the spawned WorkerW
        WindowsApi.SystemParametersInfo(
            WindowsApi.SPI_SETDESKWALLPAPER, 
            0, 
            null!, 
            WindowsApi.SPIF_UPDATEINIFILE | WindowsApi.SPIF_SENDWININICHANGE);
    }
}
