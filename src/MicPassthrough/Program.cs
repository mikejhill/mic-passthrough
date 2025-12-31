using CommandLine;
using Microsoft.Extensions.Logging;

/// <summary>
/// Entry point for the microphone passthrough application.
/// Routes command-line arguments and initializes the application framework.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // Parse command-line arguments
        return Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                opts => RunApplication(opts),
                _ => 1);
    }

    /// <summary>
    /// Initializes the application framework and executes the passthrough application.
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

        return application.Run(opts);
    }
}
