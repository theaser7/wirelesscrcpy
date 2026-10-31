# wirelesscrcpy

A lightweight, single-click Windows system tray utility for wireless Android screen mirroring using [scrcpy](https://github.com/Genymobile/scrcpy) over Wi-Fi (ADB TCP/IP).

---

## Features

- **Single-Click Connect & Disconnect:** Left-click the system tray icon to instantly toggle the wireless mirroring session.
- **Dynamic Status Icons:**
  - Red / Gray: Disconnected.
  - Amber / Yellow: Connecting.
  - Emerald Green: Active wireless mirroring.
- **1-Click USB Wireless Bootstrap:** Plug your phone in via USB once, click *Setup via USB*, and `wirelesscrcpy` will automatically detect your phone's Wi-Fi IP address, enable `adb tcpip 5555`, and configure everything. Unplug the cable and you're wireless!
- **Automatic Process Lifecycle:** Closing the `scrcpy` window automatically resets the tray state to Disconnected and cleanly terminates the ADB wireless session.
- **Official scrcpy Downloader:** Missing `scrcpy.exe`? Click *Download scrcpy (Official)* in Settings to automatically download and unpack the latest release.
- **Helpful Options:**
  - Turn off phone screen while mirroring (`--turn-screen-off`) to save battery.
  - Keep device screen awake (`--stay-awake`).
  - Stream audio to PC (Android 11+).
  - Configurable video bitrate.
- **Zero Heavy Dependencies:** Written in clean C# targeting .NET Framework 4.8 (built-in on Windows 10/11). Generates a self-contained ~40 KB executable.

---

## Quick Start

1. Run `wirelesscrcpy.exe`. The app appears in your Windows system tray (near the clock).
2. Right-click the tray icon and select **Settings...**
3. Configure your phone's Wi-Fi IP address:
   - **Easy method:** Plug your phone in with USB (with USB Debugging turned on), click **Setup via USB in 1-Click**, then unplug the cable when finished.
   - **Manual method:** Enter your phone's local Wi-Fi IP (e.g. `192.168.1.100`) and port `5555`.
4. If you don't have `scrcpy` installed yet, click **Download scrcpy (Official)**.
5. Click **Save**.
6. **Left-click the tray icon anytime** to start or stop mirroring with one click!

---

## Building from Source

To build without any external tools (using Windows' built-in C# compiler):

```cmd
build.bat
```

To run the unit test suite:

```cmd
build.bat test
```

Compiled binary output:
`bin/Release/wirelesscrcpy.exe`

---

## License

MIT License. Built for seamless wireless screen mirroring.
