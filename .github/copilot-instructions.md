# GitHub Copilot Instructions for MicPassthrough

This file provides guidelines for GitHub Copilot when generating code and documentation for the MicPassthrough project.

## Project Overview

**MicPassthrough** is a low-latency audio passthrough application that routes microphone input to [VB-Audio Virtual Cable](https://vb-audio.com/Cable/) using Windows WASAPI APIs. It solves the Phone Link USB microphone volume bug on Windows 10/11.

- **Language:** C#
- **Target Framework:** .NET 10.0
- **Type:** Console application (CLI)
- **License:** MIT
- **Status:** v1.0.0 released, active development

## Project Structure

```
MicPassthrough/
├── .github/
│   └── workflows/              # GitHub Actions CI/CD
│       ├── ci.yml              # Runs on push/PR
│       └── release.yml          # Runs on version tags (v*.*.*)
├── src/
│   ├── MicPassthrough/          # Main application
│   │   ├── Program.cs           # Entry point
│   │   ├── Options.cs           # CLI argument definitions
│   │   ├── AudioDeviceManager.cs # Device enumeration & WASAPI setup
│   │   ├── PassthroughApplication.cs # Core business logic
│   │   ├── PassthroughEngine.cs # Low-level audio passthrough
│   │   └── MicPassthrough.csproj # Version source
│   └── MicPassthrough.Tests/    # Test suite (15 tests)
│       ├── OptionsParsingTests.cs # 6 CLI argument tests
│       ├── AudioDeviceManagerTests.cs # Audio device tests
│       ├── PassthroughApplicationTests.cs # Application logic tests
│       ├── ConditionalHardwareTestAttribute.cs # Custom xUnit attribute
│       └── MicPassthrough.Tests.csproj
├── docs/
│   ├── README.md                # Documentation index
│   ├── QUICK_RELEASE.md         # 1-page release checklist
│   ├── RELEASE_GUIDE.md         # Complete release walkthrough
│   ├── VERSIONING.md            # Semantic versioning strategy
│   ├── WORKFLOWS.md             # GitHub Actions workflow diagrams
│   ├── CI_CD.md                 # CI/CD configuration details
│   ├── architecture/
│   │   └── REFACTORING.md       # Architecture decisions & history
│   └── adr/
│       └── template.md           # ADR template (MADR format)
├── README.md                    # Main project documentation
├── TESTING.md                   # Test suite documentation
├── CHANGELOG.md                 # Release history (Keep a Changelog format)
├── LICENSE                      # MIT license
└── MicPassthrough.sln           # Visual Studio solution file
```

## Code Style & Conventions

### C# Code Style

1. **Naming Conventions:**
   - **Classes:** PascalCase (e.g., `AudioDeviceManager`, `PassthroughApplication`)
   - **Methods:** PascalCase (e.g., `FindDevice()`, `RunApplication()`)
   - **Properties:** PascalCase (e.g., `DeviceId`, `BufferSize`)
   - **Local variables:** camelCase (e.g., `deviceManager`, `loggerFactory`)
   - **Private fields:** _camelCase prefix (e.g., `_logger`, `_waveInEvent`)
   - **Constants:** UPPER_SNAKE_CASE or PascalCase

2. **Code Organization:**
   - Use XML documentation comments (`///`) for public classes, methods, and properties
   - Keep methods focused and single-purpose
   - Use dependency injection (constructor parameters) for dependencies
   - Use `using` statements for resource management
   - Prefer `ImplicitUsings` enabled (global `using` statements)
   - Prefer nullable reference types disabled (`<Nullable>disable</Nullable>`)

3. **WASAPI & Audio Code:**
   - Use NAudio library (v2.2.1+) for WASAPI abstractions
   - Document buffer sizes, latency expectations, and audio format details
   - Include try-catch blocks for audio device operations (devices can be disconnected)
   - Log significant audio events (initialization, device changes, buffer issues)

4. **Logging:**
   - Use `Microsoft.Extensions.Logging` (v9.0.0+)
   - Log at appropriate levels: Debug (verbose info), Information (general progress), Warning (issues), Error (failures)
   - Include context in log messages (device names, buffer sizes, error codes)

### Example Code Pattern

```csharp
/// <summary>
/// Initializes audio device and prepares for passthrough.
/// </summary>
/// <param name="deviceId">The target audio device ID.</param>
/// <returns>True if initialization succeeded; otherwise, false.</returns>
public bool Initialize(string deviceId)
{
    try
    {
        _logger.LogInformation("Initializing audio device: {DeviceId}", deviceId);
        // Implementation
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to initialize audio device");
        return false;
    }
}
```

## Testing Standards

### Test Organization

- **Unit Tests:** Use Moq (v4.20.70+) for mocking dependencies
- **Hardware Tests:** Conditional on `RUN_HARDWARE_TESTS` environment variable
- **Test Attribute:** `[ConditionalHardwareTest]` auto-skips if env var not set
- **Framework:** xUnit (v2.6.6+)

### Test Files

1. **OptionsParsingTests.cs** (6 tests)
   - Validate CLI argument parsing with CommandLineParser
   - Test all flags and options

2. **AudioDeviceManagerTests.cs** (3 tests)
   - Constructor validation (dependency injection)
   - Hardware initialization (conditional)
   - Device discovery (conditional)

3. **PassthroughApplicationTests.cs** (5 tests)
   - Constructor validation
   - Device listing functionality
   - Error handling (missing microphone)
   - Audio routing (conditional hardware tests)

4. **ConditionalHardwareTestAttribute.cs**
   - Custom xUnit attribute for conditional test execution
   - Reads `RUN_HARDWARE_TESTS` environment variable
   - Skip logic: if not set, test is skipped; if set, test runs

### Running Tests

```bash
# All unit tests (skips hardware tests)
dotnet test

# Only hardware tests (requires env var)
$env:RUN_HARDWARE_TESTS = "1"
dotnet test --filter "_Hardware"

# All tests with hardware
$env:RUN_HARDWARE_TESTS = "1"
dotnet test
```

## Dependencies & Versions

### Runtime Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| NAudio | 2.2.1+ | WASAPI audio abstraction |
| CommandLineParser | 2.9.1+ | CLI argument parsing |
| Microsoft.Extensions.Logging | 9.0.0+ | Structured logging |
| Microsoft.Extensions.Logging.Console | 9.0.0+ | Console log output |

### Test Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| xUnit | 2.6.6+ | Test framework |
| Moq | 4.20.70+ | Mocking library |

### Build Requirements

- **.NET SDK 10.0+** - Compilation and execution
- **Visual Studio 2022** or **VS Code** with C# extensions (recommended)

## Documentation Standards

### Markdown Formatting

1. **Keep a Changelog Format:**
   - Used in [CHANGELOG.md](../CHANGELOG.md)
   - Structure: `## [Version] - YYYY-MM-DD` with Added/Fixed/Changed sections
   - Maintain [Unreleased] section at top

2. **Semantic Versioning:**
   - Format: `MAJOR.MINOR.PATCH` (e.g., 1.0.0, 1.0.1, 1.1.0, 2.0.0)
   - PATCH: Bug fixes and non-breaking changes
   - MINOR: New features (backward compatible)
   - MAJOR: Breaking changes
   - See [docs/VERSIONING.md](../docs/VERSIONING.md) for details

3. **Architecture Decision Records (ADRs):**
   - Use MADR (Markdown Any Decision Records) format
   - File naming: `adr/XXXX-descriptive-title.md` (sequential numbering)
   - Status options: proposed, rejected, accepted, deprecated, superseded by ADR-XXXX
   - Template: [adr/template.md](../docs/adr/template.md)

4. **Documentation Location Rules:**
   - **Project overview & getting started:** [README.md](../README.md)
   - **Test documentation:** [TESTING.md](../TESTING.md)
   - **Release process:** [docs/QUICK_RELEASE.md](../docs/QUICK_RELEASE.md)
   - **Detailed release walkthrough:** [docs/RELEASE_GUIDE.md](../docs/RELEASE_GUIDE.md)
   - **Version strategy:** [docs/VERSIONING.md](../docs/VERSIONING.md)
   - **GitHub Actions workflows:** [docs/WORKFLOWS.md](../docs/WORKFLOWS.md)
   - **Architecture & refactoring notes:** [docs/architecture/REFACTORING.md](../docs/architecture/REFACTORING.md)

## Release & Versioning

### Version Source

- **Single source of truth:** `src/MicPassthrough/MicPassthrough.csproj`
  ```xml
  <Version>1.0.0</Version>
  ```

### Release Process

1. **Update Version Files:**
   - Edit `src/MicPassthrough/MicPassthrough.csproj` - update `<Version>`
   - Edit `CHANGELOG.md` - add entry under `## [X.Y.Z] - YYYY-MM-DD`

2. **Commit Changes:**
   ```bash
   git commit -m "chore: Bump version to X.Y.Z"
   ```

3. **Create Git Tag:**
   ```bash
   git tag -a vX.Y.Z -m "Release version X.Y.Z"
   ```

4. **Push Tag:**
   ```bash
   git push origin vX.Y.Z
   ```

5. **GitHub Actions Automation:**
   - Release workflow triggers on tag push
   - Extracts version from tag name (vX.Y.Z → X.Y.Z)
   - Builds Release configuration
   - Runs all 15 tests
   - Creates GitHub Release with pre-filled notes
   - Uploads MicPassthrough.exe as downloadable asset

See [docs/QUICK_RELEASE.md](../docs/QUICK_RELEASE.md) for checklist.

## GitHub Actions Workflows

### CI Workflow (`.github/workflows/ci.yml`)

**Triggers:** Push to main/develop, pull requests to main/develop

**Steps:**
1. Checkout code
2. Setup .NET 10.0 SDK
3. Restore NuGet packages
4. Build in Release configuration
5. Run 11 unit tests (skip hardware tests)
6. Upload artifacts

**Hardware tests skipped:** `RUN_HARDWARE_TESTS` not set in CI

### Release Workflow (`.github/workflows/release.yml`)

**Triggers:** Push with tags matching `v*.*.*` (e.g., v1.0.0, v1.0.1)

**Steps:**
1. Checkout code
2. Extract version from tag name (e.g., v1.0.1 → 1.0.1)
3. Setup .NET 10.0 SDK
4. Restore NuGet packages
5. Build in Release configuration
6. Run all 15 tests (includes hardware integration tests)
7. Publish self-contained .exe
8. Create GitHub Release with:
   - Auto-populated description with installation instructions
   - Usage examples
   - Requirements listing
9. Upload MicPassthrough.exe as release asset

**Release Notes Template:**
- Includes installation instructions from README.md
- Lists system requirements
- Shows usage examples
- Links to VB-Audio Virtual Cable

## When Generating Code

### Audio-Related Code

- Use NAudio abstractions (IWaveIn, IWaveProvider)
- Handle device disconnection scenarios
- Document buffer sizes and latency implications
- Include error handling for WASAPI failures
- Test with multiple audio device configurations

### CLI Code

- Use CommandLineParser for argument definitions
- Add help text to all options
- Validate option combinations
- Provide clear error messages for invalid input

### Test Code

- Use Moq for complex dependencies
- Mock only external dependencies (AudioDeviceManager, ILogger)
- Test public contract behavior
- Use descriptive test names: `ClassName_Method_Scenario_ExpectedResult`
- For hardware tests, use `[ConditionalHardwareTest]` attribute

### Documentation

- Update [CHANGELOG.md](../CHANGELOG.md) for all user-facing changes
- Create/update [docs/architecture/REFACTORING.md](../docs/architecture/REFACTORING.md) for significant architecture changes
- For major decisions, create a new ADR using [MADR format](../docs/adr/template.md)
- Keep [TESTING.md](../TESTING.md) in sync with test suite changes
- Update relevant docs in [docs/](../docs/) directory for workflow/process changes

## Regularly Updated Documentation

The following files should be reviewed/updated when making relevant changes:

| File | When to Update |
|------|----------------|
| [README.md](../README.md) | New features, setup changes, requirements |
| [TESTING.md](../TESTING.md) | New tests, test prerequisites, test execution |
| [CHANGELOG.md](../CHANGELOG.md) | **Every** release (Added/Fixed/Changed sections) |
| [docs/QUICK_RELEASE.md](../docs/QUICK_RELEASE.md) | Release process changes |
| [docs/VERSIONING.md](../docs/VERSIONING.md) | Version strategy changes |
| [docs/WORKFLOWS.md](../docs/WORKFLOWS.md) | GitHub Actions workflow changes |
| [docs/architecture/REFACTORING.md](../docs/architecture/REFACTORING.md) | Architecture decisions, major refactoring |
| [docs/adr/](../docs/adr/) | Significant technical decisions |

## Performance Considerations

- **Audio Latency:** Target ~100ms default passthrough latency
- **Buffer Management:** Configurable 50-200ms buffers
- **CPU Usage:** Monitor real-time CPU consumption (shouldn't spike >10% on passthrough)
- **Memory:** Keep footprint minimal (audio-only, no large caches)
- **Device Enumeration:** Cache device list, refresh only on demand

## Security & Reliability

- **Device Disconnection:** Handle gracefully (catch exceptions, log, cleanup)
- **Invalid Options:** Validate all user input (device IDs, buffer sizes)
- **WASAPI Errors:** Provide meaningful error messages with troubleshooting hints
- **Audio Dropout Prevention:** Use appropriate buffer sizing and scheduling
- **Logging:** Enable debug logging for troubleshooting without exposing sensitive data

## Related Resources

- **NAudio Docs:** https://github.com/naudio/NAudio
- **WASAPI Documentation:** https://learn.microsoft.com/en-us/windows/win32/coreaudio/wasapi
- **CommandLineParser:** https://github.com/commandlineparser/commandline
- **Keep a Changelog:** https://keepachangelog.com/
- **Semantic Versioning:** https://semver.org/
- **MADR (ADR Format):** https://adr.github.io/madr/
- **VB-Audio Virtual Cable:** https://vb-audio.com/Cable/

## Notes for AI Contributors

- This project was AI-generated with manual review. Maintain code quality standards.
- Follow established patterns in existing code (see [src/MicPassthrough/](../src/MicPassthrough/) for examples)
- Test all audio code paths with actual WASAPI devices when possible
- Keep documentation synchronized with code changes
- Use meaningful log messages for debugging audio issues
- Reference the [CHANGELOG.md](../CHANGELOG.md) format when documenting changes
- For major changes, consider creating an ADR in [docs/adr/](../docs/adr/)
