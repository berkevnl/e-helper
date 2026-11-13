using System;
using System.Text.Json.Serialization;

namespace EHelper.Core
{
    public class AppProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty; // e.g. "VALORANT-Win64-Shipping.exe"
        public string? AppPath { get; set; }

        // Captured Hardware & Lighting Settings
        public PowerMode PowerMode { get; set; } = PowerMode.Gaming;
        public LedMode LedMode { get; set; } = LedMode.Static;
        public byte LedBrightness { get; set; } = 2;
        public int CycleSpeed { get; set; } = 0;
        public byte Red { get; set; } = 34;
        public byte Green { get; set; } = 211;
        public byte Blue { get; set; } = 238;

        public byte ZoneLeftRed { get; set; } = 0;
        public byte ZoneLeftGreen { get; set; } = 229;
        public byte ZoneLeftBlue { get; set; } = 255;
        public byte ZoneCenterRed { get; set; } = 34;
        public byte ZoneCenterGreen { get; set; } = 197;
        public byte ZoneCenterBlue { get; set; } = 94;
        public byte ZoneRightRed { get; set; } = 239;
        public byte ZoneRightGreen { get; set; } = 68;
        public byte ZoneRightBlue { get; set; } = 68;

        [JsonIgnore]
        public string CleanProcessName => !string.IsNullOrEmpty(ProcessName) ? System.IO.Path.GetFileName(ProcessName) : "";

        public AppProfile Clone()
        {
            return new AppProfile
            {
                Id = this.Id,
                Name = this.Name,
                ProcessName = this.ProcessName,
                AppPath = this.AppPath,
                PowerMode = this.PowerMode,
                LedMode = this.LedMode,
                LedBrightness = this.LedBrightness,
                CycleSpeed = this.CycleSpeed,
                Red = this.Red,
                Green = this.Green,
                Blue = this.Blue,
                ZoneLeftRed = this.ZoneLeftRed,
                ZoneLeftGreen = this.ZoneLeftGreen,
                ZoneLeftBlue = this.ZoneLeftBlue,
                ZoneCenterRed = this.ZoneCenterRed,
                ZoneCenterGreen = this.ZoneCenterGreen,
                ZoneCenterBlue = this.ZoneCenterBlue,
                ZoneRightRed = this.ZoneRightRed,
                ZoneRightGreen = this.ZoneRightGreen,
                ZoneRightBlue = this.ZoneRightBlue
            };
        }
    }
}
