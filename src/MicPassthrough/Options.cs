/// <summary>
/// Command-line options for the microphone passthrough application.
/// This class stores parsed values from System.CommandLine.
/// </summary>
public class Options
{
    /// <summary>
    /// Microphone device name (exact match). Use --list-devices to see available names.
    /// </summary>
    public string Mic { get; set; }

    /// <summary>
    /// VB-Cable render device name for audio passthrough output (exact match).
    /// This is the VB-Audio Virtual Cable INPUT device (playback/speaker side).
    /// </summary>
    public string CableRender { get; set; } = "CABLE Input (VB-Audio Virtual Cable)";

    /// <summary>
    /// VB-Cable capture device name for setting as default microphone (exact match).
    /// This is the VB-Audio Virtual Cable OUTPUT device (recording/microphone side).
    /// Only used with --auto-switch mode.
    /// </summary>
    public string CableCapture { get; set; } = "CABLE Output (VB-Audio Virtual Cable)";

    /// <summary>
    /// Monitor/speaker device name (exact match). Only used with --enable-monitor.
    /// </summary>
    public string Monitor { get; set; }

    /// <summary>
    /// Enable real-time audio monitoring through speakers/headphones.
    /// </summary>
    public bool EnableMonitor { get; set; }

    /// <summary>
    /// Buffer size in milliseconds. Larger = more stable but higher latency. 
    /// Increase if choppy (150-200ms), decrease for lower latency (50-75ms).
    /// </summary>
    public int Buffer { get; set; } = 100;

    /// <summary>
    /// Attempt exclusive audio mode for lower latency (~10ms). 
    /// Disable if other apps need audio access or device errors occur.
    /// </summary>
    public bool ExclusiveMode { get; set; } = true;

    /// <summary>
    /// Audio frames to buffer before playback. Prevents startup clicks. 
    /// Increase (4-5) if clicks occur, decrease (1-2) for faster startup.
    /// </summary>
    public int PrebufferFrames { get; set; } = 3;

    /// <summary>
    /// List all available audio devices and exit.
    /// </summary>
    public bool ListDevices { get; set; }

    /// <summary>
    /// Enable detailed logging with timestamps.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Enable automatic passthrough control based on call/activity detection.
    /// When enabled, passthrough activates only when the monitored application uses the microphone,
    /// and automatically switches Windows default microphone between the user's physical microphone and CABLE Output.
    /// </summary>
    public bool AutoSwitch { get; set; }

    /// <summary>
    /// Process name to monitor for auto-switch detection (without .exe extension).
    /// Default targets PhoneExperienceHost.
    /// </summary>
    public string TargetProcessName { get; set; } = "PhoneExperienceHost";

    /// <summary>
    /// Run in daemon mode with system tray indicator.
    /// Application will run in the background, visible only in system tray.
    /// Use right-click tray icon menu to control passthrough and exit.
    /// </summary>
    public bool Daemon { get; set; }
}
