using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Monitors whether specific processes (like PhoneLink) are actively using specified audio devices.
/// Uses Windows Core Audio APIs to detect when applications open and close microphone sessions.
/// Tracks specific process IDs to differentiate between Phone Link and other applications.
/// Monitors both the physical microphone and cable capture device to detect calls.
/// </summary>
public class ProcessAudioMonitor
{
    private readonly ILogger _logger;
    private readonly string _deviceId;  // Physical microphone device
    private readonly string _cableDeviceId;  // Cable capture device (optional)
    private volatile bool _isDeviceInUse;
    private int _ourProcessId;
    private HashSet<int> _trackedPhoneLinkProcessIds = new HashSet<int>();
    private HashSet<int> _lastSeenPhoneLinkSessions = new HashSet<int>();
    private long _lastPhoneLinkSessionTicks;  // Track when we last saw a Phone Link session
    private const long GRACE_PERIOD_MS = 500;  // Grace period in milliseconds

    /// <summary>
    /// Creates a new instance of ProcessAudioMonitor.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="deviceId">Device ID to monitor (physical microphone).</param>
    /// <param name="cableDeviceId">Optional device ID for cable capture (CABLE Output device). Used to detect if Phone Link switched to cable.</param>
    public ProcessAudioMonitor(ILogger logger, string deviceId, string cableDeviceId = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _cableDeviceId = cableDeviceId;  // May be null
        _isDeviceInUse = false;
        _lastPhoneLinkSessionTicks = 0;
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
    /// Checks if Phone Link is actively using the monitored devices.
    /// Uses a multi-stage detection process:
    /// 1. Find PhoneExperienceHost process IDs (Phone Link app running)
    /// 2. Scan audio sessions on BOTH the physical mic AND cable capture device
    /// 3. Track which sessions belong to Phone Link/Windows Runtime
    /// 4. Detect when those sessions are released or become inactive
    /// 5. Apply 500ms grace period before declaring call ended
    /// 
    /// Monitors both devices because when we switch default mic to CABLE, Phone Link may
    /// use either the original physical mic or the new CABLE device depending on timing.
    /// 
    /// This prevents false positives from other applications using the microphone.
    /// </summary>
    /// <returns>True if Phone Link is actively using either microphone device; false otherwise.</returns>
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
                _lastPhoneLinkSessionTicks = 0;
                return false;
            }

            // Phone Link is running - check for active sessions on BOTH devices
            var currentPhoneLinkSessions = new HashSet<int>();
            
            try
            {
                var enumerator = new MMDeviceEnumerator();
                
                // Check physical microphone device
                CheckDeviceForPhoneLinkSessions(enumerator, _deviceId, "Physical Mic", phoneExperienceIds, currentPhoneLinkSessions);
                
                // Also check cable capture device if provided
                if (!string.IsNullOrEmpty(_cableDeviceId))
                {
                    CheckDeviceForPhoneLinkSessions(enumerator, _cableDeviceId, "Cable Capture", phoneExperienceIds, currentPhoneLinkSessions);
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
                    // Update the timestamp - we just saw a Phone Link session
                    _lastPhoneLinkSessionTicks = DateTime.UtcNow.Ticks;
                    _logger.LogInformation("Phone Link is actively using microphone ({SessionCount} session{S})", 
                        currentPhoneLinkSessions.Count,
                        currentPhoneLinkSessions.Count != 1 ? "s" : "");
                    return true;
                }
                else
                {
                    // No active sessions - check if we're still within grace period
                    if (_lastPhoneLinkSessionTicks > 0)
                    {
                        long elapsedMs = (DateTime.UtcNow.Ticks - _lastPhoneLinkSessionTicks) / TimeSpan.TicksPerMillisecond;
                        if (elapsedMs < GRACE_PERIOD_MS)
                        {
                            _logger.LogDebug("Phone Link sessions inactive, grace period active ({ElapsedMs}ms of {GracePeriodMs}ms)", elapsedMs, GRACE_PERIOD_MS);
                            return true;  // Still consider call active during grace period
                        }
                    }
                    
                    _logger.LogDebug("Phone Link running but no active microphone sessions (grace period expired)");
                    _lastPhoneLinkSessionTicks = 0;
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
    /// Checks a specific device for Phone Link sessions.
    /// </summary>
    private void CheckDeviceForPhoneLinkSessions(MMDeviceEnumerator enumerator, string deviceId, string deviceLabel, 
        HashSet<int> phoneExperienceIds, HashSet<int> currentPhoneLinkSessions)
    {
        try
        {
            MMDevice device = enumerator.GetDevice(deviceId);
            
            if (device?.AudioSessionManager == null)
            {
                _logger.LogDebug("{DeviceLabel}: Cannot access audio session manager", deviceLabel);
                return;
            }

            var sessionEnumerator = device.AudioSessionManager.Sessions;
            _logger.LogDebug("{DeviceLabel}: {Count} audio sessions", deviceLabel, sessionEnumerator.Count);

            for (int i = 0; i < sessionEnumerator.Count; i++)
            {
                try
                {
                    var session = sessionEnumerator[i];
                    
                    // Skip system sounds
                    if (session.IsSystemSoundsSession)
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] System sounds (skipped)", deviceLabel, i);
                        continue;
                    }

                    // Only consider ACTIVE sessions (state 1)
                    int sessionState = (int)session.State;
                    if (sessionState != 1)
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Inactive session, state: {State} (skipped)", deviceLabel, i, sessionState);
                        continue;
                    }

                    // Get session process ID
                    uint sessionPid = session.GetProcessID;
                    string displayName = string.Empty;
                    try
                    {
                        displayName = session.DisplayName ?? string.Empty;
                    }
                    catch { }

                    _logger.LogDebug("  [{DeviceLabel} Session {Index}] Active - Process ID: {Pid}, DisplayName: '{DisplayName}'", 
                        deviceLabel, i, sessionPid, displayName);

                    // Skip our own process
                    if (sessionPid == _ourProcessId)
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Our process (skipped)", deviceLabel, i);
                        continue;
                    }

                    // Check if session belongs to Phone Link directly
                    if (phoneExperienceIds.Contains((int)sessionPid))
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Phone Link process detected", deviceLabel, i);
                        currentPhoneLinkSessions.Add(i);
                    }
                    // Check DisplayName for Phone Link identifiers
                    else if (IsPhoneLinkSession(displayName))
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Phone Link identified by DisplayName: '{DisplayName}'", deviceLabel, i, displayName);
                        currentPhoneLinkSessions.Add(i);
                    }
                    // Check if session belongs to svchost (Windows Runtime host used by Phone Link)
                    else if (IsServiceHostRelatedToPhoneLink((int)sessionPid, displayName))
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Windows Runtime/svchost related to Phone Link", deviceLabel, i);
                        currentPhoneLinkSessions.Add(i);
                    }
                    // Skip other applications' sessions
                    else
                    {
                        var process = ProcessById((int)sessionPid);
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Other app: {ProcessName}, DisplayName: '{DisplayName}' (skipped)", 
                            deviceLabel, i, process?.ProcessName ?? "unknown", displayName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[{DeviceLabel}] Error checking session at index {Index}", deviceLabel, i);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{DeviceLabel}] Error checking device for Phone Link sessions", deviceLabel);
        }
    }

    /// <summary>
    /// Checks if a session DisplayName indicates it belongs to Phone Link.
    /// Phone Link sessions often have identifying information in their DisplayName.
    /// </summary>
    private bool IsPhoneLinkSession(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return false;

        // Check for Phone Link identifiers in DisplayName
        // Phone Link, PhoneExperienceHost, call-related names
        var lowerName = displayName.ToLowerInvariant();
        return lowerName.Contains("phone") || 
               lowerName.Contains("call") || 
               lowerName.Contains("experience");
    }

    /// <summary>
    /// Checks if a svchost.exe process is related to Phone Link / Windows Runtime.
    /// Phone Link uses Windows Runtime which runs in svchost.exe processes.
    /// Enhanced to also check DisplayName for Phone Link identifiers.
    /// This is heuristic-based: if svchost is running and Phone Link is running,
    /// any active microphone session in svchost is likely Phone Link.
    /// </summary>
    private bool IsServiceHostRelatedToPhoneLink(int processId, string displayName)
    {
        try
        {
            var process = ProcessById(processId);
            if (process == null)
                return false;

            // Only consider svchost processes as Phone Link-related
            if (!process.ProcessName.Equals("svchost", StringComparison.OrdinalIgnoreCase))
                return false;

            // Check if DisplayName indicates Phone Link
            if (IsPhoneLinkSession(displayName))
                return true;

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
