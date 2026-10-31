using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WirelessCrcpy
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    public static class IconHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private static Icon _iconDisconnected;
        private static Icon _iconConnecting;
        private static Icon _iconConnected;
        private static readonly object _syncRoot = new object();

        public static Icon GetStatusIcon(ConnectionState state)
        {
            lock (_syncRoot)
            {
                switch (state)
                {
                    case ConnectionState.Connecting:
                        if (_iconConnecting == null)
                            _iconConnecting = GenerateIcon(Color.FromArgb(245, 166, 35)); // Amber
                        return _iconConnecting;

                    case ConnectionState.Connected:
                        if (_iconConnected == null)
                            _iconConnected = GenerateIcon(Color.FromArgb(46, 204, 113)); // Emerald Green
                        return _iconConnected;

                    case ConnectionState.Disconnected:
                    default:
                        if (_iconDisconnected == null)
                            _iconDisconnected = GenerateIcon(Color.FromArgb(231, 76, 60)); // Crimson Red
                        return _iconDisconnected;
                }
            }
        }

        private static Icon GenerateIcon(Color statusColor)
        {
            int size = 32;
            using (var bmp = new Bitmap(size, size))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                // Draw phone body frame
                var phoneRect = new Rectangle(5, 2, 22, 28);
                using (var pen = new Pen(Color.FromArgb(40, 44, 52), 2f))
                using (var brush = new SolidBrush(Color.FromArgb(240, 243, 246)))
                {
                    g.FillRectangle(brush, phoneRect);
                    g.DrawRectangle(pen, phoneRect);
                }

                // Phone screen
                var screenRect = new Rectangle(7, 5, 18, 20);
                using (var brush = new SolidBrush(Color.FromArgb(30, 33, 40)))
                {
                    g.FillRectangle(brush, screenRect);
                }

                // Large vibrant status indicator on screen
                using (var brush = new SolidBrush(statusColor))
                {
                    g.FillEllipse(brush, 10, 9, 12, 12);
                }

                // Home indicator dot at bottom
                using (var brush = new SolidBrush(Color.FromArgb(120, 125, 135)))
                {
                    g.FillEllipse(brush, 14, 26, 4, 3);
                }

                IntPtr hIcon = bmp.GetHicon();
                Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
                DestroyIcon(hIcon);
                return icon;
            }
        }
    }
}
