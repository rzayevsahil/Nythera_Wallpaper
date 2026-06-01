using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nythera.Core.Marketplace.Models;

public class MarketplaceItem : INotifyPropertyChanged
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int Downloads { get; set; }

    private string _downloadStateText = Nythera.Services.LocalizationService.GetString("Download");
    public string DownloadStateText
    {
        get => _downloadStateText;
        set
        {
            if (_downloadStateText != value)
            {
                _downloadStateText = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isDownloading = false;
    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            if (_isDownloading != value)
            {
                _isDownloading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    private bool _isDownloaded = false;
    public bool IsDownloaded
    {
        get => _isDownloaded;
        set
        {
            if (_isDownloaded != value)
            {
                _isDownloaded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    public bool CanDownload => !IsDownloading && !IsDownloaded;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
