# Design Document: wirelesscrcpy

## Intent and Context
`wirelesscrcpy` is a lightweight, single-click Windows tray utility in C# for connecting and disconnecting Android devices via `scrcpy` over Wi-Fi (ADB TCP/IP).

## Goals
- **One-click toggle:** Left-clicking the tray icon connects if disconnected, or disconnects if connected.
- **Clear status feedback ("чтобы было понятно"):**
  - Dynamic tray icon reflects state: Disconnected (Gray/Red), Connecting (Yellow), Connected (Green).
  - Tray tooltip shows current status and target IP/port.
  - Windows balloon/toast notifications on state changes or errors (e.g., connection lost, device not reachable).
  - Settings dialog opens automatically on first run or when IP is missing.
- **Settings & Diagnostics:**
  - Configuration of Phone IP and Port (default 5555).
  - Toggles for common scrcpy flags: `--turn-screen-off`, `--stay-awake`, audio forwarding, max resolution/bitrate.
  - Auto-detection / browse for `scrcpy` and `adb` executables, with option to download official scrcpy release if missing.
  - Quick ADB status / device check in settings or context menu.
- **Portability and Ease of Execution:**
  - Standalone compiled `.exe` with zero installation required, targeting .NET Framework 4.8 / Windows Desktop.

## Non-Goals & Anti-Goals
- **Non-Goal:** Full re-implementation of scrcpy video/audio decoding or H.264 rendering in C# (scrcpy handles that).
- **Anti-Goal:** Bulky, heavy UI windows that clutter the desktop during normal use.
- **Anti-Goal:** Silent failures where a user clicks and nothing happens with no explanation.

## Approaches Considered
1. **Desktop Mini-Dashboard + Tray:**
   - Small main window with dashboard, minimized to tray.
   - *Rejected:* User explicitly requested Option 2 (tray-first).
2. **Tray-First Utility with Clear Diagnostics (Chosen):**
   - Lives in system tray. Left-click for instant toggle. Right-click context menu for quick actions, Settings dialog, and Log/Troubleshooting window.
   - Balances minimal desktop footprint with clear user feedback.

## System Architecture

### Dependency Management
- Application checks for `scrcpy.exe` and `adb.exe`:
  1. Next to the application in a `scrcpy/` subfolder.
  2. In system `PATH`.
  3. Custom path defined in `settings.json`.
- If missing, the Settings dialog provides a 1-click **"Download scrcpy"** button that downloads and extracts the official GitHub release zip automatically into `scrcpy/`.

### Lifecycle and Reconnection
- **Process Monitoring:** When `scrcpy` process exits (e.g. user closes mirror window or connection lost), `wirelesscrcpy` detects it immediately via `Process.Exited`, transitions state back to `Disconnected`, updates icon to Gray/Red, and sends a notification if it exited unexpectedly.
- **Auto-Cleanup:** Option to keep ADB connected or cleanly `adb disconnect` on scrcpy exit (configurable, default: clean disconnect).

### IP and Device Setup
- Stores target Phone IP and Port in `settings.json`.
- Includes a helper button **"Fetch IP via USB"** (`adb shell ip -f inet addr show wlan0` and `adb tcpip 5555`) so the user can connect once via USB, click one button to configure wireless, and never need the cable again!

### Components
1. **TrayController (`TrayApplicationContext`):**
   - Manages `NotifyIcon`, dynamic icon generation/loading, tooltip updates, and context menu.
   - Handles mouse clicks (Single left click = Connect/Disconnect; Double-click or Right-click -> Settings/Menu).
2. **AdbManager:**
   - Manages ADB lifecycle: checking ADB status, executing `adb connect <ip>:<port>`, `adb disconnect <ip>:<port>`, `adb devices`.
3. **ScrcpyRunner:**
   - Launches `scrcpy.exe` with configured arguments without showing an unwanted console window (`CreateNoWindow = true`, `UseShellExecute = false`).
   - Tracks the active `scrcpy` process and gracefully terminates it upon disconnect.
4. **SettingsManager:**
   - Stores settings in `settings.json` (Device IP, Port, ScrcpyPath, TurnScreenOff, StayAwake, AutoConnectOnLaunch, RunOnStartup).
5. **SettingsForm / LogForm:**
   - Clean, modern Windows Forms dialogs for configuring settings and viewing diagnostic logs if ADB fails.

## What is Left to the Builder
- Detailed UI layout for SettingsForm and LogForm.
- Icon rendering logic (generating crisp vector/bitmap tray icons programmatically or using embedded resources).
- Exact arguments builder for scrcpy options.
