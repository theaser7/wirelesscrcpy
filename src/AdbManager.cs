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

        public static bool EnableTcpIp(string adbPath, int port, out string error)
        {
            string stdout, stderr;
            bool ok = RunAdbCommand(adbPath, "tcpip " + port, out stdout, out stderr, 6000);
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

        public static bool DetectDeviceWifiIp(string adbPath, out string detectedIp, out string error)
        {
            detectedIp = string.Empty;
            error = string.Empty;

            string stdout, stderr;
            // First try querying wlan0 inet address directly
            RunAdbCommand(adbPath, "shell ip -f inet addr show wlan0", out stdout, out stderr, 4000);
            detectedIp = ParseWifiIpFromOutput(stdout);

            if (string.IsNullOrEmpty(detectedIp))
            {
                // Fall back to ip route
                RunAdbCommand(adbPath, "shell ip route", out stdout, out stderr, 4000);
                detectedIp = ParseWifiIpFromOutput(stdout);
            }

            if (!string.IsNullOrEmpty(detectedIp))
            {
                return true;
            }

            error = !string.IsNullOrEmpty(stderr) ? stderr.Trim() : "No Wi-Fi IP address detected on USB device.";
            return false;
        }

        public static string ParseWifiIpFromOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return string.Empty;

            // Pattern for inet 192.168.x.x/24
            var matchInet = Regex.Match(output, @"inet\s+([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})");
            if (matchInet.Success && !matchInet.Groups[1].Value.StartsWith("127."))
            {
                return matchInet.Groups[1].Value;
            }

            // Pattern for src 192.168.x.x in ip route
            var matchRoute = Regex.Match(output, @"src\s+([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})");
            if (matchRoute.Success && !matchRoute.Groups[1].Value.StartsWith("127."))
            {
                return matchRoute.Groups[1].Value;
            }

            return string.Empty;
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
