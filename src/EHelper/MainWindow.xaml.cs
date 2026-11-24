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
        private readonly ConfigManager _config;
        private readonly HardwareMonitorService _monitor;

        // Live power + lighting state (also used as the template when capturing profiles)
        private readonly AppProfile _state = new();

        // Active 3-zone selection (0: Left, 1: Center, 2: Right)
        private int _activeZoneIndex = 0;

        private bool _isDarkTheme;

        private CancellationTokenSource? _animCts;
        private System.Threading.Tasks.Task? _animTask;

        private bool _isInitialized;
        private UpdateCheckResult? _latestUpdateResult;
        private bool _isUpdating;

        private AppProfile? _editingProfile;
        private bool _isProfileSetupMode;
        private string? _activeProfileName;
        private AppProfile? _snapshotBeforeProfile;

        private readonly KeyboardIdleWatcherService _idleWatcher;

        public MainWindow(IHardwareProvider bridge, ConfigManager config, HardwareMonitorService monitor)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

            _idleWatcher = new KeyboardIdleWatcherService(_config.CurrentSettings.KeyboardIdleTimeoutSeconds);
            _idleWatcher.IdleStateChanged += isIdle =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    if (isIdle)
                    {
                        // Keyboard sleep: turn off backlight without clearing user settings
                        StopActiveAnimation();
                        _bridge.TurnOffAllLights();
                    }
                    else
                    {
                        // Wake up: resume active lighting mode, speed, and brightness
                        ApplyLedHardwareOnly();
                    }
                });
            };

            InitializeComponent();

            Loaded += (s, e) => PositionBottomRight();
            ContentRendered += (s, e) => PositionBottomRight();

            ApplyInitialState();
            _isInitialized = true;
            StartHardwareSync();

            SubscribeTelemetry();
            TriggerBackgroundUpdateCheck();
        }

        private void ApplyInitialState()
        {
            var s = _config.CurrentSettings;
            s.CopyLightingTo(_state);
            _state.LedBrightness = Math.Min(s.LedBrightness, (byte)2);
            _state.CycleSpeed = Math.Clamp(s.CycleSpeed, 0, 2);

            _isDarkTheme = s.IsDarkTheme;

            if (_state.LedMode == LedMode.Off)
            {
                _state.LedMode = LedMode.Static;
                _state.LedBrightness = 0;
            }

            // Load Language
            LocalizationManager.CurrentLanguage = s.Language ?? "TR";

            // Apply Theme & Localization
            ApplyTheme(_isDarkTheme);
            ApplyLocalization();

            // Version Pill
            TxtAppVersionPill.Text = $"v{UpdateService.CurrentVersion}";

            // UI Initial Values (power/brightness/speed/LED mode/sleep UI are refreshed by ApplyTheme & ApplyLocalization)
            TxtModelBadge.Text = _bridge.DeviceModel;
            UpdateColorSwatches();

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

        /// <summary>
        /// Pushes the saved power mode and lighting to the EC. Runs after <see cref="_isInitialized"/> is set,
        /// otherwise ApplyLedSettings() would skip the immediate push.
        /// </summary>
        private void StartHardwareSync()
        {
            // Initial push to hardware asynchronously in background (0ms UI startup delay)
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    PushPowerModeToHardware();
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
                        PushPowerModeToHardware();
                        ApplyLedSettings();
                    });

                    await System.Threading.Tasks.Task.Delay(3500);
                    await Dispatcher.InvokeAsync(ApplyLedSettings);
                }
                catch { }
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
            _idleWatcher?.Dispose();
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

        private void BtnPowerOffice_Click(object sender, RoutedEventArgs e) => SetPowerMode(PowerMode.Office);

        private void BtnPowerGaming_Click(object sender, RoutedEventArgs e) => SetPowerMode(PowerMode.Gaming);

        private void BtnPowerTurbo_Click(object sender, RoutedEventArgs e) => SetPowerMode(PowerMode.HighPerformance);

        private void SetPowerMode(PowerMode mode)
        {
            _state.PowerMode = mode;
            UpdatePowerModeButtonsUI();
            PushPowerModeToHardware();

            _config.CurrentSettings.PowerMode = mode;
            _config.SaveSettings();
        }

        /// <summary>Applies the current power mode to the EC and the matching Windows power plan.</summary>
        private void PushPowerModeToHardware()
        {
            _bridge.SetPowerMode(_state.PowerMode);
            ExcaliburPowerPlan.ApplyPowerPlan(_state.PowerMode);
        }

        private void UpdatePowerModeButtonsUI()
        {
            ApplyPowerCardState(BtnPowerOffice, IconPowerOffice, TxtPowerOfficeTitle, TxtPowerOfficeSub, _state.PowerMode == PowerMode.Office, isStroke: true);
            ApplyPowerCardState(BtnPowerGaming, IconPowerGaming, TxtPowerGamingTitle, TxtPowerGamingSub, _state.PowerMode == PowerMode.Gaming, isStroke: true);
            ApplyPowerCardState(BtnPowerTurbo, IconPowerTurbo, TxtPowerTurboTitle, TxtPowerTurboSub, _state.PowerMode == PowerMode.HighPerformance, isStroke: false);
        }

        private void ApplyPowerCardState(Button? btn, System.Windows.Shapes.Path? icon, TextBlock? title, TextBlock? sub, bool isActive, bool isStroke)
        {
            if (btn == null || icon == null || title == null || sub == null) return;

            var iconProperty = isStroke ? System.Windows.Shapes.Path.StrokeProperty : System.Windows.Shapes.Path.FillProperty;

            btn.SetResourceReference(Button.BackgroundProperty, isActive ? "CardActiveBg" : "CardBg");
            btn.SetResourceReference(Button.BorderBrushProperty, isActive ? "CardActiveBorder" : "CardBorder");
            icon.SetResourceReference(iconProperty, isActive ? "TextPrimary" : "TextMuted");
            title.SetResourceReference(TextBlock.ForegroundProperty, isActive ? "TextPrimary" : "TextSecondary");
            title.FontWeight = isActive ? FontWeights.Bold : FontWeights.SemiBold;
            sub.SetResourceReference(TextBlock.ForegroundProperty, isActive ? "TextPrimary" : "TextMuted");
        }

        #endregion

        #region RGB Keyboard Control

        /// <summary>Cycle (Spectrum) and Rainbow are software-animated by E-Helper.</summary>
        private bool IsAnimatedMode => _state.LedMode is LedMode.Cycle or LedMode.Rainbow;

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
            _state.LedMode = mode;

            // Aydınlatma 0 ise kullanıcı mod seçtiğinde otomatik olarak %100 seviyesine (2) aç
            if (_state.LedBrightness == 0)
            {
                _state.LedBrightness = 2;
                HighlightBrightnessButtons(_state.LedBrightness);
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
                string targetTag = _state.LedMode switch
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

            // Configure dynamic right column: speed (animated) / zone colors (3-zone) / single color (Static, Breathing, Dynamic)
            bool isAnimated = IsAnimatedMode;
            bool isTripleZone = !isAnimated && _state.LedMode == LedMode.TripleZone;
            bool isSingleColor = !isAnimated && !isTripleZone;

            GridSpeedSelector.Visibility = isAnimated ? Visibility.Visible : Visibility.Collapsed;
            GridTripleZoneSelector.Visibility = isTripleZone ? Visibility.Visible : Visibility.Collapsed;
            GridSingleColorSelector.Visibility = isSingleColor ? Visibility.Visible : Visibility.Collapsed;

            if (isAnimated)
            {
                TxtDynamicControlTitle.Text = LocalizationManager.CycleSpeedTitle;
                HighlightSpeedButtons(_state.CycleSpeed);
            }
            else if (isTripleZone)
            {
                TxtDynamicControlTitle.Text = LocalizationManager.ZoneColorsTitle;
                UpdateTripleZoneSwatches();
            }
            else
            {
                TxtDynamicControlTitle.Text = LocalizationManager.ColorLabel;
                UpdateSingleColorSwatch();
            }
        }

        #region Segmented Buttons (Brightness & Speed)

        private void BtnBright0_Click(object sender, RoutedEventArgs e) => SetBrightness(0);

        private void BtnBright50_Click(object sender, RoutedEventArgs e) => SetBrightness(1);

        private void BtnBright100_Click(object sender, RoutedEventArgs e) => SetBrightness(2);

        private void SetBrightness(byte level)
        {
            _state.LedBrightness = level;
            HighlightBrightnessButtons(_state.LedBrightness);
            ApplyLedSettings();
        }

        private void HighlightBrightnessButtons(byte level)
        {
            SetSegmentActive(BtnBright0, level == 0);
            SetSegmentActive(BtnBright50, level == 1);
            SetSegmentActive(BtnBright100, level == 2);
        }

        private void BtnSpeedSlow_Click(object sender, RoutedEventArgs e) => SetCycleSpeed(0);

        private void BtnSpeedMedium_Click(object sender, RoutedEventArgs e) => SetCycleSpeed(1);

        private void BtnSpeedFast_Click(object sender, RoutedEventArgs e) => SetCycleSpeed(2);

        private void SetCycleSpeed(int speed)
        {
            _state.CycleSpeed = Math.Clamp(speed, 0, 2);
            HighlightSpeedButtons(_state.CycleSpeed);

            _config.CurrentSettings.CycleSpeed = _state.CycleSpeed;
            _config.SaveSettings();

            if (IsAnimatedMode)
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
            btn.SetResourceReference(Button.BackgroundProperty, isActive ? "SegmentActiveBg" : "SegmentBg");
            btn.SetResourceReference(Button.BorderBrushProperty, isActive ? "SegmentActiveBorder" : "SegmentBorder");
            btn.SetResourceReference(Button.ForegroundProperty, isActive ? "TextPrimary" : "TextMuted");
            btn.FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal;
        }

        #endregion

        #region Color Picker & Zone Handlers

        private void BtnOpenColorPicker_Click(object sender, RoutedEventArgs e) => OpenColorPickerForCurrentZone();

        private void BtnZoneLeft_Click(object sender, RoutedEventArgs e) => SelectZoneAndPickColor(0);

        private void BtnZoneCenter_Click(object sender, RoutedEventArgs e) => SelectZoneAndPickColor(1);

        private void BtnZoneRight_Click(object sender, RoutedEventArgs e) => SelectZoneAndPickColor(2);

        private void SelectZoneAndPickColor(int zoneIndex)
        {
            _activeZoneIndex = zoneIndex;
            HighlightZoneButtons();
            OpenColorPickerForCurrentZone();
        }

        private (byte r, byte g, byte b) GetZoneColor(int zoneIndex) => zoneIndex switch
        {
            0 => _state.ZoneLeft,
            1 => _state.ZoneCenter,
            _ => _state.ZoneRight
        };

        private void SetZoneColor(int zoneIndex, (byte r, byte g, byte b) color)
        {
            switch (zoneIndex)
            {
                case 0: _state.ZoneLeft = color; break;
                case 1: _state.ZoneCenter = color; break;
                default: _state.ZoneRight = color; break;
            }
        }

        private void HighlightZoneButtons()
        {
            SetSegmentActive(BtnZoneLeft, _activeZoneIndex == 0);
            SetSegmentActive(BtnZoneCenter, _activeZoneIndex == 1);
            SetSegmentActive(BtnZoneRight, _activeZoneIndex == 2);
        }

        private void OpenColorPickerForCurrentZone()
        {
            var (initR, initG, initB) = _state.LedMode == LedMode.TripleZone ? GetZoneColor(_activeZoneIndex) : _state.Color;

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

                                if (_state.LedBrightness > 0)
                                {
                                    if (_state.LedMode is LedMode.Static or LedMode.Breathing)
                                    {
                                        _bridge.SetAllKeyboardLed(_state.LedMode, _state.LedBrightness, current.R, current.G, current.B);
                                    }
                                    else if (_state.LedMode == LedMode.TripleZone)
                                    {
                                        var left = _activeZoneIndex == 0 ? current : _state.ZoneLeft;
                                        var center = _activeZoneIndex == 1 ? current : _state.ZoneCenter;
                                        var right = _activeZoneIndex == 2 ? current : _state.ZoneRight;
                                        _bridge.SetKeyboardTripleZone(_state.LedBrightness, left, center, right);
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

            picker.ColorPreviewChanged += (r, g, b) => ScheduleHardwarePreview((r, g, b));

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
            if (_state.LedMode == LedMode.TripleZone)
            {
                SetZoneColor(_activeZoneIndex, (r, g, b));
                UpdateTripleZoneSwatches();
            }
            else
            {
                _state.Color = (r, g, b);
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
            EllipseSingleColor.Fill = ToBrush(_state.Color);
            TxtSingleColorHex.Text = $"#{_state.Red:X2}{_state.Green:X2}{_state.Blue:X2}";
        }

        private void UpdateTripleZoneSwatches()
        {
            EllipseZoneLeft.Fill = ToBrush(_state.ZoneLeft);
            EllipseZoneCenter.Fill = ToBrush(_state.ZoneCenter);
            EllipseZoneRight.Fill = ToBrush(_state.ZoneRight);
            HighlightZoneButtons();
        }

        private static SolidColorBrush ToBrush((byte r, byte g, byte b) c) => new(MediaColor.FromRgb(c.r, c.g, c.b));

        private void ApplyLedHardwareOnly()
        {
            if (!_isInitialized) return;

            if (_state.LedBrightness == 0)
            {
                StopActiveAnimation();
                _bridge.TurnOffAllLights();
            }
            else if (IsAnimatedMode)
            {
                StartActiveAnimation();
            }
            else
            {
                StopActiveAnimation();
                if (_state.LedMode == LedMode.TripleZone)
                {
                    _bridge.SetKeyboardTripleZone(_state.LedBrightness, _state.ZoneLeft, _state.ZoneCenter, _state.ZoneRight);
                }
                else
                {
                    _bridge.SetAllKeyboardLed(_state.LedMode, _state.LedBrightness, _state.Red, _state.Green, _state.Blue);
                }
            }
        }

        private void ApplyLedSettings()
        {
            if (!_isInitialized) return;

            _idleWatcher.ResetWake();
            ApplyLedHardwareOnly();

            // Persist lighting only (power mode and cycle speed are saved by their own handlers)
            _state.CopyLightingTo(_config.CurrentSettings, includePowerAndSpeed: false);
            _config.SaveSettings();
        }

        private void StartActiveAnimation()
        {
            StopActiveAnimation();

            if (_state.LedBrightness == 0 || !IsAnimatedMode)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            _animCts = cts;

            _animTask = System.Threading.Tasks.Task.Run(async () =>
            {
                double phase = 0;
                while (!token.IsCancellationRequested && _state.LedBrightness > 0)
                {
                    try
                    {
                        if (_state.LedMode == LedMode.Cycle)
                        {
                            var c = ThemeHelper.HsvToRgb(phase, 1.0, 1.0);
                            _bridge.SetKeyboardTripleZone(_state.LedBrightness, c, c, c);
                        }
                        else if (_state.LedMode == LedMode.Rainbow)
                        {
                            var left = ThemeHelper.HsvToRgb((phase + 100.0) % 360.0, 1.0, 1.0);
                            var center = ThemeHelper.HsvToRgb((phase + 50.0) % 360.0, 1.0, 1.0);
                            var right = ThemeHelper.HsvToRgb(phase, 1.0, 1.0);
                            _bridge.SetKeyboardTripleZone(_state.LedBrightness, left, center, right);
                        }
                        else
                        {
                            break;
                        }

                        double step = _state.CycleSpeed switch
                        {
                            1 => 6.0,
                            2 => 16.0,
                            _ => 2.0
                        };
                        phase = (phase + step) % 360.0;

                        await System.Threading.Tasks.Task.Delay(14, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch { }
                }
            }, token);
        }

        private void StopActiveAnimation()
        {
            var cts = _animCts;
            var task = _animTask;
            _animCts = null;
            _animTask = null;
            if (cts == null) return;

            try
            {
                cts.Cancel();
                // Let an in-flight frame finish so it cannot overwrite the next static color / lights-off command
                task?.Wait(250);
            }
            catch { }
            finally
            {
                cts.Dispose();
            }
        }

        #endregion

        #region RGB Sleep Timer Handlers

        private void BtnRgbSleepTimer_Click(object sender, RoutedEventArgs e)
        {
            if (PopupRgbSleep != null)
            {
                PopupRgbSleep.IsOpen = !PopupRgbSleep.IsOpen;
            }
        }

        private void SleepOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string tagStr && int.TryParse(tagStr, out int seconds))
            {
                SetKeyboardSleepTimeout(seconds);
            }
        }

        private void SetKeyboardSleepTimeout(int seconds)
        {
            _config.CurrentSettings.KeyboardIdleTimeoutSeconds = seconds;
            _config.SaveSettings();
            _idleWatcher.TimeoutSeconds = seconds;
            UpdateRgbSleepUI();
            if (PopupRgbSleep != null)
            {
                PopupRgbSleep.IsOpen = false;
            }
        }

        private void UpdateRgbSleepUI()
        {
            int seconds = _config.CurrentSettings.KeyboardIdleTimeoutSeconds;

            var options = new (UIElement Check, int Seconds)[]
            {
                (CheckSleepOpt0, 0), (CheckSleepOpt15, 15), (CheckSleepOpt30, 30),
                (CheckSleepOpt45, 45), (CheckSleepOpt60, 60), (CheckSleepOpt300, 300)
            };
            foreach (var (check, value) in options)
            {
                check.Visibility = seconds == value ? Visibility.Visible : Visibility.Collapsed;
            }

            var activeBrush = seconds > 0
                ? TryFindResource("AccentColor") as Brush ?? (Brush)FindResource("AccentCyan")
                : TryFindResource("TextMuted") as Brush ?? Brushes.Gray;

            TxtRgbSleepTimerLabel.Text = LocalizationManager.GetSleepShortLabel(seconds);
            TxtRgbSleepTimerLabel.Foreground = activeBrush;
            IconTimerPath.Stroke = activeBrush;

            BtnRgbSleepTimer.ToolTip = $"{LocalizationManager.KeyboardSleepTimer}: {LocalizationManager.GetSleepOptionLabel(seconds)}";
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
            var res = Resources;
            var accent = ThemeHelper.ApplyBasePalette(res, isDark, _config.CurrentSettings.ThemeAccentColor);

            ThemeHelper.Set(res, "ProgressTrack", isDark ? ThemeHelper.Rgb(37, 40, 52) : ThemeHelper.Rgb(226, 232, 240));
            ThemeHelper.Set(res, "AccentCyan", isDark ? ThemeHelper.Rgb(34, 211, 238) : ThemeHelper.Rgb(2, 132, 199));   // CPU dot
            ThemeHelper.Set(res, "AccentGreen", isDark ? ThemeHelper.Rgb(74, 222, 128) : ThemeHelper.Rgb(22, 163, 74));
            ThemeHelper.Set(res, "AccentAmber", isDark ? ThemeHelper.Rgb(245, 158, 11) : ThemeHelper.Rgb(217, 119, 6));
            ThemeHelper.Set(res, "SegmentBg", isDark ? ThemeHelper.Rgb(30, 32, 42) : ThemeHelper.Rgb(248, 250, 252));
            ThemeHelper.Set(res, "SegmentBorder", isDark ? ThemeHelper.Rgb(46, 50, 66) : ThemeHelper.Rgb(203, 213, 225));
            ThemeHelper.Set(res, "SegmentActiveBg", ThemeHelper.WithAlpha(accent, isDark ? (byte)60 : (byte)40));
            ThemeHelper.Set(res, "SegmentActiveBorder", accent);
            ThemeHelper.Set(res, "BtnUpdateBg", isDark ? ThemeHelper.WithAlpha(accent, 70) : accent);
            ThemeHelper.Set(res, "BtnUpdateBorder", accent);
            ThemeHelper.Set(res, "BtnUpdateFg", ThemeHelper.Rgb(255, 255, 255));

            // Sun icon is stroked (dark theme), moon icon is filled (light theme)
            IconThemePath.Data = (Geometry)FindResource(isDark ? "IconSun" : "IconMoon");
            if (isDark)
            {
                IconThemePath.SetResourceReference(System.Windows.Shapes.Path.StrokeProperty, "TextMuted");
                IconThemePath.Fill = Brushes.Transparent;
            }
            else
            {
                IconThemePath.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "TextMuted");
                IconThemePath.Stroke = Brushes.Transparent;
            }

            ThemeHelper.PublishToApplication(res);

            var accentBrush = (Brush)res["AccentColor"];
            PbRamUsage.Foreground = accentBrush;
            PbSsdUsage.Foreground = accentBrush;
            TxtRamPercent.Foreground = accentBrush;
            TxtSsdPercent.Foreground = accentBrush;
            IconRamPath.Stroke = accentBrush;
            IconDiskPath.Stroke = accentBrush;

            UpdatePowerModeButtonsUI();
            HighlightBrightnessButtons(_state.LedBrightness);
            HighlightSpeedButtons(_state.CycleSpeed);
            UpdateRgbSleepUI();
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
                existingProfile.CopyLightingTo(_state);
                UpdatePowerModeButtonsUI();
                RefreshLightingUI();
            }

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
            _state.CopyLightingTo(profile);

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
                    profile.CopyLightingTo(_editingProfile);
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

            // Remember the user's own settings once, so they can be restored when the app closes
            if (_snapshotBeforeProfile == null)
            {
                _snapshotBeforeProfile = new AppProfile();
                _state.CopyLightingTo(_snapshotBeforeProfile);
            }

            _activeProfileName = profile.Name;
            profile.CopyLightingTo(_state);

            PushPowerModeToHardware();
            UpdatePowerModeButtonsUI();
            RefreshLightingUI();
            ApplyLedHardwareOnly();
            UpdateModelStatus();
        }

        public void RestoreDefaultSettings()
        {
            if (_isProfileSetupMode) return;

            _activeProfileName = null;

            var s = _snapshotBeforeProfile;
            _snapshotBeforeProfile = null;

            if (s == null)
            {
                s = new AppProfile();
                _config.CurrentSettings.CopyLightingTo(s);
            }

            s.CopyLightingTo(_state);
            PushPowerModeToHardware();
            _config.CurrentSettings.PowerMode = _state.PowerMode;
            UpdatePowerModeButtonsUI();

            RefreshLightingUI();
            ApplyLedSettings();
            UpdateModelStatus();
        }

        /// <summary>Syncs brightness/speed segments, LED mode selector and color swatches with the current state.</summary>
        private void RefreshLightingUI()
        {
            HighlightBrightnessButtons(_state.LedBrightness);
            HighlightSpeedButtons(_state.CycleSpeed);
            UpdateLedModeButtonsUI();
            UpdateColorSwatches();
        }

        /// <summary>Footer status: active profile name (accent) or "up to date" (muted).</summary>
        private void UpdateModelStatus()
        {
            bool hasProfile = !string.IsNullOrEmpty(_activeProfileName);
            TxtModelStatus.Text = hasProfile
                ? $" {LocalizationManager.ProfileActivated}{_activeProfileName}"
                : $" {LocalizationManager.UpToDate}";
            TxtModelStatus.SetResourceReference(TextBlock.ForegroundProperty, hasProfile ? "AccentCyan" : "TextMuted");
        }

        #endregion

        #region Localization & Settings Modal

        private void BtnSettings_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();

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

            TxtProfileSetupBannerTitle.Text = LocalizationManager.ProfileSetupBannerTitle;
            TxtProfileSetupBannerDesc.Text = LocalizationManager.ProfileSetupBannerDesc;
            BtnConfirmProfileSettings.Content = $"✓ {LocalizationManager.ConfirmSettings}";
            BtnCancelProfileSettings.Content = LocalizationManager.Cancel;

            UpdatePowerModeButtonsUI();
            UpdateLedModeButtonsUI();
            UpdateUpdateStatusUI();

            TxtSleepOpt0.Text = LocalizationManager.GetSleepOptionLabel(0);
            TxtSleepOpt15.Text = LocalizationManager.GetSleepOptionLabel(15);
            TxtSleepOpt30.Text = LocalizationManager.GetSleepOptionLabel(30);
            TxtSleepOpt45.Text = LocalizationManager.GetSleepOptionLabel(45);
            TxtSleepOpt60.Text = LocalizationManager.GetSleepOptionLabel(60);
            TxtSleepOpt300.Text = LocalizationManager.GetSleepOptionLabel(300);
            UpdateRgbSleepUI();
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
                string ver = _latestUpdateResult.LatestVersion ?? UpdateService.CurrentVersion;
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
                // No update: hide banner, show active profile or "- Güncel" in footer
                CardUpdateBanner.Visibility = Visibility.Collapsed;
                UpdateModelStatus();
            }
        }

        private async void BtnUpdateAction_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;

            if (_latestUpdateResult != null && _latestUpdateResult.HasUpdate && !string.IsNullOrEmpty(_latestUpdateResult.DownloadUrl))
            {
                _isUpdating = true;
                UpdateUpdateStatusUI();

                var progress = new Progress<(long bytesDownloaded, long? totalBytes, double percentage)>(p =>
                {
                    double downloadedMb = p.bytesDownloaded / (1024.0 * 1024.0);
                    if (p.percentage >= 100.0)
                    {
                        TxtUpdateBannerMessage.Text = LocalizationManager.ExtractingStatus;
                    }
                    else if (p.totalBytes is long total && total > 0)
                    {
                        double totalMb = total / (1024.0 * 1024.0);
                        TxtUpdateBannerMessage.Text = $"{LocalizationManager.DownloadingStatus} ({downloadedMb:0.#} / {totalMb:0.#} MB - %{p.percentage:0})";
                    }
                    else
                    {
                        TxtUpdateBannerMessage.Text = $"{LocalizationManager.DownloadingStatus} ({downloadedMb:0.#} MB)";
                    }
                });

                bool success = await UpdateService.DownloadAndRestartAsync(_latestUpdateResult.DownloadUrl, progress);
                if (!success)
                {
                    _isUpdating = false;
                    UpdateUpdateStatusUI();
                    await UpdateService.CheckForUpdatesAsync(!LocalizationManager.IsTurkish, this);
                }
            }
            else
            {
                await UpdateService.CheckForUpdatesAsync(!LocalizationManager.IsTurkish, this);
            }
        }

        #endregion
    }
}
