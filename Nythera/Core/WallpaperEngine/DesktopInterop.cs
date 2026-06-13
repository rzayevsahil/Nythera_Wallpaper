using System;
using System.Runtime.InteropServices;
using Nythera.Native;

namespace Nythera.Core.WallpaperEngine;

public class DesktopInterop
{
    public static IntPtr GetWorkerW()
    {
        return GetBestDesktopParent();
    }

    public static IntPtr GetBestDesktopParent()
    {
        string debugFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "monitor_debug.txt");
        IntPtr parent = IntPtr.Zero;

        // Strategy 1: WorkerW (Standard technique with MSG_SPAWN_WORKER)
        try
        {
            parent = TryWorkerWStrategy();
            if (parent != IntPtr.Zero)
            {
                System.IO.File.AppendAllText(debugFile, $"[Strategy 1] WorkerW Strategy succeeded. Parent: {parent}\n");
                return parent;
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(debugFile, $"[Strategy 1] WorkerW Strategy failed: {ex.Message}\n");
        }

        // Strategy 2: Progman
        try
        {
            parent = TryProgmanStrategy();
            if (parent != IntPtr.Zero)
            {
                System.IO.File.AppendAllText(debugFile, $"[Strategy 2] Progman Strategy succeeded. Parent: {parent}\n");
                return parent;
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(debugFile, $"[Strategy 2] Progman Strategy failed: {ex.Message}\n");
        }

        // Strategy 3: Explorer / SHELLDLL_DefView Parent
        try
        {
            parent = TryExplorerStrategy();
            if (parent != IntPtr.Zero)
            {
                System.IO.File.AppendAllText(debugFile, $"[Strategy 3] Explorer Strategy succeeded. Parent: {parent}\n");
                return parent;
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(debugFile, $"[Strategy 3] Explorer Strategy failed: {ex.Message}\n");
        }

        // Strategy 4: Fallback (Desktop Window)
        try
        {
            parent = TryFallbackStrategy();
            if (parent != IntPtr.Zero)
            {
                System.IO.File.AppendAllText(debugFile, $"[Strategy 4] Fallback Strategy succeeded. Parent: {parent}\n");
                return parent;
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(debugFile, $"[Strategy 4] Fallback Strategy failed: {ex.Message}\n");
        }

        System.IO.File.AppendAllText(debugFile, $"[Error] All strategies failed to find a desktop parent.\n");
        return IntPtr.Zero;
    }

    private static IntPtr TryWorkerWStrategy()
    {
        IntPtr progman = WindowsApi.FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return IntPtr.Zero;

        // Send a message to Progman to spawn a WorkerW behind the desktop icons
        UIntPtr result;
        WindowsApi.SendMessageTimeout(
            progman,
            WindowsApi.MSG_SPAWN_WORKER,
            UIntPtr.Zero,
            IntPtr.Zero,
            WindowsApi.SMTO_NORMAL,
            1000,
            out result);

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

    private static IntPtr TryProgmanStrategy()
    {
        return WindowsApi.FindWindow("Progman", null);
    }

    private static IntPtr TryExplorerStrategy()
    {
        IntPtr shellWindow = IntPtr.Zero;
        WindowsApi.EnumWindows(new WindowsApi.EnumWindowsProc((tophandle, topparamhandle) =>
        {
            IntPtr p = WindowsApi.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (p != IntPtr.Zero)
            {
                shellWindow = tophandle;
                return false; // Found, stop enumerating
            }
            return true;
        }), IntPtr.Zero);

        if (shellWindow != IntPtr.Zero)
        {
            return shellWindow;
        }

        IntPtr shell = WindowsApi.GetShellWindow();
        if (shell != IntPtr.Zero)
        {
            return shell;
        }

        return IntPtr.Zero;
    }

    private static IntPtr TryFallbackStrategy()
    {
        return WindowsApi.GetDesktopWindow();
    }

    public static void SetAsDesktopBackground(IntPtr hWnd)
    {
        string debugFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "monitor_debug.txt");
        
        IntPtr parentWindow = GetBestDesktopParent();
        System.IO.File.AppendAllText(debugFile, $"SetAsDesktopBackground called. hwnd={hWnd}, initial parentWindow={parentWindow}\n");
        
        if (parentWindow == IntPtr.Zero)
        {
            parentWindow = WindowsApi.GetShellWindow();
            System.IO.File.AppendAllText(debugFile, $"Fallback to ShellWindow: {parentWindow}\n");
        }
        if (parentWindow == IntPtr.Zero)
        {
            parentWindow = WindowsApi.GetDesktopWindow();
            System.IO.File.AppendAllText(debugFile, $"Fallback to DesktopWindow: {parentWindow}\n");
        }

        if (parentWindow != IntPtr.Zero)
        {
            // Update the window style to be a child window, removing borders etc.
            int style = WindowsApi.GetWindowLong(hWnd, WindowsApi.GWL_STYLE);
            style = (int)((style & ~(WindowsApi.WS_POPUP | WindowsApi.WS_CAPTION | WindowsApi.WS_THICKFRAME | WindowsApi.WS_BORDER)) 
                          | WindowsApi.WS_CHILD | WindowsApi.WS_CLIPCHILDREN | WindowsApi.WS_CLIPSIBLINGS);
            WindowsApi.SetWindowLong(hWnd, WindowsApi.GWL_STYLE, style);

            // Set the parent of our window to the desktop parent
            WindowsApi.SetParent(hWnd, parentWindow);
        }
        else
        {
            System.IO.File.AppendAllText(debugFile, $"Failed to find any parent window. Keeping as is.\n");
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
