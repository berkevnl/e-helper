using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using EHelper.Config;
using EHelper.Core;

namespace EHelper.Services
{
    public class ProfileWatcherService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private readonly ConfigManager _config;
        private readonly System.Threading.Timer _timer;
        private bool _isDisposed;
        private int _isChecking;

        private string? _currentActiveProfileId;
        private string? _activeProcessName;
        private uint _activeProcessPid;

        public event Action<AppProfile>? ProfileActivated;
        public event Action? ProfileDeactivated;

        public string? ActiveProfileName { get; private set; }

        public ProfileWatcherService(ConfigManager config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _timer = new System.Threading.Timer(CheckForegroundProcess, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _timer.Change(500, 1000);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void CheckForegroundProcess(object? state)
        {
            if (_isDisposed) return;
            if (Interlocked.CompareExchange(ref _isChecking, 1, 0) != 0) return;

            try
            {
                var profiles = _config.CurrentSettings.Profiles;
                if (profiles == null || profiles.Count == 0)
                {
                    if (_currentActiveProfileId != null)
                    {
                        _currentActiveProfileId = null;
                        _activeProcessName = null;
                        _activeProcessPid = 0;
                        ActiveProfileName = null;
                        ProfileDeactivated?.Invoke();
                    }
                    return;
                }

                // 1. Check foreground window process first (highest priority)
                string currentForegroundExe = "";
                uint fgPid = 0;
                IntPtr fgHwnd = GetForegroundWindow();
                if (fgHwnd != IntPtr.Zero)
                {
                    GetWindowThreadProcessId(fgHwnd, out fgPid);
                    if (fgPid > 0)
                    {
                        try
                        {
                            using var proc = Process.GetProcessById((int)fgPid);
                            currentForegroundExe = proc.ProcessName; // without .exe
                        }
                        catch { }
                    }
                }

                AppProfile? matchedProfile = null;
                uint matchedPid = 0;

                // Priority A: Active foreground window matches a profile
                if (!string.IsNullOrEmpty(currentForegroundExe))
                {
                    foreach (var p in profiles)
                    {
                        if (string.IsNullOrWhiteSpace(p.ProcessName)) continue;
                        string cleanTarget = Path.GetFileNameWithoutExtension(p.ProcessName).Trim();
                        if (string.Equals(cleanTarget, currentForegroundExe, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedProfile = p;
                            matchedPid = fgPid;
                            break;
                        }
                    }
                }

                // Priority B: If foreground window did not match, check if current active profile is still alive (sticky session)
                if (matchedProfile == null && _currentActiveProfileId != null && !string.IsNullOrEmpty(_activeProcessName))
                {
                    bool isCurrentActiveStillRunning = false;
                    if (_activeProcessPid > 0)
                    {
                        try
                        {
                            using var proc = Process.GetProcessById((int)_activeProcessPid);
                            isCurrentActiveStillRunning = !proc.HasExited;
                        }
                        catch
                        {
                            isCurrentActiveStillRunning = false;
                        }
                    }

                    if (!isCurrentActiveStillRunning)
                    {
                        try
                        {
                            var procs = Process.GetProcessesByName(_activeProcessName);
                            isCurrentActiveStillRunning = procs != null && procs.Length > 0;
                            if (isCurrentActiveStillRunning && procs!.Length > 0)
                            {
                                _activeProcessPid = (uint)procs[0].Id;
                            }
                        }
                        catch { }
                    }

                    if (isCurrentActiveStillRunning)
                    {
                        // Current profile application is still running (e.g. user Alt-Tabbed to desktop or browser)
                        return;
                    }
                }

                // Priority C: Check if ANY profiled application is currently running on the PC (background / launched app detection)
                if (matchedProfile == null)
                {
                    foreach (var p in profiles)
                    {
                        if (string.IsNullOrWhiteSpace(p.ProcessName)) continue;
                        string cleanTarget = Path.GetFileNameWithoutExtension(p.ProcessName).Trim();
                        try
                        {
                            var runningProcs = Process.GetProcessesByName(cleanTarget);
                            if (runningProcs != null && runningProcs.Length > 0)
                            {
                                matchedProfile = p;
                                matchedPid = (uint)runningProcs[0].Id;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                // 2. State transition
                if (matchedProfile != null)
                {
                    if (_currentActiveProfileId != matchedProfile.Id)
                    {
                        _currentActiveProfileId = matchedProfile.Id;
                        _activeProcessName = Path.GetFileNameWithoutExtension(matchedProfile.ProcessName).Trim();
                        _activeProcessPid = matchedPid;
                        ActiveProfileName = matchedProfile.Name;
                        ProfileActivated?.Invoke(matchedProfile);
                    }
                }
                else if (_currentActiveProfileId != null)
                {
                    // Profiled application closed and no other profiled app is running
                    _currentActiveProfileId = null;
                    _activeProcessName = null;
                    _activeProcessPid = 0;
                    ActiveProfileName = null;
                    ProfileDeactivated?.Invoke();
                }
            }
            catch
            {
                // Ignore transient process query exceptions
            }
            finally
            {
                Interlocked.Exchange(ref _isChecking, 0);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _timer.Dispose();
        }
    }
}
