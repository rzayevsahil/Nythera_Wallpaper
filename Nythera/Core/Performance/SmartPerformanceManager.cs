using System;
using Nythera.Core.Shared.Models;

namespace Nythera.Core.Performance;

public class SmartPerformanceManager
{
    private readonly BatteryMonitorService _batteryMonitor;
    public PerformanceMode CurrentMode { get; private set; } = PerformanceMode.Ultra;
    public event EventHandler<PerformanceMode>? PerformanceModeChanged;

    public SmartPerformanceManager()
    {
        _batteryMonitor = new BatteryMonitorService();
        _batteryMonitor.BatteryLevelChanged += OnBatteryLevelChanged;
        _batteryMonitor.PowerLineStatusChanged += OnPowerLineStatusChanged;
    }

    public void Start()
    {
        _batteryMonitor.Start();
        UpdatePerformanceMode();
    }

    public void Stop()
    {
        _batteryMonitor.Stop();
    }

    private void OnBatteryLevelChanged(object? sender, int batteryLevel)
    {
        UpdatePerformanceMode();
    }

    private void OnPowerLineStatusChanged(object? sender, bool isPluggedIn)
    {
        UpdatePerformanceMode();
    }

    private void UpdatePerformanceMode()
    {
        if (_batteryMonitor.IsPluggedIn())
        {
            SetMode(PerformanceMode.Ultra);
            return;
        }

        int battery = _batteryMonitor.GetCurrentBatteryLevel();
        
        if (battery > 50)
            SetMode(PerformanceMode.Ultra);
        else if (battery > 30)
            SetMode(PerformanceMode.High);
        else if (battery > 15)
            SetMode(PerformanceMode.Medium);
        else
            SetMode(PerformanceMode.Low);
    }

    private void SetMode(PerformanceMode newMode)
    {
        if (CurrentMode != newMode)
        {
            CurrentMode = newMode;
            PerformanceModeChanged?.Invoke(this, CurrentMode);
        }
    }
}
