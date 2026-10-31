using System;

namespace WirelessCrcpy
{
    public class AppSettings
    {
        public string DeviceIp { get; set; }
        public int Port { get; set; }
        public string ScrcpyPath { get; set; }
        public bool TurnScreenOff { get; set; }
        public bool StayAwake { get; set; }
        public bool ForwardAudio { get; set; }
        public int BitRate { get; set; }
        public bool AutoConnectOnLaunch { get; set; }

        public AppSettings()
        {
            DeviceIp = string.Empty;
            Port = 5555;
            ScrcpyPath = string.Empty;
            TurnScreenOff = true;
            StayAwake = false;
            ForwardAudio = true;
            BitRate = 8;
            AutoConnectOnLaunch = false;
        }
    }
}
