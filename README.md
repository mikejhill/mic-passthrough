# Microphone Passthrough

[![CI](https://github.com/mikejhill/mic-passthrough/actions/workflows/ci.yml/badge.svg)](https://github.com/mikejhill/mic-passthrough/actions/workflows/ci.yml)
[![Release](https://github.com/mikejhill/mic-passthrough/actions/workflows/release.yml/badge.svg)](https://github.com/mikejhill/mic-passthrough/actions/workflows/release.yml)

![Microphone Passthrough Logo](docs/assets/logo.png)

A low-latency audio passthrough application that routes a microphone's audio to [VB-Audio Virtual Cable](https://vb-audio.com/Cable/) using Windows WASAPI audio APIs.

> [!NOTE]
> **AI-Generated Code Disclaimer**: AI was heavily used in the creation of this project. While the sources have been reviewed and tested, users are strongly advised to review and understand the code themselves before using this project.

## Purpose

### Phone Link USB Microphone Volume Bug

This tool provides a workaround for the [**Phone Link microphone volume bug**](https://learn.microsoft.com/en-nz/answers/questions/5551238/microphone-volume-in-phone-link-has-gone-%28almost%29) for USB microphones. As of September 2025, when using Phone Link with certain USB microphones, calls through Phone Link are extremely quiet for the listener. Windows incorrectly applies gain to the audio signal, resulting in extremely quiet or distorted microphone input even when volume is set to maximum.

There is speculation that the issue lies in the "Generic USB Audio" driver provided by Microsoft, in which case it might impact all USB microphones using this driver. This includes the Logitech BRIO, Logitech C920, and possibly others. From [Sean Rudd](https://learn.microsoft.com/en-us/answers/questions/5551238/microphone-volume-in-phone-link-has-gone-(almost)?comment=answer-12263880&page=1#comment-2244706):
> The current hypothesis is that devices that depend upon the "Generic USB Audio" driver for their microphone are being affected due to the updated driver released in late August by a "Preview" update (in quotes because it was pushed to EVERYONE, even if you're not participating in the preview program). Details are in the first posting. It would be helpful to confirm that the device you are experiencing issues with is using this driver.


It is suspected that [KB5064081](https://support.microsoft.com/en-us/topic/august-29-2025-kb5064081-os-build-26100-5074-preview-3f9eb9e1-72ca-4b42-af97-39aace788d930) (2025-08-29) caused this regression. From [Hendrix-V, Microsoft External Staff](https://learn.microsoft.com/en-us/answers/questions/5551238/microphone-volume-in-phone-link-has-gone-(almost)?comment=answer-12231634&page=1#answer-12231634):
> Based on your description, this issue is linked to the recent Windows 11 24H2 Cumulative Update Preview (KB5064081), which introduced changes to the audio stack. These changes can cause USB microphones to show extremely low input levels in Phone Link, even though they work fine in other applications. The update includes updated audio drivers (version 10.0.26100.5074) that can affect how Phone Link handles communication devices, especially when Bluetooth and USB audio devices are both present.

**The Solution**: By routing your USB microphone through VB-Audio Virtual Cable, which use a different audio driver, Phone Link receives clear, full-volume audio from your physical microphone.

See [Microsoft Answers: Microphone volume in Phone Link has gone (almost) quiet](https://learn.microsoft.com/en-nz/answers/questions/5551238/microphone-volume-in-phone-link-has-gone-%28almost%29) for more context on this bug.

### General Uses
There are no intended uses for this beyond Phone Link. However, the technique presented here could feasibly be used for any case where one audio device needs to be streamed directly to another and where small latency is tolerable (~100ms). Examples:

- Streaming applications requiring virtual microphone input
- Real-time audio processing pipelines
- Testing audio-based applications
- Creating virtual microphone outputs from physical devices

## Requirements

### System Requirements
- **Windows 10/11** - Uses WASAPI (Windows Audio Session API)
- **.NET SDK 10.0** or later
- **VB-Audio Virtual Cable** - Free download from [vb-audio.com](https://vb-audio.com/Cable/)

### Installation

#### Install .NET 10 SDK

**Option 1: Using winget (Recommended)**

```powershell
winget install --id "Microsoft.Dotnet.SDK.10"
```

**Option 2: Manual Installation**

1. **Download** the .NET 10 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **Install** the x64 or x86 installer (x64 recommended for most systems)

**Verify Installation:**

```powershell
dotnet --version
```
Should output `10.0.x` or higher

#### Install VB-Audio Virtual Cable

1. **Download** from [vb-audio.com/Cable/](https://vb-audio.com/Cable/)
2. **Install** the driver (requires admin rights and system restart)
3. **Restart** your computer to activate the virtual cable
4. **Verify** by checking Windows Sound Settings - you should see:
   - "CABLE Input (VB-Audio Virtual Cable)" in playback devices
   - "CABLE Output (VB-Audio Virtual Cable)" in recording devices

#### Configure VB-Audio Virtual Cable

After installation, configure the cable properly for Phone Link:

1. **Enable Windows Volume Control**:
   - Open `VBCABLE_ControlPanel.exe` from the installation folder
   - In VB-Audio Cable settings, go to **Options**
   - Enable **"Windows Volume Control"** (this allows proper volume control in Phone Link)

2. **Set CABLE Output as Default Microphone**:
   - In Windows Sound Settings, navigate to **Input**
   - Click **"CABLE Output (VB-Audio Virtual Cable)"**
   - This tells Phone Link and other applications to use CABLE Output as the microphone input
   - Under **Output**, ensure your _usual speaker device_ is selected as the default playback device, **not** CABLE Input

3. **Verify Setup**:
   - MicPassthrough will route your physical USB mic → CABLE Input
   - Phone Link will listen to CABLE Output (which receives the audio)
   - Your microphone audio now bypasses the buggy Windows gain handling

## Building

### First Build

```powershell
# Navigate to project directory
cd MicPassthrough

# Restore dependencies and build
dotnet build
```

This creates the executable at: `src\MicPassthrough\bin\Debug\net10.0\MicPassthrough.exe`

### Subsequent Runs

Run the compiled executable directly:
```powershell
.\src\MicPassthrough\bin\Debug\net10.0\MicPassthrough.exe --help
```

Quick tests can be done without a full rebuild:
```powershell
cd src\MicPassthrough
dotnet run -- --help
```

### Clean Rebuild

When modifying source code:
```powershell
dotnet clean && dotnet build
```

## Usage

### Basic Usage

List available audio devices:
```powershell
.\src\MicPassthrough\bin\Debug\net10.0\MicPassthrough.exe --list-devices
```

Route microphone to virtual cable:
```powershell
.\src\MicPassthrough\bin\Debug\net10.0\MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --cable-render "CABLE Input (VB-Audio Virtual Cable)"
```

> **Note**: Replace the path with your actual .exe location if using a different build configuration (e.g., Release build)

### Command-Line Options

```
Description:
  Microphone Passthrough - Routes microphone audio to VB-Audio Virtual Cable

Usage:
  MicPassthrough [options]

Options:
  -m, --mic <mic>
      Microphone device name (exact match). Use --list-devices to see available names.

  -c, --cable-render <cable-render>
      VB-Cable render device name for audio output (exact match).
      This is the VB-Audio Virtual Cable INPUT device (playback/speaker side).
      Default: 'CABLE Input (VB-Audio Virtual Cable)'

  --cable-capture <cable-capture>
      VB-Cable capture device name for default microphone (exact match).
      This is the VB-Audio Virtual Cable OUTPUT device (recording/microphone side).
      Only used with --auto-switch.
      Default: 'CABLE Output (VB-Audio Virtual Cable)'

  -o, --monitor <monitor>
      Monitor/speaker device name (exact match). Only used with --enable-monitor.

  -e, --enable-monitor
      Enable real-time audio monitoring through speakers/headphones. [default: False]

  -b, --buffer <buffer>
      Buffer size in milliseconds. Larger = more stable but higher latency.
      Increase if choppy (150-200ms), decrease for lower latency (50-75ms).
      [default: 100]

  -x, --exclusive-mode
      Attempt exclusive audio mode for lower latency (~10ms).
      Disable if other apps need audio access or device errors occur.
      [default: True]

  -p, --prebuffer-frames <prebuffer-frames>
      Audio frames to buffer before playback. Prevents startup clicks.
      Increase (4-5) if clicks occur, decrease (1-2) for faster startup.
      [default: 3]

  -l, --list-devices
      List all available audio devices and exit. [default: False]

  -v, --verbose
      Enable detailed logging with timestamps. [default: False]

  -a, --auto-switch
      Enable automatic passthrough control and default microphone switching when calls
      are active. Requires --mic to be set. [default: False]

  --monitor-process <monitor-process>
      Process name (without .exe) to monitor for auto-switch detection.
      Default: 'PhoneExperienceHost'. [default: PhoneExperienceHost]

  -d, --daemon
      Run in daemon mode with system tray indicator. Application runs in background.
      [default: False]

  --version
      Show version information

  -?, -h, --help
      Show help and usage information
```

### Examples

> [!NOTE]
> Replace all device
[NOTE]: Replace `"Microphone (HD Pro Webcam C920)"` with your actual microphone device name as listed by `--list-devices`.

**Recommended: auto-switch in daemon mode (runs quietly in tray):**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --auto-switch --daemon
```
- Starts only when Phone Link grabs the mic
- Switches default microphone to CABLE Output during the call, then restores it
- Lives in the system tray with Start/Stop/Exit controls

**Auto-switch without daemon (console window stays open):**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --auto-switch
```
Uses the same call-detection logic but keeps everything in the console.

**Always-on passthrough (manual start/stop):**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)"
```
Routes audio immediately and keeps running until you stop it.

**With monitoring enabled (hear yourself in speakers):**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --enable-monitor --monitor "Speakers (Realtek(R) Audio)"
```

**Lower latency (higher dropout risk):**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --buffer 50
```
Smaller buffers reduce latency but increase the chance of audio gaps if the system cannot keep up. Watch the verbose buffer stats and raise the buffer if you hear clicks or dropouts.

**More stable buffering, but more latency:**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --buffer 200
```

**Disable exclusive mode if device busy:**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --exclusive-mode false
```
Exclusive mode lowers latency by taking sole control of the device. Turn it off if other apps also need the device, if you see access errors, or if you want Windows audio effects/mixers to stay active. Expect a few extra milliseconds of latency in shared mode.

**Verbose output with real-time statistics:**
```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --verbose
```
## Daemon Mode (Professional Background Operation)

For a professional experience without a console window, use daemon mode with system tray integration:

```powershell
MicPassthrough.exe --mic "Microphone (HD Pro Webcam C920)" --daemon
```

Pair daemon mode with `--auto-switch` for the recommended hands-off experience, or run without it to keep passthrough always on and control it from the tray.

Features:
- 🎯 System tray icon with real-time status display
- 🖱️ Double-click to toggle passthrough on/off
- ⚙️ Right-click menu for Start/Stop/Exit controls  
- 🔔 Status notifications for all user actions
- 🎨 Professional application icon in tray

For detailed daemon mode usage, architecture, and advanced options, see [Daemon Mode Documentation](docs/guides/daemon-mode.md).

## Verify the Phone Link fix

After setting up the passthrough, confirm that it solves the quiet-audio issue using one of these methods:

**Option 1: Windows Sound Recorder**

1. Open the built-in "Sound Recorder" app in Windows
2. Click "Start recording"
3. Speak into your microphone
4. Click "Stop recording"
5. Playback the recording - you should hear your voice clearly at full volume
6. If audio is quiet or distorted, check your VB-Audio Cable configuration (especially Windows Volume Control setting)

**Option 2: Echo Service (Test Number)**

Call an automated echo service to hear your own voice echoed back:
- **US**: +1-909-390-0003 or +1-804-222-1111
- The service will echo your voice back to you
- You should hear clear, full-volume audio
- If audio is quiet, the Phone Link bug is still affecting you; verify CABLE Output is set as default microphone and that Microphone Passthrough is enabled

**Option 3: Real Call (Recommended)**

Call someone you know to test with a real person:
1. Start MicPassthrough with your USB microphone
2. Make a call via Phone Link or your calling application
3. Ask the other person: "Can you hear me clearly at normal volume?"
4. Listen for feedback - they should NOT report you as quiet or distorted
5. This is the real-world test that confirms the fix is working

### Success Criteria
- Audio is clear and at normal volume (not quiet or distorted)
- No clicking or stuttering sounds
- Latency is not significant (~100ms is generally minor for real-time conversation)
- Consistent audio throughout the call/recording

## Audio Configuration

### Buffer Behavior

The buffer indicates how much audio is currently queued. The buffer status can be read by enabling verbose mode:
- **Reading**: "buffer: 75ms" means 75ms of audio waiting to be played
- **Healthy**: 60-90ms (good balance between latency and stability)
- **Too high** (approaching 100ms): Input faster than output, consider larger buffer or reducing source quality
- **Too low** (near 0ms): Risk of audio dropouts, increase buffer size or prebuffer frames

Typical output:
```
[21:19:34.098] dbug: Program[0] Stats: 100 frames, 1,607,680 bytes, 6.3s processed, buffer: 69.4ms
```

### Latency

Expected latency breakdown:
- **WASAPI internal**: ~30ms (OS-managed microphone buffer)
- **Application buffer**: ~100ms (configurable)
- **Total**: ~100-110ms (unnoticeable for most use cases)

To reduce latency:
1. Try `--buffer 50` or `--buffer 75` 
2. Ensure `--exclusive-mode true` is active (not forced to shared mode)
3. Monitor system resource usage - CPU contention increases latency
4. Check for other audio applications running simultaneously

### Troubleshooting

**"Device not found" error:**
- Use `--list-devices` to see exact device names
- Device names must match exactly (case-sensitive)
- Copy-paste device names from list to avoid typos

**No audio output:**
- Verify microphone is not muted in Windows Sound Settings
- Check VB-Audio Virtual Cable is installed and visible in Sound Settings
- Ensure other applications aren't using exclusive audio mode on the cable
- Ensure that "OUTPUT Cable" is selected as the default microphone (this should automatically occur during calls with `--auto-switch`)

**Audio glitches/stuttering:**
- Increase buffer size: `--buffer 150`
- Increase prebuffer frames: `--prebuffer-frames 4`
- Check CPU usage; resource contention causes audio glitches
- Close unnecessary applications

**Exclusive mode failures:**
- Other audio applications using the device
- Solution: Use `--exclusive-mode false`
- Trade-off: Slightly higher latency but better compatibility

**High latency:**
- Decrease buffer size: `--buffer 50`
- Verify `--exclusive-mode true` is active (check verbose logs)
- Check system load and CPU usage

## Architecture

For detailed design notes, see [docs/architecture/refactoring.md](docs/architecture/refactoring.md). Quick overview:

### Components

- **src/MicPassthrough/Program.cs** - Entry point, initializes logging and application framework
- **src/MicPassthrough/Options.cs** - Command-line option definitions with validation
- **src/MicPassthrough/PassthroughApplication.cs** - Main orchestrator, handles application lifecycle
- **src/MicPassthrough/AudioDeviceManager.cs** - Audio device enumeration and discovery
- **src/MicPassthrough/PassthroughEngine.cs** - Core audio processing, WASAPI integration

### Audio Flow

```
Microphone Device
    ↓ (WASAPI Capture)
AudioClient (Windows)
    ↓ (DataAvailable events)
PassthroughEngine
    ↓ (Frame buffering)
BufferedWaveProvider (100ms)
    ↓ (WASAPI Render)
VB-Audio Cable / Monitor Speaker
```

## Building for Release Distribution

To create a self-contained executable that doesn't require .NET SDK:
```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

This matches the release workflow's publish step (win-x64, self-contained, single file).

Output executable: `src/MicPassthrough/bin/Release/net10.0/win-x64/publish/MicPassthrough.exe`

This can be shared with others who don't have .NET installed. Simply run:
```powershell
MicPassthrough.exe --mic "Your Microphone Name"
```

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

### Dependency License Compliance

This project uses [ORT (OpenReuse Review Toolkit)](https://github.com/oss-review-toolkit/ort) to automatically validate that all upstream dependency licenses are compatible with the MIT license. ORT performs comprehensive compliance checks on the entire repository and all dependencies, generating detailed SBOM (Software Bill of Materials) and license reports on every build.

## Documentation

For comprehensive testing information, see [docs/guides/testing.md](docs/guides/testing.md).

For daemon mode details, see [docs/guides/daemon-mode.md](docs/guides/daemon-mode.md).

## See Also
- [VB-Audio Virtual Cable](https://vb-audio.com/Cable/)
- [NAudio Library](https://github.com/naudio/NAudio)
- [Windows WASAPI Documentation](https://docs.microsoft.com/en-us/windows/win32/coreaudio/wasapi)
