using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using EHelper.Core;
using EHelper.Hardware.Common;

namespace EHelper.Hardware.Casper
{
    public class ExcaliburBridge : IHardwareProvider
    {
        private const string WmiNamespace = @"root\wmi";
        private const string WmiClassName = "RW_GMWMI";
        
        private readonly object _syncLock = new();
        private ManagementObject? _wmiInstance;
        private bool _isDisposed;
        private LedMode _activeMode = LedMode.Off;

        public string VendorName => "Casper";
        public string DeviceModel { get; private set; }
        public bool IsHardwareConnected { get; private set; }

        public HardwareCapabilities Capabilities { get; } = new(
            HasRgbKeyboard: true,
            HasBrightnessControl: true,
            MaxBrightnessLevel: 2,
            HasManualFanControl: true,
            FanCount: 2,
            HasGpuModeSwitch: false
        );

        public ExcaliburBridge(string detectedModel)
        {
            DeviceModel = detectedModel;
            InitializeWmi();
        }

        private void InitializeWmi()
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var scope = new ManagementScope(WmiNamespace);
                    scope.Connect();

                    using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery($"SELECT * FROM {WmiClassName}"));
                    foreach (ManagementObject inst in searcher.Get())
                    {
                        _wmiInstance = inst;
                        break;
                    }

                    IsHardwareConnected = (_wmiInstance != null);
                    if (IsHardwareConnected)
                    {
                        Debug.WriteLine($"[ExcaliburBridge] WMI connection established on attempt {attempt}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExcaliburBridge] WMI attempt {attempt} failed: {ex.Message}");
                    IsHardwareConnected = false;
                }

                if (attempt < 3)
                {
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        private bool PerformSmi(ref SMI_STRUCT_S smi)
        {
            if (_wmiInstance == null)
            {
                InitializeWmi();
                if (_wmiInstance == null)
                {
                    return false;
                }
            }

            lock (_syncLock)
            {
                int size = Marshal.SizeOf<SMI_STRUCT_S>();
                byte[] inBuffer = new byte[size];
                IntPtr ptr = Marshal.AllocHGlobal(size);

                try
                {
                    Marshal.StructureToPtr(smi, ptr, false);
                    Marshal.Copy(ptr, inBuffer, 0, size);

                    // 1. Write inBuffer to WMI ACPI SMI
                    _wmiInstance["BufferBytes"] = inBuffer;
                    _wmiInstance.Put(); // Executes SMI interrupt on Quanta EC

                    // If this was a write command (LED, Fan, Power), the EC has already taken the command via SMI Put()
                    if (smi.a0 == ExcaliburSmiHelper.CMD_WRITE)
                    {
                        return true;
                    }

                    // 2. Fetch fresh output buffer from ACPI WMI for read commands (Telemetry)
                    byte[]? outBuffer = null;

                    try
                    {
                        var opts = new EnumerationOptions { ReturnImmediately = true, Timeout = TimeSpan.FromMilliseconds(500) };
                        using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT BufferBytes FROM {WmiClassName}", opts);
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (obj["BufferBytes"] is byte[] fresh && fresh.Length >= size)
                            {
                                outBuffer = fresh;
                                break;
                            }
                        }
                    }
                    catch { }

                    if (outBuffer == null || outBuffer.Length < size)
                    {
                        try
                        {
                            _wmiInstance.Get();
                            outBuffer = _wmiInstance["BufferBytes"] as byte[];
                        }
                        catch { }
                    }

                    if (outBuffer != null && outBuffer.Length >= size)
                    {
                        Marshal.Copy(outBuffer, 0, ptr, size);
                        smi = Marshal.PtrToStructure<SMI_STRUCT_S>(ptr);
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExcaliburBridge] SMI Execution error: {ex.Message}");
                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        /// <summary>Sends a write command (LED / fan / power) to the EC.</summary>
        private bool WriteSmi(ushort subFunction, uint a2 = 0, uint a3 = 0, uint a4 = 0)
        {
            var smi = new SMI_STRUCT_S
            {
                a0 = ExcaliburSmiHelper.CMD_WRITE,
                a1 = subFunction,
                a2 = a2,
                a3 = a3,
                a4 = a4
            };
            return PerformSmi(ref smi);
        }

        public HardwareTelemetry GetTelemetry()
        {
            var smi = new SMI_STRUCT_S
            {
                a0 = ExcaliburSmiHelper.CMD_READ,
                a1 = ExcaliburSmiHelper.SUB_CPU_GPU_TELEMETRY
            };

            bool smiSuccess = PerformSmi(ref smi);

            byte cpuTemp = 0;
            byte gpuTemp = 0;
            ushort cpuRpm = 0;
            ushort gpuRpm = 0;

            if (smiSuccess)
            {
                cpuTemp = (byte)(smi.a2 & 0xFF);
                gpuTemp = (byte)(smi.a3 & 0xFF);
                cpuRpm = (ushort)(smi.a4 & 0xFFFF);
                gpuRpm = (ushort)(smi.a5 & 0xFFFF);
            }

            // Fallback for CPU temperature if 0 or outside realistic thermal range
            if (cpuTemp is < 15 or > 115)
            {
                byte fallbackCpu = GetThermalZoneTemperature();
                if (fallbackCpu > 0) cpuTemp = fallbackCpu;
            }

            // GPU Temperature & Fan Metrics:
            // Dedicated NVIDIA GPUs report authoritative, real-time die temperatures via NVML.
            // On Casper Excalibur hardware, the EC register (smi.a3) is typically uncalibrated or
            // hardcoded to a static 30°C (0x1E) dummy value because the GPU die is not wired to the EC ADC.
            var nvGpu = NvmlHelper.GetGpuMetrics();
            if (nvGpu.Temp > 0)
            {
                gpuTemp = (byte)nvGpu.Temp;
            }
            else if (gpuTemp == 30)
            {
                // If NVML returned 0 (e.g. GPU is asleep in D3Cold) and EC returned the known 30°C dummy value,
                // clear it to 0 so the UI displays "--°C" instead of a false static 30°C.
                gpuTemp = 0;
            }

            if (gpuRpm == 0 && nvGpu.FanPercent > 0)
            {
                gpuRpm = (ushort)(nvGpu.FanPercent * 45); // approximate RPM from NVML fan duty %
            }

            bool isAvailable = smiSuccess || cpuTemp > 0 || gpuTemp > 0 || cpuRpm > 0 || gpuRpm > 0;
            return isAvailable
                ? new HardwareTelemetry(cpuTemp, gpuTemp, cpuRpm, gpuRpm)
                : new HardwareTelemetry(0, 0, 0, 0, IsAvailable: false);
        }

        private static byte GetThermalZoneTemperature()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(WmiNamespace, "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["CurrentTemperature"] is uint k && k > 2732)
                    {
                        int c = (int)((k - 2732) / 10);
                        if (c is > 10 and < 115) return (byte)c;
                    }
                }
            }
            catch { }
            return 0;
        }

        public bool SetPowerMode(PowerMode mode) => WriteSmi(ExcaliburSmiHelper.SUB_POWER_MODE, (uint)mode);

        public bool SetManualFanControl(bool enabled) => WriteSmi(ExcaliburSmiHelper.SUB_FAN_OVERRIDE, enabled ? 1u : 0u);

        public bool SetFanSpeed(byte cpuPercent, byte gpuPercent, byte sysPercent = 255)
            => WriteSmi(ExcaliburSmiHelper.SUB_FAN_SPEED, cpuPercent, gpuPercent, sysPercent);

        public void ResetFansToAuto()
        {
            SetManualFanControl(false);
            SetFanSpeed(255, 255, 255);
        }

        public bool SetLed(LedZone zone, LedMode mode, byte brightness, byte r, byte g, byte b)
            => WriteSmi(ExcaliburSmiHelper.SUB_LED, (uint)zone, ExcaliburSmiHelper.PackLedData(mode, brightness, r, g, b));

        /// <summary>
        /// Writes the same setting to Zone 0 (Master), Zone 6 (ALLKBLED) and individual zones (3, 4, 5)
        /// so the EC reliably wakes up. Result reflects the master / keyboard / first zone writes.
        /// </summary>
        private bool SetAllKeyboardZones(LedMode mode, byte brightness, byte r, byte g, byte b)
        {
            bool ok = SetLed(LedZone.All, mode, brightness, r, g, b);
            ok |= SetLed(LedZone.AllKeyboard, mode, brightness, r, g, b);
            ok |= SetLed(LedZone.KeyboardLeft, mode, brightness, r, g, b);
            SetLed(LedZone.KeyboardCenter, mode, brightness, r, g, b);
            SetLed(LedZone.KeyboardRight, mode, brightness, r, g, b);
            return ok;
        }

        public bool SetAllKeyboardLed(LedMode mode, byte brightness, byte r, byte g, byte b)
        {
            if (mode == LedMode.Off || brightness == 0)
            {
                _activeMode = LedMode.Off;
                return TurnOffAllLights();
            }

            // Only prime the EC PWM when first transitioning into Cycle, Dynamic or Rainbow, NEVER for Breathing or when already in the mode
            if (_activeMode != mode && mode is LedMode.Cycle or LedMode.ColorfulCycle or LedMode.Rainbow)
            {
                SetLed(LedZone.All, LedMode.Static, brightness, r, g, b);
                SetLed(LedZone.AllKeyboard, LedMode.Static, brightness, r, g, b);
            }
            _activeMode = mode;

            return SetAllKeyboardZones(mode, brightness, r, g, b);
        }

        public bool SetKeyboardTripleZone(byte brightness, (byte r, byte g, byte b) left, (byte r, byte g, byte b) center, (byte r, byte g, byte b) right)
        {
            if (brightness == 0)
            {
                return TurnOffAllLights();
            }

            // Hardware mapping on Casper Excalibur laptops:
            // EC SMI Zone 5 (LedZone.KeyboardRight) physically controls the left zone (WASD area).
            // EC SMI Zone 4 (LedZone.KeyboardCenter) physically controls the center zone.
            // EC SMI Zone 3 (LedZone.KeyboardLeft) physically controls the right zone (Numpad area).
            bool resL = SetLed(LedZone.KeyboardRight, LedMode.Static, brightness, left.r, left.g, left.b);
            bool resC = SetLed(LedZone.KeyboardCenter, LedMode.Static, brightness, center.r, center.g, center.b);
            bool resR = SetLed(LedZone.KeyboardLeft, LedMode.Static, brightness, right.r, right.g, right.b);

            return resL || resC || resR;
        }

        public bool TurnOffAllLights() => SetAllKeyboardZones(LedMode.Off, 0, 0, 0, 0);

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _wmiInstance?.Dispose();
            }
            catch
            {
                // Suppress disposal errors
            }
        }
    }
}
