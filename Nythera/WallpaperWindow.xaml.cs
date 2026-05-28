using Microsoft.UI.Xaml;
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
    }

    private void FullScreenDetector_StateChanged(object sender, bool isFullScreen)
    {
        // Must be marshalled to the UI thread if changing UI elements, 
        // but MediaPlayer controls can generally be called from background threads.
        // However, it's safer to use DispatcherQueue.
        DispatcherQueue.TryEnqueue(() =>
        {
            if (isFullScreen)
            {
                PauseVideo();
            }
            else
            {
                ResumeVideo();
            }
        });
    }

    public void PlayVideo(Windows.Storage.StorageFile file)
    {
        _mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
        if (_mediaPlayer.PlaybackSession != null)
        {
            _mediaPlayer.PlaybackSession.PlaybackRate = Nythera.Services.SettingsService.GetPlaybackSpeed();
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

    public void SetStretchMode(Microsoft.UI.Xaml.Media.Stretch stretch)
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
    }
}
