using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Nythera.Services;

public class PlaylistConfig
{
    public List<string> VideoPaths { get; set; } = new();
    public int IntervalMinutes { get; set; } = 15;
    public bool IsRandom { get; set; } = false;
    public int CurrentIndex { get; set; } = 0;
    public DateTime LastChangeTime { get; set; } = DateTime.MinValue;
}

public static class PlaylistService
{
    private static readonly string PlaylistSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nythera",
        "playlists.json"
    );

    private static Dictionary<string, PlaylistConfig> _playlists = new();

    public static void LoadPlaylists()
    {
        try
        {
            if (File.Exists(PlaylistSettingsPath))
            {
                var json = File.ReadAllText(PlaylistSettingsPath);
                _playlists = JsonSerializer.Deserialize<Dictionary<string, PlaylistConfig>>(json) ?? new();
                
                foreach (var config in _playlists.Values)
                {
                    for (int i = 0; i < config.VideoPaths.Count; i++)
                    {
                        config.VideoPaths[i] = SettingsService.ResolveFilePath(config.VideoPaths[i]);
                    }
                }
            }
        }
        catch { }
    }

    public static void SavePlaylist(string monitorId, PlaylistConfig config)
    {
        _playlists[monitorId] = config;
        SaveAll();
    }
    
    public static void ClearPlaylist(string monitorId)
    {
        if (_playlists.ContainsKey(monitorId))
        {
            _playlists.Remove(monitorId);
            SaveAll();
        }
    }

    public static void SaveAll()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PlaylistSettingsPath)!);
            var json = JsonSerializer.Serialize(_playlists);
            File.WriteAllText(PlaylistSettingsPath, json);
        }
        catch { }
    }

    public static PlaylistConfig? GetPlaylist(string monitorId)
    {
        if (_playlists.TryGetValue(monitorId, out var config))
            return config;
        return null;
    }
    
    public static Dictionary<string, PlaylistConfig> GetAllPlaylists()
    {
        return _playlists;
    }
}
