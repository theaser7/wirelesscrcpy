using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WirelessCrcpy
{
    public class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _itemStatus;
        private readonly ToolStripMenuItem _itemToggleConnect;

        private readonly SettingsManager _settingsManager;
        private AppSettings _settings;
        private readonly ScrcpyManager _scrcpyManager;
        private readonly LogForm _logForm;
        private SettingsForm _settingsForm;

        private ConnectionState _currentState = ConnectionState.Disconnected;
        private readonly object _stateLock = new object();

        public TrayAppContext()
        {
            _settingsManager = new SettingsManager();
            _settings = _settingsManager.Load();
            _scrcpyManager = new ScrcpyManager();
            _logForm = new LogForm();

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            _itemStatus = new ToolStripMenuItem("Status: Disconnected")
            {
                Enabled = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            _itemToggleConnect = new ToolStripMenuItem("Connect", null, (s, e) => ToggleConnection());

            var itemUsb = new ToolStripMenuItem("Setup Device via USB...", null, (s, e) => OpenSettingsAndUsb());
            var itemSettings = new ToolStripMenuItem("Settings...", null, (s, e) => OpenSettings());
            var itemLogs = new ToolStripMenuItem("View Logs...", null, (s, e) => OpenLogs());
            var itemExit = new ToolStripMenuItem("Exit", null, (s, e) => ExitApplication());

            _contextMenu.Items.Add(_itemStatus);
            _contextMenu.Items.Add(_itemToggleConnect);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(itemUsb);
            _contextMenu.Items.Add(itemSettings);
            _contextMenu.Items.Add(itemLogs);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(itemExit);

            _trayIcon = new NotifyIcon
            {
                Icon = IconHelper.GetStatusIcon(ConnectionState.Disconnected),
                ContextMenuStrip = _contextMenu,
                Text = "wirelesscrcpy - Disconnected",
                Visible = true
            };

            _trayIcon.MouseClick += OnTrayIconMouseClick;
            _trayIcon.DoubleClick += (s, e) => OpenSettings();

            _logForm.AppendLog("wirelesscrcpy started.");

            if (_settings.AutoConnectOnLaunch && !string.IsNullOrEmpty(_settings.DeviceIp))
            {
                _logForm.AppendLog("Auto-connecting on launch...");
                ConnectAsync();
            }
        }

        private void OnTrayIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ToggleConnection();
            }
        }

        private void ToggleConnection()
        {
            lock (_stateLock)
            {
                if (_currentState == ConnectionState.Connected || _currentState == ConnectionState.Connecting)
                {
                    DisconnectAsync();
                }
                else
                {
                    ConnectAsync();
                }
            }
        }

        private void SetState(ConnectionState newState, string statusText)
        {
            lock (_stateLock)
            {
                _currentState = newState;
            }

            // Update UI elements on main thread
            _trayIcon.Icon = IconHelper.GetStatusIcon(newState);
            _trayIcon.Text = TruncateText("wirelesscrcpy - " + statusText, 63);
            _itemStatus.Text = "Status: " + statusText;

            if (newState == ConnectionState.Connected)
            {
                _itemToggleConnect.Text = "Disconnect";
            }
            else if (newState == ConnectionState.Connecting)
            {
                _itemToggleConnect.Text = "Connecting...";
            }
            else
            {
                _itemToggleConnect.Text = "Connect";
            }
        }

        private void ConnectAsync()
        {
            _settings = _settingsManager.Load();
            string ip = _settings.DeviceIp != null ? _settings.DeviceIp.Trim() : string.Empty;
            int port = _settings.Port > 0 ? _settings.Port : 5555;

            if (string.IsNullOrEmpty(ip))
            {
                ShowNotification("No IP Address", "Please configure your phone's Wi-Fi IP in Settings first.", ToolTipIcon.Warning);
                OpenSettings();
                return;
            }

            string scrcpyPath = ScrcpyManager.FindScrcpyPath(_settings.ScrcpyPath);
            if (string.IsNullOrEmpty(scrcpyPath) || !File.Exists(scrcpyPath))
            {
                ShowNotification("scrcpy Missing", "scrcpy executable not found. Please install or download it in Settings.", ToolTipIcon.Warning);
                OpenSettings();
                return;
            }

            string adbPath = AdbManager.FindAdbPath(Path.GetDirectoryName(scrcpyPath));
            if (string.IsNullOrEmpty(adbPath) || !File.Exists(adbPath))
            {
                ShowNotification("ADB Missing", "adb executable not found. Please verify scrcpy installation in Settings.", ToolTipIcon.Warning);
                OpenSettings();
                return;
            }

            SetState(ConnectionState.Connecting, "Connecting to " + ip + "...");
            _logForm.AppendLog(string.Format("Initiating connection to {0}:{1}...", ip, port));

            ThreadPool.QueueUserWorkItem(state =>
            {
                string error;
                bool connected = AdbManager.Connect(adbPath, ip, port, out error);

                if (!connected)
                {
                    _logForm.AppendLog("ADB connection failed: " + error);
                    SetState(ConnectionState.Disconnected, "Connection failed");
                    ShowNotification("Connection Failed", "Could not connect to " + ip + ":" + port + "\r\n" + error, ToolTipIcon.Error);
                    return;
                }

                _logForm.AppendLog(string.Format("ADB connected to {0}:{1}. Starting scrcpy...", ip, port));
                SetState(ConnectionState.Connected, "Connected (" + ip + ")");

                bool started = _scrcpyManager.Start(
                    scrcpyPath,
                    _settings,
                    onExited: () =>
                    {
                        _logForm.AppendLog("scrcpy mirroring window closed.");
                        SetState(ConnectionState.Disconnected, "Disconnected");

                        // Clean up ADB wireless session
                        AdbManager.Disconnect(adbPath, ip, port);
                    },
                    onOutput: logLine =>
                    {
                        _logForm.AppendLog(logLine);
                    });

                if (!started)
                {
                    _logForm.AppendLog("Failed to start scrcpy process.");
                    SetState(ConnectionState.Disconnected, "Failed to start scrcpy");
                    AdbManager.Disconnect(adbPath, ip, port);
                    ShowNotification("Launch Failed", "scrcpy could not be launched. Check logs for details.", ToolTipIcon.Error);
                }
            });
        }

        private void DisconnectAsync()
        {
            _logForm.AppendLog("Disconnect requested by user.");
            SetState(ConnectionState.Disconnected, "Disconnected");

            string scrcpyPath = ScrcpyManager.FindScrcpyPath(_settings.ScrcpyPath);
            string adbPath = AdbManager.FindAdbPath(scrcpyPath != null ? Path.GetDirectoryName(scrcpyPath) : null);

            _scrcpyManager.Stop();

            if (!string.IsNullOrEmpty(adbPath) && !string.IsNullOrEmpty(_settings.DeviceIp))
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    AdbManager.Disconnect(adbPath, _settings.DeviceIp, _settings.Port > 0 ? _settings.Port : 5555);
                    _logForm.AppendLog("ADB disconnected.");
                });
            }
        }

        private void OpenSettings()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_settingsManager);
                _settingsForm.SettingsSaved += updatedSettings =>
                {
                    _settings = updatedSettings;
                    _logForm.AppendLog("Settings updated and saved.");
                };
            }

            if (!_settingsForm.Visible)
            {
                _settingsForm.Show();
            }
            else
            {
                _settingsForm.BringToFront();
            }
        }

        private void OpenSettingsAndUsb()
        {
            OpenSettings();
        }

        private void OpenLogs()
        {
            if (!_logForm.Visible)
            {
                _logForm.Show();
            }
            else
            {
                _logForm.BringToFront();
            }
        }

        private void ShowNotification(string title, string text, ToolTipIcon icon)
        {
            _trayIcon.ShowBalloonTip(3000, title, text, icon);
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }

        private void ExitApplication()
        {
            _logForm.AppendLog("Exiting wirelesscrcpy...");
            _scrcpyManager.Stop();

            string scrcpyPath = ScrcpyManager.FindScrcpyPath(_settings.ScrcpyPath);
            string adbPath = AdbManager.FindAdbPath(scrcpyPath != null ? Path.GetDirectoryName(scrcpyPath) : null);
            if (!string.IsNullOrEmpty(adbPath) && !string.IsNullOrEmpty(_settings.DeviceIp))
            {
                AdbManager.Disconnect(adbPath, _settings.DeviceIp, _settings.Port > 0 ? _settings.Port : 5555);
            }

            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }
    }
}
