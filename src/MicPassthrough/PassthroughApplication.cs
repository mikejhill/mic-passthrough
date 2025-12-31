using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Main application orchestrator for the microphone passthrough system.
/// Handles command-line argument parsing, initialization, and application lifecycle.
/// </summary>
public class PassthroughApplication
{
    private readonly ILogger _logger;
    private readonly AudioDeviceManager _deviceManager;

    /// <summary>
    /// Creates a new instance of PassthroughApplication.
    /// </summary>
    /// <param name="logger">Logger instance for application-level logging.</param>
    /// <param name="deviceManager">Device manager for audio device discovery.</param>
    public PassthroughApplication(ILogger logger, AudioDeviceManager deviceManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
    }

    /// <summary>
    /// Runs the application with the specified command-line options.
    /// </summary>
    /// <param name="options">Parsed command-line options.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    public int Run(Options options)
    {
        // Handle list-devices request
        if (options.ListDevices)
        {
            _deviceManager.ListAllDevices();
            return 0;
        }

        // Validate required microphone option
        if (string.IsNullOrEmpty(options.Mic))
        {
            Console.WriteLine("ERROR: --mic is required. Use --list-devices to see available microphones.");
            return 1;
        }

        // Run the passthrough engine
        RunPassthrough(options);
        return 0;
    }

    /// <summary>
    /// Initializes and runs the audio passthrough engine.
    /// If --auto-switch is enabled, monitors for call activity and controls passthrough lifecycle automatically.
    /// </summary>
    /// <param name="options">Application options containing device names and configuration.</param>
    private void RunPassthrough(Options options)
    {
        if (options.AutoSwitch)
        {
            RunAutoSwitchPassthrough(options);
        }
        else
        {
            RunContinuousPassthrough(options);
        }
    }

    /// <summary>
    /// Runs continuous passthrough mode where audio passthrough runs at all times until user exits.
    /// This is the default mode.
    /// </summary>
    private void RunContinuousPassthrough(Options options)
    {
        var engine = new PassthroughEngine(
            _logger,
            _deviceManager,
            options.Buffer,
            options.ExclusiveMode,
            options.PrebufferFrames);

        try
        {
            // Initialize with device names and settings
            engine.Initialize(
                options.Mic,
                options.CableRender,
                options.Monitor,
                options.EnableMonitor);

            // Start audio capture
            engine.Start();

            // Wait for user exit signal
            Console.WriteLine("Running (continuous mode). Press ENTER to exit. (Use --help for options)");
            Console.ReadLine();

            // Stop and cleanup
            engine.Stop();
        }
        finally
        {
            engine.Dispose();
        }
    }

    /// <summary>
    /// Runs automatic smart passthrough mode where passthrough activates only when calls are detected.
    /// Monitors when PhoneLink or other applications use the microphone and:
    /// 1. Automatically starts passthrough
    /// 2. Switches Windows default microphone to CABLE Output
    /// 3. Monitors for when the application releases the microphone
    /// 4. Stops passthrough and restores original microphone
    /// </summary>
    private void RunAutoSwitchPassthrough(Options options)
    {
        var engine = new PassthroughEngine(
            _logger,
            _deviceManager,
            options.Buffer,
            options.ExclusiveMode,
            options.PrebufferFrames);

        var micDevice = _deviceManager.FindDevice(NAudio.CoreAudioApi.DataFlow.Capture, options.Mic);
        var cableCaptureDevice = _deviceManager.FindDevice(NAudio.CoreAudioApi.DataFlow.Capture, options.CableCapture);
        
        // Create monitor that checks BOTH the physical mic AND cable capture device
        // This allows detecting if Phone Link switches between devices during the call handoff
        string cableCaptureDeviceId = cableCaptureDevice?.ID ?? null;
        var monitor = new ProcessAudioMonitor(_logger, micDevice.ID, cableCaptureDeviceId);
        var micManager = new WindowsDefaultMicrophoneManager(_logger);
        // CABLE Input is a Render device (output), not Capture
        var cableDevice = _deviceManager.FindDevice(NAudio.CoreAudioApi.DataFlow.Render, options.CableRender);

        bool engineRunning = false;

        try
        {
            // Initialize engine with settings
            engine.Initialize(
                options.Mic,
                options.CableRender,
                options.Monitor,
                options.EnableMonitor);

            _logger.LogInformation("Starting automatic smart passthrough mode");
            Console.WriteLine("Running in automatic call-detection mode. Press ENTER to exit.");
            
            // Start monitoring thread
            var cts = new CancellationTokenSource();
            var monitoringTask = monitor.StartMonitoringAsync(cts.Token);

            bool wasCallActive = false;

            // Main monitoring loop
            while (true)
            {
                // Check for user exit (non-blocking check)
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    break;
                }

                bool isCallActive = monitor.IsDeviceInUse;

                // Handle call state transitions
                if (!wasCallActive && isCallActive)
                {
                    // Call started - activate passthrough
                    _logger.LogInformation("Call detected. Activating passthrough...");
                    
                    try
                    {
                        // Find the configured CABLE capture device (default: CABLE Output) for setting as default microphone
                        var cableOutputDevice = _deviceManager.FindDevice(
                            NAudio.CoreAudioApi.DataFlow.Capture, 
                            options.CableCapture);
                        
                        if (cableOutputDevice != null)
                        {
                            // Switch default microphone to CABLE capture device
                            if (micManager.SetDefaultMicrophone(cableOutputDevice.ID))
                            {
                                _logger.LogInformation("Windows default microphone switched to: {Device}", cableOutputDevice.FriendlyName);
                            }
                            else
                            {
                                _logger.LogWarning("Could not switch Windows default microphone");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("CABLE capture device not found: {Device}", options.CableCapture);
                        }

                        // Start audio passthrough
                        engine.Start();
                        engineRunning = true;
                        _logger.LogInformation("Audio passthrough activated");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error activating passthrough");
                    }
                }
                else if (wasCallActive && !isCallActive)
                {
                    // Call ended - deactivate passthrough
                    _logger.LogInformation("Call ended. Deactivating passthrough...");

                    try
                    {
                        if (engineRunning)
                        {
                            engine.Stop();
                            engineRunning = false;
                        }

                        // Restore original microphone
                        if (micManager.RestoreOriginalMicrophone())
                        {
                            _logger.LogInformation("Windows default microphone restored");
                        }
                        else
                        {
                            _logger.LogWarning("Could not restore Windows default microphone");
                        }

                        _logger.LogInformation("Audio passthrough deactivated");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deactivating passthrough");
                    }
                }

                wasCallActive = isCallActive;
                Thread.Sleep(100); // Check 10 times per second
            }

            // Stop monitoring
            cts.Cancel();
            Task.WaitAll(monitoringTask);

            // Ensure passthrough is stopped and mic is restored
            if (engineRunning)
            {
                engine.Stop();
            }

            micManager.RestoreOriginalMicrophone();
        }
        finally
        {
            monitor.Stop();
            engine.Dispose();
            _logger.LogInformation("Automatic passthrough mode terminated");
        }
    }
}

