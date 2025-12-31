using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Monitors whether specific processes (like PhoneLink) are actively using a specified audio device.
/// Uses Windows Core Audio APIs to detect when applications open and close microphone sessions.
/// Tracks specific process IDs to differentiate between Phone Link and other applications.
/// </summary>
public class ProcessAudioMonitor
{
    private readonly ILogger _logger;
    private readonly string _deviceId;
    private volatile bool _isDeviceInUse;
    private int _ourProcessId;
    private HashSet<int> _trackedPhoneLinkProcessIds = new HashSet<int>();
    private HashSet<int> _lastSeenPhoneLinkSessions = new HashSet<int>();

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
        _ourProcessId = Process.GetCurrentProcess().Id;
        _logger.LogDebug("ProcessAudioMonitor initialized (our process ID: {ProcessId})", _ourProcessId);
    }

    /// <summary>
    /// Gets whether the monitored device is currently in use by Phone Link.
    /// </summary>
    public bool IsDeviceInUse => _isDeviceInUse;

    /// <summary>
    /// Starts monitoring the device for Phone Link activity.
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
    /// Checks every 500ms if Phone Link is actively using the device.
    /// Tracks sessions and detects when Phone Link releases the microphone.
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
                        _logger.LogInformation("Phone Link released the microphone");
                    }
                    else if (!wasDeviceInUse && _isDeviceInUse)
                    {
                        _logger.LogInformation("Phone Link is actively using the microphone");
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
    /// Checks if Phone Link is actively using the monitored device.
    /// Uses a multi-stage detection process:
    /// 1. Find PhoneExperienceHost process IDs (Phone Link app running)
    /// 2. Scan audio sessions on the device for Phone Link or svchost processes
    /// 3. Track which sessions belong to Phone Link/Windows Runtime
    /// 4. Detect when those sessions are released or become inactive
    /// 
    /// This prevents false positives from other applications using the microphone.
    /// </summary>
    /// <returns>True if Phone Link is actively using the microphone; false otherwise.</returns>
    private bool CheckDeviceUsage()
    {
        try
        {
            // Get current Phone Link process IDs
            var phoneExperienceProcesses = Process.GetProcessesByName("PhoneExperienceHost");
            var phoneExperienceIds = new HashSet<int>(phoneExperienceProcesses.Select(p => p.Id));

            _logger.LogDebug("Phone Link process check: {Count} instance{S} running", 
                phoneExperienceIds.Count, 
                phoneExperienceIds.Count != 1 ? "s" : "");

            // Phone Link not running at all
            if (phoneExperienceIds.Count == 0)
            {
                _lastSeenPhoneLinkSessions.Clear();
                return false;
            }

            // Phone Link is running - check for active sessions
            var currentPhoneLinkSessions = new HashSet<int>();
            
            try
            {
                var enumerator = new MMDeviceEnumerator();
                MMDevice device = enumerator.GetDevice(_deviceId);
                
                if (device?.AudioSessionManager == null)
                {
                    _logger.LogWarning("Cannot access audio session manager for device");
                    return true;  // If we can't check, assume Phone Link is using (safer)
                }

                var sessionEnumerator = device.AudioSessionManager.Sessions;
                _logger.LogDebug("Device audio sessions count: {Count}", sessionEnumerator.Count);

                for (int i = 0; i < sessionEnumerator.Count; i++)
                {
                    try
                    {
                        var session = sessionEnumerator[i];
                        
                        // Skip system sounds
                        if (session.IsSystemSoundsSession)
                        {
                            _logger.LogDebug("  [Session {Index}] System sounds (skipped)", i);
                            continue;
                        }

                        // Get session process ID
                        uint sessionPid = session.GetProcessID;
                        _logger.LogDebug("  [Session {Index}] Process ID: {Pid}, State: {State}", 
                            i, sessionPid, (int)session.State);

                        // Skip our own process
                        if (sessionPid == _ourProcessId)
                        {
                            _logger.LogDebug("  [Session {Index}] Our process (skipped)", i);
                            continue;
                        }

                        // Check if session belongs to Phone Link
                        if (phoneExperienceIds.Contains((int)sessionPid))
                        {
                            _logger.LogDebug("  [Session {Index}] Phone Link process detected", i);
                            currentPhoneLinkSessions.Add(i);
                        }
                        // Check if session belongs to svchost (Windows Runtime host used by Phone Link)
                        else if (IsServiceHostRelatedToPhoneLink((int)sessionPid))
                        {
                            _logger.LogDebug("  [Session {Index}] Windows Runtime/svchost related to Phone Link", i);
                            currentPhoneLinkSessions.Add(i);
                        }
                        // Skip other applications' sessions
                        else
                        {
                            var process = ProcessById((int)sessionPid);
                            _logger.LogDebug("  [Session {Index}] Other app: {ProcessName} (skipped)", i, process?.ProcessName ?? "unknown");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error checking session at index {Index}", i);
                    }
                }

                // Detect Phone Link session changes
                var newSessions = currentPhoneLinkSessions.Except(_lastSeenPhoneLinkSessions).ToList();
                var endedSessions = _lastSeenPhoneLinkSessions.Except(currentPhoneLinkSessions).ToList();

                if (newSessions.Count > 0)
                {
                    _logger.LogDebug("New Phone Link sessions: {Sessions}", string.Join(", ", newSessions));
                }
                if (endedSessions.Count > 0)
                {
                    _logger.LogInformation("Phone Link sessions ended: {Sessions}", string.Join(", ", endedSessions));
                }

                _lastSeenPhoneLinkSessions = currentPhoneLinkSessions;

                // Phone Link is in use if it has any active sessions
                if (currentPhoneLinkSessions.Count > 0)
                {
                    _logger.LogInformation("Phone Link is actively using microphone ({SessionCount} session{S})", 
                        currentPhoneLinkSessions.Count,
                        currentPhoneLinkSessions.Count != 1 ? "s" : "");
                    return true;
                }
                else
                {
                    _logger.LogDebug("Phone Link running but no active microphone sessions");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking audio sessions");
                // If we can't check, assume Phone Link is using (safer for false negatives)
                return currentPhoneLinkSessions.Count > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error in device usage check");
            return false;
        }
    }

    /// <summary>
    /// Checks if a svchost.exe process is related to Phone Link / Windows Runtime.
    /// Phone Link uses Windows Runtime which runs in svchost.exe processes.
    /// This is heuristic-based: if svchost is running and Phone Link is running,
    /// any active microphone session in svchost is likely Phone Link.
    /// </summary>
    private bool IsServiceHostRelatedToPhoneLink(int processId)
    {
        try
        {
            var process = ProcessById(processId);
            if (process == null)
                return false;

            // Only consider svchost processes as Phone Link-related
            if (!process.ProcessName.Equals("svchost", StringComparison.OrdinalIgnoreCase))
                return false;

            // svchost is Phone Link-related if Phone Link is currently running
            // This is a heuristic, but phone calls typically use svchost for audio sessions
            var phoneExperienceProcesses = Process.GetProcessesByName("PhoneExperienceHost");
            return phoneExperienceProcesses.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a process by ID, with exception handling.
    /// </summary>
    private Process ProcessById(int processId)
    {
        try
        {
            return Process.GetProcessById(processId);
        }
        catch
        {
            return null;
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
