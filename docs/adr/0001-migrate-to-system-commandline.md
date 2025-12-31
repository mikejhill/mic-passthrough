# Migrate from CommandLineParser to System.CommandLine

* Status: accepted
* Date: 2025-12-31

Technical Story: Investigated reducing executable size from 37MB (compression-only) to 12MB (compression + trimming) by migrating CLI library

## Context and Problem Statement

The MicPassthrough v0.1.0 release candidate uses CommandLineParser 2.9.1 for CLI argument parsing. The self-contained single-file executable with compression is 37MB. Initial investigation showed that enabling .NET trimming could reduce the size to 12MB (67% reduction), but CommandLineParser uses heavy reflection that's incompatible with trimming.

Decision question: Should we migrate to a trim-compatible CLI library to enable size optimization?

## Decision Drivers

* Executable size: 37MB is large for a simple CLI tool; users prefer smaller downloads
* Trimming compatibility: CommandLineParser 2.9.1 uses reflection patterns incompatible with .NET trimming
* Modern library: System.CommandLine is Microsoft's next-generation CLI library
* Maintenance: CommandLineParser is mature but not actively adding trim support
* Performance: System.CommandLine has better startup performance (no runtime reflection)
* Future-proofing: Trim-ready architecture even if not immediately usable

## Considered Options

1. **Keep CommandLineParser** - Stay with current 37MB size, no code changes
2. **Migrate to System.CommandLine** - Replace with Microsoft's trim-compatible library
3. **Custom parser** - Roll our own argument parsing (manual validation, no library)

## Decision Outcome

Chosen option: **"Migrate to System.CommandLine"**, because:

- **Enables trimming**: System.CommandLine is trim-compatible, achieving 12MB executable (67% reduction from 37MB)
- **Microsoft-maintained**: Official Microsoft library with long-term support roadmap
- **Better performance**: No runtime reflection = faster startup
- **Cleaner API**: More modern, type-safe API than CommandLineParser's attribute-based approach
- **Same functionality**: All 11 CLI options work identically after migration

### Positive Consequences

* ✅ **67% size reduction**: Executable trimmed from 37MB to 12MB with compression
* ✅ Trim-compatible architecture with `<BuiltInComInteropSupport>true</BuiltInComInteropSupport>`
* ✅ Improved startup performance (no reflection overhead)
* ✅ Modern, type-safe API that's easier to maintain
* ✅ Microsoft-backed library with active development
* ✅ All 12 unit tests pass after migration
* ✅ User experience unchanged (same CLI flags and behavior)

### Negative Consequences

* ⚠️ **Trimming with COM support**: Trimming works with `<BuiltInComInteropSupport>true</BuiltInComInteropSupport>` but generates trim warning IL2026 (built-in COM not fully trim-compatible)
* ⚠️ NAudio WASAPI COM interop kept in trimmed build (required for functionality)
* ❌ Requires updating CLI parsing tests (migrated to test Options class properties instead of parser behavior)
* ❌ System.CommandLine is still in beta (2.0.0-beta4), though widely used and stable

## Pros and Cons of the Options

### Keep CommandLineParser

* Good: No migration effort, no risk of introducing bugs
* Good: Mature, stable library (v2.9.1)
* Bad: Executable would remain 37MB
* Bad: Not trim-compatible; blocks size optimization
* Bad: Reflection overhead at startup

### Migrate to System.CommandLine

* Good: Trim-compatible library (future-ready)
* Good: Better performance (no reflection)
* Good: Microsoft-maintained, modern API
* Bad: Migration effort (3 files changed: MicPassthrough.csproj, Options.cs, Program.cs)
* Bad: Trim warning IL2026 (built-in COM not fully trim-compatible, but works)
* Bad: Beta version (though stable and widely adopted)

### Custom parser

* Good: Complete control, minimal dependencies
* Good: Could be trim-compatible
* Bad: Significant development effort
* Bad: No help text generation, validation, or error messages out-of-the-box
* Bad: Maintenance burden
* Bad: Reinventing the wheel

## Technical Details

### Migration Summary

**Files Changed**:
1. **MicPassthrough.csproj**: Replaced `CommandLineParser` 2.9.1 with `System.CommandLine` 2.0.0-beta4.22272.1
2. **Options.cs**: Removed `[Option]` attributes, converted to plain C# class (POCO)
3. **Program.cs**: Replaced `Parser.Default.ParseArguments<Options>(args)` with `RootCommand` + option configuration
4. **OptionsTests.cs**: Updated tests to validate Options class properties instead of parser behavior

**Build Configuration**:
- Added `<BuiltInComInteropSupport>true</BuiltInComInteropSupport>` to MicPassthrough.csproj
- Enabled `PublishTrimmed=true` with `EnableCompressionInSingleFile=true`
- **Result**: Trimming produces 12MB executable with full COM support for NAudio WASAPI

### NAudio COM Compatibility

NAudio's WASAPI implementation uses `MMDeviceEnumerator` which relies on classic COM interop:
```csharp
// AudioDeviceManager.cs line 22
_enumerator = new MMDeviceEnumerator();  // Requires COM support
```

The `<BuiltInComInteropSupport>true</BuiltInComInteropSupport>` property preserves built-in COM support during trimming. This allows NAudio to function correctly while still achieving significant size reduction.

**Trade-off**: Generates trim warning IL2026 indicating built-in COM is not fully trim-compatible, but the functionality works correctly in testing.

### Size Reduction Achieved

- **Before (compression only)**: 37MB
- **After (trimming + compression)**: 12MB
- **Reduction**: 25MB (67% smaller)

All functionality tested and working:
- ✅ Device enumeration (`--list-devices`)
- ✅ Help and version output
- ✅ Audio passthrough with WASAPI
- ✅ All 12 unit tests pass

## Links

* [System.CommandLine GitHub](https://github.com/dotnet/command-line-api)
* [.NET Trimming Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)
* [NAudio GitHub](https://github.com/naudio/NAudio)
* [COM Interop and Trimming](https://aka.ms/dotnet-illink/com)

---

<!-- This ADR is based on MADR 3.0.0 - https://adr.github.io/madr/ -->
