using System;
using System.IO;
using System.Web.Script.Serialization;

namespace WirelessCrcpy
{
    public class SettingsManager
    {
        private readonly string _configPath;
        private readonly JavaScriptSerializer _serializer;

        public SettingsManager(string baseDir = null)
        {
            _serializer = new JavaScriptSerializer();
            if (string.IsNullOrEmpty(baseDir))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                baseDir = Path.Combine(appData, "wirelesscrcpy");
            }

            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            _configPath = Path.Combine(baseDir, "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var settings = _serializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // Fall back to default settings on read error
            }

            var defaultSettings = new AppSettings();
            Save(defaultSettings);
            return defaultSettings;
        }

        public void Save(AppSettings settings)
        {
            try
            {
                string json = _serializer.Serialize(settings);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving settings: " + ex.Message);
            }
        }
    }
}
