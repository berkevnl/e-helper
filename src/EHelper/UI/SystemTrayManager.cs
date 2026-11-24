using System;
using System.Drawing;
using System.Windows.Forms;
using EHelper.Services;

namespace EHelper.UI
{
    public class SystemTrayManager : IDisposable
    {
        private readonly MainWindow _mainWindow;
        private readonly Action _onExit;
        private readonly NotifyIcon _notifyIcon;
        private bool _isDisposed;

        public SystemTrayManager(MainWindow mainWindow, Action onExit)
        {
            _mainWindow = mainWindow;
            _onExit = onExit;

            _notifyIcon = new NotifyIcon
            {
                Icon = IconHelper.CreateTrayIcon(),
                Text = "CPU: --°C Fan: --RPM\nGPU: --°C Fan: --RPM",
                Visible = true
            };

            SetupContextMenu();
            SetupEvents();
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();

            menu.Opening += (s, e) =>
            {
                menu.Items.Clear();

                var openItem = new ToolStripMenuItem(LocalizationManager.TrayOpen, null, (o, ev) => ToggleFlyout())
                {
                    Font = new Font(menu.Font, System.Drawing.FontStyle.Bold)
                };

                var exitItem = new ToolStripMenuItem(LocalizationManager.Exit, null, (o, ev) => _onExit());

                menu.Items.Add(openItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(exitItem);
            };

            _notifyIcon.ContextMenuStrip = menu;
        }

        private void SetupEvents()
        {
            // Left click toggles flyout panel
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) ToggleFlyout();
            };
            _notifyIcon.DoubleClick += (s, e) => ToggleFlyout();
        }

        private void ToggleFlyout() => _mainWindow.Dispatcher.Invoke(_mainWindow.ToggleFlyout);

        public void UpdateTooltip(string text)
        {
            if (!_isDisposed && _notifyIcon != null)
            {
                try
                {
                    _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
                }
                catch
                {
                    // Ignore transient Win32 tray notification errors
                }
            }
        }

        public void EnsureVisible()
        {
            if (!_isDisposed && _notifyIcon != null)
            {
                try
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Visible = true;
                }
                catch
                {
                    // Ignore transient Win32 tray notification errors
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
