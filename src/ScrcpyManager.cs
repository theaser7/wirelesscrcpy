using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WirelessCrcpy
{
    public class ScrcpyManager
    {
        private Process _process;
        private readonly object _lock = new object();

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    try
                    {
                        return _process != null && !_process.HasExited;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        public static string FindScrcpyPath(string customDir = null)
        {
            if (!string.IsNullOrEmpty(customDir))
            {
                string candidate = Path.Combine(customDir, "scrcpy.exe");
                if (File.Exists(candidate)) return candidate;
                if (File.Exists(customDir)) return customDir;
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string localScrcpy = Path.Combine(appDir, "scrcpy.exe");
            if (File.Exists(localScrcpy)) return localScrcpy;

            string localSubScrcpy = Path.Combine(appDir, "scrcpy", "scrcpy.exe");
            if (File.Exists(localSubScrcpy)) return localSubScrcpy;

            string parentSubScrcpy = Path.Combine(appDir, "..", "scrcpy", "scrcpy.exe");
            if (File.Exists(parentSubScrcpy)) return Path.GetFullPath(parentSubScrcpy);

            // Check system PATH
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string path in pathEnv.Split(Path.PathSeparator))
            {
                try
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    string fullPath = Path.Combine(path.Trim(), "scrcpy.exe");
                    if (File.Exists(fullPath)) return fullPath;
                }
                catch { }
            }

            return null;
        }

        public static string BuildArguments(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(settings.DeviceIp))
            {
                sb.AppendFormat("-s {0}:{1} ", settings.DeviceIp.Trim(), settings.Port > 0 ? settings.Port : 5555);
            }

            if (settings.TurnScreenOff)
            {
                sb.Append("--turn-screen-off ");
            }

            if (settings.StayAwake)
            {
                sb.Append("--stay-awake ");
            }

            if (!settings.ForwardAudio)
            {
                sb.Append("--no-audio ");
            }

            if (settings.BitRate > 0 && settings.BitRate != 8)
            {
                sb.AppendFormat("-b {0}M ", settings.BitRate);
            }

            return sb.ToString().Trim();
        }

        public bool Start(string scrcpyPath, AppSettings settings, Action onExited = null, Action<string> onOutput = null)
        {
            lock (_lock)
            {
                if (IsRunning)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(scrcpyPath) || !File.Exists(scrcpyPath))
                {
                    if (onOutput != null) onOutput("scrcpy executable not found at: " + scrcpyPath);
                    return false;
                }

                try
                {
                    string args = BuildArguments(settings);
                    var psi = new ProcessStartInfo
                    {
                        FileName = scrcpyPath,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(scrcpyPath)
                    };

                    var proc = new Process
                    {
                        StartInfo = psi,
                        EnableRaisingEvents = true
                    };

                    proc.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null && onOutput != null)
                        {
                            onOutput(e.Data);
                        }
                    };

                    proc.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null && onOutput != null)
                        {
                            onOutput("[scrcpy error] " + e.Data);
                        }
                    };

                    proc.Exited += (s, e) =>
                    {
                        lock (_lock)
                        {
                            _process = null;
                        }
                        if (onExited != null)
                        {
                            onExited();
                        }
                    };

                    if (!proc.Start())
                    {
                        if (onOutput != null) onOutput("Failed to start scrcpy process.");
                        return false;
                    }

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    _process = proc;
                    if (onOutput != null) onOutput("scrcpy started with arguments: " + args);
                    return true;
                }
                catch (Exception ex)
                {
                    if (onOutput != null) onOutput("Exception launching scrcpy: " + ex.Message);
                    return false;
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_process != null)
                {
                    try
                    {
                        if (!_process.HasExited)
                        {
                            _process.Kill();
                        }
                    }
                    catch { }
                    finally
                    {
                        _process = null;
                    }
                }
            }
        }
    }
}
