# Testing Guide

This document describes how to run the test suite for the Microphone Passthrough project.

## Test Suite Overview

The project includes **15 tests** organized into three categories:

### 1. CLI Argument Parsing Tests (6 tests) ✅ Always Run
These validate command-line argument parsing with the CommandLineParser library:
- Help flag (`--help`)
- Version flag (`--version`)
- List devices flag (`--list-devices`)
- Microphone argument (`--mic`)
- Multiple arguments combined
- Verbose flag (`--verbose`)

**Run with:** `dotnet test --filter "OptionsParsingTests"`

### 2. Unit Tests with Mocks (5 tests) ✅ Always Run
These use Moq to validate core logic without requiring hardware:

- **AudioDeviceManager_Constructor_RequiresLogger**: Validates null argument handling
- **PassthroughApplication_Constructor_RequiresLogger**: Validates constructor contracts
- **PassthroughApplication_Constructor_RequiresDeviceManager**: Validates constructor contracts
- **PassthroughApplication_Run_WithListDevicesFlag_ListsDevicesAndReturnsZero**: Validates device listing
- **PassthroughApplication_Run_WithoutMicrophone_ReturnsNonZero**: Validates error handling

**Run with:** `dotnet test --filter "PassthroughApplicationIntegrationTests"`

### 3. Hardware Integration Tests (4 tests) ⏸️ Skipped by Default
These require actual Windows WASAPI audio devices and VB-Audio Virtual Cable:

#### Prerequisites for Hardware Tests:
1. **Windows 10/11** with working audio subsystem
2. **VB-Audio Virtual Cable** installed ([download here](https://vb-audio.com/Cable/))
3. At least one audio **input device** (microphone)
4. At least one audio **output device** (speakers/headphones)

## How to Run Hardware Tests

Hardware tests are **conditionally skipped** based on the `RUN_HARDWARE_TESTS` environment variable using a custom `[ConditionalHardwareTest]` attribute.

### Setting the Environment Variable

**Windows PowerShell:**
```powershell
$env:RUN_HARDWARE_TESTS = "1"
dotnet test
```

**Windows Command Prompt:**
```batch
set RUN_HARDWARE_TESTS=1
dotnet test
```

**Linux/macOS (Bash/Zsh):**
```bash
export RUN_HARDWARE_TESTS=1
dotnet test
```

### What Happens

- **Without `RUN_HARDWARE_TESTS` set:** 11 tests pass, 4 hardware tests are skipped ✅
- **With `RUN_HARDWARE_TESTS=1`:** All 15 tests run
  - Tests will pass if devices are configured correctly
  - Tests will fail gracefully if devices are missing (expected behavior)

#### Hardware Tests Description:

1. **AudioDeviceManager_CanInitialize_Hardware**
   - Tests that AudioDeviceManager can initialize with real WASAPI
   - Validates MMDeviceEnumerator initialization
   - **Expected result:** Pass on systems with audio devices

2. **AudioDeviceManager_FindDevice_WithValidDevice_ReturnsDevice_Hardware**
   - Attempts to locate "CABLE Input (VB-Audio Virtual Cable)" device
   - Falls back gracefully if VB-Audio not installed
   - **Expected result:** Pass if VB-Audio is installed; graceful failure otherwise

3. **PassthroughApplication_CanInitializeWithValidDevices_Hardware**
   - Full application initialization with real audio devices
   - Tests device discovery, WASAPI mode negotiation, and buffer config
   - Validates that audio engine initializes without crashing
   - **Expected result:** Pass with proper device setup

4. **PassthroughApplication_CanRouteAudio_Hardware**
   - Most comprehensive integration test
   - Routes actual audio through the system
   - Validates no buffer underruns, proper latency, reasonable CPU usage
   - **Expected result:** Pass if audio flows successfully end-to-end

## Running All Tests

```bash
# Run all tests (unit tests only, skips hardware tests by default)
dotnet test

# Run ALL tests including hardware integration tests
# Windows PowerShell:
$env:RUN_HARDWARE_TESTS = "1"
dotnet test

# Windows Command Prompt:
set RUN_HARDWARE_TESTS=1
dotnet test

# Linux/macOS:
export RUN_HARDWARE_TESTS=1
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "OptionsParsingTests"

# Run only hardware tests
dotnet test --filter "_Hardware"

# Run tests with detailed logging
dotnet test --logger "console;verbosity=detailed"

# Run tests matching a pattern
dotnet test --filter "Constructor"
```

## Test Results Interpretation

### Success
```
Test Run Successful.
Total tests: 15
     Passed: 11
    Skipped: 4
```

This means:
- 6 CLI parsing tests passed ✅
- 5 unit tests with mocks passed ✅
- 4 hardware tests are skipped (not enabled) ⏸️

### With Hardware Tests Enabled
```
Test Run Successful.
Total tests: 15
     Passed: 15
    Skipped: 0
```

This means all hardware tests passed ✅

### Common Hardware Test Failures

**"Device not found"**
- The specified audio device doesn't exist on your system
- Run `dotnet test --filter "CommandLine_ListDevices"` to see available devices
- Update the device names in the hardware test to match your system

**"WASAPI error"**
- Windows audio system issue
- Try: Restart audio services, check device drivers, close exclusive audio apps

**"CABLE Input not found"**
- VB-Audio Virtual Cable not installed
- Install from: https://vb-audio.com/Cable/
- The test will gracefully handle this with expected error handling

## Architecture Notes

Tests use:
- **xUnit** for test framework
- **Moq** for mocking dependencies
- **CommandLineParser** validation integrated with real CLI parsing
- **NAudio** WASAPI access for real device enumeration

Unit tests mock AudioDeviceManager dependencies, while integration tests use real WASAPI for authentic testing on your hardware.

## Continuous Integration

In CI/CD pipelines, hardware tests are skipped automatically since `RUN_HARDWARE_TESTS` is not set. Only the 11 portable tests run:
- 6 CLI parsing tests
- 5 unit tests with mocks

This ensures fast, reliable CI builds without requiring audio hardware in the build environment.
