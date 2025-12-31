using System.CommandLine;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;

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
    /// Runs the application in daemon mode with system tray indicator and status window.
    /// Supports auto-switch mode for automatic passthrough control based on call detection.
    /// </summary>
    static int RunDaemonMode(PassthroughApplication application, Options opts, ILogger logger)
    {
        logger.LogInformation("Starting in daemon mode");

        // Validate microphone is specified (required for daemon)
        if (string.IsNullOrEmpty(opts.Mic))
        {
            logger.LogError("Daemon mode requires --mic to be specified");
            logger.LogInformation("Use --list-devices to see available microphones");
            return 1;
        }

        // Determine daemon mode based on options
        string daemonMode = opts.AutoSwitch ? "auto-switch" : "enabled";  // Default to enabled if not auto-switch
        logger.LogInformation("Daemon control mode: {Mode}", daemonMode);

        // Initialize system tray UI
        using var trayUI = new SystemTrayUI(logger);
        trayUI.MicrophoneDevice = opts.Mic;
        trayUI.CableDevice = opts.CableRender;

        // Initialize status window
        var statusWindow = new StatusWindow(logger)
        {
            ShowInTaskbar = false
        };
        statusWindow.SetMicrophoneDevice(opts.Mic);
        statusWindow.SetCableDevice(opts.CableRender);

        // Shared state for controlling passthrough
        bool isPassthroughActive = false;
        PassthroughEngine engine = null;
        object engineLock = new object();
        ProcessAudioMonitor audioMonitor = null;  // For auto-switch mode
        CancellationTokenSource monitorCts = null;  // To stop monitor thread

        // Create a wrapper logger that also updates the status window
        var wrappedLogger = new DaemonLoggerWrapper(logger, (msg) =>
        {
            statusWindow.AddLog(msg);
        });

        // Helper function to start passthrough
        void StartPassthrough()
        {
            lock (engineLock)
            {
                if (isPassthroughActive)
                {
                    logger.LogDebug("Passthrough already active, ignoring start request");
                    return;
                }

                try
                {
                    logger.LogInformation("Starting passthrough engine");
                    var deviceManager = new AudioDeviceManager(wrappedLogger);
                    engine = new PassthroughEngine(wrappedLogger, deviceManager,
                        opts.Buffer, opts.ExclusiveMode, opts.PrebufferFrames);

                    engine.Initialize(opts.Mic, opts.CableRender, opts.Monitor, opts.EnableMonitor);
                    engine.Start();

                    isPassthroughActive = true;
                    trayUI.IsPassthroughActive = true;
                    statusWindow.SetStatus(true);
                    trayUI.ShowNotification("Microphone Passthrough", "Passthrough started");
                    logger.LogInformation("Passthrough engine started successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start passthrough engine");
                    statusWindow.AddLog($"ERROR: {ex.Message}");
                    isPassthroughActive = false;
                    trayUI.IsPassthroughActive = false;
                    statusWindow.SetStatus(false);
                    trayUI.ShowNotification("Microphone Passthrough", $"Failed to start: {ex.Message}");
                }
            }
        }

        // Helper function to stop passthrough
        void StopPassthrough()
        {
            lock (engineLock)
            {
                if (!isPassthroughActive || engine == null)
                {
                    logger.LogDebug("Passthrough not active, ignoring stop request");
                    return;
                }

                try
                {
                    logger.LogInformation("Stopping passthrough engine");
                    engine.Stop();
                    engine.Dispose();
                    engine = null;

                    isPassthroughActive = false;
                    trayUI.IsPassthroughActive = false;
                    statusWindow.SetStatus(false);
                    trayUI.ShowNotification("Microphone Passthrough", "Passthrough stopped");
                    logger.LogInformation("Passthrough engine stopped successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error stopping passthrough engine");
                    statusWindow.AddLog($"ERROR stopping: {ex.Message}");
                }
            }
        }

        // Wire up tray UI events
        trayUI.StartRequested += (s, e) => StartPassthrough();
        trayUI.StopRequested += (s, e) => StopPassthrough();
        trayUI.ExitRequested += (s, e) =>
        {
            logger.LogInformation("Exit requested from tray menu");
            StopPassthrough();
            System.Windows.Forms.Application.Exit();
        };

        // Wire up status window toggle
        statusWindow.ToggleRequested += (s, e) =>
        {
            if (isPassthroughActive)
                StopPassthrough();
            else
                StartPassthrough();
        };

        // Wire up status window to show on tray double-click
        trayUI.DoubleClickAction = () =>
        {
            if (statusWindow.Visible)
            {
                statusWindow.Hide();
                statusWindow.ShowInTaskbar = false;
            }
            else
            {
                statusWindow.Show();
                statusWindow.ShowInTaskbar = true;
                statusWindow.Activate();
            }
        };

        // Show initial notification
        trayUI.ShowNotification("Microphone Passthrough",
            $"Daemon started\nMode: {daemonMode}\nMic: {opts.Mic}");
        statusWindow.AddLog("Daemon mode started");
        statusWindow.AddLog($"Mode: {daemonMode}");
        statusWindow.AddLog($"Microphone: {opts.Mic}");
        statusWindow.AddLog($"Cable: {opts.CableRender}");

        // Start auto-switch monitor if in auto-switch mode
        if (opts.AutoSwitch)
        {
            try
            {
                monitorCts = new CancellationTokenSource();
                var deviceManager = new AudioDeviceManager(wrappedLogger);
                
                // Find the microphone device to get its ID for monitoring
                try
                {
                    var micDevice = deviceManager.FindDevice(DataFlow.Capture, opts.Mic);
                    string deviceId = micDevice.ID;
                    
                    // Create monitor with device ID and cable capture device name
                    audioMonitor = new ProcessAudioMonitor(wrappedLogger, deviceId, opts.CableCapture);
                    statusWindow.AddLog("Auto-switch monitor initialized");
                    
                    // Start background monitoring thread
                    var monitorThread = new Thread(() =>
                    {
                        logger.LogDebug("Auto-switch monitor thread started");
                        while (!monitorCts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                bool isInUse = audioMonitor.IsDeviceInUse;
                                
                                // Start passthrough if device is in use and not already active
                                if (isInUse && !isPassthroughActive)
                                {
                                    logger.LogInformation("Call detected by auto-switch monitor");
                                    StartPassthrough();
                                }
                                // Stop passthrough if device is not in use and currently active
                                else if (!isInUse && isPassthroughActive)
                                {
                                    logger.LogInformation("Call ended, stopping passthrough");
                                    StopPassthrough();
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error in auto-switch monitor");
                            }
                            
                            // Check every 500ms
                            System.Threading.Thread.Sleep(500);
                        }
                        logger.LogDebug("Auto-switch monitor thread exiting");
                    })
                    {
                        IsBackground = true
                    };
                    monitorThread.Start();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not find microphone device for auto-switch");
                    statusWindow.AddLog("WARNING: Could not initialize auto-switch - device not found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize auto-switch monitor");
                statusWindow.AddLog($"ERROR initializing auto-switch: {ex.Message}");
            }
        }

        logger.LogDebug("Running Windows Forms application loop for system tray");

        // Run the message loop
        System.Windows.Forms.Application.Run();

        // Cleanup
        logger.LogInformation("Daemon mode exiting");
        StopPassthrough();
        
        // Stop auto-switch monitor if running
        if (monitorCts != null)
        {
            monitorCts.Cancel();
            monitorCts.Dispose();
        }
        
        statusWindow?.Dispose();

        return 0;
    }
}
