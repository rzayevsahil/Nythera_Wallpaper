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

    private static readonly string ThemePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoraWallpaper",
        "theme.txt"
    );

    public static void SaveTheme(string theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
            File.WriteAllText(ThemePath, theme);
        }
        catch (Exception ex) { }
    }

    public static string GetTheme()
    {
        if (File.Exists(ThemePath))
            return File.ReadAllText(ThemePath).Trim();
        return "Dark"; // Default
    }

    private static readonly string LanguagePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoraWallpaper",
        "language.txt"
    );

    public static void SaveLanguage(string language)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LanguagePath));
            File.WriteAllText(LanguagePath, language);
        }
        catch (Exception ex) { }
    }

    public static string GetLanguage()
    {
        if (File.Exists(LanguagePath))
            return File.ReadAllText(LanguagePath).Trim();
        return "en"; // Default
    }
}
