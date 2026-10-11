using System;
using EHelper.Core;

namespace EHelper.Services
{
    public static class LocalizationManager
    {
        public static string CurrentLanguage { get; set; } = "TR";

        public static bool IsTurkish => CurrentLanguage.Equals("TR", StringComparison.OrdinalIgnoreCase);

        public static string GetPowerModeTitle(PowerMode mode) => (IsTurkish, mode) switch
        {
            (true, PowerMode.Office) => "Performans Modu: Tasarruf",
            (true, PowerMode.Gaming) => "Performans Modu: Dengeli",
            (true, PowerMode.HighPerformance) => "Performans Modu: Performans",
            (false, PowerMode.Office) => "Performance Mode: Eco",
            (false, PowerMode.Gaming) => "Performance Mode: Balanced",
            (false, PowerMode.HighPerformance) => "Performance Mode: Performance",
            (true, _) => "Performans Modu",
            (false, _) => "Performance Mode"
        };

        public static string PowerOffice => IsTurkish ? "Tasarruf" : "Eco";
        public static string PowerGaming => IsTurkish ? "Dengeli" : "Balanced";
        public static string PowerTurbo => IsTurkish ? "Performans" : "Performance";
        public static string PowerHighPerformance => PowerTurbo;

        public static string GpuHeader => IsTurkish ? "GPU Modu: Ayrık / Hibrit" : "GPU Mode: Discrete / Hybrid";
        public static string SystemResourcesHeader => IsTurkish ? "Sistem Kaynakları" : "System Resources";
        public static string RamLabel => IsTurkish ? "RAM Bellek" : "RAM Memory";
        public static string SsdLabel => IsTurkish ? "SSD Disk (C:)" : "SSD Drive (C:)";

        public static string KeyboardHeader => IsTurkish ? "Laptop Klavyesi" : "Laptop Keyboard";
        public static string BrightnessLabel => IsTurkish ? "Parlaklık: " : "Brightness: ";
        public static string BrightnessSliderTitle => IsTurkish ? "Parlaklık" : "Brightness";

        public static string LedStatic => IsTurkish ? "✨ Sabit" : "✨ Static";
        public static string LedBreathing => IsTurkish ? "💨 Nefes" : "💨 Breathing";
        public static string LedDynamic => IsTurkish ? "💫 Dinamik" : "💫 Dynamic";
        public static string LedRainbow => IsTurkish ? "🌈 Gökkuşağı" : "🌈 Rainbow";
        public static string LedTripleZone => IsTurkish ? "🎨 3-Bölge" : "🎨 3-Zone";
        public static string ColorLabel => IsTurkish ? "Renk" : "Color";

        public static string ZoneLeftLabel => IsTurkish ? "Sol Bölge" : "Left Zone";
        public static string ZoneCenterLabel => IsTurkish ? "Orta Bölge" : "Center Zone";
        public static string ZoneRightLabel => IsTurkish ? "Sağ Bölge" : "Right Zone";
        public static string KeyboardDiagramTitle => IsTurkish ? "Bölge Seç ve Rengini Belirle:" : "Select Zone & Set Color:";

        public static string CustomColorButton => IsTurkish ? "🎨 Özel Renk Seç" : "🎨 Custom Color";
        public static string CustomColorTitle => IsTurkish ? "Özel Renk" : "Custom Color";
        public static string PresetsTitle => IsTurkish ? "Hazır Renkler" : "Color Presets";

        public static string AutoStart => IsTurkish ? "Başlangıçta Çalıştır" : "Start with Windows";
        public static string CheckUpdates => IsTurkish ? "📥 Güncellemeler" : "📥 Updates";

        public static string ThemeLight => IsTurkish ? "☀️ Açık" : "☀️ Light";
        public static string ThemeDark => IsTurkish ? "🌙 Koyu" : "🌙 Dark";
        public static string Refresh => IsTurkish ? "🔃 Yenile" : "🔃 Refresh";
        public static string Exit => IsTurkish ? "➔ Çıkış" : "➔ Exit";
        public static string LangButton => IsTurkish ? "🌐 EN" : "🌐 TR";

        public static string ColorDialogTitle => IsTurkish ? "Renk Seçimi" : "Color";
        public static string ColorOkButton => IsTurkish ? "Tamam" : "OK";
        public static string ColorCancelButton => IsTurkish ? "İptal" : "Cancel";
    }
}
