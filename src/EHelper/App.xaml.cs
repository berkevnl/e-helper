using System;
using System.Threading;
using System.Windows;
using EHelper.Config;
using EHelper.Core;
using EHelper.Hardware.Casper;
using EHelper.Hardware.Simulation;
using EHelper.Services;
using EHelper.UI;

namespace EHelper
{
    public partial class App : System.Windows.Application
    {
        private const string AppMutexName = "EHelper_SingleInstance_Mutex";
        private const string ShowWindowEventName = "EHelper_Show_Window_Event";
        private Mutex? _mutex;
        private EventWaitHandle? _showEvent;
        private RegisteredWaitHandle? _waitHandleRegistration;
        private IHardwareProvider? _bridge;
        private HardwareMonitorService? _monitor;
        private SystemTrayManager? _trayManager;
        private MainWindow? _mainWindow;
        private int _telemetryCounter;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global Exception Handling
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Beklenmeyen bir hata oluştu:\n{ex.Message}\n\n{ex.StackTrace}",
                        "E-Helper Hatası",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            };

            DispatcherUnhandledException += (s, args) =>
            {
                args.Handled = true;
                System.Windows.MessageBox.Show(
                    $"Arayüz hatası yakalandı:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                    "E-Helper Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            };

            // Single-instance enforcement
            _mutex = new Mutex(true, AppMutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                try
                {
                    if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out var existingEvent))
                    {
                        existingEvent.Set();
                        existingEvent.Dispose();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "E-Helper zaten arka planda çalışıyor. Sistem tepsisindeki (Gizli Simgeler) simgeye tıklayabilirsiniz.",
                            "E-Helper",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                }
                catch
                {
                    // Fallback if IPC event cannot be reached
                }
                Shutdown();
                return;
            }

            try
            {
                // 1. Initialize Hardware Provider (Auto-detect Casper Excalibur or Mock fallback)
                var (bridge, isSimulated) = HardwareDetector.DetectAndCreate();
                _bridge = bridge;

                // 2. Initialize Configuration Engine
                var config = new ConfigManager();

                // 3. Initialize Hardware Telemetry Service (do not start yet)
                _monitor = new HardwareMonitorService(_bridge, config.CurrentSettings.PollingIntervalSeconds);

                // 4. Initialize Main Flyout Window (stays hidden until tray icon clicked or shown)
                _mainWindow = new MainWindow(_bridge, isSimulated, config, _monitor);

                // 5. Initialize System Tray (Notification Area Icon)
                _trayManager = new SystemTrayManager(_mainWindow, ShutdownApp);

                // 6. Wire up Telemetry Tooltip & Periodic Memory Optimizer
                _monitor.TelemetryUpdated += t =>
                {
                    if (_trayManager != null)
                    {
                        if (t.IsAvailable)
                        {
                            string cpuStr = t.CpuTemperature > 0 ? $"{t.CpuTemperature}°C" : "--°C";
                            string gpuStr = t.GpuTemperature > 0 ? $"{t.GpuTemperature}°C" : "--°C";
                            _trayManager.UpdateTooltip($"CPU: {cpuStr} Fan: {t.CpuFanRpm}RPM\nGPU: {gpuStr} Fan: {t.GpuFanRpm}RPM");
                        }
                        else
                        {
                            _trayManager.UpdateTooltip("CPU: --°C Fan: --RPM\nGPU: --°C Fan: --RPM");
                        }
                    }

                    // Periodically keep background memory lean if flyout window is not open
                    if (++_telemetryCounter % 10 == 0)
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            if (_mainWindow != null && !_mainWindow.IsVisible)
                            {
                                MemoryOptimizer.TrimMemory();
                            }
                        });
                    }
                };

                // 7. Start Telemetry Service after all UI & Tray objects are ready
                _monitor.Start();

                // 8. Register IPC event to allow subsequent launches to activate this window
                try
                {
                    _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowWindowEventName);
                    _waitHandleRegistration = ThreadPool.RegisterWaitForSingleObject(_showEvent, (state, timedOut) =>
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            if (_mainWindow != null)
                            {
                                if (!_mainWindow.IsVisible)
                                {
                                    _mainWindow.ToggleFlyout();
                                }
                                else
                                {
                                    _mainWindow.Activate();
                                    _mainWindow.Focus();
                                }
                            }
                        });
                    }, null, -1, false);
                }
                catch
                {
                    // Non-critical IPC event failure
                }

                // 9. If started interactively (not via Windows autostart), show flyout immediately
                bool startMinimized = false;
                if (e.Args != null)
                {
                    foreach (var arg in e.Args)
                    {
                        if (arg.Equals("--autostart", StringComparison.OrdinalIgnoreCase) ||
                            arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
                        {
                            startMinimized = true;
                            break;
                        }
                    }
                }

                if (!startMinimized)
                {
                    _mainWindow.ToggleFlyout();
                }
                else
                {
                    // If started via Windows logon, refresh tray icon after a brief delay
                    // in case Windows Explorer was still finishing taskbar notification area registration
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await System.Threading.Tasks.Task.Delay(2500);
                            _trayManager?.EnsureVisible();
                        }
                        catch { }
                    });
                }

                // Initial working set trim to drop memory to ultra-lightweight levels (~2-5 MB)
                MemoryOptimizer.TrimMemory();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"E-Helper başlatılırken bir hata oluştu:\n{ex.Message}\n\nDetay:\n{ex.StackTrace}",
                    "E-Helper Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Shutdown();
            }
        }

        public void ShutdownApp()
        {
            try
            {
                _waitHandleRegistration?.Unregister(null);
                _showEvent?.Dispose();
                _monitor?.Stop();
                _monitor?.Dispose();
                _trayManager?.Dispose();
                _bridge?.Dispose();
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors during shutdown
            }
            finally
            {
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ShutdownApp();
            base.OnExit(e);
        }
    }
}
