using System;
using System.Diagnostics;
using System.Management;
using EHelper.Hardware.Casper;
using EHelper.Hardware.Simulation;

namespace EHelper.Core
{
    public static class HardwareDetector
    {
        public static (IHardwareProvider Provider, bool IsSimulated) DetectAndCreate()
        {
            var (vendor, model, isExcalibur) = ScanSystemHardware();

            Debug.WriteLine($"[HardwareDetector] Detected Vendor: {vendor}, Model: {model}, IsExcalibur: {isExcalibur}");

            // Dedicated Hardware Bridge: Casper Excalibur (Quanta ODM)
            if (isExcalibur)
            {
                try
                {
                    var excaliburBridge = new ExcaliburBridge(model);
                    Debug.WriteLine($"[HardwareDetector] Casper Excalibur bridge created. IsHardwareConnected: {excaliburBridge.IsHardwareConnected}");
                    return (excaliburBridge, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HardwareDetector] Excalibur bridge init failed: {ex.Message}");
                }
            }

            // Fallback: Casper Excalibur Simulation Provider (for development & non-Excalibur machines)
            Debug.WriteLine("[HardwareDetector] Falling back to MockHardwareProvider (Casper Excalibur Simulation Mode).");
            return (new MockHardwareProvider(vendor, model), true);
        }

        private static (string Vendor, string Model, bool IsExcalibur) ScanSystemHardware()
        {
            string vendor = "Casper";
            string modelName = "Casper Excalibur";
            bool isExcalibur = false;

            // 1. Fast Registry BIOS check (0ms latency, bypasses slow WMI service spin-up)
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
                if (key != null)
                {
                    string mfg = key.GetValue("SystemManufacturer")?.ToString() ?? "";
                    string model = key.GetValue("SystemProductName")?.ToString() ?? "";
                    string family = key.GetValue("SystemFamily")?.ToString() ?? "";
                    string bbMfg = key.GetValue("BaseBoardManufacturer")?.ToString() ?? "";
                    string bbProduct = key.GetValue("BaseBoardProduct")?.ToString() ?? "";

                    if (!string.IsNullOrWhiteSpace(mfg)) vendor = mfg;
                    if (!string.IsNullOrWhiteSpace(model)) modelName = model;
                    else if (!string.IsNullOrWhiteSpace(family)) modelName = family;

                    isExcalibur = IsExcaliburSystem(mfg, model, family) || IsExcaliburBoard(bbMfg, bbProduct);

                    if (isExcalibur || !string.IsNullOrWhiteSpace(mfg))
                    {
                        return NormalizeModel(vendor, modelName, isExcalibur);
                    }
                }
            }
            catch { }

            // 2. Fallback to WMI only if Registry query could not be completed
            try
            {
                using var csSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Model, SystemFamily FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in csSearcher.Get())
                {
                    string mfg = obj["Manufacturer"]?.ToString() ?? "";
                    string model = obj["Model"]?.ToString() ?? "";
                    string family = obj["SystemFamily"]?.ToString() ?? "";

                    if (!string.IsNullOrWhiteSpace(mfg)) vendor = mfg;
                    if (!string.IsNullOrWhiteSpace(model)) modelName = model;
                    else if (!string.IsNullOrWhiteSpace(family)) modelName = family;

                    if (IsExcaliburSystem(mfg, model, family))
                    {
                        isExcalibur = true;
                    }
                }
            }
            catch { }

            if (!isExcalibur)
            {
                try
                {
                    using var bbSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
                    foreach (ManagementObject obj in bbSearcher.Get())
                    {
                        string bbProduct = obj["Product"]?.ToString() ?? "";
                        string bbMfg = obj["Manufacturer"]?.ToString() ?? "";

                        if (IsExcaliburBoard(bbMfg, bbProduct))
                        {
                            isExcalibur = true;
                        }
                    }
                }
                catch { }
            }

            return NormalizeModel(vendor, modelName, isExcalibur);
        }

        private static readonly string[] KnownBoards = { "G920", "G870", "G770" };

        private static bool ContainsIgnoreCase(string value, string token) => value.Contains(token, StringComparison.OrdinalIgnoreCase);

        private static bool IsExcaliburSystem(string manufacturer, string model, string family) =>
            ContainsIgnoreCase(manufacturer, "Casper") ||
            ContainsIgnoreCase(model, "Excalibur") ||
            ContainsIgnoreCase(family, "Excalibur");

        private static bool IsExcaliburBoard(string boardManufacturer, string boardProduct) =>
            ContainsIgnoreCase(boardManufacturer, "Casper") ||
            Array.Exists(KnownBoards, board => ContainsIgnoreCase(boardProduct, board));

        private static (string Vendor, string Model, bool IsExcalibur) NormalizeModel(string vendor, string modelName, bool isExcalibur)
        {
            string? board = Array.Find(KnownBoards, b => ContainsIgnoreCase(modelName, b));
            if (board != null) modelName = $"Casper Excalibur {board}";
            else if (isExcalibur && !ContainsIgnoreCase(modelName, "Excalibur"))
                modelName = $"Casper Excalibur ({modelName})";
            else if (!isExcalibur)
                modelName = "Casper Excalibur";

            return (vendor, modelName, isExcalibur);
        }
    }
}
