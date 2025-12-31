using CommandLine;

/// <summary>
/// Command-line options for the microphone passthrough application.
/// </summary>
public class Options
{
    /// <summary>
    /// Microphone device name (exact match). Use --list-devices to see available names.
    /// </summary>
    [Option('m', "mic",
        HelpText = "Microphone device name (exact match). Use --list-devices to see available names.")]
    public string Mic { get; set; }

    /// <summary>
    /// VB-Cable input device name (exact match).
    /// </summary>
    [Option('c', "cable", Default = "CABLE Input (VB-Audio Virtual Cable)",
        HelpText = "VB-Cable input device name (exact match).")]
    public string Cable { get; set; }

    /// <summary>
    /// Monitor/speaker device name (exact match). Only used with --enable-monitor.
    /// </summary>
    [Option('o', "monitor",
        HelpText = "Monitor/speaker device name (exact match). Only used with --enable-monitor.")]
    public string Monitor { get; set; }

    /// <summary>
    /// Enable real-time audio monitoring through speakers/headphones.
    /// </summary>
    [Option('e', "enable-monitor", Default = false,
        HelpText = "Enable real-time audio monitoring through speakers/headphones.")]
    public bool EnableMonitor { get; set; }

    /// <summary>
    /// Buffer size in milliseconds. Larger = more stable but higher latency. 
    /// Increase if choppy (150-200ms), decrease for lower latency (50-75ms).
    /// </summary>
    [Option('b', "buffer", Default = 100,
        HelpText = "Buffer size in milliseconds. Larger = more stable but higher latency. Increase if choppy (150-200ms), decrease for lower latency (50-75ms).")]
    public int Buffer { get; set; }

    /// <summary>
    /// Attempt exclusive audio mode for lower latency (~10ms). 
    /// Disable if other apps need audio access or device errors occur.
    /// </summary>
    [Option('x', "exclusive-mode", Default = true,
        HelpText = "Attempt exclusive audio mode for lower latency (~10ms). Disable if other apps need audio access or device errors occur.")]
    public bool ExclusiveMode { get; set; }

    /// <summary>
    /// Audio frames to buffer before playback. Prevents startup clicks. 
    /// Increase (4-5) if clicks occur, decrease (1-2) for faster startup.
    /// </summary>
    [Option('p', "prebuffer-frames", Default = 3,
        HelpText = "Audio frames to buffer before playback. Prevents startup clicks. Increase (4-5) if clicks occur, decrease (1-2) for faster startup.")]
    public int PrebufferFrames { get; set; }

    /// <summary>
    /// List all available audio devices and exit.
    /// </summary>
    [Option('l', "list-devices", Default = false,
        HelpText = "List all available audio devices and exit.")]
    public bool ListDevices { get; set; }

    /// <summary>
    /// Enable detailed logging with timestamps.
    /// </summary>
    [Option('v', "verbose", Default = false,
        HelpText = "Enable detailed logging with timestamps.")]
    public bool Verbose { get; set; }

    /// <summary>
    /// Enable automatic passthrough control based on call activity detection.
    /// When enabled, passthrough activates only when PhoneLink (or other apps) 
    /// actively use the microphone, and automatically switches Windows default microphone
    /// between the user's physical microphone and CABLE Output.
    /// </summary>
    [Option('a', "auto-switch", Default = false,
        HelpText = "Enable automatic passthrough control and default microphone switching when calls are active. Requires --mic to be set.")]
    public bool AutoSwitch { get; set; }
}
