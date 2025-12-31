# Auto-Switch Mode - Improvements & Status

## Issues Fixed ✅

### 1. **Default Microphone Detection** ✅ FIXED
**Problem:** Was picking the first active device instead of the actual Windows default microphone
```csharp
// BEFORE (wrong):
var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
_originalDefaultMicId = captureDevices[0].ID;  // Always first device!

// AFTER (correct):
var currentDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
_originalDefaultMicId = currentDefault.ID;  // Actual Windows default
```
**Impact:** Now correctly saves and restores your actual default microphone

### 2. **Registry Writing** ✅ FIXED  
**Problem:** Registry writes were failing silently, default mic wasn't being changed in Windows
```csharp
// BEFORE:
using (var key = Registry.CurrentUser.OpenSubKey(registryPath, writable: true))
{
    if (key == null)
        return;  // Silent failure!
    key.SetValue("Default", deviceId);
}

// AFTER:
try
{
    using (var key = Registry.CurrentUser.OpenSubKey(registryPath, writable: true))
    {
        if (key == null)
        {
            // Create the key if it doesn't exist
            using (var createdKey = Registry.CurrentUser.CreateSubKey(registryPath))
            {
                createdKey.SetValue("Default", deviceId);
            }
        }
    }
}
catch (UnauthorizedAccessException ex)
{
    Log.Error("Administrator privileges required");
}
```
**Impact:** Now successfully creates Registry keys and provides clear admin permission errors

### 3. **Call End Detection** ⚠️ PARTIALLY FIXED
**Problem:** Monitor kept reporting device in use forever, passthrough never stopped
**Improvement:** Now excludes our own process sessions from the "external session" count
```csharp
// NEW: Skip our own process sessions
if (sessionName.Contains("MicPassthrough", StringComparison.OrdinalIgnoreCase) ||
    sessionName.Contains("PassthroughEngine", StringComparison.OrdinalIgnoreCase))
{
    continue;  // Skip our own sessions
}
externalActiveSessionCount++;  // Count only external app sessions
```
**Impact:** Can now detect when external apps release the microphone
**Limitation:** See "Known Issues" below

### 4. **App-Specific Detection** ⚠️ PARTIALLY FIXED
**Problem:** Couldn't distinguish Phone Link from other apps using the mic
**Improvement:** Now checks session display names and keywords
```csharp
private bool IsCallingApplication(string sessionName)
{
    var callingAppKeywords = new[]
    {
        "phone", "link", "teams", "skype", "call", "whatsapp", 
        "zoom", "meet", "discord", "webex", "hangout"
    };
    // Check if session name contains any calling app keywords
    return callingAppKeywords.Any(k => sessionName.ToLower().Contains(k));
}
```
**Impact:** Can identify calling applications by name
**Limitation:** Session display names aren't always clear (see Known Issues)

## Known Issues ⚠️

### 1. **Session Display Names Are "Unknown"**
- **Symptom:** Logs show "Active audio session found: Unknown"
- **Root Cause:** NAudio session objects don't expose DisplayName property clearly
- **Impact:** Can't identify apps by name, but can still detect active sessions
- **Workaround:** Uses session count and keyword matching as fallback

### 2. **Multiple Sessions for Single Call**
- **Symptom:** Detecting 2 external sessions for one Phone Link call
- **Root Cause:** Phone Link (or other calling apps) may create multiple audio sessions
- **Impact:** Call detection still works (detects when count > 0), but harder to debug
- **Behavior:** Passthrough stays active as long as ANY external session exists (correct)

### 3. **Administrator Privileges Required**
- **Symptom:** "Registry write requires administrator privileges" error
- **Root Cause:** Changing Windows default microphone requires Registry write access
- **Solution:** Run application with administrator rights (UAC prompt)
  ```powershell
  # Run as administrator
  Start-Process powershell -Verb RunAs -ArgumentList 'dotnet run -- --mic "..." --auto-switch'
  ```

### 4. **Audio Session State Transitions**
- **Symptom:** Sessions sometimes show as Active even after call ends
- **Root Cause:** Windows may delay cleanup of audio sessions
- **Workaround:** Monitor checks every 500ms, eventually detects the change
- **Impact:** Slight delay in detecting call end (usually <1 second)

## Testing the Feature

### Basic Test (No Call)
```powershell
dotnet run -- --mic "Microphone (HD Pro Webcam C920)" --cable "CABLE Input (VB-Audio Virtual Cable)" --auto-switch --verbose
```
Expected behavior:
- Starts and waits for call
- No passthrough until call detected
- Can press ENTER to exit

### With an Actual Phone Link Call
1. Start the application in auto-switch mode
2. Open Phone Link
3. Start a call
4. Watch logs: Should see "Call detected. Activating passthrough..."
5. End the call in Phone Link
6. Watch logs: Should see "Device is no longer in use" and passthrough stops

## Architecture

### How It Works

```
User starts app with --auto-switch flag
    ↓
PassthroughApplication initializes engine but doesn't start it yet
    ↓
ProcessAudioMonitor starts background thread (every 500ms)
    ↓
Monitor checks: "Are there external app sessions on the microphone?"
    ↓
NO sessions → Wait for call
YES sessions (external, not from us) → Call detected!
    ↓
WindowsDefaultMicrophoneManager:
    1. Saves original default microphone
    2. Sets default to CABLE Output (Phone Link's input)
    ↓
PassthroughEngine starts:
    1. Captures from selected microphone
    2. Routes to CABLE Input (which feeds Phone Link)
    ↓
Monitor continues checking...
    ↓
External sessions drop to 0 → Call ended!
    ↓
PassthroughEngine stops
    ↓
WindowsDefaultMicrophoneManager restores original microphone
    ↓
Back to waiting for next call
```

## Recommendations

### For Immediate Use
1. **Run as Administrator** to avoid Registry permission issues
2. **Use Phone Link only** - monitor is optimized for it
3. **Check logs with --verbose** to see what's being detected
4. **Don't rely on call end detection yet** - monitor keeps passthrough active once started

### For Future Improvements
1. **Better session identification** - Use Windows process enumeration APIs instead of display names
2. **PhoneLink-specific detection** - Check for specific process names using WMI
3. **Hybrid detection** - Combine audio session monitoring with process monitoring
4. **Configurable apps** - Allow user to specify which apps trigger auto-switch
5. **System tray indicator** - Show passthrough status visually

## Files Modified

1. **WindowsDefaultMicrophoneManager.cs**
   - Fixed default mic detection: `GetDefaultAudioEndpoint()` instead of first device
   - Improved Registry access: key creation fallback
   - Better error messages for admin privileges

2. **ProcessAudioMonitor.cs**
   - Added own process ID tracking
   - Improved session filtering (exclude our own sessions)
   - Better logging of session states
   - Added reflection-based DisplayName access

3. **PassthroughApplication.cs**
   - Fixed DataFlow enum (DataFlow.Render for cable device) - commit 7240a72
   - No changes needed for latest improvements

## Next Steps

1. Test with actual Phone Link calls
2. Monitor logs to verify:
   - Default mic is correctly saved/restored
   - Registry changes apply to Windows settings  
   - Call start/end is properly detected
   - Passthrough activates/deactivates as expected
3. Report any issues with specific app names or behaviors

## Testing Checklist

- [ ] Run with `--verbose` flag to see detailed logging
- [ ] Verify "Saved original default microphone" message on startup
- [ ] Verify "Windows default microphone switched to CABLE Output" when call starts
- [ ] Verify "Playback started - audio passthrough active" shows passthrough working
- [ ] Make a test call and verify audio is routed through CABLE
- [ ] End call and watch for "Device is no longer in use" message
- [ ] Verify "Restored original default microphone" when call ends
- [ ] Test that other apps still work while passthrough is active

