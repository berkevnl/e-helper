using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using EHelper.Hardware.Common;

namespace EHelper.Services
{
    public record SystemResourceTelemetry(
        double RamUsedGb,
        double RamTotalGb,
        int RamPercent,
        double SsdUsedGb,
        double SsdTotalGb,
        int SsdPercent,
        int CpuUsagePercent,
        int GpuUsagePercent,
        string CpuName,
        string GpuName
    );

    public static class SystemResourceMonitor
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct FileTimeUnion
        {
            [FieldOffset(0)] public uint Low;
            [FieldOffset(4)] public uint High;
            [FieldOffset(0)] public ulong Value;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FileTimeUnion idleTime, out FileTimeUnion kernelTime, out FileTimeUnion userTime);

        private static ulong _prevIdleTime;
        private static ulong _prevKernelTime;
        private static ulong _prevUserTime;
        private static ulong _prevTotalTime;
        private static int _lastCpuPercent;

        private static readonly string _cachedCpuName = DetectCpuName();
        private static readonly string _sysDrivePath = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        private static readonly DriveInfo _sysDrive = new(_sysDrivePath);

        private static string DetectCpuName()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                string name = key?.GetValue("ProcessorNameString")?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                {
                    string cleaned = name.Replace("14th Gen ", "").Replace("13th Gen ", "").Replace("12th Gen ", "")
                                         .Replace("Intel(R) ", "").Replace("Core(TM) ", "")
                                         .Replace("AMD ", "");
                    int atIdx = cleaned.IndexOf('@');
                    if (atIdx > 0) cleaned = cleaned.Substring(0, atIdx);
                    int wIdx = cleaned.IndexOf(" w/", StringComparison.OrdinalIgnoreCase);
                    if (wIdx > 0) cleaned = cleaned.Substring(0, wIdx);
                    cleaned = cleaned.Trim();
                    if (!string.IsNullOrEmpty(cleaned)) return cleaned;
                }
            }
            catch { }
            return "i7-13700H";
        }

        public static SystemResourceTelemetry GetMetrics()
        {
            // 1. RAM Metrics via Win32 GlobalMemoryStatusEx (0 CPU overhead)
            double ramUsedGb = 0;
            double ramTotalGb = 16.0;
            int ramPercent = 0;

            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                double total = memStatus.ullTotalPhys / (1024.0 * 1024 * 1024);
                double avail = memStatus.ullAvailPhys / (1024.0 * 1024 * 1024);
                ramTotalGb = total;
                ramUsedGb = total - avail;
                ramPercent = (int)memStatus.dwMemoryLoad;
            }

            // 2. SSD Metrics (System Drive C:)
            double ssdUsedGb = 0;
            double ssdTotalGb = 512.0;
            int ssdPercent = 0;

            try
            {
                if (_sysDrive.IsReady)
                {
                    double total = _sysDrive.TotalSize / (1024.0 * 1024 * 1024);
                    double free = _sysDrive.TotalFreeSpace / (1024.0 * 1024 * 1024);
                    ssdTotalGb = total;
                    ssdUsedGb = total - free;
                    ssdPercent = (int)((ssdUsedGb / total) * 100);
                }
            }
            catch { }

            // 3. High-Precision CPU Usage via GetSystemTimes
            int cpuPercent = CalculateCpuUsage();

            // 4. GPU Metrics via NvmlHelper
            var gpuMetrics = NvmlHelper.GetGpuMetrics();
            int gpuPercent = (int)gpuMetrics.GpuUsagePercent;
            string gpuName = !string.IsNullOrWhiteSpace(gpuMetrics.GpuName) ? gpuMetrics.GpuName : "RTX 4070";

            return new SystemResourceTelemetry(
                RamUsedGb: ramUsedGb,
                RamTotalGb: ramTotalGb,
                RamPercent: ramPercent,
                SsdUsedGb: ssdUsedGb,
                SsdTotalGb: ssdTotalGb,
                SsdPercent: ssdPercent,
                CpuUsagePercent: cpuPercent,
                GpuUsagePercent: gpuPercent,
                CpuName: _cachedCpuName,
                GpuName: gpuName
            );
        }

        private static int CalculateCpuUsage()
        {
            try
            {
                if (GetSystemTimes(out var idle, out var kernel, out var user))
                {
                    ulong idleVal = idle.Value;
                    ulong kernelVal = kernel.Value;
                    ulong userVal = user.Value;
                    ulong totalVal = kernelVal + userVal;

                    if (_prevTotalTime > 0 && totalVal > _prevTotalTime)
                    {
                        ulong deltaIdle = idleVal - _prevIdleTime;
                        ulong deltaTotal = totalVal - _prevTotalTime;

                        if (deltaTotal > 0 && deltaTotal >= deltaIdle)
                        {
                            _lastCpuPercent = (int)(((deltaTotal - deltaIdle) * 100) / deltaTotal);
                        }
                    }

                    _prevIdleTime = idleVal;
                    _prevKernelTime = kernelVal;
                    _prevUserTime = userVal;
                    _prevTotalTime = totalVal;
                }
            }
            catch { }

            return Math.Clamp(_lastCpuPercent, 0, 100);
        }
    }
}
