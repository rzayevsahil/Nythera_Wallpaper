using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using System;
using Nythera.Core.WallpaperEngine;
using Nythera.Native;

namespace Nythera;

public sealed partial class WallpaperWindow : Window
{
    private MediaPlayer _mediaPlayer;

    private Core.PerformanceManager.FullScreenDetector _fullScreenDetector;

    public WallpaperWindow()
    {
        InitializeComponent();
        
        _mediaPlayer = new MediaPlayer();
        _mediaPlayer.IsLoopingEnabled = true;
        _mediaPlayer.Volume = 0.0; // Mute by default

        BackgroundPlayer.SetMediaPlayer(_mediaPlayer);

        _fullScreenDetector = new Core.PerformanceManager.FullScreenDetector();
        _fullScreenDetector.FullScreenStateChanged += FullScreenDetector_StateChanged;
        _fullScreenDetector.Start();

        App.PerformanceManager.PerformanceModeChanged += PerformanceManager_StateChanged;


        if (App.AudioController != null)
        {
            App.AudioController.NeonFlashTriggered += AudioController_NeonFlashTriggered;
        }
    }

    public string MonitorId { get; set; } = "All";

    private void AudioController_NeonFlashTriggered(object? sender, float bassIntensity)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            string currentFilter = Nythera.Services.SettingsService.GetVideoFilter(MonitorId);
            double baseOpacity = currentFilter == "None" ? 0.0 : 0.15;
            double flashOpacity = Math.Min(0.8, baseOpacity + bassIntensity);
            
            if (ColorOverlay.Opacity < flashOpacity - 0.05)
            {
                ColorOverlay.Opacity = flashOpacity;
                if (ColorOverlay.Fill is SolidColorBrush brush && brush.Color == Colors.Transparent)
                {
                    ColorOverlay.Fill = new SolidColorBrush(Colors.Fuchsia); // Default neon flash color if no filter
                }

                var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                var anim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    To = baseOpacity,
                    Duration = new Microsoft.UI.Xaml.Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.ExponentialEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
                };
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(anim, ColorOverlay);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(anim, "Opacity");
                
                storyboard.Completed += (s, e) => {
                    if (currentFilter == "None") {
                        ColorOverlay.Fill = new SolidColorBrush(Colors.Transparent);
                    }
                };
                
                storyboard.Children.Add(anim);
                storyboard.Begin();
            }
        });
    }

    private void FullScreenDetector_StateChanged(object sender, bool isFullScreen)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            bool pauseForFullScreen = isFullScreen && Nythera.Services.SettingsService.GetPauseOnFullscreen();
            if (pauseForFullScreen || App.PerformanceManager.CurrentMode == Nythera.Core.Shared.Models.PerformanceMode.Low)
            {
                PauseVideo();
            }
            else
            {
                ResumeVideo();
            }
        });
    }

    private void PerformanceManager_StateChanged(object sender, Nythera.Core.Shared.Models.PerformanceMode mode)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (mode == Nythera.Core.Shared.Models.PerformanceMode.Low)
            {
                PauseVideo();
            }
            else
            {
                SetPlaybackSpeed(Nythera.Services.SettingsService.GetPlaybackSpeed(MonitorId));
                if (!(_fullScreenDetector.IsFullScreen && Nythera.Services.SettingsService.GetPauseOnFullscreen())) ResumeVideo();
            }
        });
    }

    public void PlayVideo(Windows.Storage.StorageFile file)
    {
        _mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
        if (_mediaPlayer.PlaybackSession != null)
        {
            _mediaPlayer.PlaybackSession.PlaybackRate = Nythera.Services.SettingsService.GetPlaybackSpeed(MonitorId);
        }
        _mediaPlayer.Play();
    }

    public void PauseVideo()
    {
        if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            _mediaPlayer.Pause();
    }

    public void ResumeVideo()
    {
        if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            _mediaPlayer.Play();
    }
    
    public void SetVolume(double volume)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = volume;
        }
    }
    
    public void SetPlaybackSpeed(double speed)
    {
        try
        {
            if (_mediaPlayer != null && _mediaPlayer.PlaybackSession != null)
            {
                _mediaPlayer.PlaybackSession.PlaybackRate = speed;
            }
        }
        catch { }
    }
    
    public void SetBrightness(double brightness)
    {
        try
        {
            // brightness is 0-100. 100 means fully bright (opacity 0), 0 means fully dark (opacity 1)
            double opacity = 1.0 - (brightness / 100.0);
            // clamp
            opacity = Math.Max(0.0, Math.Min(1.0, opacity));
            BrightnessOverlay.Opacity = opacity;
        }
        catch { }
    }
    
    public void SetVideoFilter(string filterName)
    {
        try
        {
            switch(filterName)
            {
                case "Warm":
                    ColorOverlay.Fill = new SolidColorBrush(Colors.Orange);
                    ColorOverlay.Opacity = 0.15;
                    break;
                case "Cool":
                    ColorOverlay.Fill = new SolidColorBrush(Colors.DeepSkyBlue);
                    ColorOverlay.Opacity = 0.15;
                    break;
                case "Matrix":
                    ColorOverlay.Fill = new SolidColorBrush(Colors.LimeGreen);
                    ColorOverlay.Opacity = 0.15;
                    break;
                case "Cyberpunk":
                    ColorOverlay.Fill = new SolidColorBrush(Colors.Fuchsia);
                    ColorOverlay.Opacity = 0.15;
                    break;
                case "None":
                default:
                    ColorOverlay.Fill = new SolidColorBrush(Colors.Transparent);
                    ColorOverlay.Opacity = 0;
                    break;
            }
        }
        catch { }
    }
    
    public void Cleanup()
    {
        string debugFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "monitor_debug.txt");
        System.IO.File.AppendAllText(debugFile, $"Cleanup called for window\n");

        try 
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Nythera.Native.WindowsApi.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0080); // SWP_HIDEWINDOW
            Nythera.Native.WindowsApi.SetParent(hwnd, IntPtr.Zero);
        }
        catch { }

        if (_fullScreenDetector != null)
        {
            _fullScreenDetector.Stop();
            _fullScreenDetector.FullScreenStateChanged -= FullScreenDetector_StateChanged;
            _fullScreenDetector = null;
        }

        App.PerformanceManager.PerformanceModeChanged -= PerformanceManager_StateChanged;

        if (App.AudioController != null)
        {
            App.AudioController.NeonFlashTriggered -= AudioController_NeonFlashTriggered;
        }

        if (_mediaPlayer != null)
        {
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
        
        if (BackgroundPlayer != null)
        {
            BackgroundPlayer.SetMediaPlayer(null);
        }
    }

    public void HideWindow()
    {
        try
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            // Move window far off-screen and hide it
            Nythera.Native.WindowsApi.SetWindowPos(hwnd, IntPtr.Zero, -10000, -10000, 1, 1, 0x0080 | 0x0200 | 0x0010); // SWP_HIDEWINDOW | SWP_NOOWNERZORDER | SWP_NOACTIVATE
            PauseVideo();
        }
        catch { }
    }

    public void ShowWindow()
    {
        try
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Nythera.Native.WindowsApi.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0040 | 0x0200 | 0x0010 | 0x0001 | 0x0002); // SWP_SHOWWINDOW | SWP_NOOWNERZORDER | SWP_NOACTIVATE | SWP_NOSIZE | SWP_NOMOVE
            ResumeVideo();
        }
        catch { }
    }

    public void SetStretchMode(Stretch stretch)
    {
        if (BackgroundPlayer != null)
        {
            BackgroundPlayer.Stretch = stretch;
        }
    }
    
    public void AttachToDesktop(int x, int y, int width, int height)
    {
        // Get Window Handle (HWND)
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        
        // Remove window borders before attaching
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        
        if (appWindow != null && appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            // Do not use Maximize() here, as it may not work correctly for child windows.
            // Just remove the title bar.
            presenter.SetBorderAndTitleBar(false, false);
        }
        
        // Disable Windows 11 rounded corners which can leave a 1px white border
        int preference = WindowsApi.DWMWCP_DONOTROUND;
        WindowsApi.DwmSetWindowAttribute(hwnd, WindowsApi.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        
        // Position the window using WinUI 3 API BEFORE parenting it, so it scales correctly
        if (appWindow != null)
        {
            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
        }

        // Attach to desktop WorkerW
        DesktopInterop.SetAsDesktopBackground(hwnd);

        string debugFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nythera", "monitor_debug.txt");
        System.IO.File.AppendAllText(debugFile, $"SetWindowPos on hwnd {hwnd} to X={x}, Y={y}, W={width}, H={height}\n");

        // Force Win32 position again just in case WorkerW forces a relative offset
        WindowsApi.SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, 0x0200 | 0x0010); // SWP_NOOWNERZORDER | SWP_NOACTIVATE

        // Ensure the window is visible
        ShowWindow();
    }

    private Microsoft.UI.Xaml.Media.Animation.Storyboard _kenBurnsStoryboard;

    public async Task SetWallpaperTypeAsync(string type)
    {
        if (type == "Image")
        {
            BackgroundPlayer.Visibility = Visibility.Collapsed;
            StaticWallpaperImage.Visibility = Visibility.Visible;
            PauseVideo();
        }
        else
        {
            StaticWallpaperImage.Visibility = Visibility.Collapsed;
            BackgroundPlayer.Visibility = Visibility.Visible;
        }
    }

    public async Task ApplyImageSettingsAsync(Nythera.Core.WallpaperImage imageSettings)
    {
        try
        {
            // Set image source safely
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(imageSettings.ImagePath);
            using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                var memStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                await Windows.Storage.Streams.RandomAccessStream.CopyAsync(fileStream, memStream);
                memStream.Seek(0);
                
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                await bitmap.SetSourceAsync(memStream);
                StaticWallpaperImage.Source = bitmap;
            }

            // Apply layout
            StaticWallpaperImage.Stretch = imageSettings.LayoutMode switch
            {
                "Fill" => Stretch.UniformToFill,
                "Fit" => Stretch.Uniform,
                "Stretch" => Stretch.Fill,
                "Center" => Stretch.None,
                _ => Stretch.UniformToFill
            };

            // Apply filters
            SetBrightness(imageSettings.Brightness);
            // Blur and Contrast require composition/Win2D in native WinUI, so we use brightness as primary filter natively

            // Apply Ken Burns
            if (_kenBurnsStoryboard != null)
            {
                _kenBurnsStoryboard.Stop();
                _kenBurnsStoryboard = null;
            }

            if (imageSettings.EnableKenBurns)
            {
                _kenBurnsStoryboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                _kenBurnsStoryboard.RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever;
                _kenBurnsStoryboard.AutoReverse = true;

                var scaleXAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 1.1,
                    Duration = new Duration(TimeSpan.FromSeconds(20)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuadraticEase()
                };
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleXAnim, StaticWallpaperTransform);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

                var scaleYAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 1.1,
                    Duration = new Duration(TimeSpan.FromSeconds(20)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuadraticEase()
                };
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleYAnim, StaticWallpaperTransform);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

                _kenBurnsStoryboard.Children.Add(scaleXAnim);
                _kenBurnsStoryboard.Children.Add(scaleYAnim);
                
                // Ensure transform origin is center
                StaticWallpaperImage.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
                
                _kenBurnsStoryboard.Begin();
            }
            else
            {
                StaticWallpaperTransform.ScaleX = 1.0;
                StaticWallpaperTransform.ScaleY = 1.0;
            }
        }
        catch { }
    }
}
