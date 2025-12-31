# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- ProcessAudioMonitor for intelligent call detection
- WindowsDefaultMicrophoneManager for automatic microphone device switching
- `--auto-switch` CLI flag for automatic call-based passthrough activation
- Audio device monitoring that detects when PhoneLink uses microphone
- Automatic Windows default microphone switching to CABLE Output during calls
- Automatic restoration of original microphone when call ends
- Smart mode that only activates passthrough when needed
- Real-time audio session detection (checks every 500ms)

### Fixed
- N/A

### Changed
- CLI help text updated with auto-switch examples and explanations

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
