using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EHelper.Config;
using EHelper.Core;
using EHelper.Services;
using Button = System.Windows.Controls.Button;
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
            var accent = ThemeHelper.ApplyBasePalette(Resources, isDark, _config.CurrentSettings.ThemeAccentColor);
            ThemeHelper.Set(Resources, "AccentCyan", accent);
            ThemeHelper.Set(Resources, "CheckFg", isDark ? ThemeHelper.Rgb(20, 21, 27) : ThemeHelper.Rgb(255, 255, 255));

            ThemeHelper.PublishToApplication(Resources);
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

        private void UpdatePresetCheckmarks(string? currentHex)
        {
            string current = currentHex ?? "#22D3EE";
            var presets = new (UIElement Check, string Hex)[]
            {
                (CheckCyan, "#22D3EE"), (CheckBlue, "#3B82F6"), (CheckPurple, "#8B5CF6"), (CheckPink, "#EC4899"),
                (CheckRed, "#EF4444"), (CheckOrange, "#F97316"), (CheckAmber, "#EAB308"), (CheckGreen, "#10B981")
            };

            foreach (var (check, hex) in presets)
            {
                check.Visibility = IsHexMatch(hex, current) ? Visibility.Visible : Visibility.Collapsed;
            }
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
            var summaryBlock = new TextBlock
            {
                Text = $"⚡ {LocalizationManager.GetPowerModeName(p.PowerMode)}  •  🌈 {LocalizationManager.GetLedModeName(p.LedMode)} {LocalizationManager.FormatLedDetails(p)}",
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

            var btnEdit = CreateIconButton(EditIconData, LocalizationManager.EditProfileTooltip, new Thickness(0, 0, 4, 0));
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

            var btnDelete = CreateIconButton(DeleteIconData, LocalizationManager.DeleteProfileTooltip, new Thickness(0));
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

        private const string EditIconData = "M3,17.25 L3,21 L6.75,21 L17.81,9.94 L14.06,6.19 L3,17.25 Z M20.71,7.04 L16.96,3.29 L15.13,5.12 L18.88,8.87 L20.71,7.04 Z";
        private const string DeleteIconData = "M6,19 C6,20.1 6.9,21 8,21 L16,21 C17.1,21 18,20.1 18,19 L18,7 L6,7 L6,19 Z M19,4 L15.5,4 L14.5,3 L9.5,3 L8.5,4 L5,4 L5,6 L19,6 L19,4 Z";

        private Button CreateIconButton(string pathData, string tooltip, Thickness margin)
        {
            var icon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(pathData),
                Stretch = Stretch.Uniform,
                Width = 11,
                Height = 11
            };
            icon.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "TextSecondary");

            return new Button
            {
                Content = icon,
                ToolTip = tooltip,
                Style = (Style)Resources["MiniIconBtnStyle"],
                Margin = margin
            };
        }

        private static bool IsHexMatch(string a, string b)
        {
            return string.Equals(a.Trim().TrimStart('#'), b.Trim().TrimStart('#'), StringComparison.OrdinalIgnoreCase);
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
