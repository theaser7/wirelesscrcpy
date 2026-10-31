using System;
using System.IO;

namespace WirelessCrcpy.Tests
{
    public class TestRunner
    {
        public static int Main(string[] args)
        {
            int failed = 0;
            Console.WriteLine("=== Running wirelesscrcpy Unit Tests ===");

            try
            {
                TestSettingsDefaultValues();
                Console.WriteLine("[PASS] TestSettingsDefaultValues");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FAIL] TestSettingsDefaultValues: " + ex.Message);
                failed++;
            }

            try
            {
                TestSettingsSaveAndLoad();
                Console.WriteLine("[PASS] TestSettingsSaveAndLoad");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FAIL] TestSettingsSaveAndLoad: " + ex.Message);
                failed++;
            }

            if (failed > 0)
            {
                Console.WriteLine(string.Format("FAILED: {0} tests failed.", failed));
                return 1;
            }

            Console.WriteLine("Settings tests passed.");
            return 0;
        }

        private static void TestSettingsDefaultValues()
        {
            var settings = new WirelessCrcpy.AppSettings();
            if (settings.Port != 5555)
                throw new Exception("Default port should be 5555, but was " + settings.Port);
            if (!settings.ForwardAudio)
                throw new Exception("Default ForwardAudio should be true");
            if (settings.BitRate != 8)
                throw new Exception("Default BitRate should be 8");
            if (!settings.TurnScreenOff)
                throw new Exception("Default TurnScreenOff should be true");
        }

        private static void TestSettingsSaveAndLoad()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "wirelesscrcpy_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            try
            {
                var mgr = new WirelessCrcpy.SettingsManager(testDir);
                var settings = new WirelessCrcpy.AppSettings();
                settings.DeviceIp = "192.168.1.123";
                settings.Port = 5556;
                settings.TurnScreenOff = false;
                settings.StayAwake = true;

                mgr.Save(settings);

                var loaded = mgr.Load();
                if (loaded.DeviceIp != "192.168.1.123")
                    throw new Exception("Loaded DeviceIp mismatch: " + loaded.DeviceIp);
                if (loaded.Port != 5556)
                    throw new Exception("Loaded Port mismatch: " + loaded.Port);
                if (loaded.TurnScreenOff != false)
                    throw new Exception("Loaded TurnScreenOff mismatch");
                if (loaded.StayAwake != true)
                    throw new Exception("Loaded StayAwake mismatch");
            }
            finally
            {
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, true);
            }
        }
    }
}
