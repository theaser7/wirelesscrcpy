using System;

namespace WirelessCrcpy.Tests
{
    public class TestAdbManager
    {
        public static void RunTests()
        {
            TestIpParsingFromWlan0Output();
            Console.WriteLine("[PASS] TestIpParsingFromWlan0Output");

            TestIpParsingFromRouteOutput();
            Console.WriteLine("[PASS] TestIpParsingFromRouteOutput");

            TestConnectOutputSuccessEvaluation();
            Console.WriteLine("[PASS] TestConnectOutputSuccessEvaluation");
        }

        private static void TestIpParsingFromWlan0Output()
        {
            string sampleOutput = 
                "24: wlan0: <BROADCAST,MULTICAST,UP,LOWER_UP> mtu 1500 qdisc mq state UP group default\n" +
                "    inet 192.168.1.142/24 brd 192.168.1.255 scope global wlan0\n" +
                "       valid_lft forever preferred_lft forever\n";

            string ip = AdbManager.ParseWifiIpFromOutput(sampleOutput);
            if (ip != "192.168.1.142")
            {
                throw new Exception("Expected 192.168.1.142 but got: " + ip);
            }
        }

        private static void TestIpParsingFromRouteOutput()
        {
            string sampleRoute = 
                "192.168.0.0/24 dev wlan0 proto kernel scope link src 192.168.0.55\n" +
                "default via 192.168.0.1 dev wlan0\n";

            string ip = AdbManager.ParseWifiIpFromOutput(sampleRoute);
            if (ip != "192.168.0.55")
            {
                throw new Exception("Expected 192.168.0.55 but got: " + ip);
            }
        }

        private static void TestConnectOutputSuccessEvaluation()
        {
            if (!AdbManager.IsConnectSuccessOutput("connected to 192.168.1.50:5555"))
                throw new Exception("Should be success for 'connected to ...'");
            if (!AdbManager.IsConnectSuccessOutput("already connected to 192.168.1.50:5555"))
                throw new Exception("Should be success for 'already connected to ...'");
            if (AdbManager.IsConnectSuccessOutput("cannot connect to 192.168.1.50:5555: No connection could be made"))
                throw new Exception("Should fail for connection refused");
            if (AdbManager.IsConnectSuccessOutput("failed to connect to 192.168.1.50:5555"))
                throw new Exception("Should fail for 'failed to connect'");
        }
    }
}
