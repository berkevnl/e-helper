using System;

namespace EHelper.Core
{
    public interface IHardwareProvider : IDisposable
    {
        string VendorName { get; }
        string DeviceModel { get; }
        bool IsHardwareConnected { get; }
        HardwareCapabilities Capabilities { get; }

        HardwareTelemetry GetTelemetry();
        bool SetPowerMode(PowerMode mode);
        bool SetManualFanControl(bool enabled);
        bool SetFanSpeed(byte cpuPercent, byte gpuPercent, byte sysPercent = 255);
        void ResetFansToAuto();
        bool SetLed(LedZone zone, LedMode mode, byte brightness, byte r, byte g, byte b);
        bool SetAllKeyboardLed(LedMode mode, byte brightness, byte r, byte g, byte b);
        bool SetKeyboardTripleZone(byte brightness, (byte r, byte g, byte b) left, (byte r, byte g, byte b) center, (byte r, byte g, byte b) right);
        bool TurnOffAllLights();
    }
}
