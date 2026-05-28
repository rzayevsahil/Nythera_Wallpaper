using System;
using System.IO;

namespace Nythera.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "settings.txt"
    );

    private static readonly string StretchModePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "stretchmode.txt"
    );

    private static readonly string MonitorSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "monitor_settings.txt"
    );

    public static void SaveWallpaperPath(string monitorId, string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MonitorSettingsPath));
            var settings = new System.Collections.Generic.Dictionary<string, string>();
            if (File.Exists(MonitorSettingsPath))
            {
                var lines = File.ReadAllLines(MonitorSettingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        settings[parts[0]] = parts[1];
                }
            }
            settings[monitorId] = path;
            var newLines = new System.Collections.Generic.List<string>();
            foreach (var kvp in settings)
            {
                newLines.Add($"{kvp.Key}={kvp.Value}");
            }
            File.WriteAllLines(MonitorSettingsPath, newLines);
            
            // Also save to legacy settings.txt for backwards compatibility if it's "All"
            if (monitorId == "All")
            {
                File.WriteAllText(SettingsPath, path);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public static string GetWallpaperPath(string monitorId)
    {
        try
        {
            if (File.Exists(MonitorSettingsPath))
            {
                var lines = File.ReadAllLines(MonitorSettingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2 && parts[0] == monitorId)
                        return parts[1];
                }
            }
            // Fallback to legacy settings.txt
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

    public static string GetWallpaperPath()
    {
        return GetWallpaperPath("All");
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
        "Nythera",
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
        "Nythera",
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

    private static readonly string TargetMonitorPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "targetmonitor.txt"
    );

    public static void SaveTargetMonitor(string monitorId)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TargetMonitorPath));
            File.WriteAllText(TargetMonitorPath, monitorId);
        }
        catch (Exception ex) { }
    }

    public static string GetTargetMonitor()
    {
        if (File.Exists(TargetMonitorPath))
            return File.ReadAllText(TargetMonitorPath).Trim();
        return "All"; // Default
    }

    private static readonly string FavoritesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "favorites.txt"
    );

    public static System.Collections.Generic.HashSet<string> GetFavorites()
    {
        var favorites = new System.Collections.Generic.HashSet<string>();
        try
        {
            if (File.Exists(FavoritesPath))
            {
                var lines = File.ReadAllLines(FavoritesPath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        favorites.Add(line.Trim());
                }
            }
        }
        catch { }
        return favorites;
    }

    public static void SaveFavorites(System.Collections.Generic.HashSet<string> favorites)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FavoritesPath));
            File.WriteAllLines(FavoritesPath, favorites);
        }
        catch { }
    }
}
