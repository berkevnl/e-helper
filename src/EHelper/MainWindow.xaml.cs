using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EHelper.Config;
using EHelper.Core;
using EHelper.Hardware.Casper;
using EHelper.Hardware.Simulation;
using EHelper.Services;
using Microsoft.Win32;

using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace EHelper
{
    public partial class MainWindow : Window
    {
        private readonly IHardwareProvider _bridge;
        private readonly bool _isSimulated;
        private readonly ConfigManager _config;
        private readonly HardwareMonitorService _monitor;

        private PowerMode _currentPowerMode;
        private LedMode _currentLedMode;
        private LedMode _lastActiveLedMode = LedMode.Static;
        private byte _brightness;
        private byte _red;
        private byte _green;
        private byte _blue;

        // 3-Bölge RGB renkleri ve aktif seçili bölge (0: Sol, 1: Orta, 2: Sağ)
        private byte _zoneLeftR = 0, _zoneLeftG = 229, _zoneLeftB = 255;
        private byte _zoneCenterR = 34, _zoneCenterG = 197, _zoneCenterB = 94;
        private byte _zoneRightR = 239, _zoneRightG = 68, _zoneRightB = 68;
        private int _activeZoneIndex = 0;

        private bool _isDarkTheme;

        private bool _isInitialized;

        public MainWindow(IHardwareProvider bridge, bool isSimulated, ConfigManager config, HardwareMonitorService monitor)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _isSimulated = isSimulated;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

            InitializeComponent();

            _isInitialized = false;
            ApplyInitialState();
            _isInitialized = true;
            SubscribeTelemetry();
        }

        private void ApplyInitialState()
        {
            var s = _config.CurrentSettings;
            _currentPowerMode = s.PowerMode;
            _currentLedMode = s.LedMode;
            _brightness = Math.Min(s.LedBrightness, (byte)2);
            _red = s.Red;
            _green = s.Green;
            _blue = s.Blue;

            _zoneLeftR = s.ZoneLeftRed;
            _zoneLeftG = s.ZoneLeftGreen;
            _zoneLeftB = s.ZoneLeftBlue;
            _zoneCenterR = s.ZoneCenterRed;
            _zoneCenterG = s.ZoneCenterGreen;
            _zoneCenterB = s.ZoneCenterBlue;
            _zoneRightR = s.ZoneRightRed;
            _zoneRightG = s.ZoneRightGreen;
            _zoneRightB = s.ZoneRightBlue;

            _isDarkTheme = s.IsDarkTheme;

            if (_currentLedMode == LedMode.Off)
            {
                _currentLedMode = LedMode.Static;
                _brightness = 0;
            }
            else
            {
                _lastActiveLedMode = _currentLedMode;
            }

            // Load Language
            LocalizationManager.CurrentLanguage = s.Language ?? "TR";

            // Apply Theme & Localization
            ApplyTheme(_isDarkTheme);
            ApplyLocalization();

            // UI Initial Values
            TxtModelBadge.Text = _bridge.DeviceModel;
            if (TxtVersionInfo != null)
            {
                TxtVersionInfo.Text = $"v{UpdateService.CurrentVersion}";
            }

            SliderBrightness.Value = _brightness;
            UpdateBrightnessUI();
            UpdateColorPreview();

            // Synchronize Windows Startup state asynchronously in background (0ms UI startup delay)
            ChkAutoStart.IsChecked = s.StartWithWindows;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    StartupManager.EnsureStartupSynchronized(s.StartWithWindows);
                    bool actual = StartupManager.IsStartupEnabled();
                    Dispatcher.InvokeAsync(() =>
                    {
                        ChkAutoStart.IsChecked = actual;
                    });
                }
                catch { }
            });

            UpdatePowerModeButtonsUI();
            UpdateLedModeButtonsUI();

            // Initial push to hardware asynchronously in background (0ms UI startup delay)
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    _bridge.SetPowerMode(_currentPowerMode);
                    ExcaliburPowerPlan.ApplyPowerPlan(_currentPowerMode);
                    ApplyLedSettings();
                }
                catch { }
            });

            // Gaming laptop EC / BIOS post-boot synchronization:
            // Laptop EC firmware and ACPI drivers often perform a reset 1-3 seconds after Windows logon.
            // Re-applying the user's saved lighting and power mode after short delays ensures
            // user settings are preserved across reboots and shutdowns.
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(1500);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _bridge.SetPowerMode(_currentPowerMode);
                        ExcaliburPowerPlan.ApplyPowerPlan(_currentPowerMode);
                        ApplyLedSettings();
                    });

                    await System.Threading.Tasks.Task.Delay(3500);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ApplyLedSettings();
                    });
                }
                catch
                {
                    // Ignore background async re-apply exceptions
                }
            });

            // Initial System Metrics query
            UpdateSystemResourcesUI();
        }

        private void SubscribeTelemetry()
        {
            _monitor.TelemetryUpdated += telemetry =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    UpdateTelemetryUI(telemetry);

                    // Only refresh RAM/SSD metrics when the user is actively viewing the panel
                    if (IsVisible)
                    {
                        UpdateSystemResourcesUI();
                    }
                });
            };
        }

        private void UpdateTelemetryUI(HardwareTelemetry t)
        {
            if (!t.IsAvailable)
            {
                TxtCpuTemp.Text = "--°C";
                TxtGpuTemp.Text = "--°C";
                TxtCpuRpm.Text = "--RPM";
                TxtGpuRpm.Text = "--RPM";
                return;
            }

            TxtCpuTemp.Text = t.CpuTemperature > 0 ? $"{t.CpuTemperature}°C" : "--°C";
            TxtGpuTemp.Text = t.GpuTemperature > 0 ? $"{t.GpuTemperature}°C" : "--°C";

            TxtCpuRpm.Text = $"{t.CpuFanRpm}RPM";
            TxtGpuRpm.Text = $"{t.GpuFanRpm}RPM";
        }

        private void UpdateSystemResourcesUI()
        {
            try
            {
                var metrics = SystemResourceMonitor.GetMetrics();

                TxtRamPercent.Text = $"{metrics.RamPercent}%";
                TxtRamUsage.Text = $"{metrics.RamUsedGb:F1} / {metrics.RamTotalGb:F1} GB";
                PbRamUsage.Value = metrics.RamPercent;

                TxtSsdPercent.Text = $"{metrics.SsdPercent}%";
                TxtSsdUsage.Text = $"{metrics.SsdUsedGb:F0} / {metrics.SsdTotalGb:F0} GB";
                PbSsdUsage.Value = metrics.SsdPercent;
            }
            catch { }
        }

        #region Flyout & Auto-Hide & Memory Trim

        public void PositionBottomRight()
        {
            var workArea = SystemParameters.WorkArea;
            Left = Math.Max(workArea.Left + 8, workArea.Right - Width - 8);
            Top = Math.Max(workArea.Top + 8, workArea.Bottom - Height - 8);
        }

        public void ToggleFlyout()
        {
            if (IsVisible)
            {
                Hide();
                MemoryOptimizer.TrimMemory();
            }
            else
            {
                PositionBottomRight();
                UpdateSystemResourcesUI();
                Show();
                Activate();
                Focus();

                // Optimize memory after UI layout is rendered on screen (drops RAM down to ~15-30 MB)
                Dispatcher.InvokeAsync(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(300);
                    MemoryOptimizer.TrimMemory();
                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            // Çarpıya basılmadığı sürece pencerenin kapanmaması için OnDeactivated otomatik gizleme kaldırıldı
        }

        private void BtnCloseToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            MemoryOptimizer.TrimMemory();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        #endregion

        #region Power Modes

        private void BtnModeOffice_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(PowerMode.Office);
        }

        private void BtnModeGaming_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(PowerMode.Gaming);
        }

        private void BtnModeHighPerf_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(PowerMode.HighPerformance);
        }

        private void SetPowerMode(PowerMode mode)
        {
            _currentPowerMode = mode;
            UpdatePowerModeButtonsUI();

            _bridge.SetPowerMode(mode);
            ExcaliburPowerPlan.ApplyPowerPlan(mode);

            _config.CurrentSettings.PowerMode = mode;
            _config.SaveSettings();
        }

        private void UpdatePowerModeButtonsUI()
        {
            var normalStyle = TryFindResource("GHelperTileStyle") as Style;
            var activeStyle = TryFindResource("ActiveGHelperTileStyle") as Style;

            if (BtnModeOffice != null && normalStyle != null && activeStyle != null)
            {
                BtnModeOffice.Style = _currentPowerMode == PowerMode.Office ? activeStyle : normalStyle;
                BtnModeGaming.Style = _currentPowerMode == PowerMode.Gaming ? activeStyle : normalStyle;
                BtnModeHighPerf.Style = _currentPowerMode == PowerMode.HighPerformance ? activeStyle : normalStyle;
            }

            if (TxtPowerModeTitle != null)
            {
                TxtPowerModeTitle.Text = LocalizationManager.GetPowerModeTitle(_currentPowerMode);
            }
        }

        #endregion

        #region RGB Keyboard Control

        private void CmbLedMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || CmbLedMode.SelectedItem is not ComboBoxItem item) return;

            string tag = item.Tag?.ToString() ?? "Static";
            LedMode newMode = tag switch
            {
                "Breathing" => LedMode.Breathing,
                "ColorfulCycle" => LedMode.ColorfulCycle,
                "Rainbow" => LedMode.Rainbow,
                "TripleZone" => LedMode.TripleZone,
                _ => LedMode.Static
            };

            SetLedMode(newMode);
        }

        private void BtnModeColorAction_Click(object sender, RoutedEventArgs e)
        {
            // Only functional for Static and Breathing modes
            if (_currentLedMode == LedMode.Static || _currentLedMode == LedMode.Breathing)
            {
                BtnCustomColor_Click(sender, e);
            }
        }

        private void SetLedMode(LedMode mode)
        {
            _currentLedMode = mode;
            _lastActiveLedMode = _currentLedMode;

            // Aydınlatma 0 ise kullanıcı mod seçtiğinde otomatik olarak %100 seviyesine (2) aç
            if (_brightness == 0)
            {
                _brightness = 2;
                if (SliderBrightness != null && SliderBrightness.Value != 2)
                {
                    SliderBrightness.Value = 2;
                }
                UpdateBrightnessUI();
            }

            UpdateLedModeButtonsUI();
            UpdateTripleZoneUI();
            UpdateColorPreview();
            ApplyLedSettings();
        }

        private void UpdateLedModeButtonsUI()
        {
            // Sync ComboBox selection
            if (CmbLedMode != null)
            {
                string targetTag = _currentLedMode switch
                {
                    LedMode.Breathing => "Breathing",
                    LedMode.ColorfulCycle => "ColorfulCycle",
                    LedMode.Rainbow => "Rainbow",
                    LedMode.TripleZone => "TripleZone",
                    _ => "Static"
                };

                for (int i = 0; i < CmbLedMode.Items.Count; i++)
                {
                    if (CmbLedMode.Items[i] is ComboBoxItem item && item.Tag?.ToString() == targetTag)
                    {
                        if (CmbLedMode.SelectedIndex != i)
                        {
                            CmbLedMode.SelectedIndex = i;
                        }
                        break;
                    }
                }
            }

            // 3-Bölge diyagramı yalnızca TripleZone modunda görünür
            if (PnlTripleZoneDiagram != null)
            {
                PnlTripleZoneDiagram.Visibility = _currentLedMode == LedMode.TripleZone ? Visibility.Visible : Visibility.Collapsed;
            }

            // Yan Renk Butonu (BtnModeColorAction): Sabit ve Nefes modlarında etkindir; Dinamik, Gökkuşağı ve 3-Bölge modlarında devre dışı/soluk
            if (BtnModeColorAction != null)
            {
                bool canColor = _currentLedMode == LedMode.Static || _currentLedMode == LedMode.Breathing;
                BtnModeColorAction.IsEnabled = canColor;
                BtnModeColorAction.Opacity = canColor ? 1.0 : 0.45;
                if (BrdModeColorIndicator != null)
                {
                    BrdModeColorIndicator.Visibility = canColor ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        #region 3-Zone Keyboard Interactive Diagram Handlers

        private void BtnZoneLeft_Click(object sender, RoutedEventArgs e)
        {
            _activeZoneIndex = 0;
            UpdateTripleZoneUI();
            UpdateColorPreview();
            // Kullanıcı sol bölgeye bastığında doğrudan renk seçicisini açar
            BtnCustomColor_Click(sender, e);
        }

        private void BtnZoneCenter_Click(object sender, RoutedEventArgs e)
        {
            _activeZoneIndex = 1;
            UpdateTripleZoneUI();
            UpdateColorPreview();
            // Kullanıcı orta bölgeye bastığında doğrudan renk seçicisini açar
            BtnCustomColor_Click(sender, e);
        }

        private void BtnZoneRight_Click(object sender, RoutedEventArgs e)
        {
            _activeZoneIndex = 2;
            UpdateTripleZoneUI();
            UpdateColorPreview();
            // Kullanıcı sağ bölgeye bastığında doğrudan renk seçicisini açar
            BtnCustomColor_Click(sender, e);
        }

        private void UpdateTripleZoneUI()
        {
            // Renk fırçaları
            var leftBrush = new SolidColorBrush(Color.FromRgb(_zoneLeftR, _zoneLeftG, _zoneLeftB));
            var centerBrush = new SolidColorBrush(Color.FromRgb(_zoneCenterR, _zoneCenterG, _zoneCenterB));
            var rightBrush = new SolidColorBrush(Color.FromRgb(_zoneRightR, _zoneRightG, _zoneRightB));

            var activeBorder = TryFindResource("ZoneTileActiveBorder") as System.Windows.Media.Brush ?? TryFindResource("TileActiveBorder") as System.Windows.Media.Brush ?? new SolidColorBrush(_isDarkTheme ? Color.FromRgb(0, 144, 255) : Color.FromRgb(0, 122, 255));
            var normalBorder = TryFindResource("ZoneTileBorder") as System.Windows.Media.Brush ?? TryFindResource("TileBorder") as System.Windows.Media.Brush ?? new SolidColorBrush(_isDarkTheme ? Color.FromRgb(56, 56, 56) : Color.FromRgb(209, 209, 214));
            var activeBg = TryFindResource("ZoneTileActiveBg") as System.Windows.Media.Brush ?? TryFindResource("TileActiveBg") as System.Windows.Media.Brush ?? new SolidColorBrush(_isDarkTheme ? Color.FromRgb(25, 39, 56) : Color.FromRgb(225, 239, 255));
            var normalBg = TryFindResource("ZoneTileBg") as System.Windows.Media.Brush ?? TryFindResource("TileBg") as System.Windows.Media.Brush ?? new SolidColorBrush(_isDarkTheme ? Color.FromRgb(38, 38, 38) : Color.FromRgb(255, 255, 255));

            // Zone 0: Left
            if (BrdZoneLeft != null)
            {
                BrdZoneLeft.BorderBrush = _activeZoneIndex == 0 ? activeBorder : normalBorder;
                BrdZoneLeft.BorderThickness = new Thickness(_activeZoneIndex == 0 ? 2 : 1);
                BrdZoneLeft.Background = _activeZoneIndex == 0 ? activeBg : normalBg;
            }
            if (DotZoneLeft != null)
            {
                DotZoneLeft.Background = leftBrush;
            }

            // Zone 1: Center
            if (BrdZoneCenter != null)
            {
                BrdZoneCenter.BorderBrush = _activeZoneIndex == 1 ? activeBorder : normalBorder;
                BrdZoneCenter.BorderThickness = new Thickness(_activeZoneIndex == 1 ? 2 : 1);
                BrdZoneCenter.Background = _activeZoneIndex == 1 ? activeBg : normalBg;
            }
            if (DotZoneCenter != null)
            {
                DotZoneCenter.Background = centerBrush;
            }

            // Zone 2: Right
            if (BrdZoneRight != null)
            {
                BrdZoneRight.BorderBrush = _activeZoneIndex == 2 ? activeBorder : normalBorder;
                BrdZoneRight.BorderThickness = new Thickness(_activeZoneIndex == 2 ? 2 : 1);
                BrdZoneRight.Background = _activeZoneIndex == 2 ? activeBg : normalBg;
            }
            if (DotZoneRight != null)
            {
                DotZoneRight.Background = rightBrush;
            }

            if (TxtDiagramTitle != null)
            {
                TxtDiagramTitle.Foreground = TryFindResource("TextPrimary") as System.Windows.Media.Brush ?? new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
        }

        #endregion

        private void UpdateBrightnessUI()
        {
            string text;

            switch (_brightness)
            {
                case 0:
                    text = "%0";
                    break;
                case 1:
                    text = "%50";
                    break;
                case 2:
                default:
                    text = "%100";
                    break;
            }

            if (TxtBrightnessIcon != null)
            {
                TxtBrightnessIcon.Text = text;
                TxtBrightnessIcon.ToolTip = text;
            }

            if (SliderBrightness != null)
            {
                SliderBrightness.ToolTip = text;
            }
        }

        private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized || SliderBrightness == null) return;
            byte val = (byte)Math.Clamp(SliderBrightness.Value, 0, 2);

            _brightness = val;
            UpdateBrightnessUI();
            ApplyLedSettings();
        }

        private void BtnCustomColor_Click(object sender, RoutedEventArgs e)
        {
            byte initR, initG, initB;
            if (_currentLedMode == LedMode.TripleZone)
            {
                (initR, initG, initB) = _activeZoneIndex switch
                {
                    0 => (_zoneLeftR, _zoneLeftG, _zoneLeftB),
                    1 => (_zoneCenterR, _zoneCenterG, _zoneCenterB),
                    _ => (_zoneRightR, _zoneRightG, _zoneRightB)
                };
            }
            else
            {
                (initR, initG, initB) = (_red, _green, _blue);
            }

            var picker = new UI.ColorPickerWindow(initR, initG, initB)
            {
                Owner = this
            };

            // Donanım WMI haberleşmesini arka plan iş parçacığına taşıyıp arayüzün (UI) 0ms kasmadan çalışmasını sağlıyoruz
            (byte R, byte G, byte B)? pendingColor = null;
            int isSendingHardware = 0;

            void ScheduleHardwarePreview((byte R, byte G, byte B) rgb)
            {
                pendingColor = rgb;
                if (Interlocked.CompareExchange(ref isSendingHardware, 1, 0) == 0)
                {
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            while (pendingColor.HasValue)
                            {
                                var current = pendingColor.Value;
                                pendingColor = null;

                                if (_brightness > 0)
                                {
                                    if (_currentLedMode == LedMode.Static || _currentLedMode == LedMode.Breathing)
                                    {
                                        _bridge.SetAllKeyboardLed(_currentLedMode, _brightness, current.R, current.G, current.B);
                                    }
                                    else if (_currentLedMode == LedMode.TripleZone)
                                    {
                                        var left = _activeZoneIndex == 0 ? current : (_zoneLeftR, _zoneLeftG, _zoneLeftB);
                                        var center = _activeZoneIndex == 1 ? current : (_zoneCenterR, _zoneCenterG, _zoneCenterB);
                                        var right = _activeZoneIndex == 2 ? current : (_zoneRightR, _zoneRightG, _zoneRightB);
                                        _bridge.SetKeyboardTripleZone(_brightness, left, center, right);
                                    }
                                }

                                // Donanım veri yolunu (EC SMI) tıkamamak için kısa bekleme
                                await System.Threading.Tasks.Task.Delay(60);
                            }
                        }
                        catch { }
                        finally
                        {
                            Interlocked.Exchange(ref isSendingHardware, 0);
                            if (pendingColor.HasValue)
                            {
                                ScheduleHardwarePreview(pendingColor.Value);
                            }
                        }
                    });
                }
            }

            // Renk seçicide gezinirken donanımda asenkron gerçek zamanlı canlı önizleme
            picker.ColorPreviewChanged += (r, g, b) =>
            {
                ScheduleHardwarePreview((r, g, b));
            };

            if (picker.ShowDialog() == true)
            {
                SetRgbColor(picker.SelectedRed, picker.SelectedGreen, picker.SelectedBlue);
            }
            else
            {
                // İptal edilirse orijinal renge geri dön
                ApplyLedSettings();
            }
        }

        private void ColorPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                string cleanHex = hex.TrimStart('#');
                if (cleanHex.Length == 6 &&
                    byte.TryParse(cleanHex[..2], NumberStyles.HexNumber, null, out byte r) &&
                    byte.TryParse(cleanHex[2..4], NumberStyles.HexNumber, null, out byte g) &&
                    byte.TryParse(cleanHex[4..6], NumberStyles.HexNumber, null, out byte b))
                {
                    SetRgbColor(r, g, b);
                }
            }
        }

        private void SetRgbColor(byte r, byte g, byte b)
        {
            if (_currentLedMode == LedMode.TripleZone)
            {
                switch (_activeZoneIndex)
                {
                    case 0:
                        _zoneLeftR = r; _zoneLeftG = g; _zoneLeftB = b;
                        break;
                    case 1:
                        _zoneCenterR = r; _zoneCenterG = g; _zoneCenterB = b;
                        break;
                    case 2:
                    default:
                        _zoneRightR = r; _zoneRightG = g; _zoneRightB = b;
                        break;
                }
                UpdateTripleZoneUI();
            }
            else
            {
                _red = r;
                _green = g;
                _blue = b;
            }

            UpdateColorPreview();
            ApplyLedSettings();
        }

        private void UpdateColorPreview()
        {
            byte dispR, dispG, dispB;
            if (_currentLedMode == LedMode.TripleZone)
            {
                (dispR, dispG, dispB) = _activeZoneIndex switch
                {
                    0 => (_zoneLeftR, _zoneLeftG, _zoneLeftB),
                    1 => (_zoneCenterR, _zoneCenterG, _zoneCenterB),
                    _ => (_zoneRightR, _zoneRightG, _zoneRightB)
                };
            }
            else
            {
                (dispR, dispG, dispB) = (_red, _green, _blue);
            }

            if (BrdModeColorIndicator != null)
            {
                BrdModeColorIndicator.Background = new SolidColorBrush(Color.FromRgb(dispR, dispG, dispB));
            }
        }

        private void ApplyLedSettings()
        {
            if (!_isInitialized || _bridge == null || _config == null) return;

            if (_brightness == 0)
            {
                _bridge.TurnOffAllLights();
            }
            else
            {
                if (_currentLedMode == LedMode.TripleZone)
                {
                    _bridge.SetKeyboardTripleZone(
                        _brightness,
                        (_zoneLeftR, _zoneLeftG, _zoneLeftB),
                        (_zoneCenterR, _zoneCenterG, _zoneCenterB),
                        (_zoneRightR, _zoneRightG, _zoneRightB));
                }
                else
                {
                    // Hardware EC brightness levels:
                    // Level 1 => 50% Brightness
                    // Level 2 => 100% Brightness
                    _bridge.SetAllKeyboardLed(_currentLedMode, _brightness, _red, _green, _blue);
                }
            }

            _config.CurrentSettings.LedMode = _currentLedMode;
            _config.CurrentSettings.LedBrightness = _brightness;
            _config.CurrentSettings.Red = _red;
            _config.CurrentSettings.Green = _green;
            _config.CurrentSettings.Blue = _blue;

            _config.CurrentSettings.ZoneLeftRed = _zoneLeftR;
            _config.CurrentSettings.ZoneLeftGreen = _zoneLeftG;
            _config.CurrentSettings.ZoneLeftBlue = _zoneLeftB;
            _config.CurrentSettings.ZoneCenterRed = _zoneCenterR;
            _config.CurrentSettings.ZoneCenterGreen = _zoneCenterG;
            _config.CurrentSettings.ZoneCenterBlue = _zoneCenterB;
            _config.CurrentSettings.ZoneRightRed = _zoneRightR;
            _config.CurrentSettings.ZoneRightGreen = _zoneRightG;
            _config.CurrentSettings.ZoneRightBlue = _zoneRightB;

            _config.SaveSettings();
        }

        #endregion

        #region Theme Switcher (G-Helper Dark / Clean Light)

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme(_isDarkTheme);
            _config.CurrentSettings.IsDarkTheme = _isDarkTheme;
            _config.SaveSettings();
        }

        private void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                // Authentic G-Helper Matte Dark
                Resources["WindowBg"] = new SolidColorBrush(Color.FromRgb(22, 22, 22));        // #161616
                Resources["TileBg"] = new SolidColorBrush(Color.FromRgb(37, 37, 37));          // #252525
                Resources["TileHoverBg"] = new SolidColorBrush(Color.FromRgb(50, 50, 50));     // #323232
                Resources["TileBorder"] = new SolidColorBrush(Color.FromRgb(56, 56, 56));      // #383838
                Resources["TileActiveBg"] = new SolidColorBrush(Color.FromRgb(25, 39, 56));    // #192738
                Resources["TileActiveBorder"] = new SolidColorBrush(Color.FromRgb(0, 144, 255)); // #0090FF
                Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));  // #FFFFFF
                Resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(204, 204, 204));// #CCCCCC
                Resources["TextMuted"] = new SolidColorBrush(Color.FromRgb(142, 142, 147));    // #8E8E93
                Resources["ProgressTrack"] = new SolidColorBrush(Color.FromRgb(42, 42, 42));   // #2A2A2A
                Resources["AccentBlue"] = new SolidColorBrush(Color.FromRgb(0, 144, 255));     // #0090FF
                Resources["DiagramBg"] = new SolidColorBrush(Color.FromRgb(28, 28, 28));       // #1C1C1C
                Resources["ZoneTileBg"] = new SolidColorBrush(Color.FromRgb(38, 38, 38));      // #262626
                Resources["ZoneTileActiveBg"] = new SolidColorBrush(Color.FromRgb(25, 39, 56));// #192738
                Resources["ZoneTileBorder"] = new SolidColorBrush(Color.FromRgb(56, 56, 56));
                Resources["ZoneTileActiveBorder"] = new SolidColorBrush(Color.FromRgb(0, 144, 255));
                if (BtnThemeToggle != null) BtnThemeToggle.Content = LocalizationManager.ThemeLight;
            }
            else
            {
                // Clean High-Contrast Light
                Resources["WindowBg"] = new SolidColorBrush(Color.FromRgb(242, 242, 247));     // #F2F2F7
                Resources["TileBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));       // #FFFFFF
                Resources["TileHoverBg"] = new SolidColorBrush(Color.FromRgb(235, 235, 240));  // #EBEBF0
                Resources["TileBorder"] = new SolidColorBrush(Color.FromRgb(209, 209, 214));   // #D1D1D6
                Resources["TileActiveBg"] = new SolidColorBrush(Color.FromRgb(225, 239, 255)); // #E1EFFF
                Resources["TileActiveBorder"] = new SolidColorBrush(Color.FromRgb(0, 122, 255)); // #007AFF
                Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(0, 0, 0));        // #000000
                Resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(60, 60, 67));   // #3C3C43
                Resources["TextMuted"] = new SolidColorBrush(Color.FromRgb(142, 142, 147));    // #8E8E93
                Resources["ProgressTrack"] = new SolidColorBrush(Color.FromRgb(229, 229, 234));
                Resources["AccentBlue"] = new SolidColorBrush(Color.FromRgb(0, 122, 255));     // #007AFF
                Resources["DiagramBg"] = new SolidColorBrush(Color.FromRgb(240, 240, 244));     // #F0F0F4
                Resources["ZoneTileBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));   // #FFFFFF
                Resources["ZoneTileActiveBg"] = new SolidColorBrush(Color.FromRgb(225, 239, 255)); // #E1EFFF
                Resources["ZoneTileBorder"] = new SolidColorBrush(Color.FromRgb(209, 209, 214));
                Resources["ZoneTileActiveBorder"] = new SolidColorBrush(Color.FromRgb(0, 122, 255));
                if (BtnThemeToggle != null) BtnThemeToggle.Content = LocalizationManager.ThemeDark;
            }

            UpdateTripleZoneUI();
        }

        #endregion

        #region Extra Actions & Settings

        private void BtnRefreshTelemetry_Click(object sender, RoutedEventArgs e)
        {
            var telemetry = _bridge.GetTelemetry();
            UpdateTelemetryUI(telemetry);
            UpdateSystemResourcesUI();
        }

        private void ChkAutoStart_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized || ChkAutoStart == null || _config == null) return;
            bool enable = ChkAutoStart.IsChecked == true;
            StartupManager.SetStartup(enable);
            _config.CurrentSettings.StartWithWindows = enable;
            _config.SaveSettings();
        }

        private void ApplyLocalization()
        {
            if (TxtModeOfficeLabel != null) TxtModeOfficeLabel.Text = LocalizationManager.PowerOffice;
            if (TxtModeGamingLabel != null) TxtModeGamingLabel.Text = LocalizationManager.PowerGaming;
            if (TxtModeHighPerfLabel != null) TxtModeHighPerfLabel.Text = LocalizationManager.PowerTurbo;

            if (TxtGpuHeader != null) TxtGpuHeader.Text = LocalizationManager.GpuHeader;
            if (TxtSysResourcesHeader != null) TxtSysResourcesHeader.Text = LocalizationManager.SystemResourcesHeader;
            if (TxtRamHeader != null) TxtRamHeader.Text = LocalizationManager.RamLabel;
            if (TxtSsdHeader != null) TxtSsdHeader.Text = LocalizationManager.SsdLabel;

            if (TxtKeyboardHeader != null) TxtKeyboardHeader.Text = LocalizationManager.KeyboardHeader;
            if (LblBrightnessHeader != null) LblBrightnessHeader.Text = LocalizationManager.BrightnessLabel;
            if (LblBrightnessTitle != null) LblBrightnessTitle.Text = LocalizationManager.BrightnessSliderTitle;

            if (CmbLedMode != null)
            {
                foreach (var item in CmbLedMode.Items)
                {
                    if (item is ComboBoxItem cbItem && cbItem.Tag is string tag)
                    {
                        cbItem.Content = tag switch
                        {
                            "Breathing" => LocalizationManager.LedBreathing,
                            "ColorfulCycle" => LocalizationManager.LedDynamic,
                            "Rainbow" => LocalizationManager.LedRainbow,
                            "TripleZone" => LocalizationManager.LedTripleZone,
                            _ => LocalizationManager.LedStatic
                        };
                    }
                }
            }

            if (TxtModeColorActionLabel != null) TxtModeColorActionLabel.Text = LocalizationManager.ColorLabel;

            if (TxtDiagramTitle != null) TxtDiagramTitle.Text = LocalizationManager.KeyboardDiagramTitle;
            if (LblZoneLeft != null)
            {
                LblZoneLeft.Text = LocalizationManager.IsTurkish ? "Sol" : "Left";
            }
            if (LblZoneCenter != null)
            {
                LblZoneCenter.Text = LocalizationManager.IsTurkish ? "Orta" : "Center";
            }
            if (LblZoneRight != null)
            {
                LblZoneRight.Text = LocalizationManager.IsTurkish ? "Sağ" : "Right";
            }
            UpdateTripleZoneUI();

            if (ChkAutoStart != null) ChkAutoStart.Content = LocalizationManager.AutoStart;
            if (BtnCheckUpdates != null) BtnCheckUpdates.Content = LocalizationManager.CheckUpdates;

            if (BtnRefreshTelemetry != null) BtnRefreshTelemetry.Content = LocalizationManager.Refresh;
            if (BtnExitApp != null) BtnExitApp.Content = LocalizationManager.Exit;
            if (BtnLangToggle != null) BtnLangToggle.Content = LocalizationManager.LangButton;

            if (BtnThemeToggle != null)
            {
                BtnThemeToggle.Content = _isDarkTheme ? LocalizationManager.ThemeLight : LocalizationManager.ThemeDark;
            }

            UpdateBrightnessUI();
        }

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (BtnCheckUpdates != null) BtnCheckUpdates.IsEnabled = false;
            try
            {
                await UpdateService.CheckForUpdatesAsync(LocalizationManager.IsTurkish == false, this);
            }
            finally
            {
                if (BtnCheckUpdates != null) BtnCheckUpdates.IsEnabled = true;
            }
        }

        private void BtnLangToggle_Click(object sender, RoutedEventArgs e)
        {
            string nextLang = LocalizationManager.IsTurkish ? "EN" : "TR";
            LocalizationManager.CurrentLanguage = nextLang;
            _config.CurrentSettings.Language = nextLang;
            _config.SaveSettings();

            ApplyLocalization();
            UpdatePowerModeButtonsUI();
        }

        private void BtnExitApp_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current is App app)
            {
                app.ShutdownApp();
            }
            else
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        #endregion
    }
}