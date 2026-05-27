using System;
using System.IO;

namespace NoraWallpaper.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoraWallpaper",
        "settings.txt"
    );

    private static readonly string StretchModePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoraWallpaper",
        "stretchmode.txt"
    );

    public static void SaveWallpaperPath(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            File.WriteAllText(SettingsPath, path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public static string GetWallpaperPath()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return File.ReadAllText(SettingsPath).Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load settings: {ex.Message}");
        }
        return null;
    }

    public static void SaveStretchMode(string stretchMode)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StretchModePath));
            File.WriteAllText(StretchModePath, stretchMode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save stretch mode: {ex.Message}");
        }
    }

    public static string GetStretchMode()
    {
        try
        {
            if (File.Exists(StretchModePath))
            {
                return File.ReadAllText(StretchModePath).Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load stretch mode: {ex.Message}");
        }
        return "UniformToFill"; // Default
    }
}
