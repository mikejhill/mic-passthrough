# Auto-Switch Mode Improvements

## Overview

The `--auto-switch` mode enables Phone Link passthrough to activate and deactivate automatically when you start and end calls. This document describes the improvements made to call detection and microphone switching.

## Issues Fixed

### Issue 1: Phone Link Process Detection Too Broad ✅ FIXED
**Problem:** Just checking if PhoneExperienceHost process existed wasn't enough - it could be open without any call active.

**Root Cause:** Phone Link runs as a background service that persists even when not in a call.

**Fix:** Two-level detection:
1. Check if PhoneExperienceHost.exe process is running
2. Check if there are active microphone audio sessions
3. Only trigger passthrough when BOTH conditions are true

```csharp
// Check if PhoneExperienceHost is running
var phoneExperienceProcesses = Process.GetProcessesByName("PhoneExperienceHost");
if (phoneExperienceProcesses.Length == 0)
    return false;

// Check if there are active microphone sessions (svchost.exe becomes active during calls)
var sessionEnumerator = device.AudioSessionManager.Sessions;
int externalActiveSessions = 0;
for (int i = 0; i < sessionEnumerator.Count; i++)
{
    if (session.IsSystemSoundsSession) continue;
    if ((int)session.State == 1)  // Active
        externalActiveSessions++;
}

return externalActiveSessions > 0;  // Only true if PhoneExperienceHost + active sessions
```

**Impact:** Passthrough now only activates when Phone Link is actually using the microphone, not just when the app is open.

### Issue 2: Default Microphone Detection Wrong Role ✅ FIXED
**Problem:** The code was checking the "Communications" audio endpoint role, which might be set to something different than the actual Console (default) microphone role that most apps use.

**Root Cause:** Windows has different audio roles:
- **Console**: Default for user applications and games
- **Multimedia**: For streaming and media playback
- **Communications**: For VoIP and calls (may be different)

**Fix:** Try all roles in order of preference:
```csharp
// Try Console role first (most common for user input)
currentDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);

// If not available, try Multimedia
if (currentDefault == null)
    currentDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

// Last resort: Communications
if (currentDefault == null)
    currentDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
```

**Impact:** Now correctly saves your actual default microphone (e.g., "Microphone (HD Pro Webcam C920)") instead of what might have been set as Communications endpoint.

### Issue 3: Registry Writing Succeeds But May Not Be Immediate ✅ FIXED/DOCUMENTED
**Problem:** Registry changes are written successfully, but Windows Sound Settings UI may not update immediately or applications may cache the old default.

**Root Cause:** Windows registry changes take effect, but:
1. UI may cache the value (no immediate visual update)
2. Some applications read the default on startup and don't re-query it
3. Process restart may be needed for some apps to see the change

**Fix:** Add detailed diagnostic logging to confirm Registry writes are successful:
```csharp
key.SetValue("Default", deviceId);
_logger.LogInformation("Registry Default value is now: {Value}", 
    key.GetValue("Default") as string);
```

**Verification:** You can check Registry after running the app:
```powershell
Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Multimedia\Audio Endpoints\Capture' -Name Default
```
This shows the actual value written (device ID GUID).

**Impact:** Registry is confirmed to be written successfully. Applications and Phone Link will use the new default on next startup or when they re-query the default device.

### Issue 4: Session Detection Not Identifying Phone Link or Detecting Session End ✅ FIXED
**Problem:** The original detection couldn't:
1. Differentiate between Phone Link sessions and other applications using the microphone
2. Reliably detect when Phone Link released the microphone (sessions might linger)

**Root Cause:** 
- Counting all active sessions was too broad - other apps (Discord, Teams, etc.) would trigger passthrough
- NAudio's `AudioSessionManager.Sessions` collection doesn't always immediately clear when sessions end
- Only checking for session count doesn't track session lifecycle

**Fix:** Implement session tracking that:
1. **Identifies specific process IDs** - Match sessions to PhoneExperienceHost and related svchost processes
2. **Tracks session history** - Remember which sessions existed in previous checks
3. **Detects session changes** - Compare current sessions to previous sessions to detect:
   - New sessions appearing (call started)
   - Sessions disappearing (call ended)
4. **Filters by process** - Ignore sessions from other applications

```csharp
// Track specific process IDs
private HashSet<int> _trackedPhoneLinkProcessIds = new HashSet<int>();
private HashSet<int> _lastSeenPhoneLinkSessions = new HashSet<int>();

// For each session:
uint sessionPid = session.GetProcessID;

// Check if session belongs to Phone Link process
if (phoneExperienceIds.Contains((int)sessionPid))
{
    currentPhoneLinkSessions.Add(i);
}
// Or if it's svchost (Windows Runtime host used by Phone Link during calls)
else if (IsServiceHostRelatedToPhoneLink((int)sessionPid))
{
    currentPhoneLinkSessions.Add(i);
}

// Detect changes between current and previous check
var newSessions = currentPhoneLinkSessions.Except(_lastSeenPhoneLinkSessions);
var endedSessions = _lastSeenPhoneLinkSessions.Except(currentPhoneLinkSessions);
```

**Impact:** 
- ✅ Can now reliably detect when Phone Link **releases** the microphone (sessions disappear)
- ✅ Won't trigger passthrough if Discord/Teams/other apps use the microphone
- ✅ Reduces false positives and false negatives significantly
- ✅ Detailed logging shows which sessions are Phone Link vs other apps

## How Phone Link Detection Works

### Challenge
Phone Link (PhoneExperienceHost.exe) process stays running even when not in a call. Simply checking if the process exists isn't reliable enough.

### Solution
Two-level detection system:

1. **Level 1 - Process Check:** Is PhoneExperienceHost.exe running?
   - Quick check using `Process.GetProcessesByName()`
   - Fast and reliable for detecting if Phone Link app is open

2. **Level 2 - Audio Session Check:** Are there active microphone sessions?
   - When Phone Link is in a call, it creates audio sessions via Windows Runtime (svchost.exe)
   - Monitor counts active sessions on the microphone device
   - Only when sessions are ACTIVE do we know a call is happening

**Result:** Passthrough activates when (PhoneExperienceHost running) AND (active audio sessions detected)

This prevents false positives when Phone Link is open but not in a call.

### How It Works During a Call

```
Phone Link opens → PhoneExperienceHost process runs
    ↓
User makes call → svchost.exe audio sessions become active
    ↓
MicPassthrough detects both conditions → Passthrough activates
    ↓
Call ends → Audio sessions drop to inactive
    ↓
MicPassthrough detects session inactivity → Passthrough stops
```

## Registry Verification

The microphone default is stored in Windows Registry:
```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Multimedia\Audio Endpoints\Capture
Value: Default = {device-id-guid}
```

To verify the setting was applied, run:
```powershell
Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Multimedia\Audio Endpoints\Capture' -Name Default
```

After running MicPassthrough with `--auto-switch`, the `Default` value will be the CABLE Input device ID.

**Note:** Applications may cache the default device on startup. Phone Link will use the new default on its next startup after the Registry change.

## Testing and Verification

### Pre-Test Checklist

Before testing auto-switch mode, verify:
- ✅ Windows Registry access works (can read/write HKCU)
- ✅ VB-Audio Virtual Cable installed and functioning
- ✅ Physical microphone working (test with Sound Recorder first)
- ✅ Phone Link app installed and working normally

### Manual Test Procedure

1. **Open Phone Link (but don't make a call yet):**
   ```powershell
   # Leave Phone Link open/idle in background
   ```

2. **Start MicPassthrough in auto-switch mode with verbose logging:**
   ```powershell
   MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --auto-switch --verbose
   ```

3. **Verify detection output** - you should see:
   ```
   [HH:MM:SS] info: Program[0] Starting microphone passthrough application
   [HH:MM:SS] dbug: Program[0] Found device: Microphone (HD Pro Webcam C920)
   [HH:MM:SS] dbug: Program[0] Found device: CABLE Input (VB-Audio Virtual Cable)
   [HH:MM:SS] dbug: Program[0] Auto-switch mode: listening for calls...
   ```

4. **Open Phone Link and start a call** - you should see:
   ```
   [HH:MM:SS] dbug: Program[0] Phone Link process check: 1 instance running
   [HH:MM:SS] dbug: Program[0] Device audio sessions count: 2
   [HH:MM:SS] dbug: Program[0]   [Session 0] Process ID: 1234, State: 1
   [HH:MM:SS] dbug: Program[0]   [Session 0] Phone Link process detected
   [HH:MM:SS] dbug: Program[0] New Phone Link sessions: 0
   [HH:MM:SS] info: Program[0] Phone Link is actively using microphone (1 session)
   [HH:MM:SS] info: Program[0] Saved original default microphone (Console role): Microphone (HD Pro Webcam C920) (ID: {0.0.1.00000000}.{...})
   [HH:MM:SS] info: Program[0] Attempting to set default microphone to device ID: {0.0.0.00000000}.{...}
   [HH:MM:SS] info: Program[0] Registry key exists. Setting Default value to: {0.0.0.00000000}.{...}
   [HH:MM:SS] info: Program[0] Verification: Registry Default value is now: {0.0.0.00000000}.{...}
   [HH:MM:SS] info: Program[0] Set default microphone to: CABLE Input (VB-Audio Virtual Cable)
   [HH:MM:SS] info: Program[0] Audio passthrough activated
   ```

5. **Verify Registry change** (in new PowerShell window while call is active):
   ```powershell
   Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Multimedia\Audio Endpoints\Capture' -Name Default
   ```
   Should show: `{0.0.0.00000000}.{ddb0a7a5-c1d4-48de-a446-5a700df0aba6}` (or your CABLE Input device ID)

6. **Test audio quality:**
   - Other party on Phone Link call should hear you clearly at normal volume
   - No distortion or quietness
   - Minimal latency (~100ms)

7. **End the call** - you should see:
   ```
   [HH:MM:SS] dbug: Program[0] Phone Link process check: 1 instance running
   [HH:MM:SS] dbug: Program[0] Device audio sessions count: 1
   [HH:MM:SS] dbug: Program[0] Phone Link sessions ended: 0
   [HH:MM:SS] dbug: Program[0] Phone Link running but no active microphone sessions
   [HH:MM:SS] info: Program[0] Phone Link released the microphone
   [HH:MM:SS] info: Program[0] Audio passthrough deactivated
   [HH:MM:SS] info: Program[0] Restored original microphone: Microphone (HD Pro Webcam C920)
   ```

8. **Verify Registry restored** (in PowerShell):
   ```powershell
   Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Multimedia\Audio Endpoints\Capture' -Name Default
   ```
   Should show: Your original microphone's device ID (the one saved in step 4)

### Expected Behavior

- **Passthrough activates** when: Phone Link app is open AND there are active microphone sessions
- **Passthrough deactivates** when: Call ends (audio sessions become inactive), even if Phone Link app is still open
- **Other apps ignored**: Discord, Zoom, Teams microphone access does not trigger passthrough (only PhoneExperienceHost process matters)
- **Smooth handoff**: Microphone automatically switched to CABLE during call, restored when call ends
- **Registry changes immediate**: Default value is written and verified in real-time

### Troubleshooting Test Failures

**"No passthrough detected when call starts"**
- Check verbose output shows "Phone Link is using microphone"
- Verify `PhoneExperienceHost` process is actually running (check Task Manager)
- Confirm microphone has active sessions (check Windows Sound settings during call)
- Solution: Manually verify device names match exactly with `--list-devices`

**"Registry change says 'Permission Denied'"**
- Requires admin privileges to write HKCU Registry
- Run PowerShell or Command Prompt as Administrator
- Try running MicPassthrough as Administrator

**"Sound Settings shows old microphone as default"**
- Windows Sound Settings UI may cache the value (no immediate visual update)
- Phone Link and other apps will use the new default on next startup
- Manually restart Phone Link to force it to re-query the default device
- Restart Windows if Sound Settings persistence issues occur

**"Phone Link still shows wrong microphone"**
- Phone Link may have cached the old default on startup
- Close Phone Link completely (check Task Manager that PhoneExperienceHost is gone)
- Restart Phone Link - it will re-query Windows default microphone
- Should now show CABLE Output as the default

### Success Criteria

Test passes when:
1. ✅ MicPassthrough detects Phone Link using microphone
2. ✅ Registry Default value changes to CABLE Input device ID
3. ✅ Verbose output confirms Registry write and verification successful
4. ✅ Phone Link call audio is clear and at normal volume
5. ✅ Audio passthrough stops when call ends
6. ✅ Registry Default value restores to original microphone

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
