using System.Collections.Generic;

namespace NoraWallpaper.Services;

public static class LocalizationService
{
    private static readonly Dictionary<string, string> _tr = new()
    {
        { "AppTitle", "Nora Duvar Kağıdı" },
        { "AppDescription", "Masaüstü arka planınız olarak ayarlamak için bir video seçin." },
        { "BrowseVideo", "Video Seç" },
        { "ApplyWallpaper", "Duvar Kağıdını Uygula" },
        { "NoVideoSelected", "Henüz video seçilmedi." },
        { "Settings", "Ayarlar" },
        { "Volume", "Ses:" },
        { "ChooseFit", "Sığdırma:" },
        { "LaunchStartup", "Windows açılışında başlat" },
        { "Theme", "Tema:" },
        { "Language", "Dil (Language):" },
        { "UpdateAvailable", "Yeni bir sürüm mevcut!" },
        { "DownloadUpdate", "İndir ve Güncelle" },
        { "Downloading", "Güncelleme indiriliyor... Lütfen bekleyin." },
        { "Restored", "Geri yüklendi:" },
        { "Selected", "Seçildi:" }
    };

    private static readonly Dictionary<string, string> _en = new()
    {
        { "AppTitle", "Nora Wallpaper" },
        { "AppDescription", "Select a video to set as your desktop background." },
        { "BrowseVideo", "Browse Video" },
        { "ApplyWallpaper", "Apply Wallpaper" },
        { "NoVideoSelected", "No video selected." },
        { "Settings", "Settings" },
        { "Volume", "Volume:" },
        { "ChooseFit", "Choose a fit:" },
        { "LaunchStartup", "Launch on Windows Startup" },
        { "Theme", "Theme:" },
        { "Language", "Language:" },
        { "UpdateAvailable", "A new version is available!" },
        { "DownloadUpdate", "Download and Update" },
        { "Downloading", "Downloading update... Please wait." },
        { "Restored", "Restored:" },
        { "Selected", "Selected:" }
    };

    public static string GetString(string key)
    {
        string lang = SettingsService.GetLanguage();
        var dict = lang == "tr" ? _tr : _en;
        
        if (dict.TryGetValue(key, out string value))
            return value;
            
        return key;
    }
}
