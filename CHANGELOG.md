# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- N/A

### Fixed
- N/A

### Changed
- N/A

## [0.1.2] - 2025-12-31

### Changed
- **CI/CD Performance Improvements**: Optimized GitHub Actions workflows with NuGet package caching
  - Added NuGet package caching to both CI and release workflows (hash-based on `**/*.csproj`)
  - Reduces dependency restore time from ~14s to ~1-2s on subsequent runs
  - Expected overall improvement: CI from 2m 10s → ~1m 55s, releases by ~12-14s
  - Cache automatically invalidates when project dependencies change (safe strategy)
- **Enhanced Release Workflow**: Added test result publishing to release workflow
  - Release workflow now generates and publishes test results (TRX and JUnit XML formats)
  - Uses same EnricoMi action as CI workflow for consistent test reporting
  - Provides release quality visibility - test results visible on release tag page
- **Visual Assets**: Added project logo and application icon with multiple resolutions
  - Logo embedded in README header for project branding
  - Application icon integrated into executable (appears in File Explorer)
  - Generated icon resolutions: 16×16, 32×32, 64×64, 128×128, 256×256

## [0.1.1] - 2025-12-31

### Changed
- **Migrated from CommandLineParser to System.CommandLine**: Replaced CommandLineParser 2.9.1 with Microsoft's System.CommandLine (2.0.0-beta4) for CLI argument parsing
  - **Benefits**: Trim-compatible library, better startup performance (no reflection), modern type-safe API, Microsoft-maintained
  - **Size impact**: Executable reduced from 37MB to 12MB (67% reduction) with trimming and compression enabled
  - **Note**: Trimming enabled with `<BuiltInComInteropSupport>true</BuiltInComInteropSupport>` to preserve NAudio WASAPI COM support
  - See [docs/adr/0001-migrate-to-system-commandline.md](docs/adr/0001-migrate-to-system-commandline.md) for detailed decision rationale
- Updated Options.cs to use plain C# class instead of attribute-based configuration
- Updated Program.cs with RootCommand-based CLI parsing
- Updated OptionsParsingTests to validate Options class properties directly

### Added
- Architecture Decision Record: [ADR-0001: Migrate to System.CommandLine](docs/adr/0001-migrate-to-system-commandline.md)

### Fixed
- N/A

### Changed
- N/A

## [0.1.0] - 2025-12-31

### Added
- **Auto-switch mode**: `--auto-switch` flag enables automatic passthrough control based on Phone Link call activity
- **ProcessAudioMonitor**: Intelligent call detection engine that monitors audio sessions every 500ms
- **WindowsDefaultMicrophoneManager**: COM-based Windows default microphone switching via IPolicyConfig
- **Dual-device monitoring**: Monitors both physical microphone and cable capture device for reliable call detection
- **Grace period framework**: Built-in (currently disabled) grace period for handling brief session interruptions
- **Session filtering**: Only considers ACTIVE audio sessions (state == 1) for accurate call detection
- **DisplayName-based identification**: Detects Phone Link via session DisplayName keywords ("phone", "call", "experience")
- **Process ID tracking**: Identifies PhoneExperienceHost and related svchost.exe processes
- **Session history tracking**: HashSet-based session comparison to detect call start/end events
- **Automatic microphone restoration**: Saves and restores original default microphone when calls end
- **New CLI options**:
  - `--cable-render`: Specify VB-Audio render device (default: "CABLE Input (VB-Audio Virtual Cable)")
  - `--cable-capture`: Specify VB-Audio capture device (default: "CABLE Output (VB-Audio Virtual Cable)")
- **Comprehensive test suite**: 19 tests total (12 unit tests, 7 hardware integration tests)

### Fixed
- Call detection now differentiates Phone Link from other applications (Discord, Teams, etc.)
- False hangup detection eliminated by monitoring both microphone devices
- Session end detection improved with proper state tracking

### Changed
- Logging: Info-level logs now only show state changes (not repetitive checks every 500ms)
- Debug logs remain verbose for troubleshooting
- CLI option renamed from `--cable` to `--cable-render` for clarity
- Help text expanded with auto-switch usage examples and explanations

### Technical Details
- Uses Windows Core Audio API session enumeration for call detection
- COM interface IPolicyConfig for programmatic default device switching
- WASAPI-based audio routing maintains ~100ms latency
- Background monitoring thread with cancellation token support

## [1.0.0] - 2025-12-30

### Added
- Initial release of Microphone Passthrough
- Core WASAPI-based audio passthrough engine
- CLI interface with CommandLineParser
- Support for:
  - Custom microphone device selection
  - VB-Audio Virtual Cable output routing
  - Real-time audio monitoring (optional)
  - Configurable buffer sizes (50-200ms)
  - Exclusive vs shared WASAPI mode
  - Prebuffer frame control
  - Verbose logging mode
- Structured logging with Microsoft.Extensions.Logging
- Device enumeration and discovery
- Low-latency audio passthrough (~100ms default)

### Fixed
- Phone Link USB microphone volume bug by using passthrough approach

### Known Issues
- Requires VB-Audio Virtual Cable for virtual microphone output
- Exclusive mode requires no other exclusive audio apps running
- Some audio devices may have compatibility issues with WASAPI

---

## Version Format

This project uses [Semantic Versioning](https://semver.org/):

- **MAJOR** version when you make incompatible API changes
- **MINOR** version when you add functionality in a backwards compatible manner
- **PATCH** version when you make backwards compatible bug fixes

Example: `v1.2.3`
- `1` = MAJOR (incompatible changes)
- `2` = MINOR (new features)
- `3` = PATCH (bug fixes)

## How to Release

1. Update version in `src/MicPassthrough/MicPassthrough.csproj`:
   ```xml
   <Version>1.0.1</Version>
   ```

2. Update this CHANGELOG.md with your changes

3. Commit your changes:
   ```bash
   git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
   git commit -m "chore: Bump version to 1.0.1"
   ```

4. Create a git tag:
   ```bash
   git tag -a v1.0.1 -m "Release version 1.0.1"
   ```

5. Push the tag to GitHub:
   ```bash
   git push origin v1.0.1
   ```

6. GitHub Actions automatically:
   - Extracts version from tag
   - Runs all tests
   - Builds the executable
   - Creates a GitHub Release
   - Uploads MicPassthrough.exe as an asset

That's it! The release will appear on the [Releases page](https://github.com/mikejhill/mic-passthrough/releases).
