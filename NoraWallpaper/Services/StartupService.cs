using Microsoft.Win32;
using System;
using System.IO;

namespace NoraWallpaper.Services;

public class StartupService
{
    private const string AppName = "NoraWallpaper";

    public static void EnableStartup()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            key.SetValue(AppName, $"\"{exePath}\" --hidden");
        }
        catch (Exception ex)
        {
            // Log or handle error
            Console.WriteLine($"Failed to enable startup: {ex.Message}");
        }
    }

    public static void DisableStartup()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.DeleteValue(AppName, false);
        }
        catch (Exception ex)
        {
            // Log or handle error
            Console.WriteLine($"Failed to disable startup: {ex.Message}");
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            return key.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }
}
