using System;
using System.Runtime.InteropServices;
using System.Text;
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

    private static bool IsValidWorkerW(IntPtr hWnd, IntPtr shellWindow)
    {
        if (hWnd == IntPtr.Zero || hWnd == shellWindow) return false;

        System.Text.StringBuilder className = new System.Text.StringBuilder(256);
        if (WindowsApi.GetClassName(hWnd, className, className.Capacity) > 0)
        {
            if (className.ToString() != "WorkerW") return false;
        }
        else
        {
            return false;
        }

        int style = WindowsApi.GetWindowLong(hWnd, WindowsApi.GWL_STYLE);
        bool isVisible = (style & 0x10000000) != 0; // WS_VISIBLE
        if (!isVisible) return false;

        WindowsApi.RECT rect;
        if (WindowsApi.GetWindowRect(hWnd, out rect))
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width > 300 && height > 300)
            {
                return true;
            }
        }
        return false;
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

        IntPtr shellWindow = IntPtr.Zero;

        // Find the window containing SHELLDLL_DefView (can be Progman or a WorkerW)
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

        if (shellWindow == IntPtr.Zero)
        {
            shellWindow = progman;
        }

        IntPtr foundWorkerW = IntPtr.Zero;

        // 1. Fast Path: Check sibling of shellWindow
        IntPtr sibling = WindowsApi.FindWindowEx(IntPtr.Zero, shellWindow, "WorkerW", null);
        if (IsValidWorkerW(sibling, shellWindow))
        {
            foundWorkerW = sibling;
        }

        // 2. Fast Path: Check child of shellWindow (for Win11 setups where WorkerW is spawned under Progman)
        if (foundWorkerW == IntPtr.Zero)
        {
            IntPtr child = WindowsApi.FindWindowEx(shellWindow, IntPtr.Zero, "WorkerW", null);
            if (IsValidWorkerW(child, shellWindow))
            {
                foundWorkerW = child;
            }
        }

        // 3. Fallback: Enumerate top-level windows to find any valid WorkerW
        if (foundWorkerW == IntPtr.Zero)
        {
            WindowsApi.EnumWindows(new WindowsApi.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                if (IsValidWorkerW(tophandle, shellWindow))
                {
                    foundWorkerW = tophandle;
                    return false; // Stop enumerating
                }
                return true;
            }), IntPtr.Zero);
        }

        // 4. Fallback: Enumerate child windows of shellWindow to find any valid WorkerW
        if (foundWorkerW == IntPtr.Zero && shellWindow != IntPtr.Zero)
        {
            WindowsApi.EnumChildWindows(shellWindow, new WindowsApi.EnumWindowsProc((childHandle, lParam) =>
            {
                if (IsValidWorkerW(childHandle, shellWindow))
                {
                    foundWorkerW = childHandle;
                    return false; // Stop enumerating
                }
                return true;
            }), IntPtr.Zero);
        }

        return foundWorkerW;
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

    private static void LogDesktopWindowTree(string debugFile)
    {
        try
        {
            System.IO.File.AppendAllText(debugFile, "--- DESKTOP WINDOW TREE DUMP ---\n");
            
            IntPtr shell = WindowsApi.GetShellWindow();
            IntPtr desktop = WindowsApi.GetDesktopWindow();
            System.IO.File.AppendAllText(debugFile, $"GetShellWindow(): {shell}, GetDesktopWindow(): {desktop}\n");

            WindowsApi.EnumWindows(new WindowsApi.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                StringBuilder className = new StringBuilder(256);
                if (WindowsApi.GetClassName(tophandle, className, className.Capacity) > 0)
                {
                    string cls = className.ToString();
                    if (cls == "WorkerW" || cls == "Progman")
                    {
                        int style = WindowsApi.GetWindowLong(tophandle, WindowsApi.GWL_STYLE);
                        WindowsApi.RECT rect;
                        WindowsApi.GetWindowRect(tophandle, out rect);
                        bool isVisible = (style & 0x10000000) != 0; // WS_VISIBLE
                        
                        IntPtr shellView = WindowsApi.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                        string hasShellView = shellView != IntPtr.Zero ? $" (Contains SHELLDLL_DefView: {shellView})" : "";
                        
                        System.IO.File.AppendAllText(debugFile, 
                            $"Top Window - Handle: {tophandle}, Class: {cls}, Visible: {isVisible}, Style: 0x{style:X8}, Rect: [{rect.Left}, {rect.Top}, {rect.Right}, {rect.Bottom}]{hasShellView}\n");
                        
                        EnumChildWindows(tophandle, debugFile);
                    }
                }
                return true;
            }), IntPtr.Zero);
            
            System.IO.File.AppendAllText(debugFile, "--------------------------------\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(debugFile, $"Error in LogDesktopWindowTree: {ex.Message}\n");
        }
    }

    private static void EnumChildWindows(IntPtr parent, string debugFile)
    {
        try
        {
            WindowsApi.EnumChildWindows(parent, new WindowsApi.EnumWindowsProc((childHandle, lParam) =>
            {
                StringBuilder className = new StringBuilder(256);
                if (WindowsApi.GetClassName(childHandle, className, className.Capacity) > 0)
                {
                    int style = WindowsApi.GetWindowLong(childHandle, WindowsApi.GWL_STYLE);
                    WindowsApi.RECT rect;
                    WindowsApi.GetWindowRect(childHandle, out rect);
                    bool isVisible = (style & 0x10000000) != 0;
                    
                    System.IO.File.AppendAllText(debugFile, 
                        $"  -> Child - Handle: {childHandle}, Class: {className.ToString()}, Visible: {isVisible}, Style: 0x{style:X8}, Rect: [{rect.Left}, {rect.Top}, {rect.Right}, {rect.Bottom}]\n");
                }
                return true;
            }), IntPtr.Zero);
        }
        catch { }
    }

    public static void SetAsDesktopBackground(IntPtr hWnd)
    {
        string debugFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "monitor_debug.txt");
        
        System.IO.File.AppendAllText(debugFile, $"\n--- SetAsDesktopBackground Start ---\n");
        LogDesktopWindowTree(debugFile);

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
            int oldStyle = WindowsApi.GetWindowLong(hWnd, WindowsApi.GWL_STYLE);
            
            // Update the window style to be a child window, removing borders etc., and ensuring WS_VISIBLE
            int style = oldStyle;
            style = (int)((style & ~(WindowsApi.WS_POPUP | WindowsApi.WS_CAPTION | WindowsApi.WS_THICKFRAME | WindowsApi.WS_BORDER)) 
                          | WindowsApi.WS_CHILD | 0x10000000 | WindowsApi.WS_CLIPCHILDREN | WindowsApi.WS_CLIPSIBLINGS);
            
            System.IO.File.AppendAllText(debugFile, $"Applying style. Old: 0x{oldStyle:X8}, New: 0x{style:X8}\n");
            WindowsApi.SetWindowLong(hWnd, WindowsApi.GWL_STYLE, style);

            // Set the parent of our window to the desktop parent
            IntPtr prevParent = WindowsApi.SetParent(hWnd, parentWindow);
            int errorCode = Marshal.GetLastWin32Error();
            
            System.IO.File.AppendAllText(debugFile, $"SetParent result: {prevParent}, LastError: {errorCode}\n");
            
            if (prevParent == IntPtr.Zero && errorCode != 0)
            {
                System.IO.File.AppendAllText(debugFile, $"[ERROR] SetParent failed with error code {errorCode}. Restoring old style.\n");
                WindowsApi.SetWindowLong(hWnd, WindowsApi.GWL_STYLE, oldStyle);
            }
        }
        else
        {
            System.IO.File.AppendAllText(debugFile, $"Failed to find any parent window. Keeping as is.\n");
        }

        LogDesktopWindowTree(debugFile);
        System.IO.File.AppendAllText(debugFile, $"--- SetAsDesktopBackground End ---\n\n");
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
