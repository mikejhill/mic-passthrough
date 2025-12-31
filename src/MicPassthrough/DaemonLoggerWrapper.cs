using Microsoft.Extensions.Logging;
using System;

/// <summary>
/// Wraps a logger to forward messages to a callback function.
/// Used in daemon mode to display logs in the status window.
/// </summary>
public class DaemonLoggerWrapper : ILogger
{
    private readonly ILogger _innerLogger;
    private readonly Action<string> _logCallback;

    public DaemonLoggerWrapper(ILogger innerLogger, Action<string> logCallback)
    {
        _innerLogger = innerLogger;
        _logCallback = logCallback;
    }

    public IDisposable BeginScope<TState>(TState state) => _innerLogger.BeginScope(state);
    public bool IsEnabled(LogLevel logLevel) => _innerLogger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _innerLogger.Log(logLevel, eventId, state, exception, formatter);

        // Also send to callback for display in status window
        var message = formatter(state, exception);
        if (!string.IsNullOrEmpty(message))
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var levelStr = logLevel.ToString().ToUpper();
            var displayMessage = $"[{timestamp}] {levelStr}: {message}";
            if (exception != null)
                displayMessage += $"\n{exception.Message}";

            _logCallback(displayMessage);
        }
    }
}
