# Audio Device Switcher

A Windows system tray application for quickly switching between audio playback devices with hotkey support and favorites management.

## Features

- **System Tray Icon**: Left-click to see all available audio devices
- **Quick Switching**: Left-click any device to switch to it immediately
- **Favorites**: Mark up to 2 devices as favorites (shown in **bold**)
- **Hotkey Toggle**: Press a customizable hotkey (default: F11) to toggle between your 2 favorite devices
- **Persistent Settings**: Favorites and hotkey preferences are saved between sessions
- **Persistent Menu**: Menu stays open while you switch devices and manage favorites
- **Visual Feedback**: The current default device is shown with a checkmark (✓)

## Requirements

- Windows 10 or 11
- .NET 8.0 SDK or runtime

## Building

```bash
cd AudioSwitcher
dotnet build -c Release
```

The executable will be in `bin/Release/net8.0-windows/AudioSwitcher.exe`

## Running

**Important**: Do NOT run using `dotnet run` as the app will close when you close PowerShell/terminal.

Instead, run the executable directly:

```bash
# Option 1: Using start command
start bin/Release/net8.0-windows/AudioSwitcher.exe

# Option 2: Double-click the .exe in File Explorer
```

The application will:
1. Start minimized to the system tray
2. Show an icon in your notification area
3. Continue running even after you close PowerShell or VS Code

## Auto-Start on Windows Boot

To make the app start automatically when Windows boots:

1. Press `Win + R` and type `shell:startup`, then press Enter
2. Create a shortcut to `AudioSwitcher.exe` in this folder
3. The app will now start automatically on login

## Usage

### Opening the Menu
- **Left-click** the tray icon to open the menu
- Menu stays open while you interact with it
- Click outside the menu or press Escape to close it

### Switching Devices
- Left-click any device name to switch to it
- The menu stays open after switching
- Current default device is shown with a checkmark (✓)

### Managing Favorites
- Hover over any device to see the submenu arrow (►)
- Click the arrow or right-click the device
- Select "Add to Favorites" or "Remove from Favorites"
- Favorited devices appear in **bold** text
- Maximum 2 favorites allowed
- The menu stays open after adding/removing favorites

### Using the Hotkey
- Set exactly 2 favorite devices
- Press F11 (or your custom hotkey) to toggle between them
- If you have 0 or 1 favorites, the hotkey does nothing
- Works globally even when the app is in the background

### Changing the Hotkey
- Left-click the tray icon
- Go to "Settings" → "Change Hotkey"
- Select a function key (F1-F12)
- Optionally add Ctrl, Alt, or Shift modifiers
- Click OK to save

## Settings Location

Settings are stored in:
```
%APPDATA%\AudioSwitcher\settings.json
```

This file contains:
- List of favorite device IDs
- Hotkey configuration (key and modifiers)

## How It Works

The application uses Windows Core Audio API (COM interop) to:
- Enumerate all active playback devices
- Get the current default device
- Set a new default device for all roles (console, multimedia, communications)

No third-party audio libraries are required - it uses native Windows APIs only.

## Troubleshooting

**No devices showing up:**
- Make sure you have audio devices enabled in Windows Sound settings
- Try closing and reopening the menu (left-click the tray icon)

**Hotkey not working:**
- Ensure you have exactly 2 devices set as favorites (shown in **bold**)
- Check if another application is using the same hotkey
- Try changing to a different key combination in Settings

**Can't switch to a device:**
- Some virtual audio devices may not support being set as default
- Run the application as administrator if you encounter permission issues

**App closes when I close PowerShell:**
- Don't use `dotnet run` - run the .exe file directly
- Use `start bin/Release/net8.0-windows/AudioSwitcher.exe`
- Or double-click the .exe in File Explorer

**Menu closes unexpectedly:**
- The menu should stay open when switching devices or managing favorites
- It only closes when you click outside the menu or click "Exit"

## Exiting the Application

Left-click the tray icon and select "Exit" to close the application.