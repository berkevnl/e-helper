using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EHelper.Hardware.Common
{
    public static class NvmlHelper
    {
        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
        private static extern int nvmlInit();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern int nvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
        private static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetFanSpeed")]
        private static extern int nvmlDeviceGetFanSpeed(IntPtr device, out uint fanSpeed);

        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlUtilization
        {
            public uint Gpu;
            public uint Memory;
        }

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
        private static extern int nvmlDeviceGetUtilizationRates(IntPtr device, ref NvmlUtilization utilization);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetName")]
        private static extern int nvmlDeviceGetName(IntPtr device, StringBuilder name, uint length);

        private static bool _initialized;
        private static IntPtr _deviceHandle = IntPtr.Zero;
        private static bool _dllMissing;
        private static int _initRetryCount;
        private static string _cachedGpuName = "";

        public static string GetCachedGpuName() => _cachedGpuName;

        public static (uint Temp, uint FanPercent, uint GpuUsagePercent, string GpuName) GetGpuMetrics()
        {
            if (_dllMissing) return (0, 0, 0, _cachedGpuName);

            try
            {
                if (!_initialized)
                {
                    if (_initRetryCount > 10 && (_initRetryCount % 5 != 0))
                    {
                        _initRetryCount++;
                        return (0, 0, 0, _cachedGpuName);
                    }

                    if (nvmlInit() == 0 && nvmlDeviceGetHandleByIndex(0, out _deviceHandle) == 0 && _deviceHandle != IntPtr.Zero)
                    {
                        _initialized = true;
                        _initRetryCount = 0;

                        // Acquire GPU Name once
                        var sb = new StringBuilder(128);
                        if (nvmlDeviceGetName(_deviceHandle, sb, (uint)sb.Capacity) == 0)
                        {
                            _cachedGpuName = FormatGpuName(sb.ToString());
                        }
                    }
                    else
                    {
                        _initRetryCount++;
                        return (0, 0, 0, _cachedGpuName);
                    }
                }

                if (_initialized && _deviceHandle != IntPtr.Zero)
                {
                    uint temp = 0;
                    uint fan = 0;
                    uint gpuUsage = 0;

                    int tempRes = nvmlDeviceGetTemperature(_deviceHandle, 0, out uint t);
                    if (tempRes == 0)
                    {
                        temp = t;
                    }
                    else if (tempRes is 15 or 999) // NVML_ERROR_GPU_IS_LOST / GPU asleep
                    {
                        _initialized = false;
                        _deviceHandle = IntPtr.Zero;
                    }

                    if (nvmlDeviceGetFanSpeed(_deviceHandle, out uint f) == 0)
                        fan = f;

                    var util = new NvmlUtilization();
                    if (nvmlDeviceGetUtilizationRates(_deviceHandle, ref util) == 0)
                    {
                        gpuUsage = Math.Min(util.Gpu, 100);
                    }

                    if (string.IsNullOrEmpty(_cachedGpuName))
                    {
                        var sb = new StringBuilder(128);
                        if (nvmlDeviceGetName(_deviceHandle, sb, (uint)sb.Capacity) == 0)
                        {
                            _cachedGpuName = FormatGpuName(sb.ToString());
                        }
                    }

                    return (temp, fan, gpuUsage, _cachedGpuName);
                }
            }
            catch (DllNotFoundException)
            {
                _dllMissing = true;
            }
            catch (EntryPointNotFoundException)
            {
                _dllMissing = true;
            }
            catch
            {
                _initialized = false;
                _deviceHandle = IntPtr.Zero;
            }

            return (0, 0, 0, _cachedGpuName);
        }

        private static string FormatGpuName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "RTX 4070";

            string s = raw.Replace("NVIDIA ", "").Replace("GeForce ", "").Replace(" Laptop GPU", "").Replace(" Graphics", "").Trim();
            if (s.Length > 16) s = s.Substring(0, 16);
            return s;
        }
    }
}
