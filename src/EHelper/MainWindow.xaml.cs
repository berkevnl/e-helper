using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using EHelper.Config;
using EHelper.Core;
using EHelper.Hardware.Casper;
using EHelper.Hardware.Simulation;
using EHelper.Services;
using EHelper.UI;

using Button = System.Windows.Controls.Button;
using MediaColor = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;

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

        private CancellationTokenSource? _animCts;
        private int _cycleSpeed = 0;

        private bool _isInitialized;
        private UpdateCheckResult? _latestUpdateResult;
        private bool _isUpdating;

        private AppProfile? _editingProfile;
        private bool _isProfileSetupMode;
        private string? _activeProfileName;
        private AppProfile? _snapshotBeforeProfile;

        public MainWindow(IHardwareProvider bridge, bool isSimulated, ConfigManager config, HardwareMonitorService monitor)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _isSimulated = isSimulated;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

            InitializeComponent();

            Loaded += (s, e) => PositionBottomRight();
            ContentRendered += (s, e) => PositionBottomRight();

            _isInitialized = false;
            ApplyInitialState();
            UpdateSystemResourcesUI();
            _isInitialized = true;

            SubscribeTelemetry();
            TriggerBackgroundUpdateCheck();
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

            // Load Language
            LocalizationManager.CurrentLanguage = s.Language ?? "TR";

            // Apply Theme & Localization
            ApplyTheme(_isDarkTheme);
            ApplyLocalization();

            // Version Pill
            TxtAppVersionPill.Text = $"v{UpdateService.CurrentVersion}";

            // UI Initial Values
            TxtModelBadge.Text = _bridge.DeviceModel;
            TxtModelStatus.Text = $" {LocalizationManager.UpToDate}";

            HighlightBrightnessButtons(_brightness);

            _cycleSpeed = Math.Clamp(s.CycleSpeed, 0, 2);
            HighlightSpeedButtons(_cycleSpeed);

            UpdateColorSwatches();
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
            // Laptop EC firmware often resets state 1-3 seconds after Windows logon.
            // Re-applying settings ensures preservation across reboots.
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
                catch { }
            });

            // Initial System Metrics query
            UpdateSystemResourcesUI();

            // Aggressive Startup Working Set Optimization (flushes initial WPF startup bloat down to ~25-35 MB)
            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(500);
                MemoryOptimizer.TrimMemory();
                await System.Threading.Tasks.Task.Delay(2000);
                MemoryOptimizer.TrimMemory();
            });
        }

        private void SubscribeTelemetry()
        {
            _monitor.TelemetryUpdated += telemetry =>
            {
                // Zero UI overhead when minimized/hidden in tray
                if (!IsVisible) return;

                Dispatcher.InvokeAsync(() =>
                {
                    UpdateTelemetryUI(telemetry);
                    UpdateSystemResourcesUI();
                });
            };
        }

        private static void SetTextIfChanged(TextBlock? tb, string text)
        {
            if (tb != null && tb.Text != text)
            {
                tb.Text = text;
            }
        }

        private void UpdateTelemetryUI(HardwareTelemetry t)
        {
            if (!t.IsAvailable)
            {
                SetTextIfChanged(TxtCpuTemp, "--");
                SetTextIfChanged(TxtGpuTemp, "--");
                SetTextIfChanged(TxtCpuRpm, "0 RPM");
                SetTextIfChanged(TxtGpuRpm, "0 RPM");
                return;
            }

            SetTextIfChanged(TxtCpuTemp, t.CpuTemperature > 0 ? $"{t.CpuTemperature}" : "--");
            SetTextIfChanged(TxtGpuTemp, t.GpuTemperature > 0 ? $"{t.GpuTemperature}" : "--");

            SetTextIfChanged(TxtCpuRpm, t.CpuFanRpm > 0 ? $"{t.CpuFanRpm} RPM" : "0 RPM");
            SetTextIfChanged(TxtGpuRpm, t.GpuFanRpm > 0 ? $"{t.GpuFanRpm} RPM" : "0 RPM");
        }

        private void UpdateSystemResourcesUI()
        {
            try
            {
                var metrics = SystemResourceMonitor.GetMetrics();

                // CPU & GPU usage percent
                SetTextIfChanged(TxtCpuUsagePercent, $"{metrics.CpuUsagePercent}%");
                SetTextIfChanged(TxtGpuUsagePercent, $"{metrics.GpuUsagePercent}%");

                // Model labels (if detected)
                if (!string.IsNullOrWhiteSpace(metrics.CpuName))
                {
                    SetTextIfChanged(TxtCpuModel, metrics.CpuName);
                }
                if (!string.IsNullOrWhiteSpace(metrics.GpuName))
                {
                    SetTextIfChanged(TxtGpuModel, metrics.GpuName);
                }

                // RAM metrics
                SetTextIfChanged(TxtRamPercent, $"{metrics.RamPercent}%");
                SetTextIfChanged(TxtRamUsage, $"•  {metrics.RamUsedGb:F1} / {metrics.RamTotalGb:F0} GB");
                PbRamUsage.Value = metrics.RamPercent;

                // SSD metrics
                SetTextIfChanged(TxtSsdPercent, $"{metrics.SsdPercent}%");
                string ssdText = metrics.SsdTotalGb >= 1000 
                    ? $"•  {metrics.SsdUsedGb / 1024.0:F1} / {metrics.SsdTotalGb / 1024.0:F1} TB" 
                    : $"•  {metrics.SsdUsedGb:F0} / {metrics.SsdTotalGb:F0} GB";
                SetTextIfChanged(TxtSsdUsage, ssdText);
                PbSsdUsage.Value = metrics.SsdPercent;
            }
            catch { }
        }

        #region Flyout & Auto-Hide & Memory Trim

        public void PositionBottomRight()
        {
            var workArea = SystemParameters.WorkArea;
            double w = !double.IsNaN(Width) && Width > 0 ? Width : (ActualWidth > 0 ? ActualWidth : 435);
            
            if (ActualHeight <= 0)
            {
                Measure(new System.Windows.Size(w, double.PositiveInfinity));
            }
            double h = ActualHeight > 0 ? ActualHeight : (DesiredSize.Height > 0 ? DesiredSize.Height : 660);

            Left = Math.Max(workArea.Left + 8, workArea.Right - w - 8);
            Top = Math.Max(workArea.Top + 8, workArea.Bottom - h - 8);
        }

        public void ToggleFlyout()
        {
            if (IsVisible)
            {
                Hide();
                System.Threading.Tasks.Task.Run(() => MemoryOptimizer.TrimMemory());
            }
            else
            {
                UpdateSystemResourcesUI();
                Show();
                PositionBottomRight();
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(PositionBottomRight));
                Activate();
                Focus();

                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(300);
                    MemoryOptimizer.TrimMemory();
                });
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            StopActiveAnimation();
            base.OnClosed(e);
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            System.Threading.Tasks.Task.Run(() => MemoryOptimizer.TrimMemory());
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            System.Threading.Tasks.Task.Run(() => MemoryOptimizer.TrimMemory());
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

        private void BtnPowerOffice_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(PowerMode.Office);
        }

        private void BtnPowerGaming_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(PowerMode.Gaming);
        }

        private void BtnPowerTurbo_Click(object sender, RoutedEventArgs e)
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
            ApplyPowerCardState(BtnPowerOffice, IconPowerOffice, TxtPowerOfficeTitle, TxtPowerOfficeSub, _currentPowerMode == PowerMode.Office, isStroke: true);
            ApplyPowerCardState(BtnPowerGaming, IconPowerGaming, TxtPowerGamingTitle, TxtPowerGamingSub, _currentPowerMode == PowerMode.Gaming, isStroke: true);
            ApplyPowerCardState(BtnPowerTurbo, IconPowerTurbo, TxtPowerTurboTitle, TxtPowerTurboSub, _currentPowerMode == PowerMode.HighPerformance, isStroke: false);
        }

        private void ApplyPowerCardState(Button? btn, System.Windows.Shapes.Path? icon, TextBlock? title, TextBlock? sub, bool isActive, bool isStroke)
        {
            if (btn == null || icon == null || title == null || sub == null) return;

            if (isActive)
            {
                btn.SetResourceReference(Button.BackgroundProperty, "CardActiveBg");
                btn.SetResourceReference(Button.BorderBrushProperty, "CardActiveBorder");
                if (isStroke)
                    icon.SetResourceReference(System.Windows.Shapes.Path.StrokeProperty, "TextPrimary");
                else
                    icon.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "TextPrimary");
                title.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
                title.FontWeight = FontWeights.Bold;
                sub.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            }
            else
            {
                btn.SetResourceReference(Button.BackgroundProperty, "CardBg");
                btn.SetResourceReference(Button.BorderBrushProperty, "CardBorder");
                if (isStroke)
                    icon.SetResourceReference(System.Windows.Shapes.Path.StrokeProperty, "TextMuted");
                else
                    icon.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "TextMuted");
                title.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                title.FontWeight = FontWeights.SemiBold;
                sub.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");
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
                "Cycle" => LedMode.Cycle,
                "Rainbow" => LedMode.Rainbow,
                "TripleZone" => LedMode.TripleZone,
                _ => LedMode.Static
            };

            SetLedMode(newMode);
        }

        private void SetLedMode(LedMode mode)
        {
            _currentLedMode = mode;

            // Aydınlatma 0 ise kullanıcı mod seçtiğinde otomatik olarak %100 seviyesine (2) aç
            if (_brightness == 0)
            {
                _brightness = 2;
                HighlightBrightnessButtons(_brightness);
            }

            UpdateLedModeButtonsUI();
            UpdateColorSwatches();
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
                    LedMode.Cycle => "Cycle",
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

            // Configure dynamic right column
            if (_currentLedMode == LedMode.Cycle || _currentLedMode == LedMode.Rainbow)
            {
                TxtDynamicControlTitle.Text = LocalizationManager.CycleSpeedTitle;
                GridSpeedSelector.Visibility = Visibility.Visible;
                GridSingleColorSelector.Visibility = Visibility.Collapsed;
                GridTripleZoneSelector.Visibility = Visibility.Collapsed;
                HighlightSpeedButtons(_cycleSpeed);
            }
            else if (_currentLedMode == LedMode.TripleZone)
            {
                TxtDynamicControlTitle.Text = LocalizationManager.ZoneColorsTitle;
                GridSpeedSelector.Visibility = Visibility.Collapsed;
                GridSingleColorSelector.Visibility = Visibility.Collapsed;
                GridTripleZoneSelector.Visibility = Visibility.Visible;
                UpdateTripleZoneSwatches();
            }
            else
            {
                // Static, Breathing, ColorfulCycle
                TxtDynamicControlTitle.Text = LocalizationManager.ColorLabel;
                GridSpeedSelector.Visibility = Visibility.Collapsed;
                GridSingleColorSelector.Visibility = Visibility.Visible;
                GridTripleZoneSelector.Visibility = Visibility.Collapsed;
                UpdateSingleColorSwatch();
            }
        }

        #region Segmented Buttons (Brightness & Speed)

        private void BtnBright0_Click(object sender, RoutedEventArgs e)
        {
            SetBrightness(0);
        }

        private void BtnBright50_Click(object sender, RoutedEventArgs e)
        {
            SetBrightness(1);
        }

        private void BtnBright100_Click(object sender, RoutedEventArgs e)
        {
            SetBrightness(2);
        }

        private void SetBrightness(byte level)
        {
            _brightness = level;
            HighlightBrightnessButtons(_brightness);
            ApplyLedSettings();
        }

        private void HighlightBrightnessButtons(byte level)
        {
            SetSegmentActive(BtnBright0, level == 0);
            SetSegmentActive(BtnBright50, level == 1);
            SetSegmentActive(BtnBright100, level == 2);
        }

        private void BtnSpeedSlow_Click(object sender, RoutedEventArgs e)
        {
            SetCycleSpeed(0);
        }

        private void BtnSpeedMedium_Click(object sender, RoutedEventArgs e)
        {
            SetCycleSpeed(1);
        }

        private void BtnSpeedFast_Click(object sender, RoutedEventArgs e)
        {
            SetCycleSpeed(2);
        }

        private void SetCycleSpeed(int speed)
        {
            _cycleSpeed = Math.Clamp(speed, 0, 2);
            HighlightSpeedButtons(_cycleSpeed);

            if (_config != null)
            {
                _config.CurrentSettings.CycleSpeed = _cycleSpeed;
                _config.SaveSettings();
            }

            if (_currentLedMode == LedMode.Cycle || _currentLedMode == LedMode.Rainbow)
            {
                StartActiveAnimation();
            }
        }

        private void HighlightSpeedButtons(int speed)
        {
            SetSegmentActive(BtnSpeedSlow, speed == 0);
            SetSegmentActive(BtnSpeedMedium, speed == 1);
            SetSegmentActive(BtnSpeedFast, speed == 2);
        }

        private void SetSegmentActive(Button? btn, bool isActive)
        {
            if (btn == null) return;
            if (isActive)
            {
                btn.SetResourceReference(Button.BackgroundProperty, "SegmentActiveBg");
                btn.SetResourceReference(Button.BorderBrushProperty, "SegmentActiveBorder");
                btn.SetResourceReference(Button.ForegroundProperty, "TextPrimary");
                btn.FontWeight = FontWeights.Bold;
            }
            else
            {
                btn.SetResourceReference(Button.BackgroundProperty, "SegmentBg");
                btn.SetResourceReference(Button.BorderBrushProperty, "SegmentBorder");
                btn.SetResourceReference(Button.ForegroundProperty, "TextMuted");
                btn.FontWeight = FontWeights.Normal;
            }
        }

        #endregion

        #region Color Picker & Zone Handlers

        private void BtnOpenColorPicker_Click(object sender, RoutedEventArgs e)
        {
            OpenColorPickerForCurrentZone();
        }

        private void BtnZoneLeft_Click(object sender, RoutedEventArgs e)
        {
            _activeZoneIndex = 0;
            HighlightZoneButtons();
            OpenColorPickerForCurrentZone();
        }

        private void BtnZoneCenter_Click(object sender, RoutedEventArgs e)
        {
            _activeZoneIndex = 1;
            HighlightZoneButtons();
            OpenColorPickerForCurrentZone();
        }

        private void BtnZoneRight_Click(object sender, RoutedEventArgs e)
        {
            _activeZoneIndex = 2;
            HighlightZoneButtons();
            OpenColorPickerForCurrentZone();
        }

        private void HighlightZoneButtons()
        {
            SetSegmentActive(BtnZoneLeft, _activeZoneIndex == 0);
            SetSegmentActive(BtnZoneCenter, _activeZoneIndex == 1);
            SetSegmentActive(BtnZoneRight, _activeZoneIndex == 2);
        }

        private void OpenColorPickerForCurrentZone()
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

            var picker = new ColorPickerWindow(initR, initG, initB)
            {
                Owner = this
            };

            // Donanım WMI/EC haberleşmesini arka plan iş parçacığına taşıyıp UI akıcılığını koruyoruz
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
                ApplyLedSettings();
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
                UpdateTripleZoneSwatches();
            }
            else
            {
                _red = r;
                _green = g;
                _blue = b;
                UpdateSingleColorSwatch();
            }

            ApplyLedSettings();
        }

        private void UpdateColorSwatches()
        {
            UpdateSingleColorSwatch();
            UpdateTripleZoneSwatches();
        }

        private void UpdateSingleColorSwatch()
        {
            if (EllipseSingleColor != null)
            {
                EllipseSingleColor.Fill = new SolidColorBrush(MediaColor.FromRgb(_red, _green, _blue));
            }
            if (TxtSingleColorHex != null)
            {
                TxtSingleColorHex.Text = $"#{_red:X2}{_green:X2}{_blue:X2}";
            }
        }

        private void UpdateTripleZoneSwatches()
        {
            if (EllipseZoneLeft != null)
            {
                EllipseZoneLeft.Fill = new SolidColorBrush(MediaColor.FromRgb(_zoneLeftR, _zoneLeftG, _zoneLeftB));
            }
            if (EllipseZoneCenter != null)
            {
                EllipseZoneCenter.Fill = new SolidColorBrush(MediaColor.FromRgb(_zoneCenterR, _zoneCenterG, _zoneCenterB));
            }
            if (EllipseZoneRight != null)
            {
                EllipseZoneRight.Fill = new SolidColorBrush(MediaColor.FromRgb(_zoneRightR, _zoneRightG, _zoneRightB));
            }
            HighlightZoneButtons();
        }

        private void ApplyLedHardwareOnly()
        {
            if (!_isInitialized || _bridge == null) return;

            if (_brightness == 0)
            {
                StopActiveAnimation();
                _bridge.TurnOffAllLights();
            }
            else
            {
                if (_currentLedMode == LedMode.Cycle || _currentLedMode == LedMode.Rainbow)
                {
                    StartActiveAnimation();
                }
                else
                {
                    StopActiveAnimation();
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
                        _bridge.SetAllKeyboardLed(_currentLedMode, _brightness, _red, _green, _blue);
                    }
                }
            }
        }

        private void ApplyLedSettings()
        {
            if (!_isInitialized || _bridge == null || _config == null) return;

            ApplyLedHardwareOnly();

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

        private void StartActiveAnimation()
        {
            StopActiveAnimation();

            if (_brightness == 0 || (_currentLedMode != LedMode.Cycle && _currentLedMode != LedMode.Rainbow))
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _animCts = cts;

            System.Threading.Tasks.Task.Run(async () =>
            {
                double phase = 0;
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (_brightness == 0)
                        {
                            break;
                        }

                        if (_currentLedMode == LedMode.Cycle)
                        {
                            var (r, g, b) = HsvToRgb(phase, 1.0, 1.0);
                            _bridge.SetKeyboardTripleZone(_brightness, (r, g, b), (r, g, b), (r, g, b));

                            double step = _cycleSpeed switch
                            {
                                1 => 6.0,
                                2 => 16.0,
                                _ => 2.0
                            };
                            phase = (phase + step) % 360.0;
                        }
                        else if (_currentLedMode == LedMode.Rainbow)
                        {
                            var left = HsvToRgb((phase + 100.0) % 360.0, 1.0, 1.0);
                            var center = HsvToRgb((phase + 50.0) % 360.0, 1.0, 1.0);
                            var right = HsvToRgb(phase, 1.0, 1.0);
                            _bridge.SetKeyboardTripleZone(_brightness, left, center, right);

                            double step = _cycleSpeed switch
                            {
                                1 => 6.0,
                                2 => 16.0,
                                _ => 2.0
                            };
                            phase = (phase + step) % 360.0;
                        }
                        else
                        {
                            break;
                        }

                        await System.Threading.Tasks.Task.Delay(14, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch { }
                }
            }, cts.Token);
        }

        private void StopActiveAnimation()
        {
            if (_animCts != null)
            {
                try
                {
                    _animCts.Cancel();
                    _animCts.Dispose();
                }
                catch { }
                _animCts = null;
            }
        }

        private static (byte r, byte g, byte b) HsvToRgb(double hue, double saturation, double value)
        {
            int hi = (int)(Math.Floor(hue / 60.0)) % 6;
            double f = hue / 60.0 - Math.Floor(hue / 60.0);
            double v = value * 255.0;
            byte vByte = (byte)Math.Clamp(v, 0, 255);
            byte p = (byte)Math.Clamp(v * (1.0 - saturation), 0, 255);
            byte q = (byte)Math.Clamp(v * (1.0 - f * saturation), 0, 255);
            byte t = (byte)Math.Clamp(v * (1.0 - (1.0 - f) * saturation), 0, 255);

            return hi switch
            {
                0 => (vByte, t, p),
                1 => (q, vByte, p),
                2 => (p, vByte, t),
                3 => (p, q, vByte),
                4 => (t, p, vByte),
                _ => (vByte, p, q)
            };
        }

        #endregion

        #endregion

        #region Theme Switcher

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme(_isDarkTheme);
            _config.CurrentSettings.IsDarkTheme = _isDarkTheme;
            _config.SaveSettings();
        }

        private void ApplyTheme(bool isDark)
        {
            string accentHex = _config.CurrentSettings.ThemeAccentColor ?? "#22D3EE";
            var accent = ParseColor(accentHex, MediaColor.FromRgb(34, 211, 238));

            if (isDark)
            {
                Resources["WindowBg"] = new SolidColorBrush(MediaColor.FromRgb(20, 21, 27));       // #14151B
                Resources["CardBg"] = new SolidColorBrush(MediaColor.FromRgb(28, 30, 38));         // #1C1E26
                Resources["CardHoverBg"] = new SolidColorBrush(MediaColor.FromRgb(34, 37, 48));    // #222530
                Resources["CardBorder"] = new SolidColorBrush(MediaColor.FromRgb(37, 40, 52));     // #252834
                Resources["CardActiveBg"] = new SolidColorBrush(MediaColor.FromArgb(50, accent.R, accent.G, accent.B));
                Resources["CardActiveBorder"] = new SolidColorBrush(accent);
                Resources["TextPrimary"] = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255));
                Resources["TextSecondary"] = new SolidColorBrush(MediaColor.FromRgb(212, 215, 226));
                Resources["TextMuted"] = new SolidColorBrush(MediaColor.FromRgb(101, 106, 123));   // #656A7B
                Resources["ProgressTrack"] = new SolidColorBrush(MediaColor.FromRgb(37, 40, 52));
                Resources["AccentColor"] = new SolidColorBrush(accent);
                Resources["AccentCyan"] = new SolidColorBrush(MediaColor.FromRgb(34, 211, 238));   // #22D3EE (CPU dot)
                Resources["AccentGreen"] = new SolidColorBrush(MediaColor.FromRgb(74, 222, 128));  // #4ADE80
                Resources["AccentAmber"] = new SolidColorBrush(MediaColor.FromRgb(245, 158, 11));  // #F59E0B
                Resources["SegmentBg"] = new SolidColorBrush(MediaColor.FromRgb(30, 32, 42));      // #1E202A
                Resources["SegmentBorder"] = new SolidColorBrush(MediaColor.FromRgb(46, 50, 66));  // #2E3242
                Resources["SegmentActiveBg"] = new SolidColorBrush(MediaColor.FromArgb(60, accent.R, accent.G, accent.B));
                Resources["SegmentActiveBorder"] = new SolidColorBrush(accent);
                Resources["BadgeBg"] = new SolidColorBrush(MediaColor.FromRgb(34, 36, 47));        // #22242F
                Resources["BadgeBorder"] = new SolidColorBrush(MediaColor.FromRgb(45, 49, 63));    // #2D313F
                Resources["BtnUpdateBg"] = new SolidColorBrush(MediaColor.FromArgb(70, accent.R, accent.G, accent.B));
                Resources["BtnUpdateBorder"] = new SolidColorBrush(accent);
                Resources["BtnUpdateFg"] = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255));

                if (IconThemePath != null)
                {
                    IconThemePath.Data = (Geometry)FindResource("IconSun");
                    IconThemePath.SetResourceReference(System.Windows.Shapes.Path.StrokeProperty, "TextMuted");
                    IconThemePath.Fill = Brushes.Transparent;
                }
            }
            else
            {
                Resources["WindowBg"] = new SolidColorBrush(MediaColor.FromRgb(248, 250, 252));
                Resources["CardBg"] = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255));
                Resources["CardHoverBg"] = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249));
                Resources["CardBorder"] = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240));
                Resources["CardActiveBg"] = new SolidColorBrush(MediaColor.FromArgb(30, accent.R, accent.G, accent.B));
                Resources["CardActiveBorder"] = new SolidColorBrush(accent);
                Resources["TextPrimary"] = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42));
                Resources["TextSecondary"] = new SolidColorBrush(MediaColor.FromRgb(51, 65, 85));
                Resources["TextMuted"] = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139));
                Resources["ProgressTrack"] = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240));
                Resources["AccentColor"] = new SolidColorBrush(accent);
                Resources["AccentCyan"] = new SolidColorBrush(MediaColor.FromRgb(2, 132, 199));
                Resources["AccentGreen"] = new SolidColorBrush(MediaColor.FromRgb(22, 163, 74));
                Resources["AccentAmber"] = new SolidColorBrush(MediaColor.FromRgb(217, 119, 6));
                Resources["SegmentBg"] = new SolidColorBrush(MediaColor.FromRgb(248, 250, 252));
                Resources["SegmentBorder"] = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225));
                Resources["SegmentActiveBg"] = new SolidColorBrush(MediaColor.FromArgb(40, accent.R, accent.G, accent.B));
                Resources["SegmentActiveBorder"] = new SolidColorBrush(accent);
                Resources["BadgeBg"] = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249));
                Resources["BadgeBorder"] = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225));
                Resources["BtnUpdateBg"] = new SolidColorBrush(accent);
                Resources["BtnUpdateBorder"] = new SolidColorBrush(accent);
                Resources["BtnUpdateFg"] = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255));

                if (IconThemePath != null)
                {
                    IconThemePath.Data = (Geometry)FindResource("IconMoon");
                    IconThemePath.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "TextMuted");
                    IconThemePath.Stroke = Brushes.Transparent;
                }
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

            var accentBrush = Resources["AccentColor"] as SolidColorBrush;
            if (accentBrush != null)
            {
                if (PbRamUsage != null) PbRamUsage.Foreground = accentBrush;
                if (PbSsdUsage != null) PbSsdUsage.Foreground = accentBrush;
                if (TxtRamPercent != null) TxtRamPercent.Foreground = accentBrush;
                if (TxtSsdPercent != null) TxtSsdPercent.Foreground = accentBrush;
                if (IconRamPath != null) IconRamPath.Stroke = accentBrush;
                if (IconDiskPath != null) IconDiskPath.Stroke = accentBrush;
            }

            UpdatePowerModeButtonsUI();
            HighlightBrightnessButtons(_brightness);
            HighlightSpeedButtons(_cycleSpeed);
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

        #endregion

        #region Profile Management & Custom Presets

        public void EnterProfileSetupMode(AppProfile? existingProfile = null)
        {
            _editingProfile = existingProfile;
            _isProfileSetupMode = true;

            if (existingProfile != null)
            {
                // Pre-load existing profile settings into UI
                _currentPowerMode = existingProfile.PowerMode;
                _currentLedMode = existingProfile.LedMode;
                _brightness = existingProfile.LedBrightness;
                _cycleSpeed = existingProfile.CycleSpeed;
                _red = existingProfile.Red;
                _green = existingProfile.Green;
                _blue = existingProfile.Blue;
                _zoneLeftR = existingProfile.ZoneLeftRed;
                _zoneLeftG = existingProfile.ZoneLeftGreen;
                _zoneLeftB = existingProfile.ZoneLeftBlue;
                _zoneCenterR = existingProfile.ZoneCenterRed;
                _zoneCenterG = existingProfile.ZoneCenterGreen;
                _zoneCenterB = existingProfile.ZoneCenterBlue;
                _zoneRightR = existingProfile.ZoneRightRed;
                _zoneRightG = existingProfile.ZoneRightGreen;
                _zoneRightB = existingProfile.ZoneRightBlue;

                UpdatePowerModeButtonsUI();
                HighlightBrightnessButtons(_brightness);
                HighlightSpeedButtons(_cycleSpeed);
                UpdateLedModeButtonsUI();
                UpdateColorSwatches();
            }

            TxtProfileSetupBannerTitle.Text = LocalizationManager.ProfileSetupBannerTitle;
            TxtProfileSetupBannerDesc.Text = LocalizationManager.ProfileSetupBannerDesc;
            BtnConfirmProfileSettings.Content = $"✓ {LocalizationManager.ConfirmSettings}";
            BtnCancelProfileSettings.Content = LocalizationManager.Cancel;
            PnlProfileSetupBanner.Visibility = Visibility.Visible;

            if (!IsVisible)
            {
                ToggleFlyout();
            }
            else
            {
                Activate();
                Focus();
            }
        }

        private void BtnConfirmProfileSettings_Click(object sender, RoutedEventArgs e)
        {
            PnlProfileSetupBanner.Visibility = Visibility.Collapsed;
            _isProfileSetupMode = false;

            var profile = _editingProfile?.Clone() ?? new AppProfile();
            profile.PowerMode = _currentPowerMode;
            profile.LedMode = _currentLedMode;
            profile.LedBrightness = _brightness;
            profile.CycleSpeed = _cycleSpeed;
            profile.Red = _red;
            profile.Green = _green;
            profile.Blue = _blue;
            profile.ZoneLeftRed = _zoneLeftR;
            profile.ZoneLeftGreen = _zoneLeftG;
            profile.ZoneLeftBlue = _zoneLeftB;
            profile.ZoneCenterRed = _zoneCenterR;
            profile.ZoneCenterGreen = _zoneCenterG;
            profile.ZoneCenterBlue = _zoneCenterB;
            profile.ZoneRightRed = _zoneRightR;
            profile.ZoneRightGreen = _zoneRightG;
            profile.ZoneRightBlue = _zoneRightB;

            bool isNew = _editingProfile == null;
            var editWin = new ProfileEditWindow(profile, isNew, _isDarkTheme, _config.CurrentSettings.ThemeAccentColor)
            {
                Owner = this
            };
            PositionToLeftOf(editWin);

            if (editWin.ShowDialog() == true)
            {
                if (isNew)
                {
                    _config.CurrentSettings.Profiles.Add(profile);
                }
                else if (_editingProfile != null)
                {
                    _editingProfile.Name = profile.Name;
                    _editingProfile.ProcessName = profile.ProcessName;
                    _editingProfile.AppPath = profile.AppPath;
                    _editingProfile.PowerMode = profile.PowerMode;
                    _editingProfile.LedMode = profile.LedMode;
                    _editingProfile.LedBrightness = profile.LedBrightness;
                    _editingProfile.CycleSpeed = profile.CycleSpeed;
                    _editingProfile.Red = profile.Red;
                    _editingProfile.Green = profile.Green;
                    _editingProfile.Blue = profile.Blue;
                    _editingProfile.ZoneLeftRed = profile.ZoneLeftRed;
                    _editingProfile.ZoneLeftGreen = profile.ZoneLeftGreen;
                    _editingProfile.ZoneLeftBlue = profile.ZoneLeftBlue;
                    _editingProfile.ZoneCenterRed = profile.ZoneCenterRed;
                    _editingProfile.ZoneCenterGreen = profile.ZoneCenterGreen;
                    _editingProfile.ZoneCenterBlue = profile.ZoneCenterBlue;
                    _editingProfile.ZoneRightRed = profile.ZoneRightRed;
                    _editingProfile.ZoneRightGreen = profile.ZoneRightGreen;
                    _editingProfile.ZoneRightBlue = profile.ZoneRightBlue;
                }
                _config.SaveSettings();
                _editingProfile = null;
                OpenSettingsWindow();
            }
            else if (editWin.RecaptureRequested)
            {
                EnterProfileSetupMode(profile);
            }
            else
            {
                _editingProfile = null;
            }
        }

        private void BtnCancelProfileSettings_Click(object sender, RoutedEventArgs e)
        {
            PnlProfileSetupBanner.Visibility = Visibility.Collapsed;
            _isProfileSetupMode = false;
            _editingProfile = null;
        }

        public void ApplyProfile(AppProfile profile)
        {
            if (profile == null || _isProfileSetupMode) return;

            if (_snapshotBeforeProfile == null)
            {
                _snapshotBeforeProfile = new AppProfile
                {
                    PowerMode = _currentPowerMode,
                    LedMode = _currentLedMode,
                    LedBrightness = _brightness,
                    CycleSpeed = _cycleSpeed,
                    Red = _red,
                    Green = _green,
                    Blue = _blue,
                    ZoneLeftRed = _zoneLeftR,
                    ZoneLeftGreen = _zoneLeftG,
                    ZoneLeftBlue = _zoneLeftB,
                    ZoneCenterRed = _zoneCenterR,
                    ZoneCenterGreen = _zoneCenterG,
                    ZoneCenterBlue = _zoneCenterB,
                    ZoneRightRed = _zoneRightR,
                    ZoneRightGreen = _zoneRightG,
                    ZoneRightBlue = _zoneRightB
                };
            }

            _activeProfileName = profile.Name;

            _currentPowerMode = profile.PowerMode;
            _bridge.SetPowerMode(_currentPowerMode);
            ExcaliburPowerPlan.ApplyPowerPlan(_currentPowerMode);
            UpdatePowerModeButtonsUI();

            _currentLedMode = profile.LedMode;
            _brightness = profile.LedBrightness;
            _cycleSpeed = profile.CycleSpeed;
            _red = profile.Red;
            _green = profile.Green;
            _blue = profile.Blue;
            _zoneLeftR = profile.ZoneLeftRed;
            _zoneLeftG = profile.ZoneLeftGreen;
            _zoneLeftB = profile.ZoneLeftBlue;
            _zoneCenterR = profile.ZoneCenterRed;
            _zoneCenterG = profile.ZoneCenterGreen;
            _zoneCenterB = profile.ZoneCenterBlue;
            _zoneRightR = profile.ZoneRightRed;
            _zoneRightG = profile.ZoneRightGreen;
            _zoneRightB = profile.ZoneRightBlue;

            HighlightBrightnessButtons(_brightness);
            HighlightSpeedButtons(_cycleSpeed);
            UpdateLedModeButtonsUI();
            UpdateColorSwatches();
            ApplyLedHardwareOnly();

            if (TxtModelStatus != null)
            {
                TxtModelStatus.Text = $" {LocalizationManager.ProfileActivated}{profile.Name}";
                if (Resources["AccentCyan"] is Brush accentBrush)
                {
                    TxtModelStatus.Foreground = accentBrush;
                }
            }
        }

        public void RestoreDefaultSettings()
        {
            if (_isProfileSetupMode) return;

            _activeProfileName = null;

            var s = _snapshotBeforeProfile;
            _snapshotBeforeProfile = null;

            if (s == null)
            {
                var cfg = _config.CurrentSettings;
                s = new AppProfile
                {
                    PowerMode = cfg.PowerMode,
                    LedMode = cfg.LedMode,
                    LedBrightness = cfg.LedBrightness,
                    CycleSpeed = cfg.CycleSpeed,
                    Red = cfg.Red,
                    Green = cfg.Green,
                    Blue = cfg.Blue,
                    ZoneLeftRed = cfg.ZoneLeftRed,
                    ZoneLeftGreen = cfg.ZoneLeftGreen,
                    ZoneLeftBlue = cfg.ZoneLeftBlue,
                    ZoneCenterRed = cfg.ZoneCenterRed,
                    ZoneCenterGreen = cfg.ZoneCenterGreen,
                    ZoneCenterBlue = cfg.ZoneCenterBlue,
                    ZoneRightRed = cfg.ZoneRightRed,
                    ZoneRightGreen = cfg.ZoneRightGreen,
                    ZoneRightBlue = cfg.ZoneRightBlue
                };
            }

            _currentPowerMode = s.PowerMode;
            _bridge.SetPowerMode(_currentPowerMode);
            ExcaliburPowerPlan.ApplyPowerPlan(_currentPowerMode);
            _config.CurrentSettings.PowerMode = _currentPowerMode;
            UpdatePowerModeButtonsUI();

            _currentLedMode = s.LedMode;
            _brightness = s.LedBrightness;
            _cycleSpeed = s.CycleSpeed;
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

            HighlightBrightnessButtons(_brightness);
            HighlightSpeedButtons(_cycleSpeed);
            UpdateLedModeButtonsUI();
            UpdateColorSwatches();
            ApplyLedSettings();

            if (TxtModelStatus != null)
            {
                TxtModelStatus.Text = $" {LocalizationManager.UpToDate}";
                if (Resources["TextMuted"] is Brush mutedBrush)
                {
                    TxtModelStatus.Foreground = mutedBrush;
                }
            }
        }

        #endregion

        #region Localization & Settings Modal

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
        }

        public void PositionToLeftOf(Window targetWindow)
        {
            targetWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            var workArea = SystemParameters.WorkArea;

            double targetW = targetWindow.Width > 0 ? targetWindow.Width : 390;
            targetWindow.Measure(new System.Windows.Size(targetW, double.PositiveInfinity));
            double targetH = targetWindow.DesiredSize.Height > 0 ? targetWindow.DesiredSize.Height : 450;

            double ownerW = ActualWidth > 0 ? ActualWidth : (!double.IsNaN(Width) && Width > 0 ? Width : 435);
            double ownerH = ActualHeight > 0 ? ActualHeight : (!double.IsNaN(Height) && Height > 0 ? Height : 660);

            // 1. Calculate horizontal position (place directly to the left of MainWindow with 12px gap)
            double targetLeft = Left - targetW - 12;
            if (targetLeft < workArea.Left + 8)
            {
                // If not enough room on the left, check if right side has room
                double rightSide = Left + ownerW + 12;
                if (rightSide + targetW <= workArea.Right - 8)
                {
                    targetLeft = rightSide;
                }
                else
                {
                    targetLeft = Math.Max(workArea.Left + 8, Left - targetW);
                }
            }

            // 2. Calculate vertical position (vertically centered relative to MainWindow, clamped within workArea)
            double targetTop = Top + (ownerH - targetH) / 2;
            targetTop = Math.Max(workArea.Top + 8, Math.Min(targetTop, workArea.Bottom - targetH - 8));

            targetWindow.Left = targetLeft;
            targetWindow.Top = targetTop;
        }

        private void OpenSettingsWindow()
        {
            var settingsWin = new SettingsWindow(_config, _isDarkTheme)
            {
                Owner = this
            };
            PositionToLeftOf(settingsWin);

            settingsWin.AccentColorChanged += (newHex) =>
            {
                ApplyTheme(_isDarkTheme);
            };
            settingsWin.RecaptureRequested += (profileToRecapture) =>
            {
                EnterProfileSetupMode(profileToRecapture);
            };
            settingsWin.ShowDialog();

            if (settingsWin.RequestProfileCreation)
            {
                EnterProfileSetupMode(null);
            }
        }

        private void ApplyLocalization()
        {
            TxtHeaderPowerOptions.Text = LocalizationManager.PowerOptionsHeader;
            TxtPowerOfficeTitle.Text = LocalizationManager.PowerOffice;
            TxtPowerOfficeSub.Text = LocalizationManager.PowerOfficeSub;
            TxtPowerGamingTitle.Text = LocalizationManager.PowerGaming;
            TxtPowerGamingSub.Text = LocalizationManager.PowerGamingSub;
            TxtPowerTurboTitle.Text = LocalizationManager.PowerTurbo;
            TxtPowerTurboSub.Text = LocalizationManager.PowerTurboSub;

            TxtHeaderSystemResources.Text = LocalizationManager.SystemResourcesHeader;
            TxtCpuHeader.Text = LocalizationManager.CpuHeader;
            TxtGpuHeader.Text = LocalizationManager.GpuCardHeader;
            TxtCpuUsageLabel.Text = LocalizationManager.UsageLabel;
            TxtGpuUsageLabel.Text = LocalizationManager.UsageLabel;
            TxtCpuFanLabel.Text = LocalizationManager.FanLabel;
            TxtGpuFanLabel.Text = LocalizationManager.FanLabel;
            TxtRamLabel.Text = LocalizationManager.RamBellek;
            TxtSsdLabel.Text = LocalizationManager.SsdDepolama;

            TxtHeaderRgb.Text = LocalizationManager.RgbKeyboardHeader;
            TxtItemRainbowLabel.Text = LocalizationManager.LedRainbow;
            TxtItemCycleLabel.Text = LocalizationManager.LedCycle;
            TxtItemStaticLabel.Text = LocalizationManager.LedStatic;
            TxtItemBreathingLabel.Text = LocalizationManager.LedBreathing;
            TxtItemDynamicLabel.Text = LocalizationManager.LedDynamic;
            TxtItemTripleZoneLabel.Text = LocalizationManager.LedTripleZone;

            TxtBrightnessTitle.Text = LocalizationManager.BrightnessSliderTitle;
            BtnSpeedSlow.Content = LocalizationManager.SpeedSlow;
            BtnSpeedMedium.Content = LocalizationManager.SpeedMedium;
            BtnSpeedFast.Content = LocalizationManager.SpeedFast;

            TxtZoneLeftLabel.Text = LocalizationManager.ZoneLeftShort;
            TxtZoneCenterLabel.Text = LocalizationManager.ZoneCenterShort;
            TxtZoneRightLabel.Text = LocalizationManager.ZoneRightShort;

            BtnUpdateAction.Content = LocalizationManager.UpdateNow;
            TxtExitLabel.Text = LocalizationManager.Exit;
            TxtLangCode.Text = LocalizationManager.CurrentLanguage;

            BtnClose.ToolTip = LocalizationManager.CloseTooltip;
            BtnSettings.ToolTip = LocalizationManager.SettingsTitle;
            BtnThemeToggle.ToolTip = LocalizationManager.ThemeTooltip;
            BtnLangToggle.ToolTip = LocalizationManager.LanguageTooltip;
            BtnExitApp.ToolTip = LocalizationManager.Exit;

            if (TxtProfileSetupBannerTitle != null)
            {
                TxtProfileSetupBannerTitle.Text = LocalizationManager.ProfileSetupBannerTitle;
            }
            if (TxtProfileSetupBannerDesc != null)
            {
                TxtProfileSetupBannerDesc.Text = LocalizationManager.ProfileSetupBannerDesc;
            }
            if (BtnConfirmProfileSettings != null)
            {
                BtnConfirmProfileSettings.Content = $"✓ {LocalizationManager.ConfirmSettings}";
            }
            if (BtnCancelProfileSettings != null)
            {
                BtnCancelProfileSettings.Content = LocalizationManager.Cancel;
            }

            if (TxtModelStatus != null)
            {
                if (!string.IsNullOrEmpty(_activeProfileName))
                {
                    TxtModelStatus.Text = $" {LocalizationManager.ProfileActivated}{_activeProfileName}";
                    if (Resources["AccentCyan"] is Brush accentBrush)
                    {
                        TxtModelStatus.Foreground = accentBrush;
                    }
                }
                else
                {
                    TxtModelStatus.Text = $" {LocalizationManager.UpToDate}";
                    if (Resources["TextMuted"] is Brush mutedBrush)
                    {
                        TxtModelStatus.Foreground = mutedBrush;
                    }
                }
            }

            UpdatePowerModeButtonsUI();
            UpdateLedModeButtonsUI();
            UpdateUpdateStatusUI();
        }

        private void BtnLangToggle_Click(object sender, RoutedEventArgs e)
        {
            string nextLang = LocalizationManager.IsTurkish ? "EN" : "TR";
            LocalizationManager.CurrentLanguage = nextLang;
            _config.CurrentSettings.Language = nextLang;
            _config.SaveSettings();

            ApplyLocalization();
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

        #region Update Management

        private void TriggerBackgroundUpdateCheck()
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                var result = await UpdateService.CheckUpdateSilentAsync();
                await Dispatcher.InvokeAsync(() =>
                {
                    _latestUpdateResult = result;
                    UpdateUpdateStatusUI();
                });
            });
        }

        private void UpdateUpdateStatusUI()
        {
            if (CardUpdateBanner == null || TxtModelStatus == null) return;

            if (_isUpdating)
            {
                CardUpdateBanner.Visibility = Visibility.Visible;
                TxtUpdateBannerMessage.Text = LocalizationManager.UpdatingStatus;
                BtnUpdateAction.IsEnabled = false;
                TxtModelStatus.Text = "";
                return;
            }

            if (_latestUpdateResult != null && _latestUpdateResult.HasUpdate)
            {
                // Update detected: display banner, hide "- Güncel" from footer
                CardUpdateBanner.Visibility = Visibility.Visible;
                string ver = _latestUpdateResult.LatestVersion ?? "v1.3.0";
                if (!ver.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    ver = "v" + ver;
                }
                TxtUpdateBannerMessage.Text = $"{ver} {LocalizationManager.UpdateBannerTitle}";
                BtnUpdateAction.Content = LocalizationManager.UpdateNow;
                BtnUpdateAction.IsEnabled = true;
                TxtModelStatus.Text = "";
            }
            else
            {
                // No update or up-to-date: hide banner, show "- Güncel" in footer
                CardUpdateBanner.Visibility = Visibility.Collapsed;
                TxtModelStatus.Text = $" {LocalizationManager.UpToDate}";
            }
        }

        private async void BtnUpdateAction_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;

            if (_latestUpdateResult != null && _latestUpdateResult.HasUpdate && !string.IsNullOrEmpty(_latestUpdateResult.DownloadUrl))
            {
                _isUpdating = true;
                UpdateUpdateStatusUI();

                bool success = await UpdateService.DownloadAndRestartAsync(_latestUpdateResult.DownloadUrl);
                if (!success)
                {
                    _isUpdating = false;
                    UpdateUpdateStatusUI();
                    await UpdateService.CheckForUpdatesAsync(LocalizationManager.IsTurkish == false, this);
                }
            }
            else
            {
                await UpdateService.CheckForUpdatesAsync(LocalizationManager.IsTurkish == false, this);
            }
        }

        #endregion
    }
}
