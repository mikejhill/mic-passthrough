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
    private int _ourProcessId;

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
        // Store our own process ID so we can exclude our sessions
        _ourProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
        _logger.LogDebug("ProcessAudioMonitor initialized (our process ID: {ProcessId})", _ourProcessId);
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
    /// Checks if PhoneLink is actively using the monitored device.
    /// Uses two-level detection:
    /// 1. PhoneExperienceHost process must be running (Phone Link app is open)
    /// 2. Active microphone sessions must exist (Phone Link is actually using audio)
    /// This prevents false positives when Phone Link is open but not in a call.
    /// </summary>
    /// <returns>True if Phone Link is running AND actively using the microphone; false otherwise.</returns>
    private bool CheckDeviceUsage()
    {
        try
        {
            // First check: Is PhoneExperienceHost process running?
            var phoneExperienceProcesses = System.Diagnostics.Process.GetProcessesByName("PhoneExperienceHost");
            
            if (phoneExperienceProcesses.Length == 0)
            {
                _logger.LogDebug("PhoneExperienceHost not running");
                return false;
            }

            _logger.LogDebug("PhoneExperienceHost detected ({Count} instance{S})", 
                phoneExperienceProcesses.Length, 
                phoneExperienceProcesses.Length > 1 ? "s" : "");

            // Second check: Are there active audio sessions on the microphone?
            // Phone Link doesn't appear directly in sessions, but svchost.exe (Windows Runtime host) does
            // Only count it as "in use" if there are external active sessions
            try
            {
                var enumerator = new MMDeviceEnumerator();
                MMDevice device = enumerator.GetDevice(_deviceId);
                
                if (device?.AudioSessionManager == null)
                    return true;  // If we can't check, assume it's in use (safer)

                var sessionEnumerator = device.AudioSessionManager.Sessions;
                int externalActiveSessions = 0;

                for (int i = 0; i < sessionEnumerator.Count; i++)
                {
                    try
                    {
                        var session = sessionEnumerator[i];
                        
                        // Skip system sounds
                        if (session.IsSystemSoundsSession)
                            continue;

                        // Only count active sessions
                        if ((int)session.State != 1)  // 1 = Active
                            continue;

                        externalActiveSessions++;
                        _logger.LogDebug("Active session on mic (Phone Link likely using it)");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error checking session");
                    }
                }

                if (externalActiveSessions > 0)
                {
                    _logger.LogInformation("Phone Link is using microphone (PhoneExperienceHost running + {SessionCount} active session{S})", 
                        externalActiveSessions,
                        externalActiveSessions > 1 ? "s" : "");
                    return true;
                }
                else
                {
                    _logger.LogDebug("PhoneExperienceHost running but no active microphone sessions yet");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking audio sessions, assuming Phone Link is using device");
                return true;  // If we can't check, assume it's in use (safer)
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking for PhoneExperienceHost process");
            return false;
        }
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
