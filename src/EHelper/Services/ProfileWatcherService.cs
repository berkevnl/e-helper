using System;
using System.Collections.Generic;
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

        private static string TargetName(AppProfile p) => Path.GetFileNameWithoutExtension(p.ProcessName).Trim();

        private void CheckForegroundProcess(object? state)
        {
            if (_isDisposed) return;
            if (Interlocked.CompareExchange(ref _isChecking, 1, 0) != 0) return;

            try
            {
                // Snapshot: the list may be edited from the Settings window while we iterate
                var profiles = _config.CurrentSettings.Profiles?.ToArray();
                if (profiles == null || profiles.Length == 0)
                {
                    DeactivateCurrent();
                    return;
                }

                // Priority A: Active foreground window matches a profile
                var (fgName, fgPid) = GetForegroundProcess();
                AppProfile? matchedProfile = null;
                uint matchedPid = 0;

                if (!string.IsNullOrEmpty(fgName))
                {
                    foreach (var p in profiles)
                    {
                        if (!string.IsNullOrWhiteSpace(p.ProcessName) &&
                            string.Equals(TargetName(p), fgName, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedProfile = p;
                            matchedPid = fgPid;
                            break;
                        }
                    }
                }

                // Priority B: Foreground did not match, keep the current profile while its app is still running
                // (e.g. user Alt-Tabbed to desktop or browser)
                if (matchedProfile == null && _currentActiveProfileId != null && !string.IsNullOrEmpty(_activeProcessName) &&
                    IsActiveProcessStillRunning())
                {
                    return;
                }

                // Priority C: Any profiled application running in the background (first match in profile order)
                if (matchedProfile == null)
                {
                    var running = GetRunningProcessIds();
                    foreach (var p in profiles)
                    {
                        if (!string.IsNullOrWhiteSpace(p.ProcessName) && running.TryGetValue(TargetName(p), out int pid))
                        {
                            matchedProfile = p;
                            matchedPid = (uint)pid;
                            break;
                        }
                    }
                }

                // State transition
                if (matchedProfile != null)
                {
                    if (_currentActiveProfileId != matchedProfile.Id)
                    {
                        _currentActiveProfileId = matchedProfile.Id;
                        _activeProcessName = TargetName(matchedProfile);
                        _activeProcessPid = matchedPid;
                        ProfileActivated?.Invoke(matchedProfile);
                    }
                }
                else
                {
                    // Profiled application closed and no other profiled app is running
                    DeactivateCurrent();
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

        private void DeactivateCurrent()
        {
            if (_currentActiveProfileId == null) return;

            _currentActiveProfileId = null;
            _activeProcessName = null;
            _activeProcessPid = 0;
            ProfileDeactivated?.Invoke();
        }

        private static (string Name, uint Pid) GetForegroundProcess()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return ("", 0);

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return ("", 0);

            try
            {
                using var proc = Process.GetProcessById((int)pid);
                return (proc.ProcessName, pid); // without .exe
            }
            catch
            {
                return ("", pid);
            }
        }

        private bool IsActiveProcessStillRunning()
        {
            if (_activeProcessPid > 0)
            {
                try
                {
                    using var proc = Process.GetProcessById((int)_activeProcessPid);
                    if (!proc.HasExited) return true;
                }
                catch { }
            }

            // PID gone (e.g. launcher restarted the game): look the process up by name
            try
            {
                var procs = Process.GetProcessesByName(_activeProcessName);
                try
                {
                    if (procs.Length > 0)
                    {
                        _activeProcessPid = (uint)procs[0].Id;
                        return true;
                    }
                }
                finally
                {
                    foreach (var p in procs) p.Dispose();
                }
            }
            catch { }

            return false;
        }

        /// <summary>One process enumeration per tick: name (without .exe) → first PID.</summary>
        private static Dictionary<string, int> GetRunningProcessIds()
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    result.TryAdd(proc.ProcessName, proc.Id);
                }
                catch { }
                finally
                {
                    proc.Dispose();
                }
            }
            return result;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _timer.Dispose();
        }
    }
}
