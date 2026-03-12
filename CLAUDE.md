# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ZeroFN is a Windows Forms (.NET 8, C#) application that provides kernel-level input macros using the Interception driver. It intercepts keyboard input at the driver level to bypass Windows keyboard repeat delay.

## Build Commands

```bash
# Debug build
dotnet build

# Release single-file publish
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

No test suite exists.

## Architecture

The app has a linear startup flow: `Program.cs` → `SetupManager` (ensures Interception driver is installed) → `MainForm` (UI) → `InputEngine` (macro threads).

**Key files:**

- **InputEngine.cs** — Core macro engine. Runs a main loop thread intercepting all keyboard/mouse input via the Interception driver, plus a dedicated loot-repeat thread. Uses volatile fields for thread-safe config updates.
- **Interception.cs** — P/Invoke bindings for `interception.dll` (kernel-level input API).
- **MainForm.cs** — WinForms GUI with dark theme, combo boxes for key selection, toggle switches, and system tray support.
- **SetupManager.cs** — Downloads and installs the Interception driver on first run. Requires admin privileges and a reboot.
- **AppConfig.cs** — JSON config stored at `%APPDATA%/ZeroFN/config.json`.
- **ScanCodes.cs** — Maps key names to Windows scan codes used by the Interception API.
- **ToggleSwitch.cs** — Custom animated toggle switch control.

## Key Constraints

- Target platform: Windows x64 only (`net8.0-windows`, unsafe code enabled)
- Requires the Interception kernel driver (auto-downloaded from GitHub on first run)
- Driver installation requires admin + system reboot
- The `interception.dll` native library must be present alongside the executable
