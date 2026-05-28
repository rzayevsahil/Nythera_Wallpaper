using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nythera.Services;

public class UpdateService
{
    private static string GetAppVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
    }
    private static readonly string CurrentVersion = GetAppVersion();
    private const string GithubRepoOwner = "rzayevsahil";
    private const string GithubRepoName = "Nythera_Wallpaper";
    
    public class ReleaseInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public bool IsUpdateAvailable { get; set; }
    }

    public static async Task<ReleaseInfo> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Nythera-Updater");
            
            string url = $"https://api.github.com/repos/{GithubRepoOwner}/{GithubRepoName}/releases/latest";
            var response = await client.GetStringAsync(url);
            
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            
            string latestVersion = root.GetProperty("tag_name").GetString() ?? "";
            
            // Check if update is available
            bool isUpdateAvailable = !string.IsNullOrEmpty(latestVersion) && latestVersion != CurrentVersion;
            
            string downloadUrl = "";
            if (isUpdateAvailable && root.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
            {
                // Find the first .exe asset
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                        break;
                    }
                }
            }

            return new ReleaseInfo
            {
                Version = latestVersion,
                DownloadUrl = downloadUrl,
                IsUpdateAvailable = isUpdateAvailable && !string.IsNullOrEmpty(downloadUrl)
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
            return new ReleaseInfo { IsUpdateAvailable = false };
        }
    }

    public static async Task DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<double> progress)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Nythera-Updater");

            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1 && progress != null;

            string tempPath = Path.Combine(Path.GetTempPath(), "Nythera_Update.exe");

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalReadBytes = 0;
            int readBytes;

            while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, readBytes);
                totalReadBytes += readBytes;

                if (canReportProgress)
                {
                    progress!.Report((double)totalReadBytes / totalBytes * 100);
                }
            }

            fileStream.Close();

            // Run the downloaded installer silently
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /FORCECLOSEAPPLICATIONS /SP- /AUTORESTART",
                    UseShellExecute = true
                }
            };
            process.Start();

            // Exit the current application to allow overwrite
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update download failed: {ex.Message}");
            throw;
        }
    }
}
