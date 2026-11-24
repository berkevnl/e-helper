using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EHelper.Core;
using EHelper.Services;
using Microsoft.Win32;

using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace EHelper.UI
{
    public partial class ProfileEditWindow : Window
    {
        private readonly AppProfile _profile;
        private readonly bool _isNew;
        private string? _browsedAppPath;

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
            var accent = ThemeHelper.ApplyBasePalette(Resources, isDark, accentHex);
            ThemeHelper.Set(Resources, "AccentCyan", accent);
            ThemeHelper.Set(Resources, "SegmentBg", isDark ? ThemeHelper.Rgb(34, 37, 48) : ThemeHelper.Rgb(241, 245, 249));
            ThemeHelper.Set(Resources, "SegmentBorder", isDark ? ThemeHelper.Rgb(45, 49, 63) : ThemeHelper.Rgb(226, 232, 240));

            // Propagate theme brushes to Application resources so that Popups/dropdowns render correctly themed
            ThemeHelper.PublishToApplication(Resources);
        }

        private void ApplyLocalization()
        {
            TxtDialogTitle.Text = _isNew ? LocalizationManager.NewProfileHeader : LocalizationManager.EditProfileHeader;
            LblCapturedSettings.Text = LocalizationManager.CapturedSettingsLabel;
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

            TxtSummaryPower.Text = $"⚡ {LocalizationManager.PowerLabel}: {LocalizationManager.GetPowerModeName(_profile.PowerMode)}";
            TxtSummaryRgb.Text = $"🌈 RGB: {LocalizationManager.GetLedModeName(_profile.LedMode)} {LocalizationManager.FormatLedDetails(_profile)}";
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

                var running = Process.GetProcesses();
                var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in running)
                {
                    try
                    {
                        string name = p.ProcessName;
                        if (!ignored.Contains(name) && !name.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                        {
                            names.Add(name + ".exe");
                        }
                    }
                    catch { }
                    finally { p.Dispose(); }
                }

                foreach (var exe in names)
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
                _browsedAppPath = dlg.FileName; // committed to the profile only on Save

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
                MessageBox.Show(LocalizationManager.ProfileNameRequired,
                    "E-Helper", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtProfileName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(exe))
            {
                MessageBox.Show(LocalizationManager.ProfileAppRequired,
                    "E-Helper", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                exe += ".exe";
            }

            _profile.Name = name;
            _profile.ProcessName = exe;
            if (_browsedAppPath != null)
            {
                _profile.AppPath = _browsedAppPath;
            }

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
    }
}
