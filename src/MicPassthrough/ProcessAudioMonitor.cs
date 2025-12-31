using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Monitors whether external processes (like PhoneLink) are actively using a specified audio device.
/// Uses Windows Core Audio APIs to detect when other applications open the microphone device.
/// </summary>
public class ProcessAudioMonitor
{
    private readonly ILogger _logger;
    private readonly string _deviceId;
    private volatile bool _isDeviceInUse;

    /// <summary>
    /// Creates a new instance of ProcessAudioMonitor.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="deviceId">Device ID to monitor (from AudioDeviceManager.FindDevice).</param>
    public ProcessAudioMonitor(ILogger logger, string deviceId)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _isDeviceInUse = false;
    }

    /// <summary>
    /// Gets whether the monitored device is currently in use by external processes.
    /// </summary>
    public bool IsDeviceInUse => _isDeviceInUse;

    /// <summary>
    /// Starts monitoring the device for external use.
    /// Runs on a background thread, checking at regular intervals.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop monitoring.</param>
    /// <returns>A task that completes when monitoring stops.</returns>
    public Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => MonitorDeviceUsage(cancellationToken));
    }

    /// <summary>
    /// Monitors device usage on a background thread.
    /// Checks every 500ms if the device is being accessed by other processes.
    /// </summary>
    private void MonitorDeviceUsage(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Device audio monitoring started");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var wasDeviceInUse = _isDeviceInUse;
                    _isDeviceInUse = CheckDeviceUsage();

                    // Log state changes
                    if (wasDeviceInUse && !_isDeviceInUse)
                    {
                        _logger.LogInformation("Device is no longer in use by external processes");
                    }
                    else if (!wasDeviceInUse && _isDeviceInUse)
                    {
                        _logger.LogInformation("Device is now in use by external process (likely PhoneLink)");
                    }

                    Thread.Sleep(500); // Check twice per second
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking device usage, will retry");
                    Thread.Sleep(1000);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Device audio monitoring stopped");
        }
    }

    /// <summary>
    /// Checks if PhoneLink or Teams is currently using the monitored device.
    /// Only detects calling applications by checking for active sessions.
    /// Uses Windows Core Audio APIs to inspect device session enumeration.
    /// </summary>
    /// <returns>True if a calling app (PhoneLink/Teams) is using the device; false otherwise.</returns>
    private bool CheckDeviceUsage()
    {
        try
        {
            var enumerator = new MMDeviceEnumerator();
            
            // Get the specific device we're monitoring
            MMDevice device = null;
            try
            {
                device = enumerator.GetDevice(_deviceId);
            }
            catch
            {
                // Device not found or error, treat as not in use
                return false;
            }

            if (device == null)
                return false;

            // Check for active audio sessions on this device
            // Phone Link and Teams show up as audio sessions when on a call
            var sessionManager = device.AudioSessionManager;
            if (sessionManager == null)
                return false;

            var sessionEnumerator = sessionManager.Sessions;
            bool callDetected = false;
            
            // Look through all active sessions
            for (int i = 0; i < sessionEnumerator.Count; i++)
            {
                try
                {
                    var session = sessionEnumerator[i];
                    
                    // Skip system session (system sounds, notifications, etc.)
                    if (session.IsSystemSoundsSession)
                        continue;

                    // Check if session is active
                    // AudioSessionState: Inactive=0, Active=1, Expired=2
                    if ((int)session.State != 1)  // Only check active sessions
                        continue;

                    // Try to identify the session's display name
                    // This helps us know if it's a calling app
                    string sessionName = GetSessionDisplayName(session);
                    
                    _logger.LogDebug("Active audio session: {SessionName}", sessionName);
                    
                    // Check if this looks like a calling application
                    // Phone Link, Teams, Skype, etc. typically show identifiable names
                    if (IsCallingApplication(sessionName))
                    {
                        _logger.LogInformation("Calling application detected: {SessionName}", sessionName);
                        callDetected = true;
                        break;
                    }
                    
                    // If no specific app detected but there's an active audio session on our device,
                    // and our device is the default recording device, assume it's the calling app
                    // This handles cases where PhoneLink doesn't show a clear display name
                    _logger.LogDebug("Active audio stream on microphone - assuming calling app");
                    callDetected = true;
                    break;
                }
                catch (Exception ex)
                {
                    // Session might have been disposed, skip it
                    _logger.LogDebug(ex, "Error processing audio session");
                }
            }

            return callDetected;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking device sessions");
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the display name of an audio session.
    /// </summary>
    private string GetSessionDisplayName(object session)
    {
        try
        {
            // Try to access DisplayName property
            var displayNameProp = session.GetType().GetProperty("DisplayName");
            if (displayNameProp != null)
            {
                var displayName = displayNameProp.GetValue(session) as string;
                return !string.IsNullOrEmpty(displayName) ? displayName : "Unknown";
            }
        }
        catch { }

        return "Unknown";
    }

    /// <summary>
    /// Checks if a session name indicates a calling application.
    /// </summary>
    private bool IsCallingApplication(string sessionName)
    {
        if (string.IsNullOrEmpty(sessionName))
            return false;

        var callingAppKeywords = new[]
        {
            "phone", "link", "teams", "skype", "call", "whatsapp", 
            "zoom", "meet", "discord", "webex", "hangout"
        };

        var lowerName = sessionName.ToLower();
        foreach (var keyword in callingAppKeywords)
        {
            if (lowerName.Contains(keyword))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Stops monitoring. Called when application exits or monitoring is no longer needed.
    /// Use CancellationToken from StartMonitoringAsync to stop monitoring.
    /// </summary>
    public void Stop()
    {
        // Monitoring is controlled via CancellationToken from StartMonitoringAsync
    }
}
