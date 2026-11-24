using System;
using EHelper.Core;

namespace EHelper.Services
{
    public static class LocalizationManager
    {
        public static string CurrentLanguage { get; set; } = "TR";

        public static bool IsTurkish => CurrentLanguage.Equals("TR", StringComparison.OrdinalIgnoreCase);

        public static string PowerOffice => IsTurkish ? "Tasarruf" : "Eco";
        public static string PowerGaming => IsTurkish ? "Dengeli" : "Balanced";
        public static string PowerTurbo => IsTurkish ? "Performans" : "Performance";

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

        public static string RgbKeyboardHeader => IsTurkish ? "RGB Klavye Aydınlatma" : "RGB Keyboard Lighting";
        public static string BrightnessSliderTitle => IsTurkish ? "Parlaklık" : "Brightness";

        public static string LedStatic => IsTurkish ? "Sabit" : "Static";
        public static string LedBreathing => IsTurkish ? "Nefes" : "Breathing";
        public static string LedDynamic => IsTurkish ? "Dinamik" : "Dynamic";
        public static string LedCycle => IsTurkish ? "Spektrum" : "Spectrum";
        public static string LedRainbow => IsTurkish ? "Gökkuşağı" : "Rainbow";
        public static string LedTripleZone => IsTurkish ? "3-Bölge" : "3-Zone";
        public static string ColorLabel => IsTurkish ? "Renk" : "Color";
        public static string ZoneColorsTitle => IsTurkish ? "Bölge Renkleri" : "Zone Colors";

        public static string CycleSpeedTitle => IsTurkish ? "Hız" : "Speed";
        public static string SpeedSlow => IsTurkish ? "Yavaş" : "Slow";
        public static string SpeedMedium => IsTurkish ? "Orta" : "Medium";
        public static string SpeedFast => IsTurkish ? "Hızlı" : "Fast";

        public static string ZoneLeftShort => IsTurkish ? "Sol" : "Left";
        public static string ZoneCenterShort => IsTurkish ? "Orta" : "Center";
        public static string ZoneRightShort => IsTurkish ? "Sağ" : "Right";

        public static string SettingsTitle => IsTurkish ? "Ayarlar" : "Settings";
        public static string StartWithWindowsTitle => IsTurkish ? "Başlangıçta Çalıştır" : "Start with Windows";
        public static string StartWithWindowsDesc => IsTurkish ? "Windows açıldığında E-Helper otomatik başlatılsın" : "Launch E-Helper automatically on Windows startup";
        public static string AppColorThemeTitle => IsTurkish ? "Uygulama Renk Teması" : "Application Color Theme";
        public static string AppColorThemeDesc => IsTurkish ? "Çerçeve, vurgu ve aktif seçimlerin parlama rengi" : "Border, accent, and active selection glow color";

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

        public static string Refresh => IsTurkish ? "Yenile" : "Refresh";
        public static string Exit => IsTurkish ? "Çıkış" : "Exit";

        public static string ColorDialogTitle => IsTurkish ? "Renk Seçimi" : "Color";
        public static string ColorOkButton => IsTurkish ? "Tamam" : "OK";
        public static string ColorCancelButton => IsTurkish ? "İptal" : "Cancel";

        public static string UpdatingStatus => IsTurkish ? "Güncelleniyor..." : "Updating...";
        public static string DownloadingStatus => IsTurkish ? "İndiriliyor" : "Downloading";
        public static string ExtractingStatus => IsTurkish ? "Paket açılıyor ve güncelleniyor..." : "Extracting & updating...";
        public static string UpdateBannerTitle => IsTurkish ? "Güncellemesi Mevcut" : "Update Available";
        public static string UpdateNow => IsTurkish ? "Güncelle" : "Update";
        public static string UpToDate => IsTurkish ? "- Güncel" : "- Up to date";
        public static string CloseTooltip => IsTurkish ? "Kapat / Tepsiye Gizle" : "Close / Hide to Tray";
        public static string ThemeTooltip => IsTurkish ? "Tema (Koyu / Açık)" : "Theme (Dark / Light)";
        public static string LanguageTooltip => IsTurkish ? "Dil (TR / EN)" : "Language (TR / EN)";

        public static string KeyboardSleepTimer => IsTurkish ? "Klavye Uyku Zamanlayıcısı" : "Keyboard Sleep Timer";
        public static string SleepDisabled => IsTurkish ? "Devre Dışı" : "Disabled";
        public static string Sleep15s => IsTurkish ? "15 saniye" : "15 seconds";
        public static string Sleep30s => IsTurkish ? "30 saniye" : "30 seconds";
        public static string Sleep45s => IsTurkish ? "45 saniye" : "45 seconds";
        public static string Sleep60s => IsTurkish ? "60 saniye" : "60 seconds";
        public static string Sleep5m => IsTurkish ? "5 dakika" : "5 minutes";
        public static string SleepOffLabel => IsTurkish ? "Kapalı" : "Off";

        public static string TrayOpen => IsTurkish ? "E-Helper'ı Aç" : "Open E-Helper";
        public static string PowerLabel => IsTurkish ? "Güç" : "Power";
        public static string CapturedSettingsLabel => IsTurkish ? "Seçilen Donanım Ayarları:" : "Captured Hardware Settings:";
        public static string ProfileNameRequired => IsTurkish ? "Lütfen profil adı girin." : "Please enter a profile name.";
        public static string ProfileAppRequired => IsTurkish ? "Lütfen bir uygulama (.exe) seçin." : "Please select an application (.exe).";

        public static string GetPowerModeName(PowerMode mode) => mode switch
        {
            PowerMode.Office => PowerOffice,
            PowerMode.HighPerformance => PowerTurbo,
            _ => PowerGaming
        };

        public static string GetLedModeName(LedMode mode) => mode switch
        {
            LedMode.Rainbow => LedRainbow,
            LedMode.Cycle => LedCycle,
            LedMode.Breathing => LedBreathing,
            LedMode.ColorfulCycle => LedDynamic,
            LedMode.TripleZone => LedTripleZone,
            _ => LedStatic
        };

        public static string GetSpeedName(int speed) => speed switch
        {
            1 => SpeedMedium,
            2 => SpeedFast,
            _ => SpeedSlow
        };

        /// <summary>"(%100)" or "(%100, Hızlı)" for animated modes.</summary>
        public static string FormatLedDetails(ILightingProfile p)
        {
            string bright = p.LedBrightness switch { 0 => "%0", 1 => "%50", _ => "%100" };
            return p.LedMode is LedMode.Rainbow or LedMode.Cycle
                ? $"({bright}, {GetSpeedName(p.CycleSpeed)})"
                : $"({bright})";
        }

        /// <summary>Long label used in the sleep timer menu and tooltip.</summary>
        public static string GetSleepOptionLabel(int seconds) => seconds switch
        {
            15 => Sleep15s,
            30 => Sleep30s,
            45 => Sleep45s,
            60 => Sleep60s,
            300 => Sleep5m,
            _ => SleepDisabled
        };

        /// <summary>Compact label shown next to the sleep timer icon.</summary>
        public static string GetSleepShortLabel(int seconds) => seconds switch
        {
            15 or 30 or 45 or 60 => $"{seconds}s",
            300 => IsTurkish ? "5dk" : "5m",
            _ => SleepOffLabel
        };
    }
}
