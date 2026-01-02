using Xunit;

namespace MicPassthrough.Tests
{
    /// <summary>
    /// Tests for Options class structure and default values.
    /// Command-line parsing is now handled by System.CommandLine and tested via integration tests.
    /// </summary>
    public class OptionsParsingTests
    {
        [Fact]
        public void Options_Constructor_SetsCorrectDefaults()
        {
            var options = new Options();

            Assert.Equal("CABLE Input (VB-Audio Virtual Cable)", options.CableRender);
            Assert.Equal("CABLE Output (VB-Audio Virtual Cable)", options.CableCapture);
            Assert.Equal(100, options.Buffer);
            Assert.True(options.ExclusiveMode);
            Assert.Equal(3, options.PrebufferFrames);
            Assert.False(options.EnableMonitor);
            Assert.False(options.ListDevices);
            Assert.False(options.Verbose);
            Assert.False(options.AutoSwitch);
            Assert.Equal("PhoneExperienceHost", options.TargetProcessName);
        }

        [Fact]
        public void Options_CanSetMicrophone()
        {
            var options = new Options { Mic = "Test Microphone" };
            Assert.Equal("Test Microphone", options.Mic);
        }

        [Fact]
        public void Options_CanSetCableRender()
        {
            var options = new Options { CableRender = "Custom Cable" };
            Assert.Equal("Custom Cable", options.CableRender);
        }

        [Fact]
        public void Options_CanSetCableCapture()
        {
            var options = new Options { CableCapture = "Custom Capture" };
            Assert.Equal("Custom Capture", options.CableCapture);
        }

        [Fact]
        public void Options_CanSetBuffer()
        {
            var options = new Options { Buffer = 150 };
            Assert.Equal(150, options.Buffer);
        }

        [Fact]
        public void Options_CanSetVerbose()
        {
            var options = new Options { Verbose = true };
            Assert.True(options.Verbose);
        }

        [Fact]
        public void Options_CanSetTargetProcessName()
        {
            var options = new Options { TargetProcessName = "Skype" };
            Assert.Equal("Skype", options.TargetProcessName);
        }
    }
}
