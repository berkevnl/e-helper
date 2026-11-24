using System;
using EHelper.Core;

namespace EHelper.Hardware.Simulation
{
    public class MockHardwareProvider : IHardwareProvider
    {
        private readonly Random _rand = new();
        private PowerMode _currentPowerMode = PowerMode.Gaming;

        public string VendorName { get; }
        public string DeviceModel { get; }
        public bool IsHardwareConnected => true;

        public MockHardwareProvider(string? vendor = null, string? deviceModel = null)
        {
            VendorName = vendor ?? "Casper";
            DeviceModel = !string.IsNullOrWhiteSpace(deviceModel)
                ? $"{deviceModel} (Simülasyon Modu)"
                : "Casper Excalibur (Simülasyon Modu)";
        }

        public HardwareCapabilities Capabilities { get; } = new(
            HasRgbKeyboard: true,
            HasBrightnessControl: true,
            MaxBrightnessLevel: 2,
            HasManualFanControl: true,
            FanCount: 2,
            HasGpuModeSwitch: false
        );

        public HardwareTelemetry GetTelemetry()
        {
            var (baseCpuTemp, baseGpuTemp, baseCpuRpm) = _currentPowerMode switch
            {
                PowerMode.Office => (45, 40, 1800),
                PowerMode.Gaming => (68, 62, 3400),
                PowerMode.HighPerformance => (78, 74, 4900),
                _ => (50, 45, 2500)
            };

            byte cpuTemp = (byte)(baseCpuTemp + _rand.Next(-2, 3));
            byte gpuTemp = (byte)(baseGpuTemp + _rand.Next(-1, 3));
            ushort cpuRpm = (ushort)(baseCpuRpm + _rand.Next(-80, 80));
            ushort gpuRpm = (ushort)(baseCpuRpm - 150 + _rand.Next(-60, 60));

            return new HardwareTelemetry(
                CpuTemperature: cpuTemp,
                GpuTemperature: gpuTemp,
                CpuFanRpm: cpuRpm,
                GpuFanRpm: gpuRpm,
                SysTemperature: 38,
                SysFanRpm: 0,
                IsAvailable: true
            );
        }

        public bool SetPowerMode(PowerMode mode)
        {
            _currentPowerMode = mode;
            return true;
        }

        public bool SetManualFanControl(bool enabled) => true;

        public bool SetFanSpeed(byte cpuPercent, byte gpuPercent, byte sysPercent = 255) => true;

        public void ResetFansToAuto() { }

        public bool SetLed(LedZone zone, LedMode mode, byte brightness, byte r, byte g, byte b) => true;

        public bool SetAllKeyboardLed(LedMode mode, byte brightness, byte r, byte g, byte b) => true;

        public bool SetKeyboardTripleZone(byte brightness, (byte r, byte g, byte b) left, (byte r, byte g, byte b) center, (byte r, byte g, byte b) right) => true;

        public bool TurnOffAllLights() => true;

        public void Dispose() { }
    }
}
