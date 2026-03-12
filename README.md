# ZeroFN

Kernel-level input macros for Fortnite. Uses the Interception driver to bypass Windows keyboard repeat delay entirely.

## What it does

**Loot** — Hold a key and it repeats at max speed. No more waiting for Windows repeat rate to kick in when looting chests, ammo boxes, etc. Pick the key in the dropdown (default `E`), flip the toggle on.

**Edit** — Press a key and it clicks left mouse for you. Lets you bind edit to a key and have it register as a click. One press = one click, no repeat. Default key is `F`.

Both macros go through the Interception driver at the kernel level, so there's zero added latency compared to your normal inputs.

## Setup

1. Run `ZeroFN.exe`
2. First launch will offer to download the Interception driver files automatically
3. If the driver isn't installed yet, it'll prompt you to install it (needs admin + a reboot)
4. After reboot, run it again and you're good

## Usage

- Pick your keys from the dropdowns
- Toggle macros on/off
- Close or minimize sends it to the system tray — right-click the tray icon to exit for real
- Settings save automatically to `%APPDATA%/ZeroFN/config.json`

## Build from source

Needs .NET 8 SDK.

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output lands in `bin/Release/net8.0-windows/win-x64/publish/`.
