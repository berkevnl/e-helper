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
                    if (excaliburBridge.IsHardwareConnected)
                    {
                        Debug.WriteLine("[HardwareDetector] Casper Excalibur WMI bridge initialized successfully.");
                        return (excaliburBridge, false);
                    }
                    excaliburBridge.Dispose();
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

                    if (mfg.Contains("Casper", StringComparison.OrdinalIgnoreCase) ||
                        model.Contains("Excalibur", StringComparison.OrdinalIgnoreCase) ||
                        family.Contains("Excalibur", StringComparison.OrdinalIgnoreCase))
                    {
                        isExcalibur = true;
                    }
                }
            }
            catch { }

            try
            {
                using var bbSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
                foreach (ManagementObject obj in bbSearcher.Get())
                {
                    string bbProduct = obj["Product"]?.ToString() ?? "";
                    string bbMfg = obj["Manufacturer"]?.ToString() ?? "";

                    if (bbMfg.Contains("Casper", StringComparison.OrdinalIgnoreCase) ||
                        bbProduct.Contains("G920", StringComparison.OrdinalIgnoreCase) ||
                        bbProduct.Contains("G870", StringComparison.OrdinalIgnoreCase) ||
                        bbProduct.Contains("G770", StringComparison.OrdinalIgnoreCase))
                    {
                        isExcalibur = true;
                    }
                }
            }
            catch { }

            // Presentation model normalization for Casper Excalibur models
            if (modelName.Contains("G920", StringComparison.OrdinalIgnoreCase)) modelName = "Casper Excalibur G920";
            else if (modelName.Contains("G870", StringComparison.OrdinalIgnoreCase)) modelName = "Casper Excalibur G870";
            else if (modelName.Contains("G770", StringComparison.OrdinalIgnoreCase)) modelName = "Casper Excalibur G770";
            else if (isExcalibur && !modelName.Contains("Excalibur", StringComparison.OrdinalIgnoreCase))
                modelName = $"Casper Excalibur ({modelName})";
            else if (!isExcalibur)
                modelName = "Casper Excalibur";

            return (vendor, modelName, isExcalibur);
        }
    }
}
