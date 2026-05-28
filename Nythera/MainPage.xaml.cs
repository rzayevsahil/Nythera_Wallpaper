using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Storage.Pickers;
using Nythera.Services;

namespace Nythera;

public class DefaultVideo : System.ComponentModel.INotifyPropertyChanged
{
    public string Title { get; set; }
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
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsFavorite)));
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(FavoriteIcon)));
            }
        }
    }

    public string FavoriteIcon => IsFavorite ? "\uEB52" : "\uEB51";

    private Microsoft.UI.Xaml.Media.ImageSource _thumbnail;
    public Microsoft.UI.Xaml.Media.ImageSource Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Thumbnail)));
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
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(AppliedMonitorsText)));
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsApplied)));
            }
        }
    }

    public bool IsApplied => !string.IsNullOrEmpty(AppliedMonitorsText);

    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
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
    
    private System.Collections.ObjectModel.ObservableCollection<DefaultVideo> _allVideos = new();
    private System.Collections.ObjectModel.ObservableCollection<DefaultVideo> _filteredVideos = new();
    private string _currentFilter = "All";

    public MainPage()
    {
        InitializeComponent();
        
        // Initialize startup toggle state
        StartupToggle.IsOn = StartupService.IsStartupEnabled();
        
        // Initialize stretch mode combo box
        string savedStretchMode = SettingsService.GetStretchMode();
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
        
        this.Loaded += MainPage_Loaded;
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
                RootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
            }
            else
            {
                RootGrid.Background = null;
            }
        }
        
        try
        {
            var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(logoPath));
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
        VolumeText.Text = LocalizationService.GetString("Volume");
        ChooseFitText.Text = LocalizationService.GetString("ChooseFit");
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
            StatusText.Text = LocalizationService.GetString("NoVideoSelected");
        
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

        UIElement[] pages = { Page1, Page2, Page3 };
        int numPages = pages.Length;

        for (int i = 0; i < numPages; i++)
        {
            int offset = i - newIndex;
            // Wrap the offset so it is always -1, 0, or 1 for 3 pages
            if (offset > 1) offset -= numPages;
            if (offset < -1) offset += numPages;

            double targetScale = offset == 0 ? 1.0 : 0.8;
            double targetOpacity = offset == 0 ? 1.0 : 0.4;
            double blurOpacity = offset == 0 ? 0.0 : 0.6;
            double targetX = offset * 220;
            int zIndex = offset == 0 ? 2 : 1;

            AnimateCoverFlow(pages[i], targetScale, targetX, targetOpacity, zIndex, blurOpacity);
            
            pages[i].IsHitTestVisible = (offset == 0);
        }
    }

    private void AnimateCoverFlow(UIElement element, double targetScale, double targetTranslateX, double targetOpacity, int zIndex, double blurOpacity = 0.0)
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
            var blurOverlay = (Border)fwElement.FindName(fwElement.Name + "BlurOverlay");
            if (blurOverlay != null)
            {
                var blurAnim = new DoubleAnimation { To = blurOpacity, Duration = duration, EasingFunction = easing };
                Storyboard.SetTarget(blurAnim, blurOverlay);
                Storyboard.SetTargetProperty(blurAnim, "Opacity");
                storyboard.Children.Add(blurAnim);
            }
        }
        storyboard.Children.Add(opacityAnim);
        
        storyboard.Begin();
    }
    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string langTag = item.Tag.ToString();
            SettingsService.SaveLanguage(langTag);
            UpdateLanguageUI();
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
        
        // We will set the preview size in UpdatePreviewBounds() instead of here.
        // Check for updates asynchronously without blocking the UI
        _ = CheckForUpdatesAsync();

        string savedPath = SettingsService.GetWallpaperPath();
        if (!string.IsNullOrEmpty(savedPath))
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(savedPath);
                if (file != null)
                {
                    _selectedFile = file;
                    StatusText.Text = $"{LocalizationService.GetString("Restored")} {_selectedFile.Name}";
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
            PreviewPlayer.MediaPlayer.IsLoopingEnabled = true;
            PreviewPlayer.MediaPlayer.Volume = 0;
            PreviewPlaceholderIcon.Visibility = Visibility.Collapsed;
        }

        InitializeMonitors();
        
        _isInitializing = false;
        UpdateAppliedBadge();
    }

    private void InitializeMonitors()
    {
        _monitors.Clear();
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
            return true;
        }, IntPtr.Zero);

        string savedMonitor = SettingsService.GetTargetMonitor();
        foreach (ComboBoxItem item in TargetMonitorComboBox.Items)
        {
            if (item.Tag.ToString() == savedMonitor)
            {
                TargetMonitorComboBox.SelectedItem = item;
                break;
            }
        }
        if (TargetMonitorComboBox.SelectedItem == null && TargetMonitorComboBox.Items.Count > 0)
            TargetMonitorComboBox.SelectedIndex = 0;
            
        UpdatePreviewBounds(TargetMonitorComboBox.SelectedItem is ComboBoxItem cbItem && cbItem.Tag != null ? cbItem.Tag.ToString() : "All");
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
            double fixedWidth = 432.0;
            PreviewBorder.Height = fixedWidth * ((double)targetHeight / targetWidth);
        }
        
        PreviewInfoText.Text = previewText;
    }

    private void UpdateVideoListBadges()
    {
        string allPath = SettingsService.GetWallpaperPath("All");
        var monitorPaths = new Dictionary<string, string>();
        
        foreach (var mon in _monitors)
        {
            string p = SettingsService.GetWallpaperPath(mon.Id);
            if (string.IsNullOrEmpty(p)) p = allPath;
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

            if (appliedToAll && _monitors.Count > 0)
            {
                video.AppliedMonitorsText = "Tümü";
            }
            else
            {
                foreach (var mon in _monitors)
                {
                    if (video.VideoPath.Equals(monitorPaths[mon.Id], StringComparison.OrdinalIgnoreCase))
                    {
                        matchedMonitors.Add($"M{mon.Id}");
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

    private async void TargetMonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TargetMonitorComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string tag = item.Tag.ToString();
            SettingsService.SaveTargetMonitor(tag);
            UpdatePreviewBounds(tag);
            
            if (!_isInitializing)
            {
                string assignedPath = SettingsService.GetWallpaperPath(tag);
                if (string.IsNullOrEmpty(assignedPath) || !System.IO.File.Exists(assignedPath))
                {
                    assignedPath = SettingsService.GetWallpaperPath("All");
                }

                if (!string.IsNullOrEmpty(assignedPath) && System.IO.File.Exists(assignedPath))
                {
                    try
                    {
                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(assignedPath);
                        _selectedFile = file;
                        
                        if (PreviewPlayer != null)
                        {
                            PreviewPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(_selectedFile);
                            PreviewPlayer.MediaPlayer.IsLoopingEnabled = true;
                            PreviewPlayer.MediaPlayer.Volume = 0;
                            PreviewPlaceholderIcon.Visibility = Visibility.Collapsed;
                        }
                        
                        var existing = System.Linq.Enumerable.FirstOrDefault(_allVideos, v => v.VideoPath == assignedPath);
                        if (existing != null && DefaultVideosGrid != null)
                        {
                            DefaultVideosGrid.SelectedItem = existing;
                        }
                    }
                    catch { }
                }
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

        DownloadUpdateButton.IsEnabled = false;
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
            DownloadUpdateButton.IsEnabled = true;
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
            
            
            var fallbackBitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/logo1.png"));
            
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
                        Title = Path.GetFileNameWithoutExtension(file),
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
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
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
            if (File.Exists(selected.VideoPath))
            {
                try
                {
                    _selectedFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(selected.VideoPath);
                    StatusText.Text = $"{LocalizationService.GetString("VideoReady")}: {selected.Title}";
                    ApplyButton.IsEnabled = true;
                    
                    PreviewPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(_selectedFile);
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
                StatusText.Text = "Video dosyası bulunamadı. Lütfen Assets/Videos içine gerekli MP4'leri ekleyin.";
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
            // We will invalidate WorkerW at the end of the method after all windows are repositioned
            
            // Mark all existing windows as 'unseen' this pass so we can clean up disconnected ones
            var unseenMonitors = new HashSet<string>(_wallpaperWindows.Keys);

            string targetMonitor = SettingsService.GetTargetMonitor();
            
            // If user explicitly clicked Apply, save the selected video to the target monitor(s)
            if (sender != null && _selectedFile != null)
            {
                SettingsService.SaveWallpaperPath(targetMonitor, _selectedFile.Path);
            }

            int currentMonitorIndex = 0;
            string debugFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "monitor_debug.txt");
            System.IO.File.AppendAllText(debugFile, $"\n--- ApplyWallpaper_Click called. TargetMonitor: {targetMonitor} ---\n");

            // Enumerate monitors and create a window for each
            Native.WindowsApi.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Native.WindowsApi.RECT lprcMonitor, IntPtr dwData)
            {
                currentMonitorIndex++;
                System.IO.File.AppendAllText(debugFile, $"Found Monitor {currentMonitorIndex} at ({lprcMonitor.Left}, {lprcMonitor.Top}) with size {lprcMonitor.Right - lprcMonitor.Left}x{lprcMonitor.Bottom - lprcMonitor.Top}\n");

                string monitorId = currentMonitorIndex.ToString();
                unseenMonitors.Remove(monitorId);

                if (!_wallpaperWindows.TryGetValue(monitorId, out WallpaperWindow wallpaperWindow))
                {
                    wallpaperWindow = new WallpaperWindow();
                    _wallpaperWindows[monitorId] = wallpaperWindow;
                }

                // Determine which video to play on this monitor
                string pathToPlay = null;
                if (targetMonitor == "All")
                {
                    pathToPlay = SettingsService.GetWallpaperPath("All");
                }
                else
                {
                    pathToPlay = SettingsService.GetWallpaperPath(monitorId);
                    if (string.IsNullOrEmpty(pathToPlay))
                    {
                        // Fallback to "All" if no specific wallpaper is set for this monitor
                        pathToPlay = SettingsService.GetWallpaperPath("All");
                    }
                }

                if (string.IsNullOrEmpty(pathToPlay) || !System.IO.File.Exists(pathToPlay))
                {
                    System.IO.File.AppendAllText(debugFile, $" -> Hiding Monitor {currentMonitorIndex} because no valid wallpaper path was found.\n");
                    wallpaperWindow.HideWindow();
                    return true;
                }
                
                System.IO.File.AppendAllText(debugFile, $" -> Applying {pathToPlay} to Monitor {currentMonitorIndex}!\n");
                int x = lprcMonitor.Left;
                int y = lprcMonitor.Top;
                int width = lprcMonitor.Right - lprcMonitor.Left;
                int height = lprcMonitor.Bottom - lprcMonitor.Top;

                wallpaperWindow.AttachToDesktop(x, y, width, height);
                wallpaperWindow.ShowWindow();
                
                // We use async here, but EnumDisplayMonitors callback is synchronous.
                // PlayVideo async execution inside synchronous callback is fine since it doesn't await here.
                _ = PlayVideoOnWindowAsync(wallpaperWindow, pathToPlay);
                
                // Apply saved stretch mode
                if (StretchModeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    if (Enum.TryParse(item.Tag.ToString(), out Microsoft.UI.Xaml.Media.Stretch stretchValue))
                    {
                        wallpaperWindow.SetStretchMode(stretchValue);
                    }
                }

                // Apply volume
                wallpaperWindow.SetVolume(VolumeSlider.Value / 100.0);

                return true;
            }, IntPtr.Zero);

            // Cleanup windows for monitors that no longer exist
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

            // Force WorkerW to redraw its background (clear ghost frames) AFTER all windows are adjusted
            IntPtr workerW = Native.WindowsApi.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
            if (workerW != IntPtr.Zero)
            {
                // Force a full redraw of the desktop background behind the windows
                Native.WindowsApi.InvalidateRect(workerW, IntPtr.Zero, true);
                Native.WindowsApi.UpdateWindow(workerW);
            }

            if (sender != null && _selectedFile != null)
            {
                StatusText.Text = $"Wallpaper applied: {_selectedFile.Name}";
            }
            
            UpdateAppliedBadge();
            UpdateVideoListBadges();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{LocalizationService.GetString("ErrorApplying")} {ex.Message}";
        }
    }

    private async Task PlayVideoOnWindowAsync(WallpaperWindow wallpaperWindow, string pathToPlay)
    {
        try
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(pathToPlay);
            wallpaperWindow.PlayVideo(file);
        }
        catch { }
    }

    private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        foreach (var win in _wallpaperWindows.Values)
        {
            // Slider is 0-100, MediaPlayer volume is 0.0-1.0
            win.SetVolume(e.NewValue / 100.0);
        }
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

    private void StretchModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StretchModeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string stretchTag = item.Tag.ToString();
            SettingsService.SaveStretchMode(stretchTag);
            
            if (Enum.TryParse(stretchTag, out Microsoft.UI.Xaml.Media.Stretch stretchValue))
            {
                if (PreviewPlayer != null)
                {
                    PreviewPlayer.Stretch = stretchValue;
                }
                
                foreach (var win in _wallpaperWindows.Values)
                {
                    win.SetStretchMode(stretchValue);
                }
            }
        }
    }

    private double _pointerStartX;
    private bool _isSwiping;

    private void RootGrid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _pointerStartX = e.GetCurrentPoint(RootGrid).Position.X;
        _isSwiping = true;
    }

    private void RootGrid_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isSwiping) return;

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

    private void RootGrid_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isSwiping = false;
    }

    private void RootGrid_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isSwiping = false;
    }
}
