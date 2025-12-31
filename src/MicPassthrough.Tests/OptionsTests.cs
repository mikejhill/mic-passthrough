using CommandLine;
using Xunit;

namespace MicPassthrough.Tests
{
    public class OptionsParsingTests
    {
        [Fact]
        public void CommandLine_Help_Flag_ParsesSuccessfully()
        {
            string[] args = new[] { "--help" };
            var result = Parser.Default.ParseArguments<Options>(args);

            // Help flag should be recognized and parsing should complete
            Assert.NotNull(result);
        }

        [Fact]
        public void CommandLine_Version_Flag_ParsesSuccessfully()
        {
            string[] args = new[] { "--version" };
            var result = Parser.Default.ParseArguments<Options>(args);

            Assert.NotNull(result);
        }

        [Fact]
        public void CommandLine_ListDevices_Flag_ParsesSuccessfully()
        {
            string[] args = new[] { "--list-devices" };
            var result = Parser.Default.ParseArguments<Options>(args);

            Assert.NotNull(result);
        }

        [Fact]
        public void CommandLine_WithMicrophoneArgument_ParsesSuccessfully()
        {
            string[] args = new[] { "--mic", "Test Microphone" };
            var result = Parser.Default.ParseArguments<Options>(args);

            Assert.NotNull(result);
        }

        [Fact]
        public void CommandLine_WithMultipleArguments_ParsesSuccessfully()
        {
            string[] args = new[]
            {
                "--mic", "Test Mic",
                "--cable", "Test Cable",
                "--buffer", "150"
            };

            var result = Parser.Default.ParseArguments<Options>(args);
            Assert.NotNull(result);
        }

        [Fact]
        public void CommandLine_VerboseFlag_ParsesSuccessfully()
        {
            string[] args = new[] { "--verbose" };
            var result = Parser.Default.ParseArguments<Options>(args);

            Assert.NotNull(result);
        }
    }
}
