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

            TestParseDevicesListWithMultipleAndOffline();
            Console.WriteLine("[PASS] TestParseDevicesListWithMultipleAndOffline");

            TestPrivateIpRangeDetection();
            Console.WriteLine("[PASS] TestPrivateIpRangeDetection");
        }

        private static void TestParseDevicesListWithMultipleAndOffline()
        {
            string output =
                "List of devices attached\n" +
                "85.21.168.138:5555     offline transport_id:4\n" +
                "91.211.114.212:5555    unauthorized transport_id:1\n" +
                "R58M34ABCD             device usb:1-1 product:a52sxq model:SM_A528B device:a52sxq transport_id:2\n";

            var devices = AdbManager.ParseDevicesOutput(output);
            if (devices.Count != 3)
                throw new Exception("Expected 3 devices parsed, got: " + devices.Count);

            string usbSerial = AdbManager.FindActiveUsbDeviceSerial(output);
            if (usbSerial != "R58M34ABCD")
                throw new Exception("Expected USB serial R58M34ABCD, got: " + usbSerial);

            string unauthSerial = AdbManager.FindUnauthorizedUsbDeviceSerial(output);
            if (unauthSerial != null) // 91.211.114.212 is not USB because it has ':'
                throw new Exception("Expected null unauthorized USB, but got: " + unauthSerial);

            string usbUnauthOutput = "List of devices attached\nRF8N1234567\tunauthorized usb:1-1\n";
            if (AdbManager.FindUnauthorizedUsbDeviceSerial(usbUnauthOutput) != "RF8N1234567")
                throw new Exception("Expected RF8N1234567 to be detected as unauthorized USB");
        }

        private static void TestPrivateIpRangeDetection()
        {
            string output10 = "inet 10.0.0.45/24 brd 10.0.0.255 scope global wlan0";
            if (AdbManager.ParseWifiIpFromOutput(output10) != "10.0.0.45")
                throw new Exception("Failed to parse 10.x.x.x IP");

            string output172 = "inet 172.20.10.2/28 brd 172.20.10.15 scope global wlan1";
            if (AdbManager.ParseWifiIpFromOutput(output172) != "172.20.10.2")
                throw new Exception("Failed to parse 172.x.x.x IP");
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
