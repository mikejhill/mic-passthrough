using System.CommandLine;
using Microsoft.Extensions.Logging;

/// <summary>
/// Entry point for the microphone passthrough application.
/// Routes command-line arguments and initializes the application framework.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = BuildRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Builds the root command with all options and their configurations.
    /// </summary>
    static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Microphone Passthrough - Routes microphone audio to VB-Audio Virtual Cable");

        // Define all options with aliases, descriptions, and defaults
        var micOption = new Option<string>(
            aliases: new[] { "-m", "--mic" },
            description: "Microphone device name (exact match). Use --list-devices to see available names.");

        var cableRenderOption = new Option<string>(
            aliases: new[] { "-c", "--cable-render" },
            getDefaultValue: () => "CABLE Input (VB-Audio Virtual Cable)",
            description: "VB-Cable render device name for audio output (exact match). Default: 'CABLE Input (VB-Audio Virtual Cable)'.");

        var cableCaptureOption = new Option<string>(
            aliases: new[] { "--cable-capture" },
            getDefaultValue: () => "CABLE Output (VB-Audio Virtual Cable)",
            description: "VB-Cable capture device name for default microphone (exact match). Default: 'CABLE Output (VB-Audio Virtual Cable)'. Only used with --auto-switch.");

        var monitorOption = new Option<string>(
            aliases: new[] { "-o", "--monitor" },
            description: "Monitor/speaker device name (exact match). Only used with --enable-monitor.");

        var enableMonitorOption = new Option<bool>(
            aliases: new[] { "-e", "--enable-monitor" },
            getDefaultValue: () => false,
            description: "Enable real-time audio monitoring through speakers/headphones.");

        var bufferOption = new Option<int>(
            aliases: new[] { "-b", "--buffer" },
            getDefaultValue: () => 100,
            description: "Buffer size in milliseconds. Larger = more stable but higher latency. Increase if choppy (150-200ms), decrease for lower latency (50-75ms).");

        var exclusiveModeOption = new Option<bool>(
            aliases: new[] { "-x", "--exclusive-mode" },
            getDefaultValue: () => true,
            description: "Attempt exclusive audio mode for lower latency (~10ms). Disable if other apps need audio access or device errors occur.");

        var prebufferFramesOption = new Option<int>(
            aliases: new[] { "-p", "--prebuffer-frames" },
            getDefaultValue: () => 3,
            description: "Audio frames to buffer before playback. Prevents startup clicks. Increase (4-5) if clicks occur, decrease (1-2) for faster startup.");

        var listDevicesOption = new Option<bool>(
            aliases: new[] { "-l", "--list-devices" },
            getDefaultValue: () => false,
            description: "List all available audio devices and exit.");

        var verboseOption = new Option<bool>(
            aliases: new[] { "-v", "--verbose" },
            getDefaultValue: () => false,
            description: "Enable detailed logging with timestamps.");

        var autoSwitchOption = new Option<bool>(
            aliases: new[] { "-a", "--auto-switch" },
            getDefaultValue: () => false,
            description: "Enable automatic passthrough control and default microphone switching when calls are active. Requires --mic to be set.");

        var daemonOption = new Option<bool>(
            aliases: new[] { "-d", "--daemon" },
            getDefaultValue: () => false,
            description: "Run in daemon mode with system tray indicator. Application runs in background.");

        // Add all options to root command
        rootCommand.AddOption(micOption);
        rootCommand.AddOption(cableRenderOption);
        rootCommand.AddOption(cableCaptureOption);
        rootCommand.AddOption(monitorOption);
        rootCommand.AddOption(enableMonitorOption);
        rootCommand.AddOption(bufferOption);
        rootCommand.AddOption(exclusiveModeOption);
        rootCommand.AddOption(prebufferFramesOption);
        rootCommand.AddOption(listDevicesOption);
        rootCommand.AddOption(verboseOption);
        rootCommand.AddOption(autoSwitchOption);
        rootCommand.AddOption(daemonOption);

        // Set the handler using context to access parsed values
        rootCommand.SetHandler(context =>
        {
            var options = new Options
            {
                Mic = context.ParseResult.GetValueForOption(micOption),
                CableRender = context.ParseResult.GetValueForOption(cableRenderOption),
                CableCapture = context.ParseResult.GetValueForOption(cableCaptureOption),
                Monitor = context.ParseResult.GetValueForOption(monitorOption),
                EnableMonitor = context.ParseResult.GetValueForOption(enableMonitorOption),
                Buffer = context.ParseResult.GetValueForOption(bufferOption),
                ExclusiveMode = context.ParseResult.GetValueForOption(exclusiveModeOption),
                PrebufferFrames = context.ParseResult.GetValueForOption(prebufferFramesOption),
                ListDevices = context.ParseResult.GetValueForOption(listDevicesOption),
                Verbose = context.ParseResult.GetValueForOption(verboseOption),
                AutoSwitch = context.ParseResult.GetValueForOption(autoSwitchOption),
                Daemon = context.ParseResult.GetValueForOption(daemonOption)
            };

            context.ExitCode = RunApplication(options);
        });

        return rootCommand;
    }

    /// <summary>
    /// Initializes the application framework and executes the passthrough application.
    /// Supports both CLI mode and daemon mode with system tray.
    /// </summary>
    /// <param name="opts">Parsed command-line options.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    static int RunApplication(Options opts)
    {
        // Configure logging based on verbosity setting
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(opts.Verbose ? LogLevel.Debug : LogLevel.Information);
            builder.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "[HH:mm:ss.fff] ";
                options.SingleLine = true;
            });
        });

        var logger = loggerFactory.CreateLogger<Program>();
        var deviceManager = new AudioDeviceManager(logger);
        var application = new PassthroughApplication(logger, deviceManager);

        if (opts.Daemon)
        {
            return RunDaemonMode(application, opts, logger);
        }
        else
        {
            return application.Run(opts);
        }
    }

    /// <summary>
    /// Runs the application in daemon mode with system tray indicator.
    /// </summary>
    static int RunDaemonMode(PassthroughApplication application, Options opts, ILogger logger)
    {
        logger.LogInformation("Starting in daemon mode");

        // Initialize system tray UI
        using var trayUI = new SystemTrayUI(logger);
        trayUI.MicrophoneDevice = opts.Mic;
        trayUI.CableDevice = opts.CableRender;

        // Wire up tray UI events to control passthrough
        var passthroughThread = new Thread(() =>
        {
            try
            {
                application.Run(opts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Passthrough application failed");
            }
        });
        passthroughThread.Start();

        // Show initial notification
        trayUI.ShowNotification("Microphone Passthrough",
            $"Daemon started\nMic: {opts.Mic}\nCable: {opts.CableRender}");

        // Keep the application alive while daemon is running
        System.Windows.Forms.Application.Run();

        logger.LogInformation("Daemon mode exiting");
        return 0;
    }
}
