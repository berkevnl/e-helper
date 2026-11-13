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

        public static string PowerOptionsHeader => IsTurkish ? "GÜÇ SEÇENEKLERİ" : "POWER OPTIONS";
        public static string PowerOfficeSub => IsTurkish ? "Sessiz" : "Silent";
        public static string PowerGamingSub => IsTurkish ? "Günlük" : "Daily";
        public static string PowerTurboSub => IsTurkish ? "Turbo" : "Turbo";

        public static string SystemResourcesHeader => IsTurkish ? "SİSTEM KAYNAKLARI" : "SYSTEM RESOURCES";
        public static string CpuHeader => IsTurkish ? "İŞLEMCİ" : "CPU";
        public static string GpuCardHeader => IsTurkish ? "EKRAN KARTI" : "GPU";
        public static string UsageLabel => IsTurkish ? "Kullanım" : "Usage";
        public static string FanLabel => IsTurkish ? "Fan" : "Fan";
        public static string RamBellek => IsTurkish ? "RAM (Bellek)" : "RAM (Memory)";
        public static string SsdDepolama => IsTurkish ? "SSD (Depolama)" : "SSD (Storage)";

        public static string GpuHeader => IsTurkish ? "GPU Modu: Ayrık / Hibrit" : "GPU Mode: Discrete / Hybrid";
        public static string RamLabel => IsTurkish ? "RAM Bellek" : "RAM Memory";
        public static string SsdLabel => IsTurkish ? "SSD Disk (C:)" : "SSD Drive (C:)";

        public static string KeyboardHeader => IsTurkish ? "Laptop Klavyesi" : "Laptop Keyboard";
        public static string RgbKeyboardHeader => IsTurkish ? "RGB Klavye Aydınlatma" : "RGB Keyboard Lighting";
        public static string BrightnessLabel => IsTurkish ? "Parlaklık: " : "Brightness: ";
        public static string BrightnessSliderTitle => IsTurkish ? "Parlaklık" : "Brightness";

        public static string LedStatic => IsTurkish ? "Sabit" : "Static";
        public static string LedBreathing => IsTurkish ? "Nefes" : "Breathing";
        public static string LedDynamic => IsTurkish ? "Dinamik" : "Dynamic";
        public static string LedCycle => IsTurkish ? "Spektrum" : "Spectrum";
        public static string LedRainbow => IsTurkish ? "Gökkuşağı" : "Rainbow";
        public static string LedTripleZone => IsTurkish ? "3-Bölge" : "3-Zone";
        public static string ColorLabel => IsTurkish ? "Renk" : "Color";
        public static string ColorAuto => IsTurkish ? "Renk: Otomatik" : "Color: Auto";
        public static string ColorSelectionTitle => IsTurkish ? "Renk" : "Color";
        public static string ZoneColorsTitle => IsTurkish ? "Bölge Renkleri" : "Zone Colors";

        public static string CycleSpeedTitle => IsTurkish ? "Hız" : "Speed";
        public static string AnimationSpeedTitle => IsTurkish ? "Hız" : "Speed";
        public static string SpeedSlow => IsTurkish ? "Yavaş" : "Slow";
        public static string SpeedMedium => IsTurkish ? "Orta" : "Medium";
        public static string SpeedFast => IsTurkish ? "Hızlı" : "Fast";

        public static string ZoneLeftLabel => IsTurkish ? "Sol Bölge" : "Left Zone";
        public static string ZoneCenterLabel => IsTurkish ? "Orta Bölge" : "Center Zone";
        public static string ZoneRightLabel => IsTurkish ? "Sağ Bölge" : "Right Zone";
        public static string ZoneLeftShort => IsTurkish ? "Sol" : "Left";
        public static string ZoneCenterShort => IsTurkish ? "Orta" : "Center";
        public static string ZoneRightShort => IsTurkish ? "Sağ" : "Right";
        public static string KeyboardDiagramTitle => IsTurkish ? "Bölge Seç ve Rengini Belirle:" : "Select Zone & Set Color:";

        public static string CustomColorButton => IsTurkish ? "🎨 Özel Renk Seç" : "🎨 Custom Color";
        public static string CustomColorTitle => IsTurkish ? "Özel Renk" : "Custom Color";
        public static string PresetsTitle => IsTurkish ? "Hazır Renkler" : "Color Presets";

        public static string AutoStart => IsTurkish ? "Başlangıçta Çalıştır" : "Start with Windows";
        public static string SettingsTitle => IsTurkish ? "Ayarlar" : "Settings";
        public static string StartWithWindowsTitle => IsTurkish ? "Başlangıçta Çalıştır" : "Start with Windows";
        public static string StartWithWindowsDesc => IsTurkish ? "Windows açıldığında E-Helper otomatik başlatılsın" : "Launch E-Helper automatically on Windows startup";
        public static string AppColorThemeTitle => IsTurkish ? "Uygulama Renk Teması" : "Application Color Theme";
        public static string AppColorThemeDesc => IsTurkish ? "Çerçeve, vurgu ve aktif seçimlerin parlama rengi" : "Border, accent, and active selection glow color";
        public static string SelectCustomColor => IsTurkish ? "🎨 Renk Seç..." : "🎨 Pick Color...";

        public static string ProfilesTitle => IsTurkish ? "Hazır Profiller" : "Custom Profiles";
        public static string ProfilesDesc => IsTurkish ? "Oyun veya uygulamalara özel otomatik güç ve aydınlatma profilleri" : "Automatic power and lighting profiles for games or apps";
        public static string AddProfileBtn => IsTurkish ? "+ Profil Ekle" : "+ Add Profile";
        public static string NoProfiles => IsTurkish ? "Henüz bir profil eklenmedi." : "No profiles added yet.";
        public static string ProfileSetupBannerTitle => IsTurkish ? "🎯 Profil Ayar Modu" : "🎯 Profile Setup Mode";
        public static string ProfileSetupBannerDesc => IsTurkish ? "Güç ve RGB ayarlarını seçip onaylayın" : "Select power & RGB settings to confirm";
        public static string ConfirmSettings => IsTurkish ? "Onayla" : "Confirm";
        public static string Cancel => IsTurkish ? "İptal" : "Cancel";
        public static string Save => IsTurkish ? "Kaydet" : "Save";
        public static string ProfileNameLabel => IsTurkish ? "Profil Adı" : "Profile Name";
        public static string ProfileAppLabel => IsTurkish ? "Bağlı Uygulama (.exe)" : "Linked Application (.exe)";
        public static string SelectRunningApp => IsTurkish ? "-- Çalışan Uygulamalardan Seçin --" : "-- Select From Running Apps --";
        public static string Browse => IsTurkish ? "Gözat..." : "Browse...";
        public static string NewProfileHeader => IsTurkish ? "Yeni Profil Kaydet" : "Save New Profile";
        public static string EditProfileHeader => IsTurkish ? "Profili Düzenle" : "Edit Profile";
        public static string RecaptureSettingsBtn => IsTurkish ? "⚙️ Ayarları Yeniden Belirle" : "⚙️ Re-select Settings";
        public static string DeleteProfileTooltip => IsTurkish ? "Profili Sil" : "Delete Profile";
        public static string EditProfileTooltip => IsTurkish ? "Profili Düzenle" : "Edit Profile";
        public static string ProfileActivated => IsTurkish ? "- Profil: " : "- Profile: ";

        public static string CheckUpdates => IsTurkish ? "📥 Güncellemeler" : "📥 Updates";

        public static string ThemeLight => IsTurkish ? "Açık" : "Light";
        public static string ThemeDark => IsTurkish ? "Koyu" : "Dark";
        public static string Refresh => IsTurkish ? "Yenile" : "Refresh";
        public static string Exit => IsTurkish ? "Çıkış" : "Exit";
        public static string LangButton => IsTurkish ? "EN" : "TR";

        public static string ColorDialogTitle => IsTurkish ? "Renk Seçimi" : "Color";
        public static string ColorOkButton => IsTurkish ? "Tamam" : "OK";
        public static string ColorCancelButton => IsTurkish ? "İptal" : "Cancel";

        public static string VersionPrefix => IsTurkish ? "Sürüm: " : "Version: ";
        public static string DownloadPrefix => IsTurkish ? "İndir: " : "Download: ";
        public static string UpdatingStatus => IsTurkish ? "Güncelleniyor..." : "Updating...";
        public static string UpdateBannerTitle => IsTurkish ? "Güncellemesi Mevcut" : "Update Available";
        public static string UpdateNow => IsTurkish ? "Güncelle" : "Update";
        public static string UpToDate => IsTurkish ? "- Güncel" : "- Up to date";
        public static string CheckingUpdates => IsTurkish ? "- Kontrol ediliyor..." : "- Checking...";
        public static string CloseTooltip => IsTurkish ? "Kapat / Tepsiye Gizle" : "Close / Hide to Tray";
        public static string MinimizeTooltip => IsTurkish ? "Simge Durumuna Küçült" : "Minimize";
        public static string ThemeTooltip => IsTurkish ? "Tema (Koyu / Açık)" : "Theme (Dark / Light)";
        public static string LanguageTooltip => IsTurkish ? "Dil (TR / EN)" : "Language (TR / EN)";
    }
}
