using System;
using System.Text.Json.Serialization;

namespace EHelper.Core
{
    public class AppProfile : ILightingProfile
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

        [JsonIgnore]
        public (byte r, byte g, byte b) Color
        {
            get => (Red, Green, Blue);
            set => (Red, Green, Blue) = value;
        }

        [JsonIgnore]
        public (byte r, byte g, byte b) ZoneLeft
        {
            get => (ZoneLeftRed, ZoneLeftGreen, ZoneLeftBlue);
            set => (ZoneLeftRed, ZoneLeftGreen, ZoneLeftBlue) = value;
        }

        [JsonIgnore]
        public (byte r, byte g, byte b) ZoneCenter
        {
            get => (ZoneCenterRed, ZoneCenterGreen, ZoneCenterBlue);
            set => (ZoneCenterRed, ZoneCenterGreen, ZoneCenterBlue) = value;
        }

        [JsonIgnore]
        public (byte r, byte g, byte b) ZoneRight
        {
            get => (ZoneRightRed, ZoneRightGreen, ZoneRightBlue);
            set => (ZoneRightRed, ZoneRightGreen, ZoneRightBlue) = value;
        }

        public AppProfile Clone() => (AppProfile)MemberwiseClone();
    }
}
