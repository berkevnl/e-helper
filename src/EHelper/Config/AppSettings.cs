using System.Text.Json.Serialization;
using EHelper.Core;

namespace EHelper.Config
{
    public class AppSettings
    {
        public PowerMode PowerMode { get; set; } = PowerMode.Gaming;
        public LedMode LedMode { get; set; } = LedMode.Static;
        public byte LedBrightness { get; set; } = 2;
        public byte Red { get; set; } = 0;
        public byte Green { get; set; } = 180;
        public byte Blue { get; set; } = 255;

        // 3-Zone RGB Lighting colors
        public byte ZoneLeftRed { get; set; } = 0;
        public byte ZoneLeftGreen { get; set; } = 229;
        public byte ZoneLeftBlue { get; set; } = 255;

        public byte ZoneCenterRed { get; set; } = 34;
        public byte ZoneCenterGreen { get; set; } = 197;
        public byte ZoneCenterBlue { get; set; } = 94;

        public byte ZoneRightRed { get; set; } = 239;
        public byte ZoneRightGreen { get; set; } = 68;
        public byte ZoneRightBlue { get; set; } = 68;
        public int CycleSpeed { get; set; } = 0; // 0: Slow, 1: Medium, 2: Fast
        public bool StartWithWindows { get; set; } = true;
        public int PollingIntervalSeconds { get; set; } = 3;
        public bool IsDarkTheme { get; set; } = true;
        public string Language { get; set; } = "TR";
        public string ThemeAccentColor { get; set; } = "#22D3EE";
        public System.Collections.Generic.List<AppProfile> Profiles { get; set; } = new();

        [JsonIgnore]
        public string HexColor => $"#{Red:X2}{Green:X2}{Blue:X2}";
    }
}
