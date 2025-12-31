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
        bool autoSwitchStarted = false;  // Track if auto-switch started passthrough (vs. manual)
        PassthroughEngine engine = null;
        object engineLock = new object();
        ProcessAudioMonitor audioMonitor = null;  // For auto-switch mode
        CancellationTokenSource monitorCts = null;  // To stop monitor thread
        Thread monitorThread = null;  // Reference to monitor thread so we can join it
        WindowsDefaultMicrophoneManager micManager = null;  // For microphone switching in enabled mode

        // Create a wrapper logger that also updates the status window
        var wrappedLogger = new DaemonLoggerWrapper(logger, (msg) =>
        {
            statusWindow.AddLog(msg);
        });

        // Helper function to start passthrough
        void StartPassthrough(bool isAutoStarted = false)
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

                    // Set CABLE Output as default microphone (for both enabled and auto-switch modes)
                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            micManager = new WindowsDefaultMicrophoneManager(wrappedLogger);
                            // Find the device ID for CABLE Output
                            var cableDevice = deviceManager.FindDevice(DataFlow.Capture, opts.CableCapture);
                            micManager.SetDefaultMicrophone(cableDevice.ID);
                            logger.LogInformation("Set default microphone to CABLE Output");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Could not set default microphone (may require admin rights)");
                        }
                    }

                    isPassthroughActive = true;
                    autoSwitchStarted = isAutoStarted;
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
                    autoSwitchStarted = false;
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

                    // Restore original microphone if we switched it
                    if (micManager != null)
                    {
                        try
                        {
                            micManager.RestoreOriginalMicrophone();
                            logger.LogInformation("Restored original microphone");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Could not restore original microphone");
                        }
                        micManager = null;  // Clear micManager after restoration
                    }

                    isPassthroughActive = false;
                    autoSwitchStarted = false;
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

        // Track current daemon mode state (for runtime switching)
        string currentMode = daemonMode;

        // Wire up tray UI events
        trayUI.ExitRequested += (s, e) =>
        {
            logger.LogInformation("Exit requested from tray menu");
            StopPassthrough();
            System.Windows.Forms.Application.Exit();
        };

        trayUI.ModeRequested += (s, mode) =>
        {
            HandleModeSwitch(mode);
        };

        // Wire up status window mode switching
        statusWindow.ModeRequested += (s, mode) =>
        {
            HandleModeSwitch(mode);
        };

        // Helper method for mode switching logic
        void HandleModeSwitch(string mode)
        {
            // Stop passthrough before switching modes
            if (isPassthroughActive)
                StopPassthrough();
            
            // Update mode and apply behavior
            string oldMode = currentMode;
            if (mode == "enabled")
            {
                currentMode = "enabled";
                opts.AutoSwitch = false;
            }
            else if (mode == "auto-switch")
            {
                currentMode = "auto-switch";
                opts.AutoSwitch = true;
            }
            else if (mode == "disabled")
            {
                currentMode = "disabled";
                opts.AutoSwitch = false;
            }
            
            if (oldMode != currentMode)
            {
                logger.LogInformation("Mode switched: {OldMode} -> {NewMode}", oldMode, currentMode);
                statusWindow.AddLog($"Mode switched: {oldMode} -> {currentMode}");
                trayUI.ShowNotification("Mode Changed", $"Daemon mode: {currentMode}");
                
                // Handle mode-specific state changes
                if (currentMode == "enabled")
                {
                    // Enabled mode: Stop monitor if running, then start passthrough
                    if (monitorCts != null)
                    {
                        logger.LogDebug("Stopping auto-switch monitor for enabled mode");
                        monitorCts.Cancel();
                        if (monitorThread != null && monitorThread.IsAlive)
                        {
                            if (!monitorThread.Join(1000))
                                logger.LogWarning("Monitor thread did not exit in time");
                        }
                        audioMonitor = null;
                        monitorCts.Dispose();
                        monitorCts = null;
                        monitorThread = null;
                        statusWindow.AddLog("Auto-switch monitor stopped");
                    }
                    // Start passthrough in enabled mode
                    if (!isPassthroughActive)
                    {
                        logger.LogInformation("Enabled mode: Starting passthrough");
                        StartPassthrough(isAutoStarted: false);
                    }
                }
                else if (currentMode == "auto-switch")
                {
                    // Auto-switch mode: Start monitor for automatic control
                    if (isPassthroughActive)
                    {
                        // Stop manual passthrough before starting auto-switch
                        logger.LogInformation("Stopping manual passthrough to start auto-switch");
                        StopPassthrough();
                    }
                    
                    try
                    {
                        // Stop old monitor thread if still running
                        if (monitorCts != null)
                        {
                            logger.LogDebug("Canceling old monitor thread...");
                            monitorCts.Cancel();
                            monitorCts.Dispose();
                            monitorCts = null;
                        }
                        
                        // Wait for old monitor thread to exit
                        if (monitorThread != null && monitorThread.IsAlive)
                        {
                            logger.LogDebug("Waiting for old monitor thread to exit...");
                            if (!monitorThread.Join(1000))
                                logger.LogWarning("Old monitor thread did not exit in time");
                        }
                        
                        // Create new cancellation token for the new monitor
                        monitorCts = new CancellationTokenSource();
                        var deviceManager = new AudioDeviceManager(wrappedLogger);
                        var micDevice = deviceManager.FindDevice(DataFlow.Capture, opts.Mic);
                        var cableCaptureDevice = deviceManager.FindDevice(DataFlow.Capture, opts.CableCapture);
                        string cableDeviceId = cableCaptureDevice?.ID ?? null;
                        audioMonitor = new ProcessAudioMonitor(wrappedLogger, micDevice.ID, cableDeviceId);
                        statusWindow.AddLog("Auto-switch monitor initialized");
                        
                        // Start ProcessAudioMonitor's internal device usage monitoring thread with proper cancellation token
                        _ = audioMonitor.StartMonitoringAsync(monitorCts.Token);
                        
                        monitorThread = new Thread(() =>
                        {
                            bool lastDetectedInUse = false;
                            while (monitorCts != null && !monitorCts.Token.IsCancellationRequested)
                            {
                                try
                                {
                                    // Check if monitor was nulled out by mode switching
                                    if (audioMonitor == null || monitorCts == null)
                                    {
                                        logger.LogDebug("Auto-switch monitor nulled, exiting thread");
                                        break;
                                    }
                                    
                                    bool isInUse = audioMonitor.IsDeviceInUse;
                                    if (isInUse && !lastDetectedInUse && !isPassthroughActive)
                                    {
                                        logger.LogInformation("Call detected (device in use)");
                                        StartPassthrough(isAutoStarted: true);
                                    }
                                    else if (!isInUse && lastDetectedInUse && isPassthroughActive && autoSwitchStarted)
                                    {
                                        logger.LogInformation("Call ended, stopping passthrough");
                                        StopPassthrough();
                                    }
                                    lastDetectedInUse = isInUse;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Error in auto-switch monitor");
                                }
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
                        logger.LogError(ex, "Failed to enable auto-switch");
                        statusWindow.AddLog($"ERROR enabling auto-switch: {ex.Message}");
                    }
                }
                else if (currentMode == "disabled")
                {
                    // Disabled mode: Stop everything
                    if (monitorCts != null)
                    {
                        logger.LogDebug("Stopping auto-switch monitor for disabled mode");
                        monitorCts.Cancel();
                        if (monitorThread != null && monitorThread.IsAlive)
                        {
                            if (!monitorThread.Join(1000))
                                logger.LogWarning("Monitor thread did not exit in time");
                        }
                        audioMonitor = null;
                        monitorCts.Dispose();
                        monitorCts = null;
                        monitorThread = null;
                        statusWindow.AddLog("Auto-switch monitor stopped");
                    }
                    if (isPassthroughActive)
                    {
                        logger.LogInformation("Disabled mode: Stopping passthrough");
                        StopPassthrough();
                    }
                }
            }
        }

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
            $"Daemon started\nMode: {currentMode}\nMic: {opts.Mic}");
        statusWindow.AddLog("Daemon mode started");
        statusWindow.AddLog($"Mode: {currentMode}");
        statusWindow.AddLog($"Microphone: {opts.Mic}");
        statusWindow.AddLog($"Cable: {opts.CableRender}");
        statusWindow.AddLog("Click [Enabled], [Auto-Switch], or [Disabled] to change modes");
        

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
                    var cableCaptureDevice = deviceManager.FindDevice(DataFlow.Capture, opts.CableCapture);
                    string deviceId = micDevice.ID;
                    string cableDeviceId = cableCaptureDevice?.ID ?? null;
                    
                    // Create monitor with device IDs (both as GUIDs, not names)
                    audioMonitor = new ProcessAudioMonitor(wrappedLogger, deviceId, cableDeviceId);
                    statusWindow.AddLog("Auto-switch monitor initialized");
                    
                    // Start ProcessAudioMonitor's internal device usage monitoring thread with proper cancellation token
                    _ = audioMonitor.StartMonitoringAsync(monitorCts.Token);
                    
                    // Start background monitoring thread
                    monitorThread = new Thread(() =>
                    {
                        logger.LogDebug("Auto-switch monitor thread started");
                        bool lastDetectedInUse = false;  // Track previous state to detect changes
                        
                        while (monitorCts != null && !monitorCts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                // Check if monitor was nulled out by mode switching
                                if (audioMonitor == null || monitorCts == null)
                                {
                                    logger.LogDebug("Auto-switch monitor nulled, exiting thread");
                                    break;
                                }
                                
                                bool isInUse = audioMonitor.IsDeviceInUse;
                                
                                // Only auto-start if it just became in-use (rising edge detection)
                                if (isInUse && !lastDetectedInUse && !isPassthroughActive)
                                {
                                    logger.LogInformation("Call detected by auto-switch monitor (device in use)");
                                    StartPassthrough(isAutoStarted: true);
                                }
                                // Only auto-stop if it just became not-in-use AND we auto-started it (falling edge detection)
                                else if (!isInUse && lastDetectedInUse && isPassthroughActive && autoSwitchStarted)
                                {
                                    logger.LogInformation("Call ended, stopping auto-started passthrough");
                                    StopPassthrough();
                                }
                                
                                lastDetectedInUse = isInUse;
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
