using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Monitors whether a target process is actively using specified audio devices.
/// Uses Windows Core Audio APIs to detect when applications open and close microphone sessions.
/// Monitors both the physical microphone and cable capture device to detect activity.
/// </summary>
public class ProcessAudioMonitor
{
    private readonly ILogger _logger;
    private readonly string _deviceId;  // Physical microphone device
    private readonly string _cableDeviceId;  // Cable capture device (optional)
    private readonly string _targetProcessName;
    private volatile bool _isDeviceInUse;
    private int _ourProcessId;
    private HashSet<int> _trackedProcessIds = new HashSet<int>();
    private HashSet<int> _lastSeenTargetSessions = new HashSet<int>();
    private long _lastTargetSessionTicks;  // Track when we last saw a target session
    private readonly string[] _defaultSessionKeywords = new[] { "phone", "call", "experience" };
    
    /// <summary>
    /// Grace period (in milliseconds) to wait after target sessions disappear before declaring the call ended.
    /// 
    /// Currently set to 0 (disabled) because dual-device monitoring eliminates the need for it.
    /// The target process can be detected on either the physical microphone or cable capture device
    /// without interruption from switching the default microphone device.
    /// 
    /// Kept as a constant for future use in case edge cases are discovered during testing 
    /// (e.g., if an app briefly loses all audio sessions during complex call scenarios).
    /// If re-enabled, a value of 500ms was the previous sweet spot.
    /// </summary>
    private const long GRACE_PERIOD_MS = 0;

    /// <summary>
    /// Creates a new instance of ProcessAudioMonitor.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="deviceId">Device ID to monitor (physical microphone).</param>
    /// <param name="cableDeviceId">Optional device ID for cable capture (CABLE Output device).</param>
    /// <param name="targetProcessName">Process name (without .exe) to monitor for audio sessions.</param>
    public ProcessAudioMonitor(ILogger logger, string deviceId, string cableDeviceId = null, string targetProcessName = "PhoneExperienceHost")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _cableDeviceId = cableDeviceId;  // May be null
        _targetProcessName = string.IsNullOrWhiteSpace(targetProcessName) ? "PhoneExperienceHost" : targetProcessName;
        _isDeviceInUse = false;
        _lastTargetSessionTicks = 0;
        // Store our own process ID so we can exclude our sessions
        _ourProcessId = Process.GetCurrentProcess().Id;
        _logger.LogDebug("ProcessAudioMonitor initialized (our process ID: {ProcessId})", _ourProcessId);
    }

    /// <summary>
    /// Gets whether the monitored device is currently in use by the target process.
    /// </summary>
    public bool IsDeviceInUse => _isDeviceInUse;

    /// <summary>
    /// Starts monitoring the device for target process activity.
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
    /// Checks every 500ms if the target process is actively using the device.
    /// Tracks sessions and detects when the process releases the microphone.
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
                        _logger.LogInformation("Target application released the microphone");
                    }
                    else if (!wasDeviceInUse && _isDeviceInUse)
                    {
                        _logger.LogInformation("Target application is actively using the microphone");
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
    /// Checks if the target process is actively using the monitored devices.
    /// Uses a multi-stage detection process:
    /// 1. Find the configured process IDs
    /// 2. Scan audio sessions on BOTH the physical mic AND cable capture device
    /// 3. Track which sessions belong to the target process/Windows Runtime
    /// 4. Detect when those sessions are released or become inactive
    /// 5. Apply 500ms grace period before declaring call ended
    /// 
    /// Monitors both devices because the target app may use either the original physical mic
    /// or the CABLE device depending on timing.
    /// 
    /// This prevents false positives from other applications using the microphone.
    /// </summary>
    /// <returns>True if the target process is actively using either monitored device; false otherwise.</returns>
    private bool CheckDeviceUsage()
    {
        try
        {
            // Get current target process IDs
            var targetProcesses = Process.GetProcessesByName(_targetProcessName);
            var targetProcessIds = new HashSet<int>(targetProcesses.Select(p => p.Id));

            _logger.LogDebug("Process check ({Process}): {Count} instance{S} running", 
                _targetProcessName,
                targetProcessIds.Count, 
                targetProcessIds.Count != 1 ? "s" : "");

            // Target not running at all
            if (targetProcessIds.Count == 0)
            {
                _lastSeenTargetSessions.Clear();
                _lastTargetSessionTicks = 0;
                return false;
            }

            // Target is running - check for active sessions on BOTH devices
            var currentTargetSessions = new HashSet<int>();
            
            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    // Check physical microphone device
                    CheckDeviceForTargetSessions(enumerator, _deviceId, "Physical Mic", targetProcessIds, currentTargetSessions);
                    
                    // Also check cable capture device if provided
                    if (!string.IsNullOrEmpty(_cableDeviceId))
                    {
                        CheckDeviceForTargetSessions(enumerator, _cableDeviceId, "Cable Capture", targetProcessIds, currentTargetSessions);
                    }
                }

                // Detect target session changes
                var newSessions = currentTargetSessions.Except(_lastSeenTargetSessions).ToList();
                var endedSessions = _lastSeenTargetSessions.Except(currentTargetSessions).ToList();

                if (newSessions.Count > 0)
                {
                    _logger.LogDebug("New target sessions: {Sessions}", string.Join(", ", newSessions));
                }
                if (endedSessions.Count > 0)
                {
                    _logger.LogDebug("Target sessions ended: {Sessions}", string.Join(", ", endedSessions));
                }

                _lastSeenTargetSessions = currentTargetSessions;

                // Target is in use if it has any active sessions
                if (currentTargetSessions.Count > 0)
                {
                    // Update the timestamp - we just saw a target session
                    _lastTargetSessionTicks = DateTime.UtcNow.Ticks;
                    _logger.LogDebug("Target process detected: {SessionCount} active session{S}", 
                        currentTargetSessions.Count,
                        currentTargetSessions.Count != 1 ? "s" : "");
                    return true;
                }
                else
                {
                    // No active sessions - check if we're still within grace period
                    if (_lastTargetSessionTicks > 0)
                    {
                        long elapsedMs = (DateTime.UtcNow.Ticks - _lastTargetSessionTicks) / TimeSpan.TicksPerMillisecond;
                        if (elapsedMs < GRACE_PERIOD_MS)
                        {
                            _logger.LogDebug("Target sessions inactive, grace period active ({ElapsedMs}ms of {GracePeriodMs}ms)", elapsedMs, GRACE_PERIOD_MS);
                            return true;  // Still consider call active during grace period
                        }
                    }
                    
                    _logger.LogDebug("Target sessions no longer active");
                    _lastTargetSessionTicks = 0;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking audio sessions");
                // If we can't check, assume the target process is using (safer for false negatives)
                return currentTargetSessions.Count > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error in device usage check");
            return false;
        }
    }

    /// <summary>
    /// Checks a specific device for target sessions.
    /// </summary>
    private void CheckDeviceForTargetSessions(MMDeviceEnumerator enumerator, string deviceId, string deviceLabel, 
        HashSet<int> targetProcessIds, HashSet<int> currentTargetSessions)
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

                    // Check if session belongs to target process directly
                    if (targetProcessIds.Contains((int)sessionPid))
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Target process detected", deviceLabel, i);
                        currentTargetSessions.Add(i);
                    }
                    // Check DisplayName for target identifiers
                    else if (IsTargetSession(displayName))
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Target identified by DisplayName: '{DisplayName}'", deviceLabel, i, displayName);
                        currentTargetSessions.Add(i);
                    }
                    // Check if session belongs to svchost (Windows Runtime host used by the target app)
                    else if (IsServiceHostRelatedToTarget((int)sessionPid, displayName))
                    {
                        _logger.LogDebug("  [{DeviceLabel} Session {Index}] Windows Runtime/svchost related to target process", deviceLabel, i);
                        currentTargetSessions.Add(i);
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
            _logger.LogWarning(ex, "[{DeviceLabel}] Error checking device for target sessions", deviceLabel);
        }
    }

    /// <summary>
    /// Checks if a session DisplayName indicates it belongs to the target process.
    /// </summary>
    private bool IsTargetSession(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return false;

        var lowerName = displayName.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(_targetProcessName) && lowerName.Contains(_targetProcessName.ToLowerInvariant()))
        {
            return true;
        }

        // Fallback to common call-related keywords for typical calling apps
        return _defaultSessionKeywords.Any(keyword => lowerName.Contains(keyword));
    }

    /// <summary>
    /// Checks if a svchost.exe process is related to the target / Windows Runtime.
    /// This is heuristic-based: if svchost is running and the target process is running,
    /// any active microphone session in svchost is likely part of the target pipeline.
    /// </summary>
    private bool IsServiceHostRelatedToTarget(int processId, string displayName)
    {
        try
        {
            var process = ProcessById(processId);
            if (process == null)
                return false;

            // Only consider svchost processes as target-related
            if (!process.ProcessName.Equals("svchost", StringComparison.OrdinalIgnoreCase))
                return false;

            // Check if DisplayName indicates the target
            if (IsTargetSession(displayName))
                return true;

            // svchost is target-related if the target is currently running
            var targetProcesses = Process.GetProcessesByName(_targetProcessName);
            return targetProcesses.Length > 0;
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
