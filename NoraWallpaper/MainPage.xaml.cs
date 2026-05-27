using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

using NoraWallpaper.Services;

namespace NoraWallpaper;

public sealed partial class MainPage : Page
{
    private List<WallpaperWindow> _wallpaperWindows = new List<WallpaperWindow>();
    private Windows.Storage.StorageFile _selectedFile;

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
        {
            StretchModeComboBox.SelectedIndex = 0; // Default to first item
        }
        
        this.Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        string savedPath = SettingsService.GetWallpaperPath();
        if (!string.IsNullOrEmpty(savedPath))
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(savedPath);
                if (file != null)
                {
                    _selectedFile = file;
                    StatusText.Text = $"Restored: {_selectedFile.Name}";
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
                StatusText.Text = $"Selected: {_selectedFile.Name} - Ready to apply.";
                ApplyButton.IsEnabled = true;
            }
            else
            {
                StatusText.Text = "Operation cancelled.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.ToString()}";
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
                wallpaperWindow.Activate();
                wallpaperWindow.AttachToDesktop(x, y, width, height);
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
            StatusText.Text = $"Error applying wallpaper: {ex.ToString()}";
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
            StatusText.Text = "Launch on startup enabled.";
        }
        else
        {
            StartupService.DisableStartup();
            StatusText.Text = "Launch on startup disabled.";
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
}
