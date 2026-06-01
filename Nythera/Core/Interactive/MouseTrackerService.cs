using System;
using System.Threading;
using Nythera.Core.Interactive.Models;
using Nythera.Native;

namespace Nythera.Core.Interactive;

public class MouseTrackerService
{
    private Timer? _timer;
    public event EventHandler<MousePosition>? MouseMoved;
    private MousePosition _lastPosition = new MousePosition();

    public void Start()
    {
        // Track at ~30 FPS
        _timer = new Timer(TrackMouse, null, 0, 33);
    }

    public void Stop()
    {
        _timer?.Dispose();
    }

    private void TrackMouse(object? state)
    {
        if (WindowsApi.GetCursorPos(out WindowsApi.POINT point))
        {
            if (point.X != (int)_lastPosition.X || point.Y != (int)_lastPosition.Y)
            {
                _lastPosition = new MousePosition { X = point.X, Y = point.Y };
                MouseMoved?.Invoke(this, _lastPosition);
            }
        }
    }
}
