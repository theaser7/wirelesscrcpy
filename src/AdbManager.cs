using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace WirelessCrcpy
{
    public static class AdbManager
    {
        public static string FindAdbPath(string customDir = null)
        {
            if (!string.IsNullOrEmpty(customDir))
            {
                string candidate = Path.Combine(customDir, "adb.exe");
                if (File.Exists(candidate)) return candidate;
                if (File.Exists(customDir)) return customDir;
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string localAdb = Path.Combine(appDir, "adb.exe");
            if (File.Exists(localAdb)) return localAdb;

            string localScrcpyAdb = Path.Combine(appDir, "scrcpy", "adb.exe");
            if (File.Exists(localScrcpyAdb)) return localScrcpyAdb;

            string parentScrcpyAdb = Path.Combine(appDir, "..", "scrcpy", "adb.exe");
            if (File.Exists(parentScrcpyAdb)) return Path.GetFullPath(parentScrcpyAdb);

            // Check system PATH
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string path in pathEnv.Split(Path.PathSeparator))
            {
                try
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    string fullPath = Path.Combine(path.Trim(), "adb.exe");
                    if (File.Exists(fullPath)) return fullPath;
                }
                catch { }
            }

            return null;
        }

        public static bool RunAdbCommand(string adbPath, string arguments, out string output, out string error, int timeoutMs = 8000)
        {
            output = string.Empty;
            error = string.Empty;

            if (string.IsNullOrEmpty(adbPath) || !File.Exists(adbPath))
            {
                error = "ADB executable not found.";
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        error = "Failed to start ADB process.";
                        return false;
                    }

                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();

                    if (!process.WaitForExit(timeoutMs))
                    {
                        try { process.Kill(); } catch { }
                        error = "ADB command timed out.";
                        return false;
                    }

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool Connect(string adbPath, string ip, int port, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrEmpty(ip))
            {
                error = "IP address is empty.";
                return false;
            }

            string target = string.Format("{0}:{1}", ip.Trim(), port);
            string stdout, stderr;
            RunAdbCommand(adbPath, "connect " + target, out stdout, out stderr, 6000);

            string combined = (stdout + " " + stderr).Trim();
            if (IsConnectSuccessOutput(combined))
            {
                return true;
            }

            error = !string.IsNullOrEmpty(stderr) ? stderr.Trim() : (!string.IsNullOrEmpty(stdout) ? stdout.Trim() : "Failed to connect to " + target);
            return false;
        }

        public static bool Disconnect(string adbPath, string ip, int port)
        {
            if (string.IsNullOrEmpty(adbPath) || !File.Exists(adbPath))
                return false;

            string target = string.IsNullOrEmpty(ip) ? "" : string.Format("{0}:{1}", ip.Trim(), port);
            string stdout, stderr;
            return RunAdbCommand(adbPath, "disconnect " + target, out stdout, out stderr, 4000);
        }

        public static bool EnableTcpIp(string adbPath, int port, out string error, string targetSerial = null)
        {
            string stdout, stderr;
            string prefix = !string.IsNullOrEmpty(targetSerial) ? string.Format("-s {0} ", targetSerial) : "-d ";
            bool ok = RunAdbCommand(adbPath, prefix + "tcpip " + port, out stdout, out stderr, 6000);

            // Fallback without -d if -d failed because no USB prefix was recognized
            if (!ok && string.IsNullOrEmpty(targetSerial))
            {
                ok = RunAdbCommand(adbPath, "tcpip " + port, out stdout, out stderr, 6000);
            }

            string combined = (stdout + " " + stderr).ToLower();
            if (combined.Contains("restarting in tcp mode") || combined.Contains("restarting in tcpip port"))
            {
                error = string.Empty;
                return true;
            }

            error = !string.IsNullOrEmpty(stderr) ? stderr.Trim() : stdout.Trim();
            if (string.IsNullOrEmpty(error) && ok) return true;
            return false;
        }

        public static bool DetectDeviceWifiIp(string adbPath, out string detectedIp, out string error, out string targetSerial)
        {
            detectedIp = string.Empty;
            error = string.Empty;
            targetSerial = null;

            // Query devices list to find active USB device
            string devOut, devErr;
            RunAdbCommand(adbPath, "devices -l", out devOut, out devErr, 4000);

            string unauthSerial = FindUnauthorizedUsbDeviceSerial(devOut);
            if (!string.IsNullOrEmpty(unauthSerial))
            {
                error = string.Format("Device {0} is UNAUTHORIZED.\r\n\r\nUnlock your phone screen and tap 'Allow USB debugging' (check 'Always allow from this computer').", unauthSerial);
                return false;
            }

            targetSerial = FindActiveUsbDeviceSerial(devOut);
            if (string.IsNullOrEmpty(targetSerial))
            {
                error = "No Android device detected over USB by ADB.\r\n\r\nPlease check:\r\n1. Unlock phone and switch USB mode to 'File Transfer (MTP)' in notification drawer.\r\n2. Verify 'USB Debugging' is ON in Developer Options.\r\n3. Try reconnecting the USB cable to another USB port.";
                return false;
            }

            string targetPrefix = string.Format("-s {0} ", targetSerial);

            string stdout, stderr;
            // 1. Try querying wlan0 directly
            RunAdbCommand(adbPath, targetPrefix + "shell ip -f inet addr show wlan0", out stdout, out stderr, 4000);
            detectedIp = ParseWifiIpFromOutput(stdout);

            // 2. Try querying all interfaces
            if (string.IsNullOrEmpty(detectedIp))
            {
                RunAdbCommand(adbPath, targetPrefix + "shell ip -f inet addr show", out stdout, out stderr, 4000);
                detectedIp = ParseWifiIpFromOutput(stdout);
            }

            // 3. Try ip route
            if (string.IsNullOrEmpty(detectedIp))
            {
                RunAdbCommand(adbPath, targetPrefix + "shell ip route", out stdout, out stderr, 4000);
                detectedIp = ParseWifiIpFromOutput(stdout);
            }

            // 4. Try getprop dhcp.wlan0.ipaddress
            if (string.IsNullOrEmpty(detectedIp))
            {
                RunAdbCommand(adbPath, targetPrefix + "shell getprop dhcp.wlan0.ipaddress", out stdout, out stderr, 4000);
                detectedIp = ParseWifiIpFromOutput(stdout);
            }

            if (!string.IsNullOrEmpty(detectedIp))
            {
                return true;
            }

            // If targetPrefix failed, provide clear diagnostic message
            error = !string.IsNullOrEmpty(stderr) ? stderr.Trim() : (!string.IsNullOrEmpty(stdout) ? stdout.Trim() : "No Wi-Fi IP address detected on USB device.");
            return false;
        }

        public static bool DetectDeviceWifiIp(string adbPath, out string detectedIp, out string error)
        {
            string serial;
            return DetectDeviceWifiIp(adbPath, out detectedIp, out error, out serial);
        }

        public static string FindActiveUsbDeviceSerial(string devicesOutput)
        {
            var list = ParseDevicesOutput(devicesOutput);
            // First look for active USB device (not containing colon)
            foreach (var dev in list)
            {
                if (dev.State.Equals("device", StringComparison.OrdinalIgnoreCase) && dev.IsUsb)
                {
                    return dev.Serial;
                }
            }

            // Next look for any active device
            foreach (var dev in list)
            {
                if (dev.State.Equals("device", StringComparison.OrdinalIgnoreCase))
                {
                    return dev.Serial;
                }
            }

            return null;
        }

        public static string FindUnauthorizedUsbDeviceSerial(string devicesOutput)
        {
            var list = ParseDevicesOutput(devicesOutput);
            foreach (var dev in list)
            {
                if (dev.State.Equals("unauthorized", StringComparison.OrdinalIgnoreCase) && dev.IsUsb)
                {
                    return dev.Serial;
                }
            }
            return null;
        }

        public class AdbDevice
        {
            public string Serial { get; set; }
            public string State { get; set; }
            public bool IsUsb { get { return Serial != null && !Serial.Contains(":"); } }
        }

        public static System.Collections.Generic.List<AdbDevice> ParseDevicesOutput(string output)
        {
            var result = new System.Collections.Generic.List<AdbDevice>();
            if (string.IsNullOrEmpty(output)) return result;

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("List of devices") || trimmed.StartsWith("*"))
                    continue;

                string[] parts = Regex.Split(trimmed, @"\s+");
                if (parts.Length >= 2)
                {
                    result.Add(new AdbDevice
                    {
                        Serial = parts[0],
                        State = parts[1]
                    });
                }
            }

            return result;
        }

        public static string ParseWifiIpFromOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return string.Empty;

            // 1. Check "src <ip>" from ip route output
            var matchRoute = Regex.Match(output, @"\bsrc\s+([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})\b");
            if (matchRoute.Success && IsValidHostIp(matchRoute.Groups[1].Value))
            {
                return matchRoute.Groups[1].Value;
            }

            // 2. Check "inet <ip>" from ip addr output
            var matchInet = Regex.Match(output, @"\binet\s+([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})");
            if (matchInet.Success && IsValidHostIp(matchInet.Groups[1].Value))
            {
                return matchInet.Groups[1].Value;
            }

            // 3. Fallback to any private IPv4 host address
            var matchPrivate = Regex.Match(output, @"\b(192\.168\.\d{1,3}\.\d{1,3}|10\.\d{1,3}\.\d{1,3}\.\d{1,3}|172\.(?:1[6-9]|2\d|3[01])\.\d{1,3}\.\d{1,3})\b");
            if (matchPrivate.Success && IsValidHostIp(matchPrivate.Groups[1].Value))
            {
                return matchPrivate.Groups[1].Value;
            }

            return string.Empty;
        }

        private static bool IsValidHostIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            if (ip.StartsWith("127.") || ip.Equals("0.0.0.0")) return false;
            if (ip.EndsWith(".0") || ip.EndsWith(".255")) return false;
            return true;
        }

        public static bool IsConnectSuccessOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return false;
            string lower = output.ToLower();
            if (lower.Contains("connected to") || lower.Contains("already connected to"))
            {
                if (!lower.Contains("cannot connect") && !lower.Contains("failed to connect"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
