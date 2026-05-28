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
        
        // Attach to desktop WorkerW
        DesktopInterop.SetAsDesktopBackground(hwnd);

        // Position and size the window for the specific monitor
        WindowsApi.SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, WindowsApi.SWP_SHOWWINDOW);
    }
}
