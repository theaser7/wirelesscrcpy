# wirelesscrcpy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `wirelesscrcpy`, a lightweight, single-click Windows tray utility in C# that connects and disconnects `scrcpy` over Wi-Fi (ADB TCP/IP) with automatic dependency setup, USB bootstrap, and real-time status indication.

**Architecture:** A standalone Windows Forms tray application (`System.Windows.Forms.NotifyIcon`) consisting of an `AdbManager` for ADB TCP/IP operations, a `ScrcpyManager` for launching and monitoring the mirroring process lifecycle, a `SettingsManager` for JSON configuration, and an `IconHelper` for generating dynamic state-based tray icons.

**Tech Stack:** C# 5 / .NET Framework 4.8 (using Windows built-in `csc.exe` / `MSBuild.exe`), WinForms, `System.Drawing`, `System.Web.Extensions` (JavaScriptSerializer).

**Spec:** `docs/superpowers/specs/2026-10-31-wirelesscrcpy-design.md`

## Global Constraints
- Target platform: Windows 10/11 x64.
- Zero runtime install required: builds a standalone portable `.exe` without requiring external SDKs or NuGet packages.
- Friendly, clear user experience: single-click toggle in system tray, visual color-coded status, balloon notifications on errors.
- Clean process lifecycle: closing the scrcpy window immediately cleans up and resets status to Disconnected.
- Repository: `https://github.com/theaser7/wirelesscrcpy`.

## Review Focus
1. **Invalid or Unreachable Phone IP:** Connection times out or fails; must show balloon notification and reset tray icon to Disconnected without crashing or hanging the UI thread.
2. **Missing `scrcpy` / `adb` Executable:** If not found in path or local folder, prompt user clearly with 1-click download option rather than throwing unhandled `Win32Exception`.
3. **Closing scrcpy window externally:** When the user closes the video window (Alt+F4 or X), `wirelesscrcpy` must detect exit immediately and reset state without dangling ADB TCP connections.
4. **USB Setup with multiple or no devices connected:** If user clicks "Setup via USB" without a phone plugged in or with multiple devices, provide a clear error message.
5. **Double click vs Single click on Tray Icon:** Prevent race conditions where rapid clicks trigger simultaneous duplicate `scrcpy` launch attempts.

---

### Task 1: Project Setup, Build Script & Configuration Manager

**Files:**
- Create: `wirelesscrcpy.csproj`
- Create: `src/AppSettings.cs`
- Create: `src/SettingsManager.cs`
- Create: `tests/TestSettings.cs`
- Create: `build.bat`

**Interfaces:**
- Produces: `AppSettings` class with properties: `DeviceIp` (string), `Port` (int, default 5555), `ScrcpyPath` (string), `TurnScreenOff` (bool), `StayAwake` (bool), `ForwardAudio` (bool), `BitRate` (int), `AutoConnectOnLaunch` (bool).
- Produces: `SettingsManager.Load()` -> `AppSettings` and `SettingsManager.Save(AppSettings settings)` -> `void`.

- [ ] **Step 1: Write test for SettingsManager serialization and default values**
  Create `tests/TestSettings.cs` verifying defaults and JSON save/load roundtrip.

- [ ] **Step 2: Run test and verify failure**
  Run: `build.bat test` (fails because classes do not exist).

- [ ] **Step 3: Implement `AppSettings.cs`, `SettingsManager.cs`, and `wirelesscrcpy.csproj`**
  Implement model and JSON serialization using `System.Web.Script.Serialization.JavaScriptSerializer`.

- [ ] **Step 4: Run test and verify pass**
  Run: `build.bat test` -> PASS with output `Settings tests passed`.

- [ ] **Step 5: Commit**
  ```bash
  git add wirelesscrcpy.csproj src/AppSettings.cs src/SettingsManager.cs tests/TestSettings.cs build.bat
  git commit -m "feat: add project file, build script and settings manager"
  ```

---

### Task 2: ADB Manager (Connect, Disconnect & USB Bootstrap)

**Files:**
- Create: `src/AdbManager.cs`
- Create: `tests/TestAdbManager.cs`

**Interfaces:**
- Consumes: `AppSettings` from Task 1.
- Produces:
  - `AdbManager.FindAdbPath(string customDir)` -> `string`
  - `AdbManager.Connect(string adbPath, string ip, int port, out string error)` -> `bool`
  - `AdbManager.Disconnect(string adbPath, string ip, int port)` -> `bool`
  - `AdbManager.EnableTcpIp(string adbPath, int port, out string error)` -> `bool`
  - `AdbManager.DetectDeviceWifiIp(string adbPath, out string detectedIp, out string error)` -> `bool`

- [ ] **Step 1: Write test for AdbManager parsing and command formatting**
  Create `tests/TestAdbManager.cs` testing IP regex extraction from `adb shell ip route` / `ip addr` outputs and connect response parsing.

- [ ] **Step 2: Run test and verify failure**
  Run: `build.bat test` (fails because `AdbManager` does not exist).

- [ ] **Step 3: Implement `AdbManager.cs`**
  Implement process execution (`adb.exe`), stdout/stderr capturing, timeout handling, and IP detection logic.

- [ ] **Step 4: Run test and verify pass**
  Run: `build.bat test` -> PASS.

- [ ] **Step 5: Commit**
  ```bash
  git add src/AdbManager.cs tests/TestAdbManager.cs
  git commit -m "feat: implement AdbManager with Wi-Fi connect and USB bootstrap"
  ```

---

### Task 3: Scrcpy Process Manager & Argument Builder

**Files:**
- Create: `src/ScrcpyManager.cs`
- Create: `tests/TestScrcpyManager.cs`

**Interfaces:**
- Consumes: `AppSettings` from Task 1.
- Produces:
  - `ScrcpyManager.BuildArguments(AppSettings settings)` -> `string`
  - `ScrcpyManager.FindScrcpyPath(string customDir)` -> `string`
  - `ScrcpyManager.Start(string scrcpyPath, AppSettings settings, Action onExited, Action<string> onOutput)` -> `bool`
  - `ScrcpyManager.Stop()` -> `void`
  - `ScrcpyManager.IsRunning` -> `bool`

- [ ] **Step 1: Write test for ScrcpyManager argument generator**
  Create `tests/TestScrcpyManager.cs` testing arguments generation: `-s 192.168.1.50:5555 --turn-screen-off --stay-awake -b 8M`.

- [ ] **Step 2: Run test and verify failure**
  Run: `build.bat test` -> FAIL.

- [ ] **Step 3: Implement `ScrcpyManager.cs`**
  Implement argument formatting, `Process` start with `EnableRaisingEvents = true`, `Process.Exited` handling, and process termination.

- [ ] **Step 4: Run test and verify pass**
  Run: `build.bat test` -> PASS.

- [ ] **Step 5: Commit**
  ```bash
  git add src/ScrcpyManager.cs tests/TestScrcpyManager.cs
  git commit -m "feat: implement ScrcpyManager process runner and argument builder"
  ```

---

### Task 4: Dynamic Status Icons & Dependency Downloader

**Files:**
- Create: `src/IconHelper.cs`
- Create: `src/Downloader.cs`

**Interfaces:**
- Produces:
  - `IconHelper.CreateStatusIcon(ConnectionState state)` -> `System.Drawing.Icon` (Red/Gray for Disconnected, Yellow for Connecting, Green for Connected).
  - `Downloader.DownloadScrcpyAsync(string targetDir, Action<int> progressCallback, Action<bool, string> onComplete)` -> `void` (downloads GitHub release zip and unpacks using `System.IO.Compression.FileSystem`).

- [ ] **Step 1: Implement `IconHelper.cs` with dynamic icon rendering**
  Draws 32x32 bitmap with high-DPI scaling, smooth anti-aliased phone frame, status dot, and converts to `System.Drawing.Icon`.

- [ ] **Step 2: Implement `Downloader.cs`**
  Implements downloading official scrcpy zip package with progress reporting and extraction to `scrcpy/` folder.

- [ ] **Step 3: Test icon generation and verify valid icon handle**
  Run test verifying icons generate valid GDI handles for all states (`Disconnected`, `Connecting`, `Connected`).

- [ ] **Step 4: Commit**
  ```bash
  git add src/IconHelper.cs src/Downloader.cs
  git commit -m "feat: add dynamic status icon generator and scrcpy downloader"
  ```

---

### Task 5: UI Dialogs (SettingsForm & Diagnostic LogForm)

**Files:**
- Create: `src/Forms/SettingsForm.cs`
- Create: `src/Forms/LogForm.cs`

**Interfaces:**
- Consumes: `AppSettings`, `AdbManager`, `ScrcpyManager`, `Downloader`.
- Produces:
  - `SettingsForm`: Clean dialog allowing user to edit Phone IP/Port, toggle flags, click "Configure via USB in 1-Click", or click "Download scrcpy".
  - `LogForm`: Real-time text area showing ADB and scrcpy stdout/stderr output for easy troubleshooting.

- [ ] **Step 1: Create `SettingsForm.cs`**
  Build clean modern Windows Forms layout with fields for IP, Port, options checkboxes, USB setup button, and validation.

- [ ] **Step 2: Create `LogForm.cs`**
  Build diagnostic log window with auto-scroll and "Clear" / "Copy" buttons.

- [ ] **Step 3: Verify dialogs compile and run**
  Run: `build.bat` -> Output `wirelesscrcpy.exe`.

- [ ] **Step 4: Commit**
  ```bash
  git add src/Forms/SettingsForm.cs src/Forms/LogForm.cs
  git commit -m "feat: create SettingsForm and diagnostic LogForm"
  ```

---

### Task 6: Tray Application Context & One-Click Toggle Integration

**Files:**
- Create: `src/TrayAppContext.cs`
- Create: `src/Program.cs`

**Interfaces:**
- Consumes: All components from Tasks 1-5.
- Produces: Standalone `wirelesscrcpy.exe` tray application.

- [ ] **Step 1: Implement `TrayAppContext.cs`**
  - Initializes `NotifyIcon`.
  - Configures Left-Click handler: One-click toggle between Connect / Disconnect.
  - Configures ContextMenu: Status, Connect/Disconnect, Configure via USB, Settings, Logs, Exit.
  - Handles state changes and updates icon/tooltip dynamically.
  - Shows Windows balloon notifications on connection errors.

- [ ] **Step 2: Implement `Program.cs`**
  - Configures single-instance mutex (`wirelesscrcpy_Mutex`) to prevent duplicate instances.
  - Sets visual styles and runs `Application.Run(new TrayAppContext())`.

- [ ] **Step 3: Compile full application and test**
  Run: `build.bat` -> produces `bin/Release/wirelesscrcpy.exe`.
  Execute smoke test verifying startup, tray icon registration, and clean shutdown.

- [ ] **Step 4: Commit**
  ```bash
  git add src/TrayAppContext.cs src/Program.cs
  git commit -m "feat: implement TrayAppContext and single-instance Program entry point"
  ```

---

### Task 7: End-to-End Verification, Release Build (1.0.0) & Git Push

**Files:**
- Create: `README.md`
- Compile: `wirelesscrcpy.exe` (Standalone release)

- [ ] **Step 1: Write `README.md`**
  Provide clear instructions on how to use `wirelesscrcpy`, 1-click connect/disconnect, USB initial setup, and shortcuts.

- [ ] **Step 2: Full build and compile final executable**
  Compile release build to root directory or `bin/wirelesscrcpy.exe`.

- [ ] **Step 3: Git Tag & Push**
  Tag version `1.0.0`, commit, and push to remote `https://github.com/theaser7/wirelesscrcpy`.
