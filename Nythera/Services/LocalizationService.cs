using System.Collections.Generic;

namespace Nythera.Services;

public static class LocalizationService
{
    private static readonly Dictionary<string, string> _tr = new()
    {
        { "AppTitle", "Nythera" },
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
        { "Selected", "Seçildi:" },
        { "StartupEnabled", "Windows başlangıcında başlatılacak." },
        { "StartupDisabled", "Windows başlangıcında başlatılmayacak." },
        { "On", "Açık" },
        { "Off", "Kapalı" },
        { "OperationCancelled", "İşlem iptal edildi." },
        { "ErrorApplying", "Hata:" },
        { "AboutTitle", "Hakkında" },
        { "AppInfoTitle", "Uygulama Bilgileri" },
        { "AboutAppDesc", "Sisteminizi yormadan masaüstünüzü yüksek kaliteli ve hareketli videolarla canlandıran, performans odaklı yeni nesil duvar kağıdı motoru." },
        { "Feature1", "• Düşük RAM ve CPU kullanımı" },
        { "Feature2", "• Akıllı karanlık/aydınlık tema desteği" },
        { "Feature3", "• Otomatik arka plan güncelleme sistemi" },
        { "Feature4", "• Tam ekran oyun/uygulama algılama ile otomatik duraklatma" },
        { "DeveloperTitle", "Geliştirici" },
        { "DeveloperRole", "Software Developer" },
        { "DeveloperDesc", "Modern masaüstü ve web uygulamaları tasarlayan tutkulu bir yazılım geliştirici." },
        { "GithubButton", "GitHub Profilim" },
        { "LinkedInButton", "LinkedIn" },
        { "EmailButton", "E-Posta" }
    };

    private static readonly Dictionary<string, string> _en = new()
    {
        { "AppTitle", "Nythera" },
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
        { "Selected", "Selected:" },
        { "StartupEnabled", "Will launch on Windows startup." },
        { "StartupDisabled", "Will not launch on Windows startup." },
        { "On", "On" },
        { "Off", "Off" },
        { "OperationCancelled", "Operation cancelled." },
        { "ErrorApplying", "Error applying:" },
        { "AboutTitle", "About" },
        { "AppInfoTitle", "Application Info" },
        { "AboutAppDesc", "A performance-focused next-generation wallpaper engine that brings your desktop to life with high-quality animated videos without straining your system." },
        { "Feature1", "• Low RAM and CPU usage" },
        { "Feature2", "• Smart dark/light theme support" },
        { "Feature3", "• Automatic background update system" },
        { "Feature4", "• Auto-pause with full-screen game/app detection" },
        { "DeveloperTitle", "Developer" },
        { "DeveloperRole", "Software Developer" },
        { "DeveloperDesc", "A passionate software developer designing modern desktop and web applications." },
        { "GithubButton", "My GitHub Profile" },
        { "LinkedInButton", "LinkedIn" },
        { "EmailButton", "Email" }
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
