using Microsoft.Extensions.Logging;
using System;

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
    /// </summary>
    /// <param name="options">Application options containing device names and configuration.</param>
    private void RunPassthrough(Options options)
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
                options.Cable,
                options.Monitor,
                options.EnableMonitor);

            // Start audio capture
            engine.Start();

            // Wait for user exit signal
            Console.WriteLine("Running. Press ENTER to exit. (Use --help for options)");
            Console.ReadLine();

            // Stop and cleanup
            engine.Stop();
        }
        finally
        {
            engine.Dispose();
        }
    }
}
