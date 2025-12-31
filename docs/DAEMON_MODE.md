# Daemon Mode with System Tray UI

Daemon mode allows Microphone Passthrough to run in the background with a system tray indicator. This is the professional way to use the application without a console window.

## Starting Daemon Mode

```powershell
MicPassthrough.exe --mic "Your Microphone Name" --daemon
```

Or with short flag:
```powershell
MicPassthrough.exe -m "Your Microphone Name" -d
```

## System Tray Icon

When daemon mode is active:

1. **Tray Icon** - Shows the Microphone Passthrough logo
   - Located in the Windows system tray (bottom-right)
   - Displays current passthrough status in tooltip
   - Shows microphone and cable device names

2. **Tooltip** - Hover over the icon to see:
   ```
   Microphone Passthrough
   Status: [Active/Inactive]
   Mic: [Microphone Name]
   Cable: [Cable Output Name]
   ```

## Tray Icon Features

### Double-Click
- **Toggles passthrough on/off**
- If passthrough is active, double-clicking stops it
- If passthrough is inactive, double-clicking starts it
- Visual feedback with status notifications

### Right-Click Context Menu

**Start Passthrough**
- Activates audio passthrough
- Updates tray icon status
- Shows notification confirming start
- Disabled when passthrough already active

**Stop Passthrough**
- Deactivates audio passthrough
- Updates tray icon status
- Shows notification confirming stop
- Disabled when passthrough already inactive

**Exit**
- Gracefully shuts down daemon mode
- Stops audio passthrough
- Removes tray icon
- Closes application

## Usage Examples

### Phone Link Setup (Recommended)

```bash
# List available microphones first
MicPassthrough.exe --list-devices

# Start daemon mode with your USB microphone
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --daemon

# Now in Phone Link calls:
# - Microphone audio routes through passthrough at full volume
# - Phone Link hears you clearly without gain issues
# - Close daemon from tray menu when done
```

### With Custom Cable Name

If your VB-Audio cable has a different name:

```powershell
MicPassthrough.exe --mic "MyMicrophone" --cable-render "My Custom Cable Name" --daemon
```

### With Auto-Switch (Experimental)

Automatically activate/deactivate based on call detection:

```powershell
MicPassthrough.exe --mic "MyMicrophone" --daemon --auto-switch
```

**Note:** Auto-switch in daemon mode is not fully integrated yet. Use continuous mode for now.

### With Monitoring

Hear yourself in speakers while passthrough is active:

```powershell
MicPassthrough.exe --mic "MyMicrophone" --daemon --enable-monitor --monitor "Speakers (Realtek)"
```

### With Custom Buffer

For lower latency or better stability:

```powershell
# Lower latency (higher CPU usage)
MicPassthrough.exe --mic "MyMicrophone" --daemon --buffer 50

# More stable (higher latency)
MicPassthrough.exe --mic "MyMicrophone" --daemon --buffer 150
```

## Architecture

### How It Works

```
┌─────────────────────────────────────────┐
│   Windows System Tray                   │
│                                         │
│  [Microphone Passthrough Icon] ◄───────┼──┐
│   Status: Active/Inactive              │  │
│                                         │  │
│   Right-Click Menu:                    │  │
│   • Start Passthrough                  │  │
│   • Stop Passthrough                   │  │
│   • Exit                                │  │
└─────────────────────────────────────────┘  │
                                             │
                                    WinForms │
                                    Application
                                             │
┌─────────────────────────────────────────┐  │
│   Program.cs (Daemon Mode)              │  │
│                                         │  │
│   Exit Flag <──────────────────────────┼──┘
│   (tracks shutdown requests)            │
│                                         │
│   System Tray Events:                  │
│   • StartRequested                     │
│   • StopRequested                      │
│   • ExitRequested                      │
└─────────────────────────────────────────┘
            │
            │ Controls
            ▼
┌─────────────────────────────────────────┐
│   Background Audio Thread               │
│                                         │
│   PassthroughEngine:                   │
│   • Initialize (capture/render devices)│
│   • Start (begin audio routing)         │
│   • Poll exit flag (check for stop)    │
│   • Stop (end audio routing)            │
│   • Dispose (cleanup)                  │
└─────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────┐
│   Windows WASAPI Audio                  │
│                                         │
│   Microphone ──┐                       │
│                ├──> VB-Audio Cable    │
│   Monitor ─────┘   (output)            │
└─────────────────────────────────────────┘
```

### Key Components

1. **System Tray UI** (SystemTrayUI.cs)
   - Displays tray icon with application logo
   - Manages context menu
   - Shows status notifications
   - Fires events on user actions

2. **Daemon Mode Handler** (Program.cs, RunDaemonMode method)
   - Initializes tray UI
   - Wires tray events to control logic
   - Runs passthrough engine on background thread
   - Uses exit flag for graceful shutdown
   - Keeps WinForms application loop alive

3. **Passthrough Engine** (PassthroughEngine.cs)
   - Captures from microphone
   - Routes to VB-Audio cable
   - Handles audio buffering and WASAPI
   - Polls exit flag to stop when requested

## Icon Loading

The tray icon loads the application logo (icon.ico) from:

1. **First try:** Same directory as executable
   - `C:\path\to\MicPassthrough.exe` → looks for `icon.ico`
   - Useful when distributing published executable

2. **Second try:** Development relative path
   - `../../docs/assets/icon.ico` from executable directory
   - Useful when running from `bin/Debug/` during development

3. **Fallback:** System application icon
   - If icon.ico not found, uses Windows default application icon
   - App still runs normally, just with generic icon

## Notifications

The daemon shows notifications for key events:

- **Daemon Started:** Shows microphone and cable names
- **Passthrough Started:** Confirms passthrough is active
- **Passthrough Stopped:** Confirms passthrough is stopped
- **Errors:** Shows error message if passthrough fails

Notifications appear as balloon tips in the system tray for 5 seconds.

## Status Indication

The tray icon displays current status in the tooltip:

```
Microphone Passthrough
Status: Active              ◄─── Shows real-time status
Mic: Microphone (HD Pro Webcam C920)
Cable: CABLE Input (VB-Audio Virtual Cable)
```

Status updates when you:
- Click "Start" or "Stop" in context menu
- Double-click tray icon
- Errors occur during operation

## Log Output

Even in daemon mode, logs appear in the console. Use `--verbose` to see detailed debug output:

```powershell
MicPassthrough.exe --mic "MyMic" --daemon --verbose
```

Logs show:
- Initialization progress
- Device discovery
- Audio engine status
- User actions from tray menu
- Any errors that occur

## Troubleshooting

### Icon Not Showing
- Ensure icon.ico exists in `docs/assets/` or same directory as .exe
- Check logs for "Could not find application icon"
- Fallback system icon will still work

### Tray Menu Actions Not Working
- Ensure microphone device name is correct (use `--list-devices` to verify)
- Check logs for error messages
- Try restarting daemon

### Passthrough Stops Unexpectedly
- Check for error notifications
- Review logs with `--verbose` flag
- Ensure audio devices are not unplugged
- Verify VB-Audio cable is still installed and working

### Can't Exit Daemon
- Click "Exit" in tray context menu
- If menu not responding, use Task Manager to terminate
- Or close console window (if visible)

## Advanced Options

### Command-Line Reference

While in daemon mode, you can still use all CLI options:

```powershell
MicPassthrough.exe \
  --mic "Microphone Name" \
  --cable-render "CABLE Input" \
  --cable-capture "CABLE Output" \
  --buffer 100 \
  --exclusive-mode true \
  --prebuffer-frames 3 \
  --enable-monitor \
  --monitor "Speakers" \
  --verbose \
  --daemon
```

All options work the same as in console mode. The only difference is no console window and system tray integration.

## Future Enhancements

Possible improvements for daemon mode:

1. **Dynamic Start/Stop** - Currently all-or-nothing; could pause/resume passthrough from tray menu
2. **Auto-Switch Integration** - Better integration with `--auto-switch` flag
3. **Minimize to Tray** - Start with mini window that hides to tray
4. **Settings Dialog** - GUI to change buffer, exclusive mode, etc.
5. **Startup Integration** - Add "Run at Windows Startup" option
6. **Status Window** - Show detailed stats when clicking tray icon
7. **Call Detection Visual** - Icon changes when Phone Link detects a call

## See Also

- [README.md](../README.md) - General usage and setup
- [ADR-0002: Daemon Mode with System Tray](adr/0002-daemon-mode-with-system-tray.md) - Architecture decisions
- [CHANGELOG.md](../CHANGELOG.md) - Version history and features
