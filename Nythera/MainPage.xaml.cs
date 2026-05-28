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

public sealed partial class MainPage : Page
{
    private List<WallpaperWindow> _wallpaperWindows = new List<WallpaperWindow>();
    private Windows.Storage.StorageFile _selectedFile;
    private Services.UpdateService.ReleaseInfo _updateInfo;

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
        DownloadUpdateButton.Content = LocalizationService.GetString("DownloadUpdate");
        
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
            double targetX = offset * 220;
            int zIndex = offset == 0 ? 2 : 1;

            AnimateCoverFlow(pages[i], targetScale, targetX, targetOpacity, zIndex);
            
            pages[i].IsHitTestVisible = (offset == 0);
        }
    }

    private void AnimateCoverFlow(UIElement element, double targetScale, double targetTranslateX, double targetOpacity, int zIndex)
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
        UpdateProgressBar.Visibility = Visibility.Visible;
        UpdateProgressText.Visibility = Visibility.Visible;
        UpdateStatusText.Text = LocalizationService.GetString("Downloading");

        try
        {
            var progress = new Progress<double>(percent =>
            {
                UpdateProgressBar.Value = percent;
                UpdateProgressText.Text = $"{percent:F0}%";
            });

            await UpdateService.DownloadAndInstallUpdateAsync(_updateInfo.DownloadUrl, progress);
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Update failed: {ex.Message}";
            DownloadUpdateButton.IsEnabled = true;
            UpdateProgressBar.Visibility = Visibility.Collapsed;
            UpdateProgressText.Visibility = Visibility.Collapsed;
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

    private void ApplyWallpaper_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFile == null) return;
        
        try
        {
            // Close existing windows
            foreach (var win in _wallpaperWindows)
            {
                win.Close();
            }
            _wallpaperWindows.Clear();

            // Enumerate monitors and create a window for each
            Native.WindowsApi.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Native.WindowsApi.RECT lprcMonitor, IntPtr dwData)
            {
                int x = lprcMonitor.Left;
                int y = lprcMonitor.Top;
                int width = lprcMonitor.Right - lprcMonitor.Left;
                int height = lprcMonitor.Bottom - lprcMonitor.Top;

                var wallpaperWindow = new WallpaperWindow();
                wallpaperWindow.AttachToDesktop(x, y, width, height);
                wallpaperWindow.Activate();
                wallpaperWindow.PlayVideo(_selectedFile);
                
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

                _wallpaperWindows.Add(wallpaperWindow);
                return true;
            }, IntPtr.Zero);

            SettingsService.SaveWallpaperPath(_selectedFile.Path);
            
            StatusText.Text = $"Wallpaper applied: {_selectedFile.Name}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{LocalizationService.GetString("ErrorApplying")} {ex.Message}";
        }
    }

    private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        foreach (var win in _wallpaperWindows)
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
                foreach (var win in _wallpaperWindows)
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

            if (deltaX > 0 && CarouselPager.SelectedPageIndex > 0)
            {
                // Swiped right -> go to previous page
                CarouselPager.SelectedPageIndex -= 1;
            }
            else if (deltaX < 0 && CarouselPager.SelectedPageIndex < CarouselPager.NumberOfPages - 1)
            {
                // Swiped left -> go to next page
                CarouselPager.SelectedPageIndex += 1;
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
