# Auto-Switch Mode Improvements

## Overview

The `--auto-switch` mode enables Phone Link passthrough to activate and deactivate automatically when you start and end calls. This document describes the improvements made to call detection and microphone switching.

## Issues Fixed

### Issue 1: Default Microphone Detection
**Problem:** When checking if Windows has already set the default microphone to CABLE Output, the code was getting the wrong device.

**Root Cause:** `MMDeviceEnumerator.GetDefaultAudioEndpoint()` without parameters was returning random devices.

**Fix:** Explicitly specify both role and data flow:
```csharp
device = _enumerator.GetDefaultAudioEndpoint(DataFlow.In, Role.Communications);
```

This ensures we get the default communication input device (microphone), not any random device.

### Issue 2: Registry Writing Failure
**Problem:** When trying to write to the Windows registry to change the default microphone, the code would fail if the registry key didn't exist.

**Root Cause:** Attempting to open a non-existent registry key without CreateSubKey would throw an exception.

**Fix:** Use `CreateSubKey()` instead of `OpenSubKey()` to auto-create the key if needed:
```csharp
using (var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true))
{
    // Set values
}
```

### Issue 3: Call End Detection Failing
**Problem:** The code couldn't distinguish between when the call ended vs. when it started.

**Root Cause:** No filter to exclude our own passthrough process from the external session count.

**Fix:** Skip sessions from our own process:
```csharp
if (sessionName.Contains("MicPassthrough", StringComparison.OrdinalIgnoreCase))
    continue;
```

### Issue 4: Generic Call Detection
**Problem:** The code would trigger on any audio device usage, not just Phone Link calls.

**Root Cause:** Monitoring for any "external session" was too broad - it would activate on Discord, Teams, or any app using the microphone.

**Fix:** Monitor specifically for PhoneExperienceHost.exe process:
```csharp
var phoneExperienceProcesses = Process.GetProcessesByName("PhoneExperienceHost");
if (phoneExperienceProcesses.Length > 0)
{
    return true; // Phone Link is active
}
```

## How Phone Link Detection Works

### Challenge
Phone Link (PhoneExperienceHost.exe) doesn't directly appear in Windows Core Audio API microphone session enumeration. Instead, it delegates audio to Windows Runtime services (svchost.exe).

### Solution
Instead of trying to identify Phone Link through audio sessions, we monitor the PhoneExperienceHost process directly:

1. **Every 500ms:** Check if PhoneExperienceHost.exe is running
2. **If found:** Phone Link app is open/active
3. **Start passthrough:** Automatically activate microphone routing
4. **Monitor continuously:** Keep checking until Phone Link closes

### Limitations
This approach detects when Phone Link is **open and active**, but cannot determine if a **call is actually happening**. This is a limitation of the Windows API - there's no public interface to Phone Link's internal call state.

**Future improvements** would require:
- COM event subscriptions on Phone Link
- WMI monitoring for process hierarchies
- Windows API hooks into Phone Link internals
- Integration with Windows Runtime APIs used by Phone Link

## Testing

### Manual Test Procedure

1. **Start MicPassthrough in auto-switch mode:**
   ```powershell
   MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --auto-switch --verbose
   ```

2. **Open Phone Link app** - you should see:
   ```
   PhoneExperienceHost process detected (2 instances)
   Device is now in use by external process (likely PhoneLink)
   Microphone switched to CABLE Output
   Passthrough activated
   ```

3. **Close Phone Link** - you should see:
   ```
   Device is no longer in use by external processes
   Passthrough stopped
   Original microphone restored
   ```

4. **Make a Phone Link call** - audio should flow clearly and at full volume

### Expected Behavior

- **Passthrough activates** when: Phone Link app opens or is already open
- **Passthrough deactivates** when: Phone Link app closes completely
- **Other apps ignored**: Discord, Zoom, Teams microphone access does not trigger passthrough
- **Smooth handoff**: Microphone automatically switched to CABLE during call, restored after

## Architecture Improvements

### Before
- Monitoring generic audio sessions
- Trying to identify process names from audio session properties
- Getting empty display names from Phone Link sessions
- Triggering on any external microphone usage

### After
- Direct process monitoring (PhoneExperienceHost.exe)
- Clean, simple detection logic
- Reliable Windows API calls
- Phone Link-specific behavior

## Known Limitations

1. **Cannot detect active calls** - Only detects if Phone Link app is open
   - A call could be happening but passthrough won't activate if Phone Link is closed
   - Passthrough stays active while Phone Link is open, even between calls

2. **Two PhoneExperienceHost instances** - Phone Link runs as 2 separate processes
   - Desktop component (UI)
   - Service component (background services)
   - Both are monitored together

3. **No call state API** - Windows doesn't expose Phone Link's internal call state
   - Would require proprietary integration or API access from Microsoft

## Future Enhancements

Possible improvements for even better detection:

1. **Monitor svchost sessions** combined with PhoneExperienceHost process running
   - When both conditions true, more confident a call is active
   - More complex but more reliable

2. **Listen for Windows events** when Phone Link opens/closes calls
   - Requires COM subscription to Phone Link
   - More resource-intensive but precise

3. **Integrate with Windows Telephony API**
   - Microsoft's official API for call state
   - Proprietary, may require permissions

4. **Configuration option for always-on**
   - `--always-on` flag for users who want continuous passthrough
   - For users who always have Phone Link open

## Configuration Files

- **ProcessAudioMonitor.cs** - Detection logic (monitors PhoneExperienceHost.exe)
- **PassthroughApplication.cs** - Orchestration (activates/deactivates passthrough)
- **WindowsDefaultMicrophoneManager.cs** - Microphone switching (Registry-based)

## References

- Phone Link: https://www.microsoft.com/en-us/windows/phones/phone-link
- Windows Core Audio API: https://learn.microsoft.com/en-us/windows/win32/coreaudio/core-audio-apis
- Process.GetProcessesByName(): https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.getprocessesbyname
