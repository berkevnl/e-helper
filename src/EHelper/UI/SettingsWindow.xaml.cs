using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EHelper.Config;
using EHelper.Core;
using EHelper.Services;
using Button = System.Windows.Controls.Button;
using MediaColor = System.Windows.Media.Color;
using Orientation = System.Windows.Controls.Orientation;

namespace EHelper.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigManager _config;
        private bool _isInitialized;
        private bool _isDark;

        public event Action<string>? AccentColorChanged;
        public event Action<AppProfile>? RecaptureRequested;
        public bool RequestProfileCreation { get; private set; } = false;

        public SettingsWindow(ConfigManager config, bool isDark)
        {
            InitializeComponent();
            _config = config;
            _isDark = isDark;

            ApplyTheme(isDark);
            ApplyLocalization();

            // Load initial states
            _isInitialized = false;
            ChkStartWithWindows.IsChecked = StartupManager.IsStartupEnabled();
            UpdatePresetCheckmarks(_config.CurrentSettings.ThemeAccentColor);
            RenderProfilesList();
            _isInitialized = true;
        }

        public void ApplyTheme(bool isDark)
        {
            _isDark = isDark;
            string accentHex = _config.CurrentSettings.ThemeAccentColor ?? "#22D3EE";
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
                Resources["AccentCyan"] = new SolidColorBrush(accent);
                Resources["AccentColor"] = new SolidColorBrush(accent);
                Resources["CheckFg"] = new SolidColorBrush(MediaColor.FromRgb(20, 21, 27));
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
                Resources["AccentCyan"] = new SolidColorBrush(accent);
                Resources["AccentColor"] = new SolidColorBrush(accent);
                Resources["CheckFg"] = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255));
                Resources["BadgeBg"] = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249));
                Resources["BadgeBorder"] = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225));
            }

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
            TxtTitle.Text = LocalizationManager.SettingsTitle;
            TxtStartTitle.Text = LocalizationManager.StartWithWindowsTitle;
            TxtStartDesc.Text = LocalizationManager.StartWithWindowsDesc;
            TxtThemeColorTitle.Text = LocalizationManager.AppColorThemeTitle;
            TxtThemeColorDesc.Text = LocalizationManager.AppColorThemeDesc;
            TxtProfilesHeader.Text = LocalizationManager.ProfilesTitle;
            TxtProfilesDesc.Text = LocalizationManager.ProfilesDesc;
            BtnAddProfile.Content = LocalizationManager.AddProfileBtn;
        }

        private void ChkStartWithWindows_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            bool enable = ChkStartWithWindows.IsChecked == true;
            StartupManager.SetStartup(enable);

            _config.CurrentSettings.StartWithWindows = enable;
            _config.SaveSettings();
        }

        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                SetAccentColor(hex);
            }
        }

        private void SetAccentColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return;

            _config.CurrentSettings.ThemeAccentColor = hex;
            _config.SaveSettings();

            ApplyTheme(_isDark);
            UpdatePresetCheckmarks(hex);

            AccentColorChanged?.Invoke(hex);
        }

        private void UpdatePresetCheckmarks(string currentHex)
        {
            string upperCurrent = (currentHex ?? "#22D3EE").Trim().ToUpperInvariant();

            CheckCyan.Visibility = IsHexMatch("#22D3EE", upperCurrent) ? Visibility.Visible : Visibility.Collapsed;
            CheckBlue.Visibility = IsHexMatch("#3B82F6", upperCurrent) ? Visibility.Visible : Visibility.Collapsed;
            CheckPurple.Visibility = IsHexMatch("#8B5CF6", upperCurrent) ? Visibility.Visible : Visibility.Collapsed;
            CheckPink.Visibility = IsHexMatch("#EC4899", upperCurrent) ? Visibility.Visible : Visibility.Collapsed;
            CheckRed.Visibility = IsHexMatch("#EF4444", upperCurrent) ? Visibility.Visible : Visibility.Collapsed;
            CheckOrange.Visibility = IsHexMatch("#F97316", upperCurrent) ? Visibility.Visible : Visibility.Collapsed;
            CheckAmber.Visibility = IsHexMatch("#EAB308", upperCurrent) ? Visibility.Visible : Visibility.Collapsed;
            CheckGreen.Visibility = IsHexMatch("#10B981", upperCurrent) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnAddProfile_Click(object sender, RoutedEventArgs e)
        {
            RequestProfileCreation = true;
            Close();
        }

        public void RenderProfilesList()
        {
            PnlProfilesList.Children.Clear();
            var profiles = _config.CurrentSettings.Profiles;

            if (profiles == null || profiles.Count == 0)
            {
                var noProfiles = new TextBlock
                {
                    Text = LocalizationManager.NoProfiles,
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                noProfiles.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");
                PnlProfilesList.Children.Add(noProfiles);
                return;
            }

            foreach (var p in profiles)
            {
                var card = CreateProfileCard(p);
                PnlProfilesList.Children.Add(card);
            }
        }

        private UIElement CreateProfileCard(AppProfile p)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };
            border.SetResourceReference(Border.BackgroundProperty, "CardHoverBg");
            border.SetResourceReference(Border.BorderBrushProperty, "CardBorder");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left info
            var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            // Title + Process badge
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal };
            var titleBlock = new TextBlock
            {
                Text = p.Name,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            titlePanel.Children.Add(titleBlock);

            var badgeBorder = new Border
            {
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeBorder.SetResourceReference(Border.BackgroundProperty, "BadgeBg");
            badgeBorder.SetResourceReference(Border.BorderBrushProperty, "BadgeBorder");

            var badgeText = new TextBlock
            {
                Text = p.CleanProcessName,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold
            };
            badgeText.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");
            badgeBorder.Child = badgeText;
            titlePanel.Children.Add(badgeBorder);

            infoPanel.Children.Add(titlePanel);

            // Summary row
            string powerName = p.PowerMode switch
            {
                PowerMode.Office => LocalizationManager.PowerOffice,
                PowerMode.Gaming => LocalizationManager.PowerGaming,
                PowerMode.HighPerformance => LocalizationManager.PowerTurbo,
                _ => LocalizationManager.PowerGaming
            };

            string ledName = p.LedMode switch
            {
                LedMode.Rainbow => LocalizationManager.LedRainbow,
                LedMode.Cycle => LocalizationManager.LedCycle,
                LedMode.Static => LocalizationManager.LedStatic,
                LedMode.Breathing => LocalizationManager.LedBreathing,
                LedMode.ColorfulCycle => LocalizationManager.LedDynamic,
                LedMode.TripleZone => LocalizationManager.LedTripleZone,
                _ => LocalizationManager.LedStatic
            };

            string bright = p.LedBrightness switch { 0 => "%0", 1 => "%50", _ => "%100" };
            string ledDetails;
            if (p.LedMode == LedMode.Rainbow || p.LedMode == LedMode.Cycle)
            {
                string speedName = p.CycleSpeed switch
                {
                    1 => LocalizationManager.SpeedMedium,
                    2 => LocalizationManager.SpeedFast,
                    _ => LocalizationManager.SpeedSlow
                };
                ledDetails = $"({bright}, {speedName})";
            }
            else
            {
                ledDetails = $"({bright})";
            }

            var summaryBlock = new TextBlock
            {
                Text = $"⚡ {powerName}  •  🌈 {ledName} {ledDetails}",
                FontSize = 10,
                Margin = new Thickness(0, 3, 0, 0)
            };
            summaryBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");
            infoPanel.Children.Add(summaryBlock);

            Grid.SetColumn(infoPanel, 0);
            grid.Children.Add(infoPanel);

            // Right action buttons
            var actionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Edit button (Clean vector icon)
            var pathEdit = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M3,17.25 L3,21 L6.75,21 L17.81,9.94 L14.06,6.19 L3,17.25 Z M20.71,7.04 L16.96,3.29 L15.13,5.12 L18.88,8.87 L20.71,7.04 Z"),
                Stretch = Stretch.Uniform,
                Width = 11,
                Height = 11
            };
            pathEdit.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "TextSecondary");

            var btnEdit = new Button
            {
                Content = pathEdit,
                ToolTip = LocalizationManager.EditProfileTooltip,
                Style = (Style)Resources["MiniIconBtnStyle"],
                Margin = new Thickness(0, 0, 4, 0)
            };
            btnEdit.Click += (s, e) =>
            {
                var editWin = new ProfileEditWindow(p, isNew: false, _isDark, _config.CurrentSettings.ThemeAccentColor)
                {
                    Owner = this
                };
                double editW = editWin.Width > 0 ? editWin.Width : 410;
                editWin.Measure(new System.Windows.Size(editW, double.PositiveInfinity));
                double editH = editWin.DesiredSize.Height > 0 ? editWin.DesiredSize.Height : 450;
                double ownerW = ActualWidth > 0 ? ActualWidth : 390;
                double ownerH = ActualHeight > 0 ? ActualHeight : 400;
                editWin.Left = Left + (ownerW - editW) / 2;
                editWin.Top = Top + (ownerH - editH) / 2;

                if (editWin.ShowDialog() == true)
                {
                    _config.SaveSettings();
                    RenderProfilesList();
                }
                else if (editWin.RecaptureRequested)
                {
                    RecaptureRequested?.Invoke(p);
                    Close();
                }
            };
            actionsPanel.Children.Add(btnEdit);

            // Delete button (Clean vector icon)
            var pathDelete = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M6,19 C6,20.1 6.9,21 8,21 L16,21 C17.1,21 18,20.1 18,19 L18,7 L6,7 L6,19 Z M19,4 L15.5,4 L14.5,3 L9.5,3 L8.5,4 L5,4 L5,6 L19,6 L19,4 Z"),
                Stretch = Stretch.Uniform,
                Width = 11,
                Height = 11
            };
            pathDelete.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "TextSecondary");

            var btnDelete = new Button
            {
                Content = pathDelete,
                ToolTip = LocalizationManager.DeleteProfileTooltip,
                Style = (Style)Resources["MiniIconBtnStyle"]
            };
            btnDelete.Click += (s, e) =>
            {
                _config.CurrentSettings.Profiles.Remove(p);
                _config.SaveSettings();
                RenderProfilesList();
            };
            actionsPanel.Children.Add(btnDelete);

            Grid.SetColumn(actionsPanel, 1);
            grid.Children.Add(actionsPanel);

            border.Child = grid;
            return border;
        }

        private static bool IsHexMatch(string a, string b)
        {
            return string.Equals(a.TrimStart('#'), b.TrimStart('#'), StringComparison.OrdinalIgnoreCase);
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
