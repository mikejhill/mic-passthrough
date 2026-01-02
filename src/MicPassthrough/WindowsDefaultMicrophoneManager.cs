using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using MicPassthrough;

/// <summary>
/// Manages switching Windows' default microphone device.
/// Allows changing which microphone Windows and applications use as the default input device.
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
            
            // Save the current Windows default microphone so we can restore it later
            // Try different roles to find the actual default
            try
            {
                MMDevice currentDefault = null;
                string foundVia = "";
                
                // Try Console role first (most common for user input devices)
                try
                {
                    currentDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                    foundVia = "Console role";
                }
                catch { }
                
                // If Console role didn't work, try Multimedia
                if (currentDefault == null)
                {
                    try
                    {
                        currentDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                        foundVia = "Multimedia role";
                    }
                    catch { }
                }
                
                // Last resort: Communications role
                if (currentDefault == null)
                {
                    try
                    {
                        currentDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                        foundVia = "Communications role";
                    }
                    catch { }
                }
                
                if (currentDefault != null)
                {
                    _originalDefaultMicId = currentDefault.ID;
                    _logger.LogInformation("Saved original default microphone ({FoundVia}): {Device} (ID: {DeviceId})", 
                        foundVia, currentDefault.FriendlyName, currentDefault.ID);
                }
                else
                {
                    _logger.LogWarning("Could not determine Windows default microphone using any role");
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

            _logger.LogInformation("Restoring original default microphone (ID: {OriginalId})", _originalDefaultMicId);
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
    /// Gets the currently configured Windows default microphone device.
    /// </summary>
    /// <returns>Friendly name of the default microphone, or null if not available.</returns>
    public string GetDefaultMicrophone()
    {
        try
        {
            var enumerator = new MMDeviceEnumerator();
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            return defaultDevice?.FriendlyName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve default microphone");
            return null;
        }
    }

    /// <summary>
    /// Sets a device as the Windows default using IPolicyConfig COM interface.
    /// This is the ONLY correct way to programmatically set Windows default devices.
    /// Registry approach does NOT work - Windows ignores those values.
    /// </summary>
    private void SetDeviceAsDefault(string deviceId)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Default microphone switching is only supported on Windows");
            return;
        }

        _logger.LogInformation("Attempting to set default microphone to device ID: {DeviceId}", deviceId);

        try
        {
            // Set as default for Console role (most applications)
            bool consoleSuccess = PolicyConfigClient.SetDefaultDevice(deviceId, ERole.eConsole);
            
            // Set as default for Communications role (VoIP apps)
            bool commSuccess = PolicyConfigClient.SetDefaultDevice(deviceId, ERole.eCommunications);
            
            // Set as default for Multimedia role (media playback apps)
            bool multimediaSuccess = PolicyConfigClient.SetDefaultDevice(deviceId, ERole.eMultimedia);

            if (consoleSuccess || commSuccess || multimediaSuccess)
            {
                _logger.LogInformation("Successfully set default microphone (Console: {Console}, Communications: {Comm}, Multimedia: {Multi})",
                    consoleSuccess, commSuccess, multimediaSuccess);
            }
            else
            {
                _logger.LogError("Failed to set default microphone for any role. COM interface may not be available.");
                throw new InvalidOperationException("Failed to set default device via IPolicyConfig");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default device via COM interface");
            throw;
        }
    }
}
