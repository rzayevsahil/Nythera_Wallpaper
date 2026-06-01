using System;
using Windows.System.Power;

namespace Nythera.Core.Performance;

public class BatteryMonitorService
{
    public event EventHandler<int>? BatteryLevelChanged;
    public event EventHandler<bool>? PowerLineStatusChanged;

    public void Start()
    {
        PowerManager.RemainingChargePercentChanged += OnRemainingChargePercentChanged;
        PowerManager.PowerSupplyStatusChanged += OnPowerSupplyStatusChanged;
    }

    public void Stop()
    {
        PowerManager.RemainingChargePercentChanged -= OnRemainingChargePercentChanged;
        PowerManager.PowerSupplyStatusChanged -= OnPowerSupplyStatusChanged;
    }

    private void OnRemainingChargePercentChanged(object? sender, object e)
    {
        BatteryLevelChanged?.Invoke(this, PowerManager.RemainingChargePercent);
    }

    private void OnPowerSupplyStatusChanged(object? sender, object e)
    {
        PowerLineStatusChanged?.Invoke(this, IsPluggedIn());
    }

    public int GetCurrentBatteryLevel() => PowerManager.RemainingChargePercent;
    
    public bool IsPluggedIn() 
    {
        return PowerManager.PowerSupplyStatus == PowerSupplyStatus.Adequate || 
               PowerManager.PowerSupplyStatus == PowerSupplyStatus.Inadequate; // At least it's connected
    }
}
