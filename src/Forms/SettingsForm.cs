using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WirelessCrcpy
{
    public class SettingsForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private AppSettings _settings;

        private TextBox _txtDeviceIp;
        private NumericUpDown _numPort;
        private Button _btnUsbSetup;

        private CheckBox _chkTurnScreenOff;
        private CheckBox _chkStayAwake;
        private CheckBox _chkForwardAudio;
        private CheckBox _chkAutoConnect;
        private NumericUpDown _numBitrate;

        private TextBox _txtScrcpyPath;
        private Button _btnBrowseScrcpy;
        private Button _btnDownloadScrcpy;
        private ProgressBar _pbDownload;
        private Label _lblDownloadStatus;

        private Button _btnSave;
        private Button _btnCancel;

        public event Action<AppSettings> SettingsSaved;

        public SettingsForm(SettingsManager settingsManager)
        {
            if (settingsManager == null) throw new ArgumentNullException("settingsManager");
            _settingsManager = settingsManager;
            _settings = _settingsManager.Load();

            InitializeComponent();
            LoadValuesToUi();
        }

        private void InitializeComponent()
        {
            this.Text = "wirelesscrcpy - Settings";
            this.Size = new Size(540, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.Icon = IconHelper.GetStatusIcon(ConnectionState.Connecting);
            this.BackColor = Color.FromArgb(248, 249, 250);

            int left = 20;
            int rightWidth = 480;
            int y = 15;

            // --- Section 1: Device Connection ---
            var grpDevice = new GroupBox
            {
                Text = " Android Device Wi-Fi Connection ",
                Location = new Point(left, y),
                Size = new Size(rightWidth, 110),
                ForeColor = Color.FromArgb(33, 37, 41)
            };

            var lblIp = new Label { Text = "Phone Wi-Fi IP:", Location = new Point(15, 28), AutoSize = true };
            _txtDeviceIp = new TextBox { Location = new Point(115, 25), Size = new Size(180, 23) };

            var lblPort = new Label { Text = "Port:", Location = new Point(315, 28), AutoSize = true };
            _numPort = new NumericUpDown
            {
                Location = new Point(355, 25),
                Size = new Size(95, 23),
                Minimum = 1,
                Maximum = 65535,
                Value = 5555
            };

            _btnUsbSetup = new Button
            {
                Text = "Setup via USB in 1-Click (Auto-detect IP & Enable Wi-Fi)",
                Location = new Point(15, 62),
                Size = new Size(445, 32),
                BackColor = Color.FromArgb(230, 240, 255),
                FlatStyle = FlatStyle.System
            };
            _btnUsbSetup.Click += OnUsbSetupClick;

            grpDevice.Controls.Add(lblIp);
            grpDevice.Controls.Add(_txtDeviceIp);
            grpDevice.Controls.Add(lblPort);
            grpDevice.Controls.Add(_numPort);
            grpDevice.Controls.Add(_btnUsbSetup);
            this.Controls.Add(grpDevice);

            y += 120;

            // --- Section 2: Mirroring Options ---
            var grpOptions = new GroupBox
            {
                Text = " Mirroring Options ",
                Location = new Point(left, y),
                Size = new Size(rightWidth, 155),
                ForeColor = Color.FromArgb(33, 37, 41)
            };

            _chkTurnScreenOff = new CheckBox
            {
                Text = "Turn off phone screen while mirroring (--turn-screen-off)",
                Location = new Point(15, 25),
                Size = new Size(440, 22),
                Checked = true
            };

            _chkStayAwake = new CheckBox
            {
                Text = "Prevent phone from sleeping (--stay-awake)",
                Location = new Point(15, 50),
                Size = new Size(440, 22)
            };

            _chkForwardAudio = new CheckBox
            {
                Text = "Stream device audio to PC (Android 11+)",
                Location = new Point(15, 75),
                Size = new Size(440, 22),
                Checked = true
            };

            _chkAutoConnect = new CheckBox
            {
                Text = "Auto-connect when wirelesscrcpy launches",
                Location = new Point(15, 100),
                Size = new Size(440, 22)
            };

            var lblBitrate = new Label { Text = "Bitrate (Mbps):", Location = new Point(15, 126), AutoSize = true };
            _numBitrate = new NumericUpDown
            {
                Location = new Point(115, 124),
                Size = new Size(60, 23),
                Minimum = 1,
                Maximum = 64,
                Value = 8
            };

            grpOptions.Controls.Add(_chkTurnScreenOff);
            grpOptions.Controls.Add(_chkStayAwake);
            grpOptions.Controls.Add(_chkForwardAudio);
            grpOptions.Controls.Add(_chkAutoConnect);
            grpOptions.Controls.Add(lblBitrate);
            grpOptions.Controls.Add(_numBitrate);
            this.Controls.Add(grpOptions);

            y += 165;

            // --- Section 3: Dependencies (scrcpy) ---
            var grpDependencies = new GroupBox
            {
                Text = " Scrcpy & ADB Installation ",
                Location = new Point(left, y),
                Size = new Size(rightWidth, 120),
                ForeColor = Color.FromArgb(33, 37, 41)
            };

            var lblPath = new Label { Text = "Scrcpy path:", Location = new Point(15, 25), AutoSize = true };
            _txtScrcpyPath = new TextBox { Location = new Point(15, 45), Size = new Size(350, 23) };

            _btnBrowseScrcpy = new Button
            {
                Text = "Browse...",
                Location = new Point(375, 43),
                Size = new Size(85, 27)
            };
            _btnBrowseScrcpy.Click += OnBrowseScrcpyClick;

            _btnDownloadScrcpy = new Button
            {
                Text = "Download scrcpy (Official)",
                Location = new Point(15, 78),
                Size = new Size(200, 28),
                BackColor = Color.FromArgb(235, 245, 235)
            };
            _btnDownloadScrcpy.Click += OnDownloadScrcpyClick;

            _pbDownload = new ProgressBar
            {
                Location = new Point(225, 80),
                Size = new Size(235, 15),
                Visible = false
            };

            _lblDownloadStatus = new Label
            {
                Location = new Point(225, 98),
                Size = new Size(235, 18),
                Text = string.Empty,
                ForeColor = Color.Gray,
                Visible = false
            };

            grpDependencies.Controls.Add(lblPath);
            grpDependencies.Controls.Add(_txtScrcpyPath);
            grpDependencies.Controls.Add(_btnBrowseScrcpy);
            grpDependencies.Controls.Add(_btnDownloadScrcpy);
            grpDependencies.Controls.Add(_pbDownload);
            grpDependencies.Controls.Add(_lblDownloadStatus);
            this.Controls.Add(grpDependencies);

            y += 130;

            // --- Action Buttons ---
            _btnSave = new Button
            {
                Text = "Save",
                Location = new Point(295, y),
                Size = new Size(100, 32),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(46, 117, 222),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.System
            };
            _btnSave.Click += OnSaveClick;

            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(405, y),
                Size = new Size(95, 32),
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(_btnSave);
            this.Controls.Add(_btnCancel);
            this.AcceptButton = _btnSave;
            this.CancelButton = _btnCancel;
        }

        private void LoadValuesToUi()
        {
            _txtDeviceIp.Text = _settings.DeviceIp ?? string.Empty;
            _numPort.Value = _settings.Port > 0 ? _settings.Port : 5555;
            _chkTurnScreenOff.Checked = _settings.TurnScreenOff;
            _chkStayAwake.Checked = _settings.StayAwake;
            _chkForwardAudio.Checked = _settings.ForwardAudio;
            _chkAutoConnect.Checked = _settings.AutoConnectOnLaunch;
            _numBitrate.Value = _settings.BitRate > 0 ? _settings.BitRate : 8;

            string resolvedScrcpy = ScrcpyManager.FindScrcpyPath(_settings.ScrcpyPath);
            _txtScrcpyPath.Text = resolvedScrcpy ?? _settings.ScrcpyPath ?? string.Empty;
        }

        private void OnBrowseScrcpyClick(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "scrcpy.exe|scrcpy.exe|Executable files (*.exe)|*.exe";
                ofd.Title = "Select scrcpy.exe";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    _txtScrcpyPath.Text = ofd.FileName;
                }
            }
        }

        private void OnUsbSetupClick(object sender, EventArgs e)
        {
            string adbPath = AdbManager.FindAdbPath(Path.GetDirectoryName(_txtScrcpyPath.Text));
            if (string.IsNullOrEmpty(adbPath) || !File.Exists(adbPath))
            {
                MessageBox.Show(this, "ADB executable not found. Please set the scrcpy path or click 'Download scrcpy' first.", "ADB Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnUsbSetup.Enabled = false;
            _btnUsbSetup.Text = "Configuring USB device...";

            ThreadPool.QueueUserWorkItem(state =>
            {
                string detectedIp;
                string error;
                bool ok = AdbManager.DetectDeviceWifiIp(adbPath, out detectedIp, out error);

                if (ok && !string.IsNullOrEmpty(detectedIp))
                {
                    string tcpError;
                    bool tcpOk = AdbManager.EnableTcpIp(adbPath, (int)_numPort.Value, out tcpError);

                    this.Invoke(new Action(() =>
                    {
                        _btnUsbSetup.Enabled = true;
                        _btnUsbSetup.Text = "Setup via USB in 1-Click (Auto-detect IP & Enable Wi-Fi)";

                        if (tcpOk)
                        {
                            _txtDeviceIp.Text = detectedIp;
                            MessageBox.Show(this,
                                string.Format("Success!\r\n\r\nDevice IP detected: {0}\r\nWireless port {1} opened.\r\n\r\nYou can now disconnect the USB cable and click Save!", detectedIp, (int)_numPort.Value),
                                "USB Configuration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            _txtDeviceIp.Text = detectedIp;
                            MessageBox.Show(this, "Detected IP " + detectedIp + " but failed to enable TCP mode: " + tcpError, "TCP Mode Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }));
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        _btnUsbSetup.Enabled = true;
                        _btnUsbSetup.Text = "Setup via USB in 1-Click (Auto-detect IP & Enable Wi-Fi)";
                        MessageBox.Show(this, "Could not detect USB device Wi-Fi IP.\r\n\r\nMake sure:\r\n1. USB debugging is enabled on phone.\r\n2. Phone is connected via USB.\r\n3. Phone is connected to your Wi-Fi.\r\n\r\nError details: " + error,
                            "Device Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        private void OnDownloadScrcpyClick(object sender, EventArgs e)
        {
            _btnDownloadScrcpy.Enabled = false;
            _pbDownload.Visible = true;
            _pbDownload.Value = 0;
            _lblDownloadStatus.Visible = true;
            _lblDownloadStatus.Text = "Starting download...";

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetFolder = Path.Combine(appDir, "scrcpy");

            Downloader.DownloadScrcpyAsync(
                targetFolder,
                progress =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        _pbDownload.Value = Math.Min(100, Math.Max(0, progress));
                        _lblDownloadStatus.Text = string.Format("Downloading: {0}%", progress);
                    }));
                },
                (success, message) =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        _btnDownloadScrcpy.Enabled = true;
                        _pbDownload.Visible = false;
                        _lblDownloadStatus.Visible = false;

                        if (success)
                        {
                            string newScrcpy = Path.Combine(targetFolder, "scrcpy.exe");
                            if (File.Exists(newScrcpy))
                            {
                                _txtScrcpyPath.Text = newScrcpy;
                            }
                            MessageBox.Show(this, "scrcpy downloaded and installed successfully to:\r\n" + targetFolder, "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(this, "Failed to download scrcpy: " + message + "\r\n\r\nYou can download it manually from https://github.com/Genymobile/scrcpy/releases", "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }));
                });
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            _settings.DeviceIp = _txtDeviceIp.Text.Trim();
            _settings.Port = (int)_numPort.Value;
            _settings.TurnScreenOff = _chkTurnScreenOff.Checked;
            _settings.StayAwake = _chkStayAwake.Checked;
            _settings.ForwardAudio = _chkForwardAudio.Checked;
            _settings.AutoConnectOnLaunch = _chkAutoConnect.Checked;
            _settings.BitRate = (int)_numBitrate.Value;
            _settings.ScrcpyPath = _txtScrcpyPath.Text.Trim();

            _settingsManager.Save(_settings);

            if (SettingsSaved != null)
            {
                SettingsSaved(_settings);
            }

            this.Close();
        }
    }
}
