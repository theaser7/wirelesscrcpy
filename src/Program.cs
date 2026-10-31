using System;
using System.Threading;
using System.Windows.Forms;

namespace WirelessCrcpy
{
    public static class Program
    {
        private static Mutex _appMutex;

        [STAThread]
        public static void Main(string[] args)
        {
            bool isFirstInstance;
            _appMutex = new Mutex(true, "Global\\wirelesscrcpy_SingleInstance_Mutex_2026", out isFirstInstance);

            if (!isFirstInstance)
            {
                MessageBox.Show(
                    "wirelesscrcpy is already running in your system tray (near the clock).\r\nClick the tray icon to connect or disconnect.",
                    "wirelesscrcpy Already Running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayAppContext());
            }
            finally
            {
                if (_appMutex != null)
                {
                    _appMutex.ReleaseMutex();
                    _appMutex.Close();
                }
            }
        }
    }
}
