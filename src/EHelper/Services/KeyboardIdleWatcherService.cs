using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace EHelper.Services
{
    public class KeyboardIdleWatcherService : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private readonly System.Threading.Timer _timer;
        private int _timeoutSeconds;
        private bool _isSleeping;
        private bool _isDisposed;

        public event Action<bool>? IdleStateChanged; // true: sleep, false: wake
        public bool IsSleeping => _isSleeping;

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set
            {
                _timeoutSeconds = value;
                if (_timeoutSeconds <= 0 && _isSleeping)
                {
                    _isSleeping = false;
                    IdleStateChanged?.Invoke(false);
                }
            }
        }

        public KeyboardIdleWatcherService(int initialTimeoutSeconds)
        {
            _timeoutSeconds = initialTimeoutSeconds;
            _timer = new System.Threading.Timer(CheckIdleState, null, 1000, 500);
        }

        private void CheckIdleState(object? state)
        {
            if (_isDisposed || _timeoutSeconds <= 0) return;

            uint idleMs = GetIdleTimeMs();
            uint thresholdMs = (uint)(_timeoutSeconds * 1000);

            if (!_isSleeping && idleMs >= thresholdMs)
            {
                _isSleeping = true;
                IdleStateChanged?.Invoke(true);
            }
            else if (_isSleeping && idleMs < thresholdMs)
            {
                _isSleeping = false;
                IdleStateChanged?.Invoke(false);
            }
        }

        public static uint GetIdleTimeMs()
        {
            var lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);
            if (!GetLastInputInfo(ref lii)) return 0;

            uint currentTick = (uint)Environment.TickCount;
            return currentTick >= lii.dwTime ? (currentTick - lii.dwTime) : 0;
        }

        public void ResetWake()
        {
            if (_isSleeping)
            {
                _isSleeping = false;
                IdleStateChanged?.Invoke(false);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _timer?.Dispose();
        }
    }
}
