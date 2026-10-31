using System;
using System.Drawing;

namespace WirelessCrcpy.Tests
{
    public class TestIcons
    {
        public static void RunTests()
        {
            TestIconGeneration();
            Console.WriteLine("[PASS] TestIconGeneration");
        }

        private static void TestIconGeneration()
        {
            Icon icon1 = IconHelper.GetStatusIcon(ConnectionState.Disconnected);
            if (icon1 == null || icon1.Handle == IntPtr.Zero)
                throw new Exception("Disconnected icon is invalid");

            Icon icon2 = IconHelper.GetStatusIcon(ConnectionState.Connecting);
            if (icon2 == null || icon2.Handle == IntPtr.Zero)
                throw new Exception("Connecting icon is invalid");

            Icon icon3 = IconHelper.GetStatusIcon(ConnectionState.Connected);
            if (icon3 == null || icon3.Handle == IntPtr.Zero)
                throw new Exception("Connected icon is invalid");
        }
    }
}
