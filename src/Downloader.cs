using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;

namespace WirelessCrcpy
{
    public static class Downloader
    {
        public const string DefaultScrcpyUrl = "https://github.com/Genymobile/scrcpy/releases/download/v3.1/scrcpy-win64-v3.1.zip";

        public static void DownloadScrcpyAsync(string targetDir, Action<int> onProgress, Action<bool, string> onComplete)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    string tempZip = Path.Combine(Path.GetTempPath(), "scrcpy_win64_" + Guid.NewGuid().ToString("N") + ".zip");

                    using (var client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "wirelesscrcpy-installer");

                        client.DownloadProgressChanged += (s, e) =>
                        {
                            if (onProgress != null)
                            {
                                onProgress(e.ProgressPercentage);
                            }
                        };

                        var waitHandle = new ManualResetEvent(false);
                        Exception downloadException = null;

                        client.DownloadFileCompleted += (s, e) =>
                        {
                            if (e.Error != null)
                            {
                                downloadException = e.Error;
                            }
                            waitHandle.Set();
                        };

                        client.DownloadFileAsync(new Uri(DefaultScrcpyUrl), tempZip);
                        waitHandle.WaitOne();

                        if (downloadException != null)
                        {
                            throw downloadException;
                        }
                    }

                    // Extract zip
                    string tempExtract = Path.Combine(Path.GetTempPath(), "scrcpy_extracted_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempExtract);
                    ZipFile.ExtractToDirectory(tempZip, tempExtract);

                    // Move files to targetDir (handling nested root directory inside zip if present)
                    string[] subdirs = Directory.GetDirectories(tempExtract);
                    string sourceFolder = subdirs.Length == 1 ? subdirs[0] : tempExtract;

                    foreach (string file in Directory.GetFiles(sourceFolder))
                    {
                        string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }

                    // Clean temp files
                    try { File.Delete(tempZip); } catch { }
                    try { Directory.Delete(tempExtract, true); } catch { }

                    if (onComplete != null)
                    {
                        onComplete(true, "Successfully installed scrcpy to " + targetDir);
                    }
                }
                catch (Exception ex)
                {
                    if (onComplete != null)
                    {
                        onComplete(false, "Download failed: " + ex.Message);
                    }
                }
            });
        }
    }
}
