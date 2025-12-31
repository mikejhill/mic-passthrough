using System;
using Xunit;

namespace MicPassthrough.Tests
{
    /// <summary>
    /// Custom Fact attribute that conditionally skips hardware integration tests
    /// based on the RUN_HARDWARE_TESTS environment variable.
    /// 
    /// Usage: [ConditionalHardwareTest] instead of [Fact]
    /// 
    /// To enable hardware tests, set environment variable before running tests:
    /// Windows PowerShell: $env:RUN_HARDWARE_TESTS = "1"
    /// Windows CMD: set RUN_HARDWARE_TESTS=1
    /// Linux/Mac: export RUN_HARDWARE_TESTS=1
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ConditionalHardwareTestAttribute : FactAttribute
    {
        private static readonly bool RunHardwareTests =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUN_HARDWARE_TESTS"));

        public ConditionalHardwareTestAttribute(string skipReason = "Requires Windows audio device availability - set RUN_HARDWARE_TESTS=1")
        {
            if (!RunHardwareTests)
            {
                Skip = skipReason;
            }
        }
    }
}
