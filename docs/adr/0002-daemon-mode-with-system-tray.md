# Daemon Mode with System Tray UI

* Status: accepted
* Date: 2025-12-31

Technical Story: [Issue #X] Implement daemon mode with system tray indicator for professional UX

## Context and Problem Statement

The current CLI-only interface requires users to:
1. Open a terminal/PowerShell every time they want to use the passthrough
2. Keep the terminal window open while using the application
3. Manually start/stop passthrough from the command line

This is not user-friendly for a utility that's meant to run continuously in the background, especially when Phone Link is already using the app's --auto-switch feature.

## Decision Drivers

* **Professionalism**: Users expect background utilities to be invisible and accessible via system tray
* **Accessibility**: System tray makes the application discoverable and controllable without opening terminals
* **Non-Breaking Change**: Existing CLI should continue to work as-is for scripting and automation
* **Simplicity**: Minimal refactoring of existing passthrough logic
* **Windows Integration**: Leverage Windows Forms NotifyIcon for native look and feel

## Considered Options

* Option 1: Batch file shortcuts + AutoHotkey hotkeys (short-term quick fix)
* Option 2: Keep CLI-only, document batch file wrapper (no UI improvement)
* Option 3: Windows service with separate GUI tool (complex, overkill for v1)
* Option 4: Daemon mode with system tray UI (chosen)

## Decision Outcome

Chosen option: **"Daemon mode with system tray UI"**, because:
- Provides professional user experience without breaking existing CLI
- Uses only .NET standard libraries (System.Windows.Forms built-in)
- Preserves all existing passthrough and auto-switch logic
- Allows phased implementation: daemon mode now, enhanced UI later
- Enables future features (hotkeys, scheduling, auto-start)

### Positive Consequences

* Users can run `MicPassthrough.exe --mic "..." --daemon` and app disappears to tray
* System tray icon shows status (active/inactive) at a glance
* Right-click menu provides quick access to start/stop/exit
* Tooltip displays current device configuration
* Professional appearance matching modern Windows utilities
* All existing CLI features work identically
* Can easily add enhanced UI features later without breaking daemon

### Negative Consequences

* Adds System.Windows.Forms dependency (needed for tray icon)
* Slightly more complex Program.cs with daemon vs. CLI branches
* WinForms-based UI is basic compared to WPF (acceptable for now)
* Daemon mode not useful on non-Windows platforms (acceptable: Windows-only app anyway)

## Architecture Details

### New Components

**SystemTrayUI.cs** (new):
- Wraps NotifyIcon with context menu
- Provides events for Start/Stop/Exit actions
- Manages tray icon state and tooltip
- Shows balloon notifications for status changes

**Options.cs** (modified):
- Added `Daemon` boolean property
- Maintains backward compatibility (false by default)

**Program.cs** (modified):
- Added `--daemon` / `-d` CLI option
- New `RunDaemonMode()` method to initialize tray UI
- Conditional logic: if `--daemon`, run tray UI; else run CLI

### Message Flow

```
CLI with --daemon flag
    ↓
Program.cs builds RootCommand with daemonOption
    ↓
User runs: MicPassthrough.exe --mic "..." --daemon
    ↓
RunApplication() receives Options with Daemon=true
    ↓
RunDaemonMode() initializes SystemTrayUI
    ↓
Tray icon appears in system tray
    ↓
PassthroughApplication.Run() starts in background thread
    ↓
WinForms.Application.Run() keeps tray alive
    ↓
User right-clicks tray icon for menu
```

### Example Usage

**Start daemon mode with auto-switch:**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --cable "CABLE Input (VB-Audio Virtual Cable)" --auto-switch --daemon
```

**CLI mode (unchanged):**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --verbose
```

## Pros and Cons of the Options

### Option 1: Batch file shortcuts + AutoHotkey
- Good: Quick implementation, minimal code changes
- Good: Users familiar with batch files
- Bad: Requires users to install AutoHotkey separately
- Bad: No system tray integration
- Bad: Still requires terminal window visible

### Option 2: Keep CLI-only with batch wrapper
- Good: Minimal changes to codebase
- Good: Scriptable, automatable
- Bad: No professional UX
- Bad: Doesn't address core usability issue
- Bad: Users frustrated with terminal requirement

### Option 3: Windows Service + Separate GUI
- Good: True background service (auto-restart on crash)
- Good: Professional UX with full-featured GUI
- Bad: Complex implementation (service registration, service control, GUI communication)
- Bad: Significant refactoring of audio engine
- Bad: Harder for users to debug/troubleshoot
- Bad: Overkill for current use case

### Option 4: Daemon mode with system tray UI
- Good: Professional UX without Windows Service complexity
- Good: Uses only built-in .NET libraries
- Good: Non-breaking change to existing CLI
- Good: Simple, maintainable implementation
- Good: Positions well for future enhancements
- Bad: Requires System.Windows.Forms dependency
- Bad: Basic UI compared to WPF
- Bad: Not a true system service (app stays in user session)

## Implementation Plan

### Phase 1: Daemon Mode (Current - v0.2.0)
- ✅ Add SystemTrayUI component with basic context menu
- ✅ Add `--daemon` option to CLI
- ✅ Run PassthroughApplication in background thread when daemon mode active
- ✅ Show tray icon with status
- ✅ Right-click menu: Start/Stop/Exit
- ✅ Tooltip with device names

### Phase 2: Enhanced Daemon (Future - v0.3.0)
- [ ] Save last-used device configuration to file
- [ ] Auto-restore settings on daemon startup
- [ ] Add "Run at startup" checkbox in tray menu
- [ ] Persist preferences (window position, audio devices, etc.)
- [ ] Double-click tray to show status window

### Phase 3: Windows Service (Future - v1.0.0)
- [ ] Convert daemon to Windows Service
- [ ] Auto-restart on crash
- [ ] System event logging
- [ ] Elevated privileges for microphone switching
- [ ] Scheduled tasks integration

## Dependencies Added

- **System.Windows.Forms** v4.7.0 - For NotifyIcon and context menu (no external dependency, built-in to .NET)

## Testing Considerations

1. **CLI Mode**: Verify existing functionality unchanged
   - `MicPassthrough.exe --mic "..." --verbose` should work as before
   - Device listing, auto-switch, etc. unaffected

2. **Daemon Mode**: Verify new functionality
   - `MicPassthrough.exe --mic "..." --daemon` starts tray icon
   - Tray icon visible in system tray
   - Right-click menu functional
   - Double-click shows status balloon
   - Exit button closes application properly

3. **Integration**: Verify both modes can run
   - Start daemon in one window
   - Start CLI in another (should work independently)
   - Both should control passthrough properly

## Future Enhancements

1. **Enhanced Icon**: Replace default circle with actual app icon
2. **Start on Boot**: Windows Task Scheduler or registry autostart
3. **Configuration GUI**: Settings window accessible from tray
4. **Status Window**: Show detailed stats when double-clicking tray
5. **Hotkey Support**: Global hotkey to toggle passthrough (Win+M or similar)
6. **Service Integration**: Convert to Windows Service for true background operation
7. **Tray Menu Shortcuts**: Quick-switch between saved device configurations

## References

- [System.Windows.Forms NotifyIcon](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon)
- [Windows Forms in .NET](https://learn.microsoft.com/en-us/dotnet/desktop/winforms)
- [System Tray Applications on Windows](https://learn.microsoft.com/en-us/windows/win32/shell/notification-area)

## Related ADRs

- [ADR-0001: Migrate to System.CommandLine](0001-migrate-to-system-commandline.md) - System.CommandLine for CLI parsing

---

<!-- This ADR documents the decision to implement daemon mode with system tray UI as the preferred long-term interface for MicPassthrough, while maintaining backward compatibility with the existing CLI. -->
