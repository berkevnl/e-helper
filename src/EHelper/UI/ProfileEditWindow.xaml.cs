using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EHelper.Core;
using EHelper.Services;
using Microsoft.Win32;

using Button = System.Windows.Controls.Button;
using MediaColor = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace EHelper.UI
{
    public partial class ProfileEditWindow : Window
    {
        private readonly AppProfile _profile;
        private readonly bool _isNew;

        public AppProfile Profile => _profile;
        public bool RecaptureRequested { get; private set; } = false;

        public ProfileEditWindow(AppProfile profile, bool isNew, bool isDark, string accentHex)
        {
            InitializeComponent();
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _isNew = isNew;

            ApplyTheme(isDark, accentHex);
            ApplyLocalization();

            PopulateRunningApps();
            LoadProfileData();
        }

        private void ApplyTheme(bool isDark, string accentHex)
        {
            var accent = ParseColor(accentHex, MediaColor.FromRgb(34, 211, 238));

            if (isDark)
            {
                Resources["WindowBg"] = new SolidColorBrush(MediaColor.FromRgb(20, 21, 27));       // #14151B
                Resources["CardBg"] = new SolidColorBrush(MediaColor.FromRgb(28, 30, 38));         // #1C1E26
                Resources["CardHoverBg"] = new SolidColorBrush(MediaColor.FromRgb(34, 37, 48));    // #222530
                Resources["CardBorder"] = new SolidColorBrush(MediaColor.FromRgb(37, 40, 52));     // #252834
                Resources["CardActiveBorder"] = new SolidColorBrush(accent);
                Resources["CardActiveBg"] = new SolidColorBrush(MediaColor.FromArgb(50, accent.R, accent.G, accent.B));
                Resources["TextPrimary"] = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255));
                Resources["TextSecondary"] = new SolidColorBrush(MediaColor.FromRgb(212, 215, 226));
                Resources["TextMuted"] = new SolidColorBrush(MediaColor.FromRgb(101, 106, 123));   // #656A7B
                Resources["AccentColor"] = new SolidColorBrush(accent);
                Resources["AccentCyan"] = new SolidColorBrush(accent);
                Resources["SegmentBg"] = new SolidColorBrush(MediaColor.FromRgb(34, 37, 48));
                Resources["SegmentBorder"] = new SolidColorBrush(MediaColor.FromRgb(45, 49, 63));
                Resources["BadgeBg"] = new SolidColorBrush(MediaColor.FromRgb(34, 36, 47));
                Resources["BadgeBorder"] = new SolidColorBrush(MediaColor.FromRgb(45, 49, 63));
            }
            else
            {
                Resources["WindowBg"] = new SolidColorBrush(MediaColor.FromRgb(248, 250, 252));
                Resources["CardBg"] = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255));
                Resources["CardHoverBg"] = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249));
                Resources["CardBorder"] = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240));
                Resources["CardActiveBorder"] = new SolidColorBrush(accent);
                Resources["CardActiveBg"] = new SolidColorBrush(MediaColor.FromArgb(30, accent.R, accent.G, accent.B));
                Resources["TextPrimary"] = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42));
                Resources["TextSecondary"] = new SolidColorBrush(MediaColor.FromRgb(51, 65, 85));
                Resources["TextMuted"] = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139));
                Resources["AccentColor"] = new SolidColorBrush(accent);
                Resources["AccentCyan"] = new SolidColorBrush(accent);
                Resources["SegmentBg"] = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249));
                Resources["SegmentBorder"] = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240));
                Resources["BadgeBg"] = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249));
                Resources["BadgeBorder"] = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225));
            }

            // Propagate theme brushes to Application resources so that Popups/dropdowns render correctly themed
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Resources["CardBg"] = Resources["CardBg"];
                System.Windows.Application.Current.Resources["CardHoverBg"] = Resources["CardHoverBg"];
                System.Windows.Application.Current.Resources["CardBorder"] = Resources["CardBorder"];
                System.Windows.Application.Current.Resources["CardActiveBorder"] = Resources["CardActiveBorder"];
                System.Windows.Application.Current.Resources["CardActiveBg"] = Resources["CardActiveBg"];
                System.Windows.Application.Current.Resources["TextPrimary"] = Resources["TextPrimary"];
                System.Windows.Application.Current.Resources["TextSecondary"] = Resources["TextSecondary"];
                System.Windows.Application.Current.Resources["TextMuted"] = Resources["TextMuted"];
                System.Windows.Application.Current.Resources["AccentColor"] = Resources["AccentColor"];
            }
        }

        private void ApplyLocalization()
        {
            TxtDialogTitle.Text = _isNew ? LocalizationManager.NewProfileHeader : LocalizationManager.EditProfileHeader;
            LblCapturedSettings.Text = LocalizationManager.IsTurkish ? "Seçilen Donanım Ayarları:" : "Captured Hardware Settings:";
            LblProfileName.Text = LocalizationManager.ProfileNameLabel;
            LblProfileApp.Text = LocalizationManager.ProfileAppLabel;
            BtnBrowse.Content = LocalizationManager.Browse;
            BtnCancel.Content = LocalizationManager.Cancel;
            BtnSave.Content = LocalizationManager.Save;
            BtnRecapture.Content = LocalizationManager.RecaptureSettingsBtn;
        }

        private void BtnRecapture_Click(object sender, RoutedEventArgs e)
        {
            RecaptureRequested = true;
            DialogResult = false;
            Close();
        }

        private void LoadProfileData()
        {
            TxtProfileName.Text = _profile.Name;
            TxtProcessName.Text = _profile.ProcessName;

            // Summary power mode
            string powerName = _profile.PowerMode switch
            {
                PowerMode.Office => LocalizationManager.PowerOffice,
                PowerMode.Gaming => LocalizationManager.PowerGaming,
                PowerMode.HighPerformance => LocalizationManager.PowerTurbo,
                _ => LocalizationManager.PowerGaming
            };
            string powerLabel = LocalizationManager.IsTurkish ? "Güç" : "Power";
            TxtSummaryPower.Text = $"⚡ {powerLabel}: {powerName}";

            // Summary RGB mode
            string ledName = _profile.LedMode switch
            {
                LedMode.Rainbow => LocalizationManager.LedRainbow,
                LedMode.Cycle => LocalizationManager.LedCycle,
                LedMode.Static => LocalizationManager.LedStatic,
                LedMode.Breathing => LocalizationManager.LedBreathing,
                LedMode.ColorfulCycle => LocalizationManager.LedDynamic,
                LedMode.TripleZone => LocalizationManager.LedTripleZone,
                _ => LocalizationManager.LedStatic
            };

            string brightText = _profile.LedBrightness switch { 0 => "%0", 1 => "%50", _ => "%100" };
            string ledDetails;
            if (_profile.LedMode == LedMode.Rainbow || _profile.LedMode == LedMode.Cycle)
            {
                string speedText = _profile.CycleSpeed switch
                {
                    1 => LocalizationManager.SpeedMedium,
                    2 => LocalizationManager.SpeedFast,
                    _ => LocalizationManager.SpeedSlow
                };
                ledDetails = $"({brightText}, {speedText})";
            }
            else
            {
                ledDetails = $"({brightText})";
            }

            TxtSummaryRgb.Text = $"🌈 RGB: {ledName} {ledDetails}";
        }

        private void PopulateRunningApps()
        {
            CmbRunningApps.Items.Clear();
            CmbRunningApps.Items.Add(new ComboBoxItem
            {
                Content = LocalizationManager.SelectRunningApp,
                Tag = "",
                IsEnabled = false,
                IsSelected = true
            });

            try
            {
                var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "system", "idle", "svchost", "dwm", "csrss", "smss", "wininit", "services",
                    "lsass", "conhost", "explorer", "e-helper", "taskhostw", "sihost", "fontdrvhost"
                };

                var procs = Process.GetProcesses()
                    .Where(p => !ignored.Contains(p.ProcessName) && !p.ProcessName.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                    .Select(p =>
                    {
                        try { return p.ProcessName + ".exe"; } catch { return ""; }
                    })
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var exe in procs)
                {
                    CmbRunningApps.Items.Add(new ComboBoxItem
                    {
                        Content = exe,
                        Tag = exe
                    });
                }
            }
            catch { }
        }

        private void CmbRunningApps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRunningApps.SelectedItem is ComboBoxItem item && item.Tag is string exe && !string.IsNullOrEmpty(exe))
            {
                TxtProcessName.Text = exe;
                if (string.IsNullOrWhiteSpace(TxtProfileName.Text))
                {
                    TxtProfileName.Text = Path.GetFileNameWithoutExtension(exe);
                }
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = LocalizationManager.ProfileAppLabel,
                Filter = "Uygulamalar (*.exe)|*.exe|Tüm Dosyalar (*.*)|*.*",
                CheckFileExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                string filename = Path.GetFileName(dlg.FileName);
                TxtProcessName.Text = filename;
                _profile.AppPath = dlg.FileName;

                if (string.IsNullOrWhiteSpace(TxtProfileName.Text))
                {
                    TxtProfileName.Text = Path.GetFileNameWithoutExtension(filename);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtProfileName.Text.Trim();
            string exe = TxtProcessName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(LocalizationManager.IsTurkish ? "Lütfen profil adı girin." : "Please enter a profile name.",
                    "E-Helper", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtProfileName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(exe))
            {
                MessageBox.Show(LocalizationManager.IsTurkish ? "Lütfen bir uygulama (.exe) seçin." : "Please select an application (.exe).",
                    "E-Helper", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                exe += ".exe";
            }

            _profile.Name = name;
            _profile.ProcessName = exe;

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private static MediaColor ParseColor(string hex, MediaColor fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex)) return fallback;
                string clean = hex.Trim().TrimStart('#');
                if (clean.Length == 6 &&
                    byte.TryParse(clean.Substring(0, 2), NumberStyles.HexNumber, null, out byte r) &&
                    byte.TryParse(clean.Substring(2, 2), NumberStyles.HexNumber, null, out byte g) &&
                    byte.TryParse(clean.Substring(4, 2), NumberStyles.HexNumber, null, out byte b))
                {
                    return MediaColor.FromRgb(r, g, b);
                }
            }
            catch { }
            return fallback;
        }
    }
}
