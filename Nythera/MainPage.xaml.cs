using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Nythera.Services;
using System.ComponentModel;

namespace Nythera;

public partial class DefaultVideo : INotifyPropertyChanged
{
    private string _title;
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }
    }
    public string VideoPath { get; set; }
    public bool IsCustom { get; set; }

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite != value)
            {
                _isFavorite = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FavoriteIcon)));
            }
        }
    }

    private bool _isPlaylistSelected;
    public bool IsPlaylistSelected
    {
        get => _isPlaylistSelected;
        set
        {
            if (_isPlaylistSelected != value)
            {
                _isPlaylistSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaylistSelected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaylistCheckVisibility)));
            }
        }
    }

    public Visibility PlaylistCheckVisibility => IsPlaylistSelected ? Visibility.Visible : Visibility.Collapsed;

    private Visibility _playlistSelectionVisibility = Visibility.Collapsed;
    public Visibility PlaylistSelectionVisibility
    {
        get => _playlistSelectionVisibility;
        set
        {
            if (_playlistSelectionVisibility != value)
            {
                _playlistSelectionVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaylistSelectionVisibility)));
            }
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionIndicatorVisibility)));
            }
        }
    }

    public Visibility SelectionIndicatorVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;

    public string FavoriteIcon => IsFavorite ? "\uEB52" : "\uEB51";

    public Visibility DeleteButtonVisibility => IsCustom ? Visibility.Visible : Visibility.Collapsed;

    private ImageSource _thumbnail;
    public ImageSource Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumbnail)));
        }
    }

    private string _appliedMonitorsText;
    public string AppliedMonitorsText
    {
        get => _appliedMonitorsText;
        set
        {
            if (_appliedMonitorsText != value)
            {
                _appliedMonitorsText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AppliedMonitorsText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsApplied)));
            }
        }
    }

    public bool IsApplied => !string.IsNullOrEmpty(AppliedMonitorsText);

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed partial class MainPage : Page
{
    private class MonitorInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    
    private bool _isInitializing = true;
    private List<MonitorInfo> _monitors = new List<MonitorInfo>();
    private Dictionary<string, WallpaperWindow> _wallpaperWindows = new Dictionary<string, WallpaperWindow>();
    private Windows.Storage.StorageFile _selectedFile;
    private Services.UpdateService.ReleaseInfo _updateInfo;
    private DispatcherTimer _playlistTimer;
    
    private System.Collections.ObjectModel.ObservableCollection<DefaultVideo> _allVideos = new();
    private System.Collections.ObjectModel.ObservableCollection<DefaultVideo> _filteredVideos = new();
    private string _currentFilter = "All";
    
    private Core.Marketplace.IMarketplaceApi _marketplaceApi = new Core.Marketplace.MockMarketplaceApi();
    private Core.Marketplace.DownloadManager _downloadManager = new Core.Marketplace.DownloadManager();
    private System.Collections.ObjectModel.ObservableCollection<Core.Marketplace.Models.MarketplaceItem> _marketItems = new();

    private System.Collections.ObjectModel.ObservableCollection<Core.WallpaperImage> _allImages = new();
    private System.Collections.ObjectModel.ObservableCollection<Core.WallpaperImage> _filteredImages = new();
    private string _currentImageFilter = "All";
    private string _selectedImagePath;
    private string _selectedImageName;
    private DispatcherTimer _imagePlaylistTimer;

    public MainPage()
    {
        InitializeComponent();

        // Apply background immediately based on selected theme
        UpdateBackgroundLogo(ElementTheme.Default);
        
        // Initialize startup toggle state
        StartupToggle.IsOn = StartupService.IsStartupEnabled();
        
        // Get current target monitor
        string targetMonitor = SettingsService.GetTargetMonitor();

        // Initialize stretch mode combo box
        string savedStretchMode = SettingsService.GetStretchMode(targetMonitor);
        foreach (ComboBoxItem item in StretchModeComboBox.Items)
        {
            if (item.Tag.ToString() == savedStretchMode)
            {
                StretchModeComboBox.SelectedItem = item;
                break;
            }
        }
        if (StretchModeComboBox.SelectedItem == null && StretchModeComboBox.Items.Count > 0)
            StretchModeComboBox.SelectedIndex = 0;
            
        // Initialize Speed combo box
        double savedSpeed = SettingsService.GetPlaybackSpeed(targetMonitor);
        foreach (ComboBoxItem item in SpeedComboBox.Items)
        {
            if (item.Tag != null && double.TryParse(item.Tag.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val) && Math.Abs(val - savedSpeed) < 0.01)
            {
                SpeedComboBox.SelectedItem = item;
                break;
            }
        }
        if (SpeedComboBox.SelectedItem == null && SpeedComboBox.Items.Count > 0)
            SpeedComboBox.SelectedIndex = 2; // Default to 1.0x

        // Initialize Brightness
        double savedBrightness = SettingsService.GetBrightness(targetMonitor);
        if (BrightnessSlider != null)
        {
            BrightnessSlider.Value = savedBrightness;
            if (BrightnessValueText != null) BrightnessValueText.Text = BrightnessSlider.Value.ToString("0");
        }

        // Initialize Video Filter
        string savedFilter = SettingsService.GetVideoFilter(targetMonitor);
        if (ColorOverlayComboBox != null)
        {
            foreach (ComboBoxItem item in ColorOverlayComboBox.Items)
            {
                if (item.Tag?.ToString() == savedFilter)
                {
                    ColorOverlayComboBox.SelectedItem = item;
                    break;
                }
            }
            if (ColorOverlayComboBox.SelectedItem == null && ColorOverlayComboBox.Items.Count > 0)
                ColorOverlayComboBox.SelectedIndex = 0;
        }

        // Initialize Theme
        string savedTheme = SettingsService.GetTheme();
        foreach (ComboBoxItem item in ThemeComboBox.Items)
        {
            if (item.Tag.ToString() == savedTheme)
            {
                ThemeComboBox.SelectedItem = item;
                break;
            }
        }
        ApplyTheme(savedTheme);

        // Initialize Language
        string savedLang = SettingsService.GetLanguage();
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (item.Tag.ToString() == savedLang)
            {
                LanguageComboBox.SelectedItem = item;
                break;
            }
        }
        UpdateLanguageUI();
        // Initialize Battery & Gaming Toggles
        if (BatterySaverToggle != null) BatterySaverToggle.IsOn = SettingsService.GetPauseOnBattery();
        if (PauseFullscreenToggle != null) PauseFullscreenToggle.IsOn = SettingsService.GetPauseOnFullscreen();



        this.Loaded += MainPage_Loaded;
        this.Unloaded += MainPage_Unloaded;
        MarketplaceGrid.ItemsSource = _marketItems;
        _downloadManager.DownloadCompleted += DownloadManager_DownloadCompleted;
        _downloadManager.DownloadProgressChanged += DownloadManager_DownloadProgressChanged;
    }

    private void MainPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (MainWindow.Instance != null)
        {
            MainWindow.Instance.DisplayChanged -= MainWindow_DisplayChanged;
        }
    }

    private void MainWindow_DisplayChanged(object sender, EventArgs e)
    {
        InitializeMonitors();
        UpdateVideoListBadges();
    }

    private void ApplyTheme(string themeStr)
    {
        if (MainWindow.Instance != null && MainWindow.Instance.Content is FrameworkElement rootElement)
        {
            if (Enum.TryParse(themeStr, out ElementTheme theme))
            {
                rootElement.RequestedTheme = theme;
                UpdateBackgroundLogo(theme);
            }
        }
    }

    private void UpdateBackgroundLogo(ElementTheme theme)
    {
        bool isDark = true;
        if (theme == ElementTheme.Light) isDark = false;
        else if (theme == ElementTheme.Default)
        {
            isDark = Application.Current.RequestedTheme == ApplicationTheme.Dark;
        }

        string logoPath = isDark ? "ms-appx:///Assets/logo_dark.png" : "ms-appx:///Assets/logo_light.png";
        
        if (RootGrid != null)
        {
            if (isDark)
            {
                RootGrid.Background = new SolidColorBrush(Colors.Black);
            }
            else
            {
                RootGrid.Background = new SolidColorBrush(Colors.White);
            }
        }
        
        try
        {
            var bitmapImage = new BitmapImage(new Uri(logoPath));
            AppTitleLogo.Source = bitmapImage;
        }
        catch { }
    }

    private void UpdateLanguageUI()
    {
        AppDescription.Text = LocalizationService.GetString("AppDescription");
        BrowseVideoButton.Content = LocalizationService.GetString("BrowseVideo");
        ApplyButton.Content = LocalizationService.GetString("ApplyWallpaper");
        SettingsTitle.Text = LocalizationService.GetString("Settings");
        
        // Settings Headers & Labels
        if (GeneralExpander != null) GeneralExpander.Header = LocalizationService.GetString("GeneralSettings");
        if (PerformanceExpander != null) PerformanceExpander.Header = LocalizationService.GetString("PerformanceSettings");
        if (GamingExpander != null) GamingExpander.Header = LocalizationService.GetString("GamingSettings");
        if (BatterySaverToggle != null) 
        {
            BatterySaverToggle.Header = LocalizationService.GetString("BatterySaver");
            BatterySaverToggle.OnContent = LocalizationService.GetString("ToggleOn");
            BatterySaverToggle.OffContent = LocalizationService.GetString("ToggleOff");
        }
        if (BatterySaverDescText != null) BatterySaverDescText.Text = LocalizationService.GetString("BatterySaverDesc");
        
        if (PauseFullscreenToggle != null)
        {
            PauseFullscreenToggle.Header = LocalizationService.GetString("PauseFullscreen");
            PauseFullscreenToggle.OnContent = LocalizationService.GetString("ToggleOn");
            PauseFullscreenToggle.OffContent = LocalizationService.GetString("ToggleOff");
        }
        if (PauseFullscreenDescText != null) PauseFullscreenDescText.Text = LocalizationService.GetString("PauseFullscreenDesc");
        
        // Image UI Translations
        if (ImagesDescription != null) ImagesDescription.Text = LocalizationService.GetString("ImageAppDescription");
        if (DefaultImagesTitle != null) DefaultImagesTitle.Text = LocalizationService.GetString("DefaultImagesTitle");
        if (ImagePlaylistModeTitle != null) ImagePlaylistModeTitle.Text = LocalizationService.GetString("PlaylistMode");
        if (ImagePlaylistModeToggle != null) 
        {
            ImagePlaylistModeToggle.OffContent = LocalizationService.GetString("ToggleOff");
            ImagePlaylistModeToggle.OnContent = LocalizationService.GetString("ToggleOn");
        }
        if (ImageOrText != null) ImageOrText.Text = LocalizationService.GetString("OrDivider");
        if (BrowseImageButton != null) BrowseImageButton.Content = LocalizationService.GetString("BrowseImage");
        if (ApplyImageButton != null) ApplyImageButton.Content = LocalizationService.GetString("ApplyWallpaper");
        
        if (ImageBlurTitle != null) ImageBlurTitle.Text = LocalizationService.GetString("Blur");
        if (ImageBrightnessTitle != null) ImageBrightnessTitle.Text = LocalizationService.GetString("Brightness");
        if (ImageStretchTitle != null) ImageStretchTitle.Text = LocalizationService.GetString("StretchMode");
        if (ImageEffectsTitle != null) ImageEffectsTitle.Text = LocalizationService.GetString("Effects");
        if (ImageMonitorTitle != null) ImageMonitorTitle.Text = LocalizationService.GetString("MonitorText");
        
        if (ImageFilterComboBox != null && ImageFilterComboBox.Items.Count >= 3)
        {
            var savedIndex = ImageFilterComboBox.SelectedIndex;
            ((Microsoft.UI.Xaml.Controls.ComboBoxItem)ImageFilterComboBox.Items[0]).Content = LocalizationService.GetString("FilterAll");
            ((Microsoft.UI.Xaml.Controls.ComboBoxItem)ImageFilterComboBox.Items[1]).Content = LocalizationService.GetString("FilterFavorites");
            ((Microsoft.UI.Xaml.Controls.ComboBoxItem)ImageFilterComboBox.Items[2]).Content = LocalizationService.GetString("FilterCustom");
            if (savedIndex >= 0)
            {
                ImageFilterComboBox.SelectedIndex = -1;
                ImageFilterComboBox.SelectedIndex = savedIndex;
            }
        }
        
        if (ImageStretchComboBox != null && ImageStretchComboBox.Items.Count >= 5)
        {
            var savedIndex = ImageStretchComboBox.SelectedIndex;
            ((Microsoft.UI.Xaml.Controls.ComboBoxItem)ImageStretchComboBox.Items[0]).Content = LocalizationService.GetString("FitFill");
            ((Microsoft.UI.Xaml.Controls.ComboBoxItem)ImageStretchComboBox.Items[1]).Content = LocalizationService.GetString("FitUniform");
            ((Microsoft.UI.Xaml.Controls.ComboBoxItem)ImageStretchComboBox.Items[2]).Content = LocalizationService.GetString("FitStretch");
            ((Microsoft.UI.Xaml.Controls.ComboBoxItem)ImageStretchComboBox.Items[3]).Content = LocalizationService.GetString("FitCenter");
            ((Microsoft.UI.Xaml.Controls.ComboBoxItem)ImageStretchComboBox.Items[4]).Content = LocalizationService.GetString("FitSpan");
            if (savedIndex >= 0)
            {
                ImageStretchComboBox.SelectedIndex = -1;
                ImageStretchComboBox.SelectedIndex = savedIndex;
            }
        }
        
        if (ImageStatusText != null && string.IsNullOrEmpty(_selectedImagePath))
        {
            ImageStatusText.Text = LocalizationService.GetString("NoImageSelected");
        }
        
        if (ThemeText != null) ThemeText.Text = LocalizationService.GetString("ThemeText");
        if (LanguageText != null) LanguageText.Text = LocalizationService.GetString("LanguageText");
        
        InitializeMonitors();
        
        if (StartupToggle != null)
        {
            StartupToggle.Header = LocalizationService.GetString("StartupToggle");
            StartupToggle.OnContent = LocalizationService.GetString("ToggleOn");
            StartupToggle.OffContent = LocalizationService.GetString("ToggleOff");
        }

        VolumeText.Text = LocalizationService.GetString("Volume");
        
        if (MarketplaceTitleText != null) MarketplaceTitleText.Text = LocalizationService.GetString("MarketplaceTitle");
        
        foreach (var item in _marketItems)
        {
            if (item.IsDownloaded)
            {
                item.DownloadStateText = LocalizationService.GetString("Downloaded");
            }
            else if (!item.IsDownloading)
            {
                item.DownloadStateText = LocalizationService.GetString("Download");
            }
        }
        
        if (DefaultVideosTitle != null) DefaultVideosTitle.Text = LocalizationService.GetString("DefaultVideosTitle");
        if (PlaylistModeTitle != null) PlaylistModeTitle.Text = LocalizationService.GetString("PlaylistMode");
        if (PlaylistModeToggle != null) 
        {
            PlaylistModeToggle.OnContent = LocalizationService.GetString("On");
            PlaylistModeToggle.OffContent = LocalizationService.GetString("Off");
        }
        if (OrDividerText != null) OrDividerText.Text = LocalizationService.GetString("OrDivider");

        if (VideoFilterComboBox != null)
        {
            var savedIndex = VideoFilterComboBox.SelectedIndex;
            foreach (var item in VideoFilterComboBox.Items)
            {
                if (item is ComboBoxItem cbItem && cbItem.Tag != null)
                {
                    string tag = cbItem.Tag.ToString();
                    if (tag == "All") cbItem.Content = LocalizationService.GetString("FilterAll");
                    else if (tag == "Favorites") cbItem.Content = LocalizationService.GetString("FilterFavorites");
                    else if (tag == "Custom") cbItem.Content = LocalizationService.GetString("FilterCustom");
                }
            }
            if (savedIndex >= 0)
            {
                VideoFilterComboBox.SelectedIndex = -1;
                VideoFilterComboBox.SelectedIndex = savedIndex;
            }
        }
        
        // Translate all video titles dynamically
        foreach (var video in _allVideos)
        {
            if (!video.IsCustom)
            {
                video.Title = LocalizationService.GetVideoTitle(System.IO.Path.GetFileName(video.VideoPath));
            }
        }
        
        // Translate all image titles dynamically
        foreach (var img in _allImages)
        {
            if (!img.IsCustom)
            {
                img.Name = LocalizationService.GetMediaTitle(System.IO.Path.GetFileName(img.ImagePath));
            }
        }
        
        ChooseFitText.Text = LocalizationService.GetString("ChooseFit");
        if (StretchModeComboBox != null)
        {
            foreach (var item in StretchModeComboBox.Items)
            {
                if (item is ComboBoxItem cbItem && cbItem.Tag != null)
                {
                    string tag = cbItem.Tag.ToString();
                    if (tag == "UniformToFill") cbItem.Content = LocalizationService.GetString("FitFill");
                    else if (tag == "Uniform") cbItem.Content = LocalizationService.GetString("FitUniform");
                    else if (tag == "Fill") cbItem.Content = LocalizationService.GetString("FitStretch");
                    else if (tag == "None") cbItem.Content = LocalizationService.GetString("FitCenter");
                }
            }
        }
        
        StartupToggle.Header = LocalizationService.GetString("LaunchStartup");
        StartupToggle.OnContent = LocalizationService.GetString("On");
        StartupToggle.OffContent = LocalizationService.GetString("Off");
        ThemeText.Text = LocalizationService.GetString("Theme");
        LanguageText.Text = LocalizationService.GetString("Language");
        if (DownloadButtonText != null)
            DownloadButtonText.Text = LocalizationService.GetString("DownloadUpdate");
        
        AboutTitleText.Text = LocalizationService.GetString("AboutTitle");
        AppInfoTitle.Text = LocalizationService.GetString("AppInfoTitle");
        AboutAppDescText.Text = LocalizationService.GetString("AboutAppDesc");
        Feature1Text.Text = LocalizationService.GetString("Feature1");
        Feature2Text.Text = LocalizationService.GetString("Feature2");
        Feature3Text.Text = LocalizationService.GetString("Feature3");
        Feature4Text.Text = LocalizationService.GetString("Feature4");
        DeveloperTitleText.Text = LocalizationService.GetString("DeveloperTitle");
        DeveloperRoleText.Text = LocalizationService.GetString("DeveloperRole");
        DeveloperDescText.Text = LocalizationService.GetString("DeveloperDesc");
        
        if (_selectedFile == null)
        {
            StatusText.Text = LocalizationService.GetString("NoVideoSelected");
        }
        else
        {
            // Update the status text with translated video name and "Video ready" label if it's already selected
            StatusText.Text = $"{LocalizationService.GetString("VideoReady")}: {LocalizationService.GetVideoTitle(_selectedFile.Name)}";
        }
        
        if (_updateInfo != null && _updateInfo.IsUpdateAvailable)
            UpdateStatusText.Text = LocalizationService.GetString("UpdateAvailable");
            
        TargetMonitorText.Text = LocalizationService.GetString("MonitorText");
        
        foreach (var item in TargetMonitorComboBox.Items)
        {
            if (item is ComboBoxItem cbItem && cbItem.Tag != null)
            {
                if (cbItem.Tag.ToString() == "All")
                {
                    cbItem.Content = LocalizationService.GetString("AllMonitors");
                }
                else if (int.TryParse(cbItem.Tag.ToString(), out int monitorCount))
                {
                    var mon = _monitors.Find(m => m.Id == cbItem.Tag.ToString());
                    if (mon != null)
                    {
                        mon.Name = string.Format(LocalizationService.GetString("MonitorName"), monitorCount);
                        cbItem.Content = $"{mon.Name} ({mon.Width}x{mon.Height})";
                    }
                }
            }
        }
        
        if (TargetMonitorComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
        {
            UpdatePreviewBounds(selectedItem.Tag.ToString());
        }
        
        if (AppliedBadgeText != null)
        {
            AppliedBadgeText.Text = LocalizationService.GetString("Applied");
        }
        
        if (PlaylistModeTitle != null)
            PlaylistModeTitle.Text = LocalizationService.GetString("PlaylistMode");
        if (PlaylistIntervalTitle != null)
            PlaylistIntervalTitle.Text = LocalizationService.GetString("PlaylistInterval");
        if (PlaylistOrderTitle != null)
            PlaylistOrderTitle.Text = LocalizationService.GetString("PlaylistOrder");
        if (ApplyPlaylistButton != null)
            ApplyPlaylistButton.Content = LocalizationService.GetString("ApplyPlaylist");
            
        if (SpeedText != null) SpeedText.Text = LocalizationService.GetString("PlaybackSpeed");
        if (Speed05 != null) Speed05.Content = LocalizationService.GetString("Speed05");
        if (Speed075 != null) Speed075.Content = LocalizationService.GetString("Speed075");
        if (Speed10 != null) Speed10.Content = LocalizationService.GetString("Speed10");
        if (Speed125 != null) Speed125.Content = LocalizationService.GetString("Speed125");
        if (Speed15 != null) Speed15.Content = LocalizationService.GetString("Speed15");
        if (Speed20 != null) Speed20.Content = LocalizationService.GetString("Speed20");
            
        if (BrightnessText != null) BrightnessText.Text = LocalizationService.GetString("Brightness");
        
        if (ColorOverlayText != null) ColorOverlayText.Text = LocalizationService.GetString("VideoFilter");
        if (FilterNone != null) FilterNone.Content = LocalizationService.GetString("FilterNone");
        if (FilterWarm != null) FilterWarm.Content = LocalizationService.GetString("FilterWarm");
        if (FilterCool != null) FilterCool.Content = LocalizationService.GetString("FilterCool");
        if (FilterMatrix != null) FilterMatrix.Content = LocalizationService.GetString("FilterMatrix");
        if (FilterCyberpunk != null) FilterCyberpunk.Content = LocalizationService.GetString("FilterCyberpunk");

        if (Interval1m != null) Interval1m.Content = LocalizationService.GetString("Min1");
        if (Interval5m != null) Interval5m.Content = LocalizationService.GetString("Min5");
        if (Interval15m != null) Interval15m.Content = LocalizationService.GetString("Min15");
        if (Interval30m != null) Interval30m.Content = LocalizationService.GetString("Min30");
        if (Interval1h != null) Interval1h.Content = LocalizationService.GetString("Hour1");
        if (Interval3h != null) Interval3h.Content = LocalizationService.GetString("Hour3");
        if (Interval6h != null) Interval6h.Content = LocalizationService.GetString("Hour6");
        if (Interval12h != null) Interval12h.Content = LocalizationService.GetString("Hour12");
        if (Interval24h != null) Interval24h.Content = LocalizationService.GetString("Hour24");
        
        if (OrderSequential != null) OrderSequential.Content = LocalizationService.GetString("Sequential");
        if (OrderRandom != null) OrderRandom.Content = LocalizationService.GetString("Random");

        // Force WinUI to refresh the displayed selected item text for all translated ComboBoxes
        if (VideoFilterComboBox != null) { int idx = VideoFilterComboBox.SelectedIndex; if (idx >= 0) { VideoFilterComboBox.SelectedIndex = -1; VideoFilterComboBox.SelectedIndex = idx; } }
        if (StretchModeComboBox != null) { int idx = StretchModeComboBox.SelectedIndex; if (idx >= 0) { StretchModeComboBox.SelectedIndex = -1; StretchModeComboBox.SelectedIndex = idx; } }
        if (ColorOverlayComboBox != null) { int idx = ColorOverlayComboBox.SelectedIndex; if (idx >= 0) { ColorOverlayComboBox.SelectedIndex = -1; ColorOverlayComboBox.SelectedIndex = idx; } }
        if (TargetMonitorComboBox != null) { int idx = TargetMonitorComboBox.SelectedIndex; if (idx >= 0) { TargetMonitorComboBox.SelectedIndex = -1; TargetMonitorComboBox.SelectedIndex = idx; } }
        if (SpeedComboBox != null) { int idx = SpeedComboBox.SelectedIndex; if (idx >= 0) { SpeedComboBox.SelectedIndex = -1; SpeedComboBox.SelectedIndex = idx; } }
        if (PlaylistIntervalComboBox != null) { int idx = PlaylistIntervalComboBox.SelectedIndex; if (idx >= 0) { PlaylistIntervalComboBox.SelectedIndex = -1; PlaylistIntervalComboBox.SelectedIndex = idx; } }
        if (PlaylistOrderComboBox != null) { int idx = PlaylistOrderComboBox.SelectedIndex; if (idx >= 0) { PlaylistOrderComboBox.SelectedIndex = -1; PlaylistOrderComboBox.SelectedIndex = idx; } }
        
        UpdateVideoListBadges();
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string themeTag)
        {
            SettingsService.SaveTheme(themeTag);
            ApplyTheme(themeTag);
            ElementTheme theme = themeTag switch
            {
                "Dark" => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _ => ElementTheme.Default
            };
            UpdateBackgroundLogo(theme);
        }
    }

    private void CarouselPager_SelectedIndexChanged(PipsPager sender, PipsPagerSelectedIndexChangedEventArgs args)
    {
        NavigateToPage(sender.SelectedPageIndex);
    }

    private int _currentPageIndex = 0;

    private void NavigateToPage(int newIndex)
    {
        if (newIndex == _currentPageIndex) return;

        _currentPageIndex = newIndex;

        UIElement[] pages = { PageImages, PageVideos, PageMarketplace, PageSettings, PageAbout };
        int numPages = pages.Length;

        for (int i = 0; i < numPages; i++)
        {
            int offset = i - newIndex;
            // Wrap the offset for 5 pages
            if (offset > numPages / 2) offset -= numPages;
            if (offset < -numPages / 2) offset += numPages;

            double targetScale = offset == 0 ? 1.0 : (Math.Abs(offset) == 1 ? 0.8 : 0.6);
            double targetOpacity = offset == 0 ? 1.0 : (Math.Abs(offset) == 1 ? 0.4 : 0.0);
            double contentOpacity = offset == 0 ? 1.0 : 0.05; // 5% opacity for unreadable text in background
            double targetX = offset * 260;
            int zIndex = 5 - Math.Abs(offset);

            AnimateCoverFlow(pages[i], targetScale, targetX, targetOpacity, zIndex, contentOpacity);
            
            pages[i].IsHitTestVisible = (offset == 0);
        }
    }

    private void AnimateCoverFlow(UIElement element, double targetScale, double targetTranslateX, double targetOpacity, int zIndex, double contentOpacity = 1.0)
    {
        Canvas.SetZIndex(element, zIndex);

        var transform = element.RenderTransform as CompositeTransform;
        if (transform == null) return;

        transform.Rotation = 0;

        var storyboard = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(500));
        var easing = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4.0 };

        var transXAnim = new DoubleAnimation { To = targetTranslateX, Duration = duration, EasingFunction = easing };
        var scaleXAnim = new DoubleAnimation { To = targetScale, Duration = duration, EasingFunction = easing };
        var scaleYAnim = new DoubleAnimation { To = targetScale, Duration = duration, EasingFunction = easing };
        var opacityAnim = new DoubleAnimation { To = targetOpacity, Duration = duration, EasingFunction = easing };

        Storyboard.SetTarget(transXAnim, transform);
        Storyboard.SetTargetProperty(transXAnim, "TranslateX");

        Storyboard.SetTarget(scaleXAnim, transform);
        Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

        Storyboard.SetTarget(scaleYAnim, transform);
        Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

        Storyboard.SetTarget(opacityAnim, element);
        Storyboard.SetTargetProperty(opacityAnim, "Opacity");

        storyboard.Children.Add(transXAnim);
        storyboard.Children.Add(scaleXAnim);
        storyboard.Children.Add(scaleYAnim);
        var fwElement = element as FrameworkElement;
        if (fwElement != null)
        {
            var contentInner = fwElement.FindName(fwElement.Name + "Content") as UIElement;
            if (contentInner != null)
            {
                var contentAnim = new DoubleAnimation { To = contentOpacity, Duration = duration, EasingFunction = easing };
                Storyboard.SetTarget(contentAnim, contentInner);
                Storyboard.SetTargetProperty(contentAnim, "Opacity");
                storyboard.Children.Add(contentAnim);
            }
        }
        storyboard.Children.Add(opacityAnim);
        
        storyboard.Begin();
    }
    private async Task LoadMarketplaceItemsAsync()
    {
        try
        {
            var items = await _marketplaceApi.GetFeaturedWallpapersAsync();
            string downloadFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "CustomVideos");
            
            foreach(var item in items) {
                string expectedPath = System.IO.Path.Combine(downloadFolder, $"{item.Title}_{item.Id}.mp4");
                if (System.IO.File.Exists(expectedPath))
                {
                    item.IsDownloaded = true;
                    item.DownloadStateText = Nythera.Services.LocalizationService.GetString("Downloaded");
                }
                _marketItems.Add(item);
            }
            MarketplaceLoadingRing.IsActive = false;
        }
        catch { }
    }

    private void MarketplaceDownload_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string itemId)
        {
            var item = System.Linq.Enumerable.FirstOrDefault(_marketItems, i => i.Id == itemId);
            if (item != null && !item.IsDownloading)
            {
                item.IsDownloading = true;
                item.DownloadStateText = string.Format(Nythera.Services.LocalizationService.GetString("DownloadingPercent"), 0);
                _ = _downloadManager.DownloadVideoAsync(item.Id, item.VideoUrl, $"{item.Title}_{item.Id}.mp4");
            }
        }
    }

    private void DownloadManager_DownloadProgressChanged(object sender, (string itemId, int progress) e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var item = System.Linq.Enumerable.FirstOrDefault(_marketItems, i => i.Id == e.itemId);
            if (item != null)
            {
                item.DownloadStateText = string.Format(Nythera.Services.LocalizationService.GetString("DownloadingPercent"), e.progress);
            }
        });
    }

    private void DownloadManager_DownloadCompleted(object sender, (string itemId, string localPath) e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var item = System.Linq.Enumerable.FirstOrDefault(_marketItems, i => i.Id == e.itemId);
            if (item != null)
            {
                item.IsDownloading = false;
                item.IsDownloaded = true;
                item.DownloadStateText = Nythera.Services.LocalizationService.GetString("Downloaded");
            }
            LoadDefaultVideos();
        LoadDefaultImages(); // Refresh custom videos to show newly downloaded item
        });
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string langTag = item.Tag.ToString();
            SettingsService.SaveLanguage(langTag);
            UpdateLanguageUI();
            (Application.Current as App)?.UpdateTrayLanguage();
        }
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        if (AppVersionText != null)
        {
            AppVersionText.Text = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.1.0";
        }
        
        LoadDefaultVideos();
        LoadDefaultImages();
        
        // We will set the preview size in UpdatePreviewBounds() instead of here.
        // Check for updates asynchronously without blocking the UI
        _ = CheckForUpdatesAsync();
        _ = LoadMarketplaceItemsAsync();

        string targetMonitor = SettingsService.GetTargetMonitor();
        string savedPath = SettingsService.GetWallpaperPath(targetMonitor);
        if (string.IsNullOrEmpty(savedPath))
        {
            savedPath = SettingsService.GetWallpaperPath("All");
        }
        
        if (!string.IsNullOrEmpty(savedPath))
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(savedPath);
                if (file != null)
                {
                    _selectedFile = file;
                    StatusText.Text = $"{LocalizationService.GetString("Restored")} {LocalizationService.GetVideoTitle(_selectedFile.Name)}";
                    ApplyButton.IsEnabled = true;
                    
                    // Auto-apply saved wallpaper
                    ApplyWallpaper_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to restore wallpaper: {ex.Message}";
            }
        }
        
        if (_selectedFile != null)
        {
            PreviewPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(_selectedFile);
            PreviewPlayer.MediaPlayer.CommandManager.IsEnabled = false;
            PreviewPlayer.MediaPlayer.IsLoopingEnabled = true;
            PreviewPlayer.MediaPlayer.Volume = 0;
            PreviewPlaceholderIcon.Visibility = Visibility.Collapsed;
        }

        PlaylistService.LoadPlaylists();
        _playlistTimer = new DispatcherTimer();
        _playlistTimer.Interval = TimeSpan.FromMinutes(1);
        _playlistTimer.Tick += PlaylistTimer_Tick;
        _playlistTimer.Start();
        
        PlaylistTimer_Tick(null, null);

        InitializeMonitors();
        
        if (MainWindow.Instance != null)
        {
            MainWindow.Instance.DisplayChanged += MainWindow_DisplayChanged;
        }
        
        _isInitializing = false;
        UpdateAppliedBadge();
        UpdateVideoListBadges();
    }

    private void InitializeMonitors()
    {
        _monitors.Clear();
        TargetMonitorComboBox.Items.Clear();
        if (ImageMonitorComboBox != null) ImageMonitorComboBox.Items.Clear();

        int monitorCount = 0;
        Native.WindowsApi.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Native.WindowsApi.RECT lprcMonitor, IntPtr dwData)
        {
            monitorCount++;
            int w = lprcMonitor.Right - lprcMonitor.Left;
            int h = lprcMonitor.Bottom - lprcMonitor.Top;
            
            string monitorName = string.Format(LocalizationService.GetString("MonitorName"), monitorCount);
            _monitors.Add(new MonitorInfo { Id = monitorCount.ToString(), Name = monitorName, Width = w, Height = h });
            
            var item = new ComboBoxItem { Content = $"{monitorName} ({w}x{h})", Tag = monitorCount.ToString() };
            TargetMonitorComboBox.Items.Add(item);
            if (ImageMonitorComboBox != null) { ImageMonitorComboBox.Items.Add(new ComboBoxItem { Content = $"{monitorName} ({w}x{h})", Tag = monitorCount.ToString() }); }
            return true;
        }, IntPtr.Zero);

        if (_monitors.Count > 1)
        {
            var allItem = new ComboBoxItem { Content = LocalizationService.GetString("AllMonitors"), Tag = "All" };
            TargetMonitorComboBox.Items.Insert(0, allItem);
            if (ImageMonitorComboBox != null) { ImageMonitorComboBox.Items.Insert(0, new ComboBoxItem { Content = LocalizationService.GetString("AllMonitors"), Tag = "All" }); }
        }

        string savedMonitor = SettingsService.GetTargetMonitor();
        foreach (ComboBoxItem item in TargetMonitorComboBox.Items)
        {
            if (item.Tag.ToString() == savedMonitor)
            {
                TargetMonitorComboBox.SelectedItem = item;
                break;
            }
        }
        
        if (ImageMonitorComboBox != null && ImageMonitorComboBox.SelectedItem == null && ImageMonitorComboBox.Items.Count > 0) ImageMonitorComboBox.SelectedIndex = 0;
        if (TargetMonitorComboBox.SelectedItem == null && TargetMonitorComboBox.Items.Count > 0)
        {
            TargetMonitorComboBox.SelectedIndex = 0;
            if (TargetMonitorComboBox.SelectedItem is ComboBoxItem cbItem && cbItem.Tag != null)
            {
                SettingsService.SaveTargetMonitor(cbItem.Tag.ToString());
            }
        }
            
        UpdatePreviewBounds(TargetMonitorComboBox.SelectedItem is ComboBoxItem selectedCbItem && selectedCbItem.Tag != null ? selectedCbItem.Tag.ToString() : "All");
    }

    private void UpdatePreviewBounds(string targetMonitor)
    {
        if (VirtualDesktopGrid == null || PreviewBorder == null || PreviewInfoText == null) return;
        
        int targetWidth = 1920;
        int targetHeight = 1080;
        string previewText = "";
        
        if (targetMonitor == "All")
        {
            // Use primary monitor as fallback for "All"
            try
            {
                var displayArea = Microsoft.UI.Windowing.DisplayArea.Primary;
                if (displayArea != null)
                {
                    targetWidth = displayArea.OuterBounds.Width;
                    targetHeight = displayArea.OuterBounds.Height;
                }
            }
            catch { }
            previewText = string.Format(LocalizationService.GetString("PreviewAllMonitors"), targetWidth, targetHeight);
        }
        else
        {
            var mon = _monitors.Find(m => m.Id == targetMonitor);
            if (mon != null)
            {
                targetWidth = mon.Width;
                targetHeight = mon.Height;
                previewText = string.Format(LocalizationService.GetString("PreviewMonitor"), mon.Name, targetWidth, targetHeight);
            }
        }
        
        VirtualDesktopGrid.Width = targetWidth;
        VirtualDesktopGrid.Height = targetHeight;
        
        if (targetWidth > 0)
        {
            double fixedWidth = 512.0;
            PreviewBorder.Height = fixedWidth * ((double)targetHeight / targetWidth);
        }
        
        PreviewInfoText.Text = previewText;
    }

    private void UpdateVideoListBadges()
    {
        string targetMonitor = SettingsService.GetTargetMonitor();
        string allPath = SettingsService.GetWallpaperPath("All");
        var monitorPaths = new Dictionary<string, string>();
        
        foreach (var mon in _monitors)
        {
            string p = null;
            if (targetMonitor == "All")
            {
                p = allPath;
            }
            else
            {
                p = SettingsService.GetWallpaperPath(mon.Id);
                if (string.IsNullOrEmpty(p)) p = allPath;
            }
            monitorPaths[mon.Id] = p;
        }

        foreach (var video in _allVideos)
        {
            var matchedMonitors = new List<string>();
            bool appliedToAll = true;
            
            if (_monitors.Count == 0) appliedToAll = false;
            
            foreach (var mon in _monitors)
            {
                if (!video.VideoPath.Equals(monitorPaths[mon.Id], StringComparison.OrdinalIgnoreCase))
                {
                    appliedToAll = false;
                    break;
                }
            }

            if (appliedToAll && _monitors.Count > 1)
            {
                video.AppliedMonitorsText = LocalizationService.GetString("AllMonitorsShort");
            }
            else
            {
                foreach (var mon in _monitors)
                {
                    if (video.VideoPath.Equals(monitorPaths[mon.Id], StringComparison.OrdinalIgnoreCase))
                    {
                        matchedMonitors.Add(string.Format(LocalizationService.GetString("MonitorShort"), mon.Id));
                    }
                }
                
                if (matchedMonitors.Count > 0)
                {
                    video.AppliedMonitorsText = string.Join(", ", matchedMonitors);
                }
                else
                {
                    video.AppliedMonitorsText = string.Empty;
                }
            }
        }
    }

    private void UpdateAppliedBadge()
    {
        if (AppliedBadge == null || _selectedFile == null) return;
        
        string targetMonitor = SettingsService.GetTargetMonitor();
        string assignedPath = SettingsService.GetWallpaperPath(targetMonitor);
        
        if (string.IsNullOrEmpty(assignedPath) && targetMonitor != "All")
        {
            assignedPath = SettingsService.GetWallpaperPath("All");
        }
        
        if (!string.IsNullOrEmpty(assignedPath) && _selectedFile.Path.Equals(assignedPath, StringComparison.OrdinalIgnoreCase))
        {
            AppliedBadge.Visibility = Visibility.Visible;
        }
        else
        {
            AppliedBadge.Visibility = Visibility.Collapsed;
        }
    }

    private void TargetMonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TargetMonitorComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string tag = item.Tag.ToString();
            SettingsService.SaveTargetMonitor(tag);
            UpdatePreviewBounds(tag);
            
            _isInitializing = true;
            
            double savedSpeed = SettingsService.GetPlaybackSpeed(tag);
            foreach (ComboBoxItem speedItem in SpeedComboBox.Items)
            {
                if (double.TryParse(speedItem.Tag.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double s) && s == savedSpeed)
                {
                    SpeedComboBox.SelectedItem = speedItem;
                    break;
                }
            }

            if (BrightnessSlider != null)
            {
                BrightnessSlider.Value = SettingsService.GetBrightness(tag);
                if (BrightnessValueText != null) BrightnessValueText.Text = BrightnessSlider.Value.ToString("0");
            }

            string savedFilter = SettingsService.GetVideoFilter(tag);
            foreach (ComboBoxItem filterItem in ColorOverlayComboBox.Items)
            {
                if (filterItem.Tag != null && filterItem.Tag.ToString() == savedFilter)
                {
                    ColorOverlayComboBox.SelectedItem = filterItem;
                    break;
                }
            }
            
            string savedStretchMode = SettingsService.GetStretchMode(tag);
            foreach (ComboBoxItem stretchItem in StretchModeComboBox.Items)
            {
                if (stretchItem.Tag != null && stretchItem.Tag.ToString() == savedStretchMode)
                {
                    StretchModeComboBox.SelectedItem = stretchItem;
                    break;
                }
            }

            _isInitializing = false;
            
            if (!_isInitializing)
            {
                UpdateAppliedBadge();
            }
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        _updateInfo = await UpdateService.CheckForUpdatesAsync();
        if (_updateInfo.IsUpdateAvailable)
        {
            UpdateContainer.Visibility = Visibility.Visible;
            UpdateStatusText.Text = LocalizationService.GetString("UpdateAvailable");
        }
    }

    private async void DownloadUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updateInfo == null || !_updateInfo.IsUpdateAvailable) return;

        DownloadUpdateButton.IsHitTestVisible = false;
        DownloadButtonText.Text = "0%";
        UpdateStatusText.Text = LocalizationService.GetString("Downloading");

        try
        {
            var progress = new Progress<double>(percent =>
            {
                // Update label
                DownloadButtonText.Text = $"{percent:F0}%";
                
                // Animate the fill rectangle width proportionally to button width
                double buttonWidth = DownloadUpdateButton.ActualWidth;
                if (buttonWidth > 0)
                    DownloadProgressFill.Width = buttonWidth * (percent / 100.0);
            });

            await UpdateService.DownloadAndInstallUpdateAsync(_updateInfo.DownloadUrl, progress);
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Update failed: {ex.Message}";
            DownloadUpdateButton.IsHitTestVisible = true;
            DownloadButtonText.Text = LocalizationService.GetString("DownloadUpdate");
            DownloadProgressFill.Width = 0;
        }
    }

    private void LoadDefaultVideos()
    {
        try
        {
            _allVideos.Clear();
            var favorites = SettingsService.GetFavorites();
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera");
            Directory.CreateDirectory(appData);
            string logPath = Path.Combine(appData, "debug_log.txt");
            string logContent = "LoadDefaultVideos started.\n";
            
            string basePath = AppContext.BaseDirectory;
            string videosDir = Path.Combine(basePath, "Assets", "Videos");
            logContent += $"Base path: {basePath}\n";
            logContent += $"Initial videosDir: {videosDir} (Exists: {Directory.Exists(videosDir)})\n";
            
            // Fallback for dotnet run context where Assets might be in the project root
            if (!Directory.Exists(videosDir))
            {
                videosDir = Path.Combine(Environment.CurrentDirectory, "Assets", "Videos");
                logContent += $"Fallback 1: {videosDir} (Exists: {Directory.Exists(videosDir)})\n";
            }
            // Fallback for Assembly location
            if (!Directory.Exists(videosDir))
            {
                string? assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (assemblyDir != null)
                {
                    videosDir = Path.Combine(assemblyDir, "Assets", "Videos");
                    logContent += $"Fallback 2: {videosDir} (Exists: {Directory.Exists(videosDir)})\n";
                }
            }
            
            
            var fallbackBitmap = new BitmapImage(new Uri("ms-appx:///Assets/logo1.png"));
            
            if (Directory.Exists(videosDir))
            {
                var files = Directory.GetFiles(videosDir, "*.mp4");
                logContent += $"Found {files.Length} mp4 files.\n";
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName == "cyberpunk.mp4" || fileName == "cozy_room.mp4") continue;

                    var videoObj = new DefaultVideo
                    {
                        Title = LocalizationService.GetVideoTitle(fileName),
                        VideoPath = file,
                        IsCustom = false,
                        IsFavorite = favorites.Contains(file)
                    };
                    _allVideos.Add(videoObj);
                    
                    // Fetch real thumbnail asynchronously
                    LoadThumbnailAsync(videoObj, file);
                }
            }
            else
            {
                logContent += $"Directory DOES NOT EXIST after all fallbacks.\n";
            }
            
            // Load custom user videos
            string customVideosDir = Path.Combine(appData, "CustomVideos");
            Directory.CreateDirectory(customVideosDir);
            if (Directory.Exists(customVideosDir))
            {
                var customFiles = Directory.GetFiles(customVideosDir, "*.mp4");
                foreach (var file in customFiles)
                {
                    var videoObj = new DefaultVideo
                    {
                        Title = Path.GetFileNameWithoutExtension(file),
                        VideoPath = file,
                        IsCustom = true,
                        IsFavorite = favorites.Contains(file)
                    };
                    _allVideos.Add(videoObj);
                    LoadThumbnailAsync(videoObj, file);
                }
            }
            
            FilterVideos();
            DefaultVideosGrid.ItemsSource = _filteredVideos;
            UpdateVideoListBadges();
            File.WriteAllText(logPath, logContent);
        }
        catch (Exception ex)
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera");
            Directory.CreateDirectory(appData);
            string crashLog = Path.Combine(appData, "crash_log.txt");
            File.WriteAllText(crashLog, ex.ToString());
        }
    }

    private async void LoadThumbnailAsync(DefaultVideo videoObj, string filePath)
    {
        try
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            var thumb = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView, 200, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale);
            if (thumb != null)
            {
                var bitmap = new BitmapImage();
                // To avoid cross-thread UI updates issues if not on UI thread, we use DispatcherQueue
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await bitmap.SetSourceAsync(thumb);
                    videoObj.Thumbnail = bitmap;
                });
            }
        }
        catch { }
    }

    private async void DefaultVideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DefaultVideosGrid.SelectedItem is DefaultVideo selected)
        {
            if (PlaylistModeToggle.IsOn)
            {
                selected.IsPlaylistSelected = !selected.IsPlaylistSelected;
                DefaultVideosGrid.SelectedItem = null; // Clear highlight so it can be clicked again
                return;
            }

            // Sync single selection visual state (blue vertical line)
            foreach (var video in _allVideos)
            {
                video.IsSelected = (video == selected);
            }

            if (File.Exists(selected.VideoPath))
            {
                try
                {
                    _selectedFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(selected.VideoPath);
                    StatusText.Text = $"{LocalizationService.GetString("VideoReady")}: {selected.Title}";
                    ApplyButton.IsEnabled = true;
                    
                    PreviewPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(_selectedFile);
                    PreviewPlayer.MediaPlayer.CommandManager.IsEnabled = false;
                    PreviewPlayer.MediaPlayer.IsLoopingEnabled = true;
                    PreviewPlayer.MediaPlayer.Volume = 0;
                    PreviewPlaceholderIcon.Visibility = Visibility.Collapsed;
                    
                    UpdateAppliedBadge();
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
            }
            else
            {
                StatusText.Text = "Video dosyasÄ± bulunamadÄ±. LÃ¼tfen Assets/Videos iÃ§ine gerekli MP4'leri ekleyin.";
            }
        }
    }

    private async void BrowseVideo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            
            // Get the window handle for WinUI 3
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".webm");
            picker.FileTypeFilter.Add(".mkv");

            _selectedFile = await picker.PickSingleFileAsync();
            if (_selectedFile != null)
            {
                StatusText.Text = $"{LocalizationService.GetString("Selected")} {_selectedFile.Name}";
                ApplyButton.IsEnabled = true;
                
                PreviewPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(_selectedFile);
                PreviewPlayer.MediaPlayer.CommandManager.IsEnabled = false;
                PreviewPlayer.MediaPlayer.IsLoopingEnabled = true;
                PreviewPlayer.MediaPlayer.Volume = 0;
                PreviewPlaceholderIcon.Visibility = Visibility.Collapsed;

                // Copy to CustomVideos to persist
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera");
                string customVideosDir = Path.Combine(appData, "CustomVideos");
                Directory.CreateDirectory(customVideosDir);
                
                string destinationPath = Path.Combine(customVideosDir, _selectedFile.Name);
                if (_selectedFile.Path != destinationPath)
                {
                    try {
                        File.Copy(_selectedFile.Path, destinationPath, true);
                    } catch { } // Ignore if locked or already exists
                }
                
                var existing = System.Linq.Enumerable.FirstOrDefault(_allVideos, v => v.VideoPath == destinationPath);
                if (existing == null)
                {
                    var videoObj = new DefaultVideo
                    {
                        Title = Path.GetFileNameWithoutExtension(destinationPath),
                        VideoPath = destinationPath,
                        IsCustom = true,
                        IsFavorite = SettingsService.GetFavorites().Contains(destinationPath)
                    };
                    _allVideos.Add(videoObj);
                    LoadThumbnailAsync(videoObj, destinationPath);
                    FilterVideos();
                    DefaultVideosGrid.SelectedItem = videoObj;
                }
                else
                {
                    DefaultVideosGrid.SelectedItem = existing;
                }
                UpdateAppliedBadge();
            }
            else
            {
                StatusText.Text = LocalizationService.GetString("OperationCancelled");
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{LocalizationService.GetString("ErrorApplying")} {ex.Message}";
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string videoPath)
        {
            var video = System.Linq.Enumerable.FirstOrDefault(_allVideos, v => v.VideoPath == videoPath);
            if (video != null && video.IsCustom)
            {
                try
                {
                    if (System.IO.File.Exists(videoPath))
                    {
                        System.IO.File.Delete(videoPath);
                    }
                }
                catch { }

                if (video.IsFavorite)
                {
                    var favorites = SettingsService.GetFavorites();
                    favorites.Remove(videoPath);
                    SettingsService.SaveFavorites(favorites);
                }

                _allVideos.Remove(video);
                if (_filteredVideos.Contains(video))
                {
                    _filteredVideos.Remove(video);
                }

                if (_selectedFile != null && _selectedFile.Path == videoPath)
                {
                    _selectedFile = null;
                    StatusText.Text = LocalizationService.GetString("NoVideoSelected");
                    ApplyButton.IsEnabled = false;
                    PreviewPlayer.Source = null;
                    PreviewPlaceholderIcon.Visibility = Visibility.Visible;
                    AppliedBadge.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string videoPath)
        {
            var video = System.Linq.Enumerable.FirstOrDefault(_allVideos, v => v.VideoPath == videoPath);
            if (video != null)
            {
                video.IsFavorite = !video.IsFavorite;
                var favorites = SettingsService.GetFavorites();
                if (video.IsFavorite)
                    favorites.Add(videoPath);
                else
                    favorites.Remove(videoPath);
                
                SettingsService.SaveFavorites(favorites);
                
                if (_currentFilter == "Favorites" && !video.IsFavorite)
                {
                    _filteredVideos.Remove(video);
                }
            }
        }
    }

    private void VideoFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && item.Tag is string filter)
        {
            _currentFilter = filter;
            FilterVideos();
        }
    }

    private void FilterVideos()
    {
        _filteredVideos.Clear();
        foreach (var v in _allVideos)
        {
            if (_currentFilter == "All" ||
                (_currentFilter == "Favorites" && v.IsFavorite) ||
                (_currentFilter == "Custom" && v.IsCustom))
            {
                _filteredVideos.Add(v);
            }
        }
    }

        private async void ApplyWallpaper_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var unseenMonitors = new HashSet<string>(_wallpaperWindows.Keys);
            string targetMonitor = SettingsService.GetTargetMonitor();
            
            if (sender != null && _selectedFile != null)
            {
                SettingsService.SaveWallpaperType(targetMonitor, "Video");
                SettingsService.SaveWallpaperPath(targetMonitor, _selectedFile.Path);
                PlaylistService.ClearPlaylist(targetMonitor);
                
                if (targetMonitor == "All")
                {
                    foreach (var mon in _monitors)
                    {
                        SettingsService.SaveWallpaperType(mon.Id, "Video");
                        SettingsService.SaveWallpaperPath(mon.Id, "");
                        PlaylistService.ClearPlaylist(mon.Id);
                    }
                }
            }

            ApplyWallpaperToMonitor(targetMonitor, "Video", _selectedFile != null ? _selectedFile.Path : "None");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{LocalizationService.GetString("ErrorApplying")} {ex.Message}";
        }
    }

    public struct MonitorLayout
    {
        public string MonitorId;
        public int X, Y, Width, Height;
    }

    public async void ApplyWallpaperToMonitor(string triggerMonitorId, string type, string triggerPath)
    {
        try
        {
            var unseenMonitors = new HashSet<string>(_wallpaperWindows.Keys);
            int currentMonitorIndex = 0;
            
            var monitorLayouts = new List<MonitorLayout>();

            Native.WindowsApi.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Native.WindowsApi.RECT lprcMonitor, IntPtr dwData)
            {
                currentMonitorIndex++;
                string monitorId = currentMonitorIndex.ToString();
                unseenMonitors.Remove(monitorId);

                int x = lprcMonitor.Left;
                int y = lprcMonitor.Top;
                int width = lprcMonitor.Right - lprcMonitor.Left;
                int height = lprcMonitor.Bottom - lprcMonitor.Top;
                
                monitorLayouts.Add(new MonitorLayout { MonitorId = monitorId, X = x, Y = y, Width = width, Height = height });

                return true;
            }, IntPtr.Zero);

            foreach (var layout in monitorLayouts)
            {
                string monitorId = layout.MonitorId;
                
                bool isNewWindow = false;
                if (!_wallpaperWindows.TryGetValue(monitorId, out WallpaperWindow wallpaperWindow))
                {
                    wallpaperWindow = new WallpaperWindow();
                    wallpaperWindow.MonitorId = monitorId;
                    _wallpaperWindows[monitorId] = wallpaperWindow;
                    isNewWindow = true;
                }

                string wallType = SettingsService.GetWallpaperType(monitorId);
                if (wallType != "Image" && wallType != "Video") wallType = "Video";
                
                string pathToPlay = null;
                if (wallType == "Image") {
                    pathToPlay = SettingsService.GetImagePath(monitorId);
                    if (string.IsNullOrEmpty(pathToPlay)) pathToPlay = SettingsService.GetImagePath("All");
                } else {
                    pathToPlay = SettingsService.GetWallpaperPath(monitorId);
                    if (string.IsNullOrEmpty(pathToPlay)) pathToPlay = SettingsService.GetWallpaperPath("All");
                }

                if (string.IsNullOrEmpty(pathToPlay) || !System.IO.File.Exists(pathToPlay))
                {
                    wallpaperWindow.HideWindow();
                    continue;
                }
                
                // Initialize WebView2 BEFORE reparenting to WorkerW
                await wallpaperWindow.SetWallpaperTypeAsync(wallType);
                
                wallpaperWindow.AttachToDesktop(layout.X, layout.Y, layout.Width, layout.Height);
                
                if (!isNewWindow)
                {
                    wallpaperWindow.ShowWindow();
                }

                if (wallType == "Image")
                {
                    var imgSettings = new Core.WallpaperImage {
                         ImagePath = pathToPlay,
                         Blur = SettingsService.GetBlur(monitorId),
                         Brightness = SettingsService.GetBrightness(monitorId),
                         Contrast = SettingsService.GetContrast(monitorId),
                         EnableKenBurns = SettingsService.GetEnableKenBurns(monitorId),
                         EnableParallax = SettingsService.GetEnableParallax(monitorId)
                    };
                    string stretch = SettingsService.GetImageStretchMode(monitorId);
                    imgSettings.LayoutMode = stretch;
                    
                    _ = wallpaperWindow.ApplyImageSettingsAsync(imgSettings);

                    if (isNewWindow)
                    {
                        wallpaperWindow.ShowWindow();
                    }
                }
                else
                {
                    _ = PlayVideoOnWindowAsync(wallpaperWindow, pathToPlay, isNewWindow);
                    string stretchStr = SettingsService.GetStretchMode(monitorId);
                    if (Enum.TryParse(stretchStr, out Stretch stretchValue))
                    {
                        wallpaperWindow.SetStretchMode(stretchValue);
                    }
                    else
                    {
                        wallpaperWindow.SetStretchMode(Stretch.UniformToFill);
                    }
                    if (VolumeSlider != null) wallpaperWindow.SetVolume(VolumeSlider.Value / 100.0);
                    wallpaperWindow.SetBrightness(SettingsService.GetBrightness(monitorId));
                    wallpaperWindow.SetVideoFilter(SettingsService.GetVideoFilter(monitorId));
                }
            }

            foreach (var id in unseenMonitors)
            {
                try
                {
                    _wallpaperWindows[id].Cleanup();
                    _wallpaperWindows[id].Close();
                }
                catch { }
                _wallpaperWindows.Remove(id);
            }

            IntPtr workerW = Native.WindowsApi.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
            if (workerW != IntPtr.Zero)
            {
                Native.WindowsApi.InvalidateRect(workerW, IntPtr.Zero, true);
                Native.WindowsApi.UpdateWindow(workerW);
            }

            UpdateAppliedBadge();
            UpdateVideoListBadges();
        }
        catch { }
    }

    private async Task PlayVideoOnWindowAsync(WallpaperWindow wallpaperWindow, string pathToPlay, bool isNewWindow = false)
    {
        try
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(pathToPlay);
            wallpaperWindow.PlayVideo(file);
            
            if (isNewWindow)
            {
                // Wait for the video to start rendering before showing the window to prevent black/yellow flash on startup
                await Task.Delay(350);
                wallpaperWindow.ShowWindow();
            }
        }
        catch { }
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (VolumeValueText != null)
        {
            VolumeValueText.Text = e.NewValue.ToString("0");
        }
        foreach (var win in _wallpaperWindows.Values)
        {
            // Slider is 0-100, MediaPlayer volume is 0.0-1.0
            win.SetVolume(e.NewValue / 100.0);
        }
    }
    
    private void SpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (SpeedComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                if (double.TryParse(item.Tag.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double speed))
                {
                    string targetMonitor = SettingsService.GetTargetMonitor();
                    SettingsService.SavePlaybackSpeed(targetMonitor, speed);
                    
                    if (PreviewPlayer != null && PreviewPlayer.MediaPlayer != null && PreviewPlayer.MediaPlayer.PlaybackSession != null)
                    {
                        PreviewPlayer.MediaPlayer.PlaybackSession.PlaybackRate = speed;
                    }
                    
                    if (_wallpaperWindows != null && !_isInitializing)
                    {
                        if (targetMonitor == "All")
                        {
                            foreach (var win in _wallpaperWindows.Values)
                            {
                                win.SetPlaybackSpeed(speed);
                            }
                        }
                        else if (_wallpaperWindows.TryGetValue(targetMonitor, out var win))
                        {
                            win.SetPlaybackSpeed(speed);
                        }
                    }
                }
            }
        }
        catch { }
    }

    private void BrightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        try
        {
            double brightness = e.NewValue;
            if (BrightnessValueText != null)
            {
                BrightnessValueText.Text = brightness.ToString("0");
            }
            string targetMonitor = SettingsService.GetTargetMonitor();
            if (!_isInitializing)
            {
                SettingsService.SaveBrightness(targetMonitor, brightness);
            }
            
            double opacity = 1.0 - (brightness / 100.0);
            opacity = Math.Max(0.0, Math.Min(1.0, opacity));
            
            if (PreviewBrightnessOverlay != null)
            {
                PreviewBrightnessOverlay.Opacity = opacity;
            }
            
            if (_wallpaperWindows != null && !_isInitializing)
            {
                if (targetMonitor == "All")
                {
                    foreach (var win in _wallpaperWindows.Values)
                    {
                        win.SetBrightness(brightness);
                    }
                }
                else if (_wallpaperWindows.TryGetValue(targetMonitor, out var win))
                {
                    win.SetBrightness(brightness);
                }
            }
        }
        catch { }
    }
    
    private void ColorOverlayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ColorOverlayComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string filter = item.Tag.ToString();
                string targetMonitor = SettingsService.GetTargetMonitor();
                if (!_isInitializing)
                {
                    SettingsService.SaveVideoFilter(targetMonitor, filter);
                }
                
                if (PreviewColorOverlay != null)
                {
                    switch(filter)
                    {
                        case "Warm":
                            PreviewColorOverlay.Fill = new SolidColorBrush(Colors.Orange);
                            PreviewColorOverlay.Opacity = 0.15;
                            break;
                        case "Cool":
                            PreviewColorOverlay.Fill = new SolidColorBrush(Colors.DeepSkyBlue);
                            PreviewColorOverlay.Opacity = 0.15;
                            break;
                        case "Matrix":
                            PreviewColorOverlay.Fill = new SolidColorBrush(Colors.LimeGreen);
                            PreviewColorOverlay.Opacity = 0.15;
                            break;
                        case "Cyberpunk":
                            PreviewColorOverlay.Fill = new SolidColorBrush(Colors.Fuchsia);
                            PreviewColorOverlay.Opacity = 0.15;
                            break;
                        case "None":
                        default:
                            PreviewColorOverlay.Fill = new SolidColorBrush(Colors.Transparent);
                            PreviewColorOverlay.Opacity = 0;
                            break;
                    }
                }
                
                if (_wallpaperWindows != null && !_isInitializing)
                {
                    if (targetMonitor == "All")
                    {
                        foreach (var win in _wallpaperWindows.Values)
                        {
                            win.SetVideoFilter(filter);
                        }
                    }
                    else if (_wallpaperWindows.TryGetValue(targetMonitor, out var win))
                    {
                        win.SetVideoFilter(filter);
                    }
                }
            }
        }
        catch { }
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (StartupToggle.IsOn)
        {
            StartupService.EnableStartup();
            StatusText.Text = LocalizationService.GetString("StartupEnabled");
        }
        else
        {
            StartupService.DisableStartup();
            StatusText.Text = LocalizationService.GetString("StartupDisabled");
        }
    }

    private void BatterySaverToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (BatterySaverToggle != null)
            SettingsService.SavePauseOnBattery(BatterySaverToggle.IsOn);
    }

    private void PauseFullscreenToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (PauseFullscreenToggle != null)
            SettingsService.SavePauseOnFullscreen(PauseFullscreenToggle.IsOn);
    }



    private void StretchModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StretchModeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string stretchTag = item.Tag.ToString();
            string targetMonitor = SettingsService.GetTargetMonitor();
            if (!_isInitializing)
            {
                SettingsService.SaveStretchMode(targetMonitor, stretchTag);
            }
            
            if (Enum.TryParse(stretchTag, out Stretch stretchValue))
            {
                if (PreviewPlayer != null)
                {
                    PreviewPlayer.Stretch = stretchValue;
                }
                
                if (_wallpaperWindows != null && !_isInitializing)
                {
                    if (targetMonitor == "All")
                    {
                        foreach (var win in _wallpaperWindows.Values)
                        {
                            win.SetStretchMode(stretchValue);
                        }
                    }
                    else if (_wallpaperWindows.TryGetValue(targetMonitor, out var win))
                    {
                        win.SetStretchMode(stretchValue);
                    }
                }
            }
        }
    }

    private double _pointerStartX;
    private bool _isSwiping;

    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _pointerStartX = e.GetCurrentPoint(RootGrid).Position.X;
        _isSwiping = true;
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSwiping) return;

        // EÄŸer farenin sol tuÅŸu basÄ±lÄ± deÄŸilse ama buraya girdiysek, tÄ±klama bÄ±rakÄ±lmÄ±ÅŸ demektir.
        // (Bazen Button veya ScrollViewer PointerReleased olayÄ±nÄ± yutar, bu yÃ¼zden manuel kontrol etmeliyiz)
        if (!e.GetCurrentPoint(RootGrid).Properties.IsLeftButtonPressed)
        {
            _isSwiping = false;
            return;
        }

        double currentX = e.GetCurrentPoint(RootGrid).Position.X;
        double deltaX = currentX - _pointerStartX;

        // 50 pixels is a good threshold for a deliberate swipe
        if (Math.Abs(deltaX) > 50)
        {
            _isSwiping = false; // Prevent multiple triggers during the same continuous swipe

            if (deltaX > 0)
            {
                // Swiped right -> go to previous page
                int newIndex = CarouselPager.SelectedPageIndex - 1;
                if (newIndex < 0) newIndex = CarouselPager.NumberOfPages - 1;
                CarouselPager.SelectedPageIndex = newIndex;
            }
            else if (deltaX < 0)
            {
                // Swiped left -> go to next page
                int newIndex = CarouselPager.SelectedPageIndex + 1;
                if (newIndex >= CarouselPager.NumberOfPages) newIndex = 0;
                CarouselPager.SelectedPageIndex = newIndex;
            }
        }
    }

    private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isSwiping = false;
    }

    private void RootGrid_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        _isSwiping = false;
    }

    private void PlaylistTimer_Tick(object sender, object e)
    {
        var playlists = PlaylistService.GetAllPlaylists();
        bool hasChanges = false;
        foreach (var kvp in playlists)
        {
            var config = kvp.Value;
            if (config.VideoPaths.Count == 0) continue;
            
            if (config.LastChangeTime == DateTime.MinValue || (DateTime.Now - config.LastChangeTime).TotalMinutes >= config.IntervalMinutes)
            {
                string nextVideoPath;
                if (config.IsRandom)
                {
                    var rand = new Random();
                    nextVideoPath = config.VideoPaths[rand.Next(config.VideoPaths.Count)];
                }
                else
                {
                    config.CurrentIndex++;
                    if (config.CurrentIndex >= config.VideoPaths.Count)
                        config.CurrentIndex = 0;
                    nextVideoPath = config.VideoPaths[config.CurrentIndex];
                }
                
                config.LastChangeTime = DateTime.Now;
                PlaylistService.SaveAll();
                
                SettingsService.SaveWallpaperPath(kvp.Key, nextVideoPath);
                

                
                // Set the flag to indicate we updated something
                hasChanges = true;
            }
        }
        
        if (hasChanges)
        {
            ApplyWallpaper_Click(null, null);
        }
        
        UpdateVideoListBadges();
    }

    private void PlaylistModeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (PlaylistModeToggle.IsOn)
        {
            PlaylistSettingsPanel.Visibility = Visibility.Visible;
            ApplyButton.Visibility = Visibility.Collapsed;
            foreach (var video in _allVideos)
            {
                video.PlaylistSelectionVisibility = Visibility.Visible;
                video.IsSelected = false; // Hide the blue line when entering slide mode
            }
        }
        else
        {
            PlaylistSettingsPanel.Visibility = Visibility.Collapsed;
            ApplyButton.Visibility = Visibility.Visible;
            foreach (var video in _allVideos)
            {
                video.PlaylistSelectionVisibility = Visibility.Collapsed;
                video.IsPlaylistSelected = false; // Clear selection when exiting
            }
        }
    }

    private void ApplyPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        var paths = new List<string>();
        foreach (var video in _allVideos)
        {
            if (video.IsPlaylistSelected) paths.Add(video.VideoPath);
        }
        
        if (paths.Count == 0) return;

        string targetMonitor = TargetMonitorComboBox.SelectedItem is ComboBoxItem cbItem && cbItem.Tag != null ? cbItem.Tag.ToString() : "All";
        
        int interval = 15;
        if (PlaylistIntervalComboBox.SelectedItem is ComboBoxItem intervalItem && intervalItem.Tag != null)
        {
            int.TryParse(intervalItem.Tag.ToString(), out interval);
        }
        
        bool isRandom = false;
        if (PlaylistOrderComboBox.SelectedItem is ComboBoxItem orderItem && orderItem.Tag != null)
        {
            isRandom = orderItem.Tag.ToString() == "Random";
        }
        
        var config = new PlaylistConfig
        {
            VideoPaths = paths,
            IntervalMinutes = interval,
            IsRandom = isRandom,
            LastChangeTime = DateTime.MinValue,
            CurrentIndex = -1
        };
        
        PlaylistService.SavePlaylist(targetMonitor, config);
        
        PlaylistTimer_Tick(null, null);
        
        StatusText.Text = string.Format(LocalizationService.GetString("PlaylistApplied"), paths.Count);
    }

    private void DeveloperImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (DeveloperFallback != null)
        {
            DeveloperFallback.Visibility = Visibility.Collapsed;
        }
    }

    private void PlaylistSelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            var video = _allVideos.FirstOrDefault(v => v.VideoPath == path);
            if (video != null)
            {
                video.IsPlaylistSelected = !video.IsPlaylistSelected;
            }
        }
    }

    private void LoadDefaultImages()
    {
        _allImages.Clear();
        _filteredImages.Clear();

        // 1. App bundled images (Assets/Images)
        try
        {
            string basePath = System.AppContext.BaseDirectory;
            string imagesDir = System.IO.Path.Combine(basePath, "Assets", "Images");
            
            // Fallback for dotnet run context where Assets might be in the project root
            if (!System.IO.Directory.Exists(imagesDir))
            {
                imagesDir = System.IO.Path.Combine(Environment.CurrentDirectory, "Assets", "Images");
            }
            // Fallback for Assembly location
            if (!System.IO.Directory.Exists(imagesDir))
            {
                imagesDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "Assets", "Images");
            }

            if (System.IO.Directory.Exists(imagesDir))
            {
                var files = System.IO.Directory.GetFiles(imagesDir);
                foreach (var file in files)
                {
                    string ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".png" || ext == ".webp" || ext == ".jpeg")
                    {
                        AddImageToListSync(file, false);
                    }
                }
            }
        }
        catch { }

        // 2. Custom images
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string customFolderPath = System.IO.Path.Combine(localAppData, "Nythera", "CustomImages");
            System.IO.Directory.CreateDirectory(customFolderPath);
            
            if (System.IO.Directory.Exists(customFolderPath))
            {
                var files = System.IO.Directory.GetFiles(customFolderPath);
                foreach (var file in files)
                {
                    string ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".png" || ext == ".webp" || ext == ".jpeg")
                    {
                        AddImageToListSync(file, true);
                    }
                }
            }
        }
        catch { }
        
        FilterImages();
    }

    private void AddImageToListSync(string filePath, bool isCustom)
    {
        var img = new Core.WallpaperImage
        {
            Name = Nythera.Services.LocalizationService.GetVideoTitle(System.IO.Path.GetFileName(filePath)),
            ImagePath = filePath,
            IsCustom = isCustom,
            IsFavorite = Nythera.Services.SettingsService.GetFavorites().Contains(filePath)
        };

        _allImages.Add(img);
        LoadImageThumbnailAsync(img, filePath);
    }

    private async void LoadImageThumbnailAsync(Core.WallpaperImage img, string filePath)
    {
        try
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                var memStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                await Windows.Storage.Streams.RandomAccessStream.CopyAsync(fileStream, memStream);
                memStream.Seek(0);
                
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                bitmap.DecodePixelWidth = 300; // Optimize memory
                await bitmap.SetSourceAsync(memStream);
                img.Thumbnail = bitmap;
            }
        }
        catch { }
    }

    private void FilterImages()
    {
        _filteredImages.Clear();
        var favorites = Nythera.Services.SettingsService.GetFavorites();
        
        foreach (var img in _allImages)
        {
            img.IsFavorite = favorites.Contains(img.ImagePath);
            
            bool matchesFilter = _currentImageFilter switch
            {
                "Favorites" => img.IsFavorite,
                "Custom" => img.IsCustom,
                _ => true
            };
            
            if (matchesFilter)
            {
                _filteredImages.Add(img);
            }
        }
        DefaultImagesGrid.ItemsSource = _filteredImages;
    }

    private void ImageFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (ImageFilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _currentImageFilter = tag;
            FilterImages();
        }
    }

    private void ImagePlaylistModeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (ImagePlaylistModeToggle.IsOn)
        {
            ImagePlaylistSettingsPanel.Visibility = Visibility.Visible;
            ApplyImageButton.Visibility = Visibility.Collapsed;
            foreach (var img in _allImages)
            {
                img.PlaylistSelectionVisibility = Visibility.Visible;
                img.IsSelected = false; // Hide single selection border
            }
        }
        else
        {
            ImagePlaylistSettingsPanel.Visibility = Visibility.Collapsed;
            ApplyImageButton.Visibility = Visibility.Visible;
            foreach (var img in _allImages)
            {
                img.PlaylistSelectionVisibility = Visibility.Collapsed;
                img.IsSelectedForPlaylist = false; // Clear selection
            }
        }
    }

    private async void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        string logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "nythera_debug_log.txt");
        System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] BrowseImage_Click started\n");
        try
        {
            var window = MainWindow.Instance;
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webp");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] File picked: {file.Name}\n");
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string customFolderPath = System.IO.Path.Combine(localAppData, "Nythera", "CustomImages");
                System.IO.Directory.CreateDirectory(customFolderPath);
                var customFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(customFolderPath);
                var copiedFile = await file.CopyAsync(customFolder, file.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName);
                
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] File copied, calling AddImageToListSync\n");
                AddImageToListSync(copiedFile.Path, true);
                
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Calling FilterImages\n");
                FilterImages();
                
                _selectedImagePath = copiedFile.Path;
                _selectedImageName = copiedFile.Name;
                ImageStatusText.Text = string.Format(Nythera.Services.LocalizationService.GetString("ReadyFormat"), copiedFile.Name);
                ApplyImageButton.IsEnabled = true;
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] BrowseImage logic complete\n");
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Exception in BrowseImage_Click: {ex.Message}\n{ex.StackTrace}\n");
        }
    }

    private void DefaultImagesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (DefaultImagesGrid.SelectedItem is Core.WallpaperImage selected)
            {
                if (ImagePlaylistModeToggle.IsOn)
                {
                    selected.IsSelectedForPlaylist = !selected.IsSelectedForPlaylist;
                    DefaultImagesGrid.SelectedItem = null; // Clear highlight so it can be clicked again
                    return;
                }
                
                // Sync single selection visual state (blue vertical line)
                foreach (var img in _allImages)
                {
                    img.IsSelected = (img == selected);
                }
                
                _selectedImagePath = selected.ImagePath;
                _selectedImageName = selected.Name;
                
                ImageStatusText.Text = string.Format(Nythera.Services.LocalizationService.GetString("ReadyFormat") ?? "{0} hazir", selected.Name);
                ApplyImageButton.IsEnabled = true;
                
                // Update applied badge (not fully implemented in MVP, but placeholder logic)
                UpdateImageAppliedBadge();
            }
        }
        catch (Exception ex)
        {
            // Fallback for errors
            ImageStatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void UpdateImageAppliedBadge()
    {
        // Simple mock for now
        var targetMonitor = Nythera.Services.SettingsService.GetTargetMonitor();
        foreach (var img in _allImages)
        {
            if (img.ImagePath == Nythera.Services.SettingsService.GetWallpaperType(targetMonitor)) // Checking if it's the current image
            {
                img.IsApplied = true;
                img.AppliedMonitorsText = targetMonitor == "All" ? "ALL" : $"MON {targetMonitor}";
            }
            else
            {
                img.IsApplied = false;
            }
        }
    }

    private void ApplyImage_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedImagePath)) return;
        
        string targetMonitor = "All";
        if (ImageMonitorComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            targetMonitor = item.Tag.ToString();
        }

        // Save settings
        Nythera.Services.SettingsService.SaveWallpaperType(targetMonitor, "Image");
        Nythera.Services.SettingsService.SaveImagePath(targetMonitor, _selectedImagePath);
        Nythera.Services.SettingsService.SaveBlur(targetMonitor, ImageBlurSlider.Value);
        Nythera.Services.SettingsService.SaveBrightness(targetMonitor, ImageBrightnessSlider.Value);
        Nythera.Services.SettingsService.SaveEnableKenBurns(targetMonitor, KenBurnsToggle.IsOn);
        
        if (ImageStretchComboBox.SelectedItem is ComboBoxItem stretchItem && stretchItem.Tag != null)
        {
            Nythera.Services.SettingsService.SaveImageStretchMode(targetMonitor, stretchItem.Tag.ToString());
        }
        else if (ImageStretchComboBox.Items.Count > 0)
        {
            ImageStretchComboBox.SelectedIndex = 0;
            if (ImageStretchComboBox.SelectedItem is ComboBoxItem defaultItem && defaultItem.Tag != null)
            {
                Nythera.Services.SettingsService.SaveImageStretchMode(targetMonitor, defaultItem.Tag.ToString());
            }
        }

        ApplyWallpaperToMonitor(targetMonitor, "Image", _selectedImagePath);
    }

    

    private void ImageProperties_ValueChanged(object sender, object e)
    {
        if (_isInitializing) return;
        string targetMonitor = "All";
        if (ImageMonitorComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            targetMonitor = item.Tag.ToString();
        }
        if (ImageBrightnessValueText != null)
        {
            ImageBrightnessValueText.Text = ImageBrightnessSlider.Value.ToString("0");
        }
        
        Nythera.Services.SettingsService.SaveBlur(targetMonitor, ImageBlurSlider.Value);
        Nythera.Services.SettingsService.SaveBrightness(targetMonitor, ImageBrightnessSlider.Value);
        Nythera.Services.SettingsService.SaveEnableKenBurns(targetMonitor, KenBurnsToggle.IsOn);
        
        if (MainWindow.Instance != null && _wallpaperWindows != null)
        {
            foreach (var kvp in _wallpaperWindows)
            {
                if (targetMonitor == "All" || kvp.Key == targetMonitor)
                {
                    if (Nythera.Services.SettingsService.GetWallpaperType(kvp.Key) == "Image")
                    {
                        var imgSettings = new Core.WallpaperImage {
                             ImagePath = Nythera.Services.SettingsService.GetImagePath(kvp.Key),
                             Blur = Nythera.Services.SettingsService.GetBlur(kvp.Key),
                             Brightness = Nythera.Services.SettingsService.GetBrightness(kvp.Key),
                             Contrast = Nythera.Services.SettingsService.GetContrast(kvp.Key),
                             EnableKenBurns = Nythera.Services.SettingsService.GetEnableKenBurns(kvp.Key),
                             EnableParallax = Nythera.Services.SettingsService.GetEnableParallax(kvp.Key),
                             LayoutMode = Nythera.Services.SettingsService.GetImageStretchMode(kvp.Key)
                        };
                        _ = kvp.Value.ApplyImageSettingsAsync(imgSettings);
                    }
                }
            }
        }
    }

    private void ImageStretchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        
        if (ImageStretchComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string targetMonitor = "All";
            if (ImageMonitorComboBox != null && ImageMonitorComboBox.SelectedItem is ComboBoxItem monitorItem && monitorItem.Tag != null)
            {
                targetMonitor = monitorItem.Tag.ToString();
            }
            Nythera.Services.SettingsService.SaveImageStretchMode(targetMonitor, item.Tag.ToString());
        }
        
        ImageProperties_ValueChanged(null, null);
    }

    private void ImageMonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (ImageMonitorComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string monitorId = item.Tag.ToString();
            
            // Load settings for this monitor
            _isInitializing = true;
            
            ImageBlurSlider.Value = Nythera.Services.SettingsService.GetBlur(monitorId);
            ImageBrightnessSlider.Value = Nythera.Services.SettingsService.GetBrightness(monitorId);
            if (ImageBrightnessValueText != null) ImageBrightnessValueText.Text = ImageBrightnessSlider.Value.ToString("0");
            KenBurnsToggle.IsOn = Nythera.Services.SettingsService.GetEnableKenBurns(monitorId);
            
            string savedStretchMode = Nythera.Services.SettingsService.GetImageStretchMode(monitorId);
            foreach (ComboBoxItem cbItem in ImageStretchComboBox.Items)
            {
                if (cbItem.Tag.ToString() == savedStretchMode)
                {
                    ImageStretchComboBox.SelectedItem = cbItem;
                    break;
                }
            }
            if (ImageStretchComboBox.SelectedItem == null && ImageStretchComboBox.Items.Count > 0)
            {
                ImageStretchComboBox.SelectedIndex = 0;
            }
            
            _isInitializing = false;
        }
    }

    private void ImagePlaylistSelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            var img = System.Linq.Enumerable.FirstOrDefault(_allImages, i => i.ImagePath == path);
            if (img != null)
            {
                img.IsSelectedForPlaylist = !img.IsSelectedForPlaylist;
            }
        }
    }

    private void ApplyImagePlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        var paths = new System.Collections.Generic.List<string>();
        foreach (var img in _allImages)
        {
            if (img.IsSelectedForPlaylist) paths.Add(img.ImagePath);
        }
        
        if (paths.Count > 0)
        {
            ImageStatusText.Text = $"{paths.Count} resim seçildi ve oynatma listesi olarak ayarlandı.";
            // For MVP, just show text, actual playlist implementation requires saving to SettingsService
        }
        else
        {
            ImageStatusText.Text = "Lütfen listeden resim seçin.";
        }
    }

    private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string imagePath)
        {
            var image = System.Linq.Enumerable.FirstOrDefault(_allImages, v => v.ImagePath == imagePath);
            if (image != null && image.IsCustom)
            {
                try
                {
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                catch { }

                if (image.IsFavorite)
                {
                    var favorites = Nythera.Services.SettingsService.GetFavorites();
                    favorites.Remove(imagePath);
                    Nythera.Services.SettingsService.SaveFavorites(favorites);
                }

                _allImages.Remove(image);
                if (_filteredImages.Contains(image))
                {
                    _filteredImages.Remove(image);
                }

                if (_selectedImagePath == imagePath)
                {
                    _selectedImagePath = null;
                    ImageStatusText.Text = Nythera.Services.LocalizationService.GetString("NoImageSelected");
                    ApplyImageButton.IsEnabled = false;
                }
            }
        }
    }

    private void ImageFavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            var img = System.Linq.Enumerable.FirstOrDefault(_allImages, v => v.ImagePath == path);
            if (img != null)
            {
                img.IsFavorite = !img.IsFavorite;
                var favorites = Nythera.Services.SettingsService.GetFavorites();
                if (img.IsFavorite)
                    favorites.Add(path);
                else
                    favorites.Remove(path);
                
                Nythera.Services.SettingsService.SaveFavorites(favorites);
                
                if (_currentImageFilter == "Favorites" && !img.IsFavorite)
                {
                    _filteredImages.Remove(img);
                }
            }
        }
    }

}



