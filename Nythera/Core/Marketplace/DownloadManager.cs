using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nythera.Core.Marketplace;

public class DownloadManager
{
    private readonly string _downloadFolder;
    private readonly HttpClient _httpClient;

    public event EventHandler<(string itemId, int progress)>? DownloadProgressChanged;
    public event EventHandler<(string itemId, string localPath)>? DownloadCompleted;
    public event EventHandler<(string itemId, string errorMessage)>? DownloadFailed;

    public DownloadManager()
    {
        _downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "CustomVideos");
        if (!Directory.Exists(_downloadFolder))
        {
            Directory.CreateDirectory(_downloadFolder);
        }
        _httpClient = new HttpClient();
    }

    public async Task DownloadVideoAsync(string itemId, string url, string fileName)
    {
        try
        {
            string localPath = Path.Combine(_downloadFolder, fileName);
            
            if (File.Exists(localPath))
            {
                DownloadCompleted?.Invoke(this, (itemId, localPath));
                return;
            }

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            int lastProgress = 0;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (totalBytes.HasValue)
                {
                    int currentProgress = (int)((double)totalRead / totalBytes.Value * 100);
                    if (currentProgress > lastProgress)
                    {
                        lastProgress = currentProgress;
                        DownloadProgressChanged?.Invoke(this, (itemId, currentProgress));
                    }
                }
            }

            DownloadCompleted?.Invoke(this, (itemId, localPath));
        }
        catch (Exception ex)
        {
            DownloadFailed?.Invoke(this, (itemId, ex.Message));
        }
    }
}
