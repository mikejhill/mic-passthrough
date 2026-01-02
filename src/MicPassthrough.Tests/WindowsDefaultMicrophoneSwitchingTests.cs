using Xunit;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System.Linq;
using System;

namespace MicPassthrough.Tests
{
    /// <summary>
    /// Tests for Windows default microphone switching functionality.
    /// Tests the COM-based approach using IPolicyConfig.
    /// </summary>
    public class WindowsDefaultMicrophoneSwitchingTests
    {
        [Fact]
        public void WindowsDefaultMicrophoneManager_Constructor_RequiresLogger()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WindowsDefaultMicrophoneManager(null));
        }

        [ConditionalHardwareTest]
        public void WindowsDefaultMicrophoneManager_SetDefaultMicrophone_WithValidDevice_SwitchesDefault_Hardware()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            var logger = loggerFactory.CreateLogger<WindowsDefaultMicrophoneSwitchingTests>();
            var manager = new WindowsDefaultMicrophoneManager(logger);
            
            // Get all recording devices
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
            
            if (devices.Count < 2)
            {
                Assert.Fail($"Need at least 2 recording devices to test switching, but only found {devices.Count}. Install VB-Audio Virtual Cable or additional audio devices.");
            }

            // Get original default device
            MMDevice originalDefault = null;
            try
            {
                originalDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            }
            catch (Exception ex)
            {
                Assert.Fail($"No default recording device configured: {ex.Message}");
            }

            // Find a different device to switch to
            var targetDevice = devices.FirstOrDefault(d => d.ID != originalDefault.ID);
            if (targetDevice == null)
            {
                Assert.Fail($"Could not find a different device to switch to. Only device: {originalDefault.FriendlyName}");
            }

            logger.LogInformation("Original default: {Original}", originalDefault.FriendlyName);
            logger.LogInformation("Target device: {Target}", targetDevice.FriendlyName);

            try
            {
                // Act: Switch to target device
                bool setResult = manager.SetDefaultMicrophone(targetDevice.ID);
                
                // Assert: SetDefaultMicrophone should succeed
                Assert.True(setResult, "SetDefaultMicrophone should return true");

                // Wait a moment for Windows to update
                System.Threading.Thread.Sleep(500);

                // Verify the switch actually happened
                var newDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                logger.LogInformation("New default: {NewDefault}", newDefault.FriendlyName);
                
                Assert.Equal(targetDevice.ID, newDefault.ID);
                Assert.NotEqual(originalDefault.ID, newDefault.ID);

                logger.LogInformation("✅ Successfully switched default microphone!");
            }
            finally
            {
                // Cleanup: Restore original default
                logger.LogInformation("Restoring original default microphone...");
                bool restoreResult = manager.RestoreOriginalMicrophone();
                Assert.True(restoreResult, "RestoreOriginalMicrophone should return true");

                // Wait for restoration
                System.Threading.Thread.Sleep(500);

                // Verify restoration
                var restoredDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                logger.LogInformation("Restored default: {Restored}", restoredDefault.FriendlyName);
                Assert.Equal(originalDefault.ID, restoredDefault.ID);
                
                logger.LogInformation("✅ Successfully restored original microphone!");
            }
        }

        [ConditionalHardwareTest]
        public void WindowsDefaultMicrophoneManager_SetAndRestore_MultipleTimes_ConsistentBehavior_Hardware()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            var logger = loggerFactory.CreateLogger<WindowsDefaultMicrophoneSwitchingTests>();
            var manager = new WindowsDefaultMicrophoneManager(logger);
            
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
            
            if (devices.Count < 2)
            {
                Assert.Fail($"Need at least 2 recording devices, but only found {devices.Count}. Install VB-Audio Virtual Cable or additional audio devices.");
            }

            MMDevice originalDefault = null;
            try
            {
                originalDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            }
            catch (Exception ex)
            {
                Assert.Fail($"No default recording device configured: {ex.Message}");
            }

            var targetDevice = devices.FirstOrDefault(d => d.ID != originalDefault.ID);
            if (targetDevice == null)
            {
                Assert.Fail($"Could not find a different device to switch to. Only device: {originalDefault.FriendlyName}");
            }

            try
            {
                // Act: Switch multiple times
                for (int i = 0; i < 3; i++)
                {
                    logger.LogInformation("Iteration {Iteration}: Switching to target device", i + 1);
                    bool setResult = manager.SetDefaultMicrophone(targetDevice.ID);
                    Assert.True(setResult, $"Iteration {i + 1}: SetDefaultMicrophone should return true");
                    
                    System.Threading.Thread.Sleep(300);
                    
                    var newDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                    Assert.Equal(targetDevice.ID, newDefault.ID);
                    
                    logger.LogInformation("Iteration {Iteration}: Restoring original", i + 1);
                    bool restoreResult = manager.RestoreOriginalMicrophone();
                    Assert.True(restoreResult, $"Iteration {i + 1}: RestoreOriginalMicrophone should return true");
                    
                    System.Threading.Thread.Sleep(300);
                    
                    var restoredDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                    Assert.Equal(originalDefault.ID, restoredDefault.ID);
                }
                
                logger.LogInformation("✅ Multiple switches succeeded!");
            }
            finally
            {
                // Final cleanup
                manager.RestoreOriginalMicrophone();
            }
        }

        [ConditionalHardwareTest]
        public void PolicyConfigClient_SetDefaultDevice_WithValidDevice_ReturnsTrue_Hardware()
        {
            // Arrange
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
            
            if (devices.Count == 0)
            {
                Assert.Fail("No audio capture devices available. Install audio devices or VB-Audio Virtual Cable.");
            }

            var testDevice = devices.First();
            
            // Act
            bool result = PolicyConfigClient.SetDefaultDevice(testDevice.ID, ERole.eConsole);
            
            // Assert
            Assert.True(result, "PolicyConfigClient.SetDefaultDevice should return true for valid device");

            // Verify it actually changed
            System.Threading.Thread.Sleep(200);
            var newDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            Assert.Equal(testDevice.ID, newDefault.ID);
        }
    }
}
