using System;
using System.IO;
using System.Collections.Generic;

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
            var settings = new Dictionary<string, string>();
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
            var newLines = new List<string>();
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

    public static string ResolveFilePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (File.Exists(path)) return path;

        try
        {
            string fileName = Path.GetFileName(path);
            
            // 1. Try Assets/Videos directory
            string basePath = AppContext.BaseDirectory;
            string newPath = Path.Combine(basePath, "Assets", "Videos", fileName);
            if (File.Exists(newPath)) return newPath;

            // 2. Try Fallback Assets/Videos directory (if run from source)
            string currentDirFallback = Path.Combine(Environment.CurrentDirectory, "Assets", "Videos", fileName);
            if (File.Exists(currentDirFallback)) return currentDirFallback;

            // 3. Try Assembly directory
            string? assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (assemblyDir != null)
            {
                string assemblyFallback = Path.Combine(assemblyDir, "Assets", "Videos", fileName);
                if (File.Exists(assemblyFallback)) return assemblyFallback;
            }

            // 4. Try CustomVideos directory
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "CustomVideos");
            string customPath = Path.Combine(appData, fileName);
            if (File.Exists(customPath)) return customPath;
        }
        catch { }

        return path;
    }

    public static string GetWallpaperPath(string monitorId)
    {
        try
        {
            string path = null;
            if (File.Exists(MonitorSettingsPath))
            {
                var lines = File.ReadAllLines(MonitorSettingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2 && parts[0] == monitorId)
                    {
                        path = parts[1];
                        break;
                    }
                }
            }
            // Fallback to legacy settings.txt
            if (path == null && File.Exists(SettingsPath))
            {
                path = File.ReadAllText(SettingsPath).Trim();
            }
            
            return ResolveFilePath(path);
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

    public static void SaveStretchMode(string monitorId, string stretchMode)
    {
        SavePerMonitorSetting(StretchModePath, monitorId, stretchMode);
    }

    public static string GetStretchMode(string monitorId)
    {
        return GetPerMonitorSetting(StretchModePath, monitorId) ?? "UniformToFill";
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
    
    private static readonly string PlaybackSpeedPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "speed.txt"
    );

    public static void SavePlaybackSpeed(string monitorId, double speed)
    {
        SavePerMonitorSetting(PlaybackSpeedPath, monitorId, speed.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public static double GetPlaybackSpeed(string monitorId)
    {
        string val = GetPerMonitorSetting(PlaybackSpeedPath, monitorId);
        if (val != null && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double speed))
        {
            return speed;
        }
        return 1.0; // Default
    }

    private static readonly string BrightnessPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "brightness.txt"
    );

    public static void SaveBrightness(string monitorId, double brightness)
    {
        SavePerMonitorSetting(BrightnessPath, monitorId, brightness.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public static double GetBrightness(string monitorId)
    {
        string val = GetPerMonitorSetting(BrightnessPath, monitorId);
        if (val != null && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double brightness))
        {
            return brightness;
        }
        return 100.0; // Default is full brightness
    }

    private static readonly string VideoFilterPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "videofilter.txt"
    );

    public static void SaveVideoFilter(string monitorId, string filter)
    {
        SavePerMonitorSetting(VideoFilterPath, monitorId, filter);
    }

    public static string GetVideoFilter(string monitorId)
    {
        return GetPerMonitorSetting(VideoFilterPath, monitorId) ?? "None";
    }

    private static void SavePerMonitorSetting(string path, string monitorId, string value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var settings = new Dictionary<string, string>();
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        settings[parts[0]] = parts[1];
                }
            }
            settings[monitorId] = value;
            var newLines = new List<string>();
            foreach (var kvp in settings)
            {
                newLines.Add($"{kvp.Key}={kvp.Value}");
            }
            File.WriteAllLines(path, newLines);
            
            // For backward compatibility / global fallback
            if (monitorId == "All")
            {
                string globalPath = path + ".global";
                File.WriteAllText(globalPath, value);
            }
        }
        catch { }
    }

    private static string GetPerMonitorSetting(string path, string monitorId)
    {
        try
        {
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2 && parts[0] == monitorId)
                    {
                        return parts[1];
                    }
                }
            }
            
            // Fallback to "All" setting
            if (monitorId != "All" && File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2 && parts[0] == "All")
                    {
                        return parts[1];
                    }
                }
            }

            // Fallback to legacy global setting
            string globalPath = path + ".global";
            if (File.Exists(globalPath))
            {
                return File.ReadAllText(globalPath).Trim();
            }
            // Super legacy fallback (direct file read for files that were previously not dictionary-based)
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path).Trim();
                if (!text.Contains("=")) // Avoid reading dictionary format as a single value
                    return text;
            }
        }
        catch { }
        return null;
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

    public static HashSet<string> GetFavorites()
    {
        var favorites = new HashSet<string>();
        try
        {
            if (File.Exists(FavoritesPath))
            {
                var lines = File.ReadAllLines(FavoritesPath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string resolved = ResolveFilePath(line.Trim());
                        favorites.Add(resolved);
                    }
                }
            }
        }
        catch { }
        return favorites;
    }

    public static void SaveFavorites(HashSet<string> favorites)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FavoritesPath));
            File.WriteAllLines(FavoritesPath, favorites);
        }
        catch { }
    }

    private static readonly string PauseOnBatteryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "pauseonbattery.txt"
    );

    public static void SavePauseOnBattery(bool pause)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PauseOnBatteryPath));
            File.WriteAllText(PauseOnBatteryPath, pause.ToString());
        }
        catch { }
    }

    public static bool GetPauseOnBattery()
    {
        if (File.Exists(PauseOnBatteryPath))
            if (bool.TryParse(File.ReadAllText(PauseOnBatteryPath).Trim(), out bool val))
                return val;
        return true; // Default
    }

    private static readonly string PauseOnFullscreenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "pauseonfullscreen.txt"
    );

    public static void SavePauseOnFullscreen(bool pause)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PauseOnFullscreenPath));
            File.WriteAllText(PauseOnFullscreenPath, pause.ToString());
        }
        catch { }
    }

    public static bool GetPauseOnFullscreen()
    {
        if (File.Exists(PauseOnFullscreenPath))
            if (bool.TryParse(File.ReadAllText(PauseOnFullscreenPath).Trim(), out bool val))
                return val;
        return true; // Default
    }

    private static readonly string QualityProfilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "qualityprofile.txt"
    );

    public static void SaveQualityProfile(string profile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(QualityProfilePath));
            File.WriteAllText(QualityProfilePath, profile);
        }
        catch { }
    }

    public static string GetQualityProfile()
    {
        if (File.Exists(QualityProfilePath))
            return File.ReadAllText(QualityProfilePath).Trim();
        return "High"; // Default
    }

    private static readonly string FpsLimitPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "fpslimit.txt"
    );

    public static void SaveFpsLimit(int fps)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FpsLimitPath));
            File.WriteAllText(FpsLimitPath, fps.ToString());
        }
        catch { }
    }

    public static int GetFpsLimit()
    {
        if (File.Exists(FpsLimitPath))
            if (int.TryParse(File.ReadAllText(FpsLimitPath).Trim(), out int val))
                return val;
        return 60; // Default
    }

    private static readonly string WallpaperTypePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "wallpapertype.txt"
    );

    public static void SaveWallpaperType(string monitorId, string type)
    {
        SavePerMonitorSetting(WallpaperTypePath, monitorId, type);
    }

    public static string GetWallpaperType(string monitorId)
    {
        return GetPerMonitorSetting(WallpaperTypePath, monitorId) ?? "Video";
    }

    private static readonly string ImagePathPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "imagepath.txt"
    );

    public static void SaveImagePath(string monitorId, string path)
    {
        SavePerMonitorSetting(ImagePathPath, monitorId, path);
    }

    public static string GetImagePath(string monitorId)
    {
        return ResolveFilePath(GetPerMonitorSetting(ImagePathPath, monitorId));
    }

    private static readonly string BlurPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "blur.txt"
    );

    public static void SaveBlur(string monitorId, double blur)
    {
        SavePerMonitorSetting(BlurPath, monitorId, blur.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public static double GetBlur(string monitorId)
    {
        string val = GetPerMonitorSetting(BlurPath, monitorId);
        if (val != null && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double b))
            return b;
        return 0; // Default
    }

    private static readonly string ContrastPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "contrast.txt"
    );

    public static void SaveContrast(string monitorId, double contrast)
    {
        SavePerMonitorSetting(ContrastPath, monitorId, contrast.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public static double GetContrast(string monitorId)
    {
        string val = GetPerMonitorSetting(ContrastPath, monitorId);
        if (val != null && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double c))
            return c;
        return 100; // Default
    }

    private static readonly string KenBurnsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "kenburns.txt"
    );

    public static void SaveEnableKenBurns(string monitorId, bool enable)
    {
        SavePerMonitorSetting(KenBurnsPath, monitorId, enable.ToString());
    }

    public static bool GetEnableKenBurns(string monitorId)
    {
        string val = GetPerMonitorSetting(KenBurnsPath, monitorId);
        if (val != null && bool.TryParse(val, out bool e))
            return e;
        return false; // Default
    }

    private static readonly string ParallaxPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "parallax.txt"
    );

    public static void SaveEnableParallax(string monitorId, bool enable)
    {
        SavePerMonitorSetting(ParallaxPath, monitorId, enable.ToString());
    }

    public static bool GetEnableParallax(string monitorId)
    {
        string val = GetPerMonitorSetting(ParallaxPath, monitorId);
        if (val != null && bool.TryParse(val, out bool e))
            return e;
        return false; // Default
    }
}
