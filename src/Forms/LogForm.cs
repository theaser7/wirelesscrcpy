using System;
using System.Drawing;
using System.Windows.Forms;

namespace WirelessCrcpy
{
    public class LogForm : Form
    {
        private TextBox _txtLogs;
        private Button _btnClear;
        private Button _btnCopy;
        private Button _btnClose;

        public LogForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "wirelesscrcpy - Diagnostic Logs";
            this.Size = new Size(620, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.Icon = IconHelper.GetStatusIcon(ConnectionState.Connecting);
            this.BackColor = Color.FromArgb(245, 246, 248);

            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(235, 238, 242)
            };

            _btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(12, 10),
                Size = new Size(80, 26)
            };
            _btnClear.Click += (s, e) => _txtLogs.Clear();

            _btnCopy = new Button
            {
                Text = "Copy All",
                Location = new Point(100, 10),
                Size = new Size(80, 26)
            };
            _btnCopy.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_txtLogs.Text))
                {
                    Clipboard.SetText(_txtLogs.Text);
                    MessageBox.Show("Logs copied to clipboard!", "wirelesscrcpy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            _btnClose = new Button
            {
                Text = "Close",
                Location = new Point(510, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(85, 26)
            };
            _btnClose.Click += (s, e) => this.Hide();

            panelBottom.Controls.Add(_btnClear);
            panelBottom.Controls.Add(_btnCopy);
            panelBottom.Controls.Add(_btnClose);

            _txtLogs = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                BackColor = Color.FromArgb(24, 27, 33),
                ForeColor = Color.FromArgb(220, 230, 242),
                Font = new Font("Consolas", 9.5F, FontStyle.Regular),
                WordWrap = false
            };

            this.Controls.Add(_txtLogs);
            this.Controls.Add(panelBottom);

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            };
        }

        public void AppendLog(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _txtLogs.AppendText(string.Format("[{0}] {1}\r\n", timestamp, message));
            _txtLogs.SelectionStart = _txtLogs.TextLength;
            _txtLogs.ScrollToCaret();
        }
    }
}
