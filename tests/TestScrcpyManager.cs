using System;

namespace WirelessCrcpy.Tests
{
    public class TestScrcpyManager
    {
        public static void RunTests()
        {
            TestBuildArgumentsDefault();
            Console.WriteLine("[PASS] TestBuildArgumentsDefault");

            TestBuildArgumentsCustomOptions();
            Console.WriteLine("[PASS] TestBuildArgumentsCustomOptions");
        }

        private static void TestBuildArgumentsDefault()
        {
            var settings = new AppSettings
            {
                DeviceIp = "192.168.1.100",
                Port = 5555,
                TurnScreenOff = true,
                StayAwake = false,
                ForwardAudio = true,
                BitRate = 8
            };

            string args = ScrcpyManager.BuildArguments(settings);
            if (!args.Contains("-s 192.168.1.100:5555"))
                throw new Exception("Missing serial parameter: " + args);
            if (!args.Contains("--turn-screen-off"))
                throw new Exception("Missing --turn-screen-off flag: " + args);
            if (args.Contains("--stay-awake"))
                throw new Exception("Unexpected --stay-awake flag: " + args);
            if (args.Contains("--no-audio"))
                throw new Exception("Unexpected --no-audio flag: " + args);
        }

        private static void TestBuildArgumentsCustomOptions()
        {
            var settings = new AppSettings
            {
                DeviceIp = "10.0.0.5",
                Port = 5556,
                TurnScreenOff = false,
                StayAwake = true,
                ForwardAudio = false,
                BitRate = 12
            };

            string args = ScrcpyManager.BuildArguments(settings);
            if (!args.Contains("-s 10.0.0.5:5556"))
                throw new Exception("Missing serial parameter: " + args);
            if (args.Contains("--turn-screen-off"))
                throw new Exception("Unexpected --turn-screen-off flag: " + args);
            if (!args.Contains("--stay-awake"))
                throw new Exception("Missing --stay-awake flag: " + args);
            if (!args.Contains("--no-audio"))
                throw new Exception("Missing --no-audio flag: " + args);
            if (!args.Contains("-b 12M"))
                throw new Exception("Missing -b 12M bitrate flag: " + args);
        }
    }
}
