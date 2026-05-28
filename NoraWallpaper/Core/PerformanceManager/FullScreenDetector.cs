using System;
using System.Text;
using System.Threading;
using Nythera.Native;

namespace Nythera.Core.PerformanceManager;

public class FullScreenDetector
{
    private Timer _timer;
    private bool _isFullScreenAppRunning = false;
    
    public event EventHandler<bool> FullScreenStateChanged;

    public void Start()
    {
        // Check every 1 second
        _timer = new Timer(CheckFullScreen, null, 0, 1000);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }

    private void CheckFullScreen(object state)
    {
        bool isFullScreen = IsFullScreenAppRunning();
        
        if (isFullScreen != _isFullScreenAppRunning)
        {
            _isFullScreenAppRunning = isFullScreen;
            FullScreenStateChanged?.Invoke(this, _isFullScreenAppRunning);
        }
    }

    private bool IsFullScreenAppRunning()
    {
        IntPtr hWnd = WindowsApi.GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return false;

        // Ignore Desktop and Shell windows
        StringBuilder className = new StringBuilder(256);
        WindowsApi.GetClassName(hWnd, className, className.Capacity);
        string cName = className.ToString();

        if (cName == "WorkerW" || cName == "Progman" || cName == "Shell_TrayWnd")
            return false;

        WindowsApi.GetWindowRect(hWnd, out WindowsApi.RECT appBounds);

        IntPtr hMonitor = WindowsApi.MonitorFromWindow(hWnd, WindowsApi.MONITOR_DEFAULTTONEAREST);
        if (hMonitor != IntPtr.Zero)
        {
            WindowsApi.MONITORINFO monitorInfo = new WindowsApi.MONITORINFO();
            monitorInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WindowsApi.MONITORINFO));
            
            if (WindowsApi.GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                // Check if window bounds are equal to or larger than monitor bounds
                if (appBounds.Left <= monitorInfo.rcMonitor.Left &&
                    appBounds.Top <= monitorInfo.rcMonitor.Top &&
                    appBounds.Right >= monitorInfo.rcMonitor.Right &&
                    appBounds.Bottom >= monitorInfo.rcMonitor.Bottom)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
