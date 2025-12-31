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
    /// Checks if the monitored device is currently being used by any external process.
    /// Uses Windows Core Audio APIs to inspect device session enumeration.
    /// </summary>
    /// <returns>True if device is in use; false otherwise.</returns>
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
                // Device not found, treat as not in use
                return false;
            }

            if (device == null)
                return false;

            // Check if any audio sessions are active on this device
            // This detects when PhoneLink or other apps are recording from the microphone
            var sessionManager = device.AudioSessionManager;
            if (sessionManager == null)
                return false;

            var sessionEnumerator = sessionManager.Sessions;
            
            // If there are active sessions (other than system sounds), device is in use
            for (int i = 0; i < sessionEnumerator.Count; i++)
            {
                try
                {
                    var session = sessionEnumerator[i];
                    
                    // Skip system session and our own sessions
                    if (session.IsSystemSoundsSession)
                        continue;

                    // Check if session is active (has audio)
                    // AudioSessionState enum: Active=1
                    if ((int)session.State == 1)
                    {
                        _logger.LogDebug("Active audio session detected on device");
                        return true;
                    }
                }
                catch
                {
                    // Session might have been disposed, skip it
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking device sessions");
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
