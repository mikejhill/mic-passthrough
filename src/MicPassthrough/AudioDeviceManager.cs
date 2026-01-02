using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages audio device enumeration and discovery for the passthrough application.
/// </summary>
public class AudioDeviceManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly MMDeviceEnumerator _enumerator;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of AudioDeviceManager.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    public AudioDeviceManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enumerator = new MMDeviceEnumerator();
    }

    /// <summary>
    /// Finds a device by exact name match within a specified flow direction.
    /// </summary>
    /// <param name="flow">The audio flow direction (Capture or Render).</param>
    /// <param name="exactName">The exact device name to search for.</param>
    /// <returns>The matching audio device.</returns>
    /// <exception cref="Exception">Thrown when the device is not found.</exception>
    public MMDevice FindDevice(DataFlow flow, string exactName)
    {
        _logger.LogDebug("Searching for {Flow} device with exact name '{DeviceName}'", flow, exactName);

        var device = _enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active)
            .FirstOrDefault(x => x.FriendlyName.Equals(exactName, StringComparison.Ordinal));

        if (device == null)
        {
            _logger.LogError("Device not found: {DeviceName}", exactName);
            Console.WriteLine($"ERROR: Could not find {flow} device with exact name '{exactName}'");
            PrintAvailableDevices(flow);
            throw new Exception($"Device not found: {exactName}");
        }

        _logger.LogDebug("Found device: {DeviceName}", device.FriendlyName);
        return device;
    }

    /// <summary>
    /// Lists all available audio devices organized by flow direction.
    /// </summary>
    public void ListAllDevices()
    {
        Console.WriteLine("\n=== CAPTURE DEVICES (Microphones) ===");
        foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            Console.WriteLine($"  {device.FriendlyName}");
        }

        Console.WriteLine("\n=== RENDER DEVICES (Outputs) ===");
        foreach (var device in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            Console.WriteLine($"  {device.FriendlyName}");
        }

        Console.WriteLine("\nUsage example:");
        Console.WriteLine("  MicPassthrough --mic \"Microphone (HD Pro Webcam C920)\" --cable \"CABLE Input (VB-Audio Virtual Cable)\"");
    }

    /// <summary>
    /// Prints available devices for a given flow direction to the console.
    /// </summary>
    /// <param name="flow">The audio flow direction to list devices for.</param>
    private void PrintAvailableDevices(DataFlow flow)
    {
        Console.WriteLine($"\nAvailable {flow} devices:");
        foreach (var device in _enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            Console.WriteLine($"  {device.FriendlyName}");
        }
        Console.WriteLine("\nTip: Use --list-devices to see all device names");
    }

    /// <summary>
    /// Releases the MMDeviceEnumerator COM object.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _enumerator?.Dispose();
        _disposed = true;
    }
}
