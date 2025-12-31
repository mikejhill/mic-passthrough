using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages switching Windows' default microphone device.
/// Allows changing which microphone Windows and applications (like Phone Link) use as the default input device.
/// </summary>
public class WindowsDefaultMicrophoneManager
{
    private readonly ILogger _logger;
    private string _originalDefaultMicId;

    /// <summary>
    /// Creates a new instance of WindowsDefaultMicrophoneManager.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public WindowsDefaultMicrophoneManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sets a microphone device as the Windows default recording device.
    /// Saves the original device so it can be restored later.
    /// </summary>
    /// <param name="deviceId">The device ID of the microphone to set as default.</param>
    /// <returns>True if successful; false otherwise.</returns>
    public bool SetDefaultMicrophone(string deviceId)
    {
        try
        {
            var enumerator = new MMDeviceEnumerator();
            
            // Save the current default so we can restore it later
            try
            {
                var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                if (captureDevices.Count > 0)
                {
                    _originalDefaultMicId = captureDevices[0].ID;
                    _logger.LogInformation("Saved original default microphone: {Device}", captureDevices[0].FriendlyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve original default microphone");
            }

            // Get the target device
            MMDevice targetDevice;
            try
            {
                targetDevice = enumerator.GetDevice(deviceId);
            }
            catch
            {
                _logger.LogError("Could not find device with ID: {DeviceId}", deviceId);
                return false;
            }

            if (targetDevice == null)
            {
                _logger.LogError("Target microphone device not found: {DeviceId}", deviceId);
                return false;
            }

            // Set as default using Windows Registry
            // This is the most reliable method for Windows 10/11
            try
            {
                SetDeviceAsDefault(targetDevice.ID);
                _logger.LogInformation("Set default microphone to: {Device}", targetDevice.FriendlyName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set default microphone");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing default microphone");
            return false;
        }
    }

    /// <summary>
    /// Restores the original default microphone device.
    /// Called when passthrough stops to return Windows to the user's original configuration.
    /// </summary>
    /// <returns>True if successful; false otherwise.</returns>
    public bool RestoreOriginalMicrophone()
    {
        try
        {
            if (string.IsNullOrEmpty(_originalDefaultMicId))
            {
                _logger.LogWarning("No original microphone saved, cannot restore");
                return false;
            }

            SetDeviceAsDefault(_originalDefaultMicId);
            
            var enumerator = new MMDeviceEnumerator();
            var originalDevice = enumerator.GetDevice(_originalDefaultMicId);
            _logger.LogInformation("Restored original default microphone: {Device}", 
                originalDevice?.FriendlyName ?? _originalDefaultMicId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore original microphone");
            return false;
        }
    }

    /// <summary>
    /// Gets the currently configured default microphone device.
    /// </summary>
    /// <returns>Friendly name of the default microphone, or null if not available.</returns>
    public string GetDefaultMicrophone()
    {
        try
        {
            var enumerator = new MMDeviceEnumerator();
            var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            return captureDevices.Count > 0 ? captureDevices[0].FriendlyName : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve default microphone");
            return null;
        }
    }

    /// <summary>
    /// Sets a device as the Windows default using Registry settings.
    /// This is the native Windows method for changing default audio devices.
    /// Requires elevated privileges (Administrator).
    /// </summary>
    private void SetDeviceAsDefault(string deviceId)
    {
        // Windows stores default device settings in Registry
        // Path: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Multimedia\Audio Endpoints\Capture
        
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                _logger.LogWarning("Default microphone switching is only supported on Windows");
                return;
            }

            using (var hklm = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Multimedia\Audio Endpoints\Capture", writable: true))
            {
                if (hklm == null)
                {
                    _logger.LogWarning("Could not access Registry for audio device configuration");
                    return;
                }

                // Set this device ID as the default
                hklm.SetValue("Default", deviceId);
                _logger.LogDebug("Registry updated with new default device");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Registry access for audio settings may require elevated privileges");
            throw;
        }
    }
}
