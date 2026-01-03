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
│   └── workflows/               # GitHub Actions CI/CD
│       ├── ci.yml               # Runs on push/PR
│       └── release.yml          # Runs on version tags (v*.*.*)
├── src/
│   ├── MicPassthrough/          # Main application
│   │   ├── Program.cs           # Entry point
│   │   ├── Options.cs           # CLI argument definitions
│   │   ├── AudioDeviceManager.cs # Device enumeration & WASAPI setup
│   │   ├── PassthroughApplication.cs # Core business logic
│   │   ├── PassthroughEngine.cs # Low-level audio passthrough
│   │   ├── ProcessAudioMonitor.cs # Call detection via audio sessions
│   │   ├── WindowsDefaultMicrophoneManager.cs # Windows microphone switching
│   │   └── MicPassthrough.csproj # Version source
│   └── MicPassthrough.Tests/    # Test suite (15 tests)
│       ├── OptionsParsingTests.cs # 6 CLI argument tests
│       ├── AudioDeviceManagerTests.cs # Audio device tests
│       ├── PassthroughApplicationTests.cs # Application logic tests
│       ├── ConditionalHardwareTestAttribute.cs # Custom xUnit attribute
│       └── MicPassthrough.Tests.csproj
├── docs/
│   ├── README.md                # Documentation index
│   ├── guides/                  # User-facing guides
│   │   ├── testing.md           # Test suite documentation
│   │   ├── daemon-mode.md       # Daemon mode with system tray
│   │   └── auto-switch.md       # Auto-switch improvements
│   ├── development/             # Developer processes
│   │   ├── ci-cd.md             # CI/CD configuration details
│   │   ├── workflows.md         # GitHub Actions workflow diagrams
│   │   ├── quick-release.md     # 1-page release checklist
│   │   ├── release-guide.md     # Complete release walkthrough
│   │   └── versioning.md        # Semantic versioning strategy
│   ├── architecture/
│   │   └── refactoring.md       # Architecture decisions & history
│   ├── adr/
│   │   └── template.md          # ADR template (MADR format)
│   └── assets/
├── README.md                    # Main project documentation
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
   - Validate Options class properties and default values
   - Test property setters (Mic, CableRender, Buffer, Verbose, etc.)
   - CLI parsing is now handled by System.CommandLine (tested via integration)

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
| System.CommandLine | 2.0.0-beta4+ | CLI argument parsing (trim-compatible) |
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
   - See [docs/development/versioning.md](../docs/development/versioning.md) for details

3. **Architecture Decision Records (ADRs):**
   - Use MADR (Markdown Any Decision Records) format
   - File naming: `adr/XXXX-descriptive-title.md` (sequential numbering)
   - Status options: proposed, rejected, accepted, deprecated, superseded by ADR-XXXX
   - Template: [adr/template.md](../docs/adr/template.md)

4. **Documentation Location Rules:**
   - **Project overview & getting started:** [README.md](../README.md)
   - **User guides & features:** [docs/guides/](../docs/guides/) folder
     - [docs/guides/testing.md](../docs/guides/testing.md) - Test suite documentation
     - [docs/guides/daemon-mode.md](../docs/guides/daemon-mode.md) - Daemon mode feature
     - [docs/guides/auto-switch.md](../docs/guides/auto-switch.md) - Auto-switch improvements
   - **Developer processes:** [docs/development/](../docs/development/) folder
     - [docs/development/quick-release.md](../docs/development/quick-release.md) - Release checklist
     - [docs/development/release-guide.md](../docs/development/release-guide.md) - Detailed release walkthrough
     - [docs/development/versioning.md](../docs/development/versioning.md) - Version strategy
     - [docs/development/workflows.md](../docs/development/workflows.md) - GitHub Actions workflows
     - [docs/development/ci-cd.md](../docs/development/ci-cd.md) - CI/CD configuration
   - **Architecture & design:** [docs/architecture/refactoring.md](../docs/architecture/refactoring.md)
   - **Architecture decisions:** [docs/adr/](../docs/adr/)
   
   **Rule:** Feature-specific documentation and permanent reference guides go in `docs/` with organized subdirectories. Only critical top-level files go in project root (README.md, LICENSE, CHANGELOG.md).

   **Point-in-Time Documentation (Avoid):**
   - Do NOT create session-based summary documents (e.g., IMPLEMENTATION_SUMMARY.md, CRITICAL_FIXES_SUMMARY.md)
   - Point-in-time documentation becomes stale quickly and clutters the docs directory
   - Instead, integrate findings into permanent documentation:
     - Feature implementations → Feature-specific docs (e.g., AUTO_SWITCH_IMPROVEMENTS.md)
     - Architecture decisions → [docs/architecture/REFACTORING.md](../docs/architecture/REFACTORING.md)
     - Significant decisions → Architecture Decision Records in [docs/adr/](../docs/adr/)
     - User-facing changes → [CHANGELOG.md](../CHANGELOG.md)

## Git Commit Guidelines

### Verify Changes Before Committing

**CRITICAL: Always check what's actually changing before writing commit messages.**

Before committing:
1. Run `git status` to see which files changed
2. Run `git diff` to see the actual changes
3. Review the line count: `git diff --stat` shows insertions/deletions
4. Add only the relevant hunks to the commit
   - **Never use `git add -A` or `git commit -a`**
   - For full files, first use `git diff` to verify all changes are related, then run `git add <files>`
   - For specific hunks: `git diff > /tmp/patch ; (modify /tmp/patch) ; git apply --cached /tmp/patch`
5. Ensure unrelated changes and hunks are NOT staged
6. Write commit message that accurately reflects those changes ONLY

Common mistakes to avoid:
- ❌ Writing multi-point commit messages for single-line changes (except when justified)
- ❌ Describing features that aren't actually being added
- ❌ Exaggerating the scope of small changes
- ✅ Match message complexity to actual change size
- ✅ Be concise and accurate about what changed

Examples:
```bash
# BAD: 1 line changed, but message lists 4 bullet points
git commit -m "feat: Add daemon mode section
- Add daemon mode documentation
- Include system tray features  
- Link to detailed documentation
- Highlight professional features"

# GOOD: Accurate for a 1-line change
git commit -m "docs: Simplify AI disclaimer wording"

# BAD: Says "implement X" when only updating docs
git commit -m "feat: Implement session tracking and add comprehensive docs"

# GOOD: Separate commits for code vs docs
git commit -m "feat: Implement session tracking"
git commit -m "docs: Document session tracking feature"
```

### Commit Granularity

**Every commit must be isolated to a single logical change.** This ensures:
- Clear git history for debugging and understanding changes
- Easy rollback of specific features or fixes
- Better code review experience
- Clean separation of concerns

### Commit Message Capitalization

**Always capitalize the first letter after the commit type:**
- ✅ `feat: Add auto-switch microphone detection`
- ✅ `fix: Handle device disconnection`
- ✅ `docs: Document testing procedures`
- ❌ `feat: add auto-switch microphone detection`
- ❌ `fix: handle device disconnection`

### Commit Types and Examples

**Feature Commit** - Single new feature or capability:
```bash
git commit -m "feat: Add auto-switch microphone detection

- Implement ProcessAudioMonitor for call detection
- Track audio sessions to identify Phone Link usage
- Only trigger for PhoneExperienceHost, ignore other apps"
```

**Documentation Commit** - Updates to a single document or feature docs:
```bash
git commit -m "docs: Document auto-switch mode setup and testing

- Add installation and configuration steps
- Include troubleshooting section
- Add expected log output examples"
```

**Bug Fix Commit** - Fix for a specific issue or behavior:
```bash
git commit -m "fix: Detect when Phone Link releases microphone

- Track session history with HashSet comparison
- Detect session end events
- Properly deactivate passthrough when call ends"
```

**Refactoring Commit** - Code improvements without behavior change:
```bash
git commit -m "refactor: Simplify device enumeration logic

- Extract common validation into helper method
- Remove duplicate error handling
- Improve readability without changing behavior"
```

**Chore Commit** - Build system, dependencies, maintenance:
```bash
git commit -m "chore: Update NAudio dependency to v2.2.1

- Bump package version
- Update build configuration
- No code changes"
```

### What NOT to do

❌ **Don't mix multiple features in one commit:**
```bash
# BAD: Too many unrelated changes
git commit -m "Implement session tracking and add docs and clean up temp files"
```

❌ **Don't commit unrelated code and docs together:**
```bash
# BAD: Code feature + documentation + cleanup in one commit
git commit -m "feat: Improve detection; docs: Update guide; chore: Remove temp files"
```

✅ **Do separate by logical change:**
```bash
git commit -m "feat: Implement session tracking"
git commit -m "docs: Update auto-switch documentation"
git commit -m "docs: Remove temporary summary files"
```

### Commit Message Format

```
<type>: <Subject>

<body>

<footer>
```

- **Type:** feat, fix, docs, refactor, chore, test
- **Subject:** Imperative mood (present tense), starts with **capital letter**, max 50 chars
  - ✅ Good: `feat: Add anti-aliasing`, `fix: Handle device disconnection`
  - ❌ Bad: `feat: add anti-aliasing`, `fix: handle device disconnection`
- **Body:** Optional but recommended for complex changes
  - Explain what and why, not how
  - Wrap at 72 characters
  - Separate from subject with blank line
- **Footer:** Optional references to issues/tickets

### Example Detailed Commit

```bash
git commit -m "fix: Handle audio device disconnection gracefully

When a WASAPI device is disconnected during passthrough, the application
would crash with an unhandled exception. This commit adds proper error
handling to detect device removal and cleanly shut down the audio engine.

Changes:
- Add try-catch in audio capture loop
- Log device disconnection events
- Trigger graceful shutdown on error
- Add device reconnection delay

Fixes: #42
Related-to: #38"
```

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
   - Runs all non-hardware tests
   - Creates GitHub Release with pre-filled notes
   - Uploads MicPassthrough.exe as downloadable asset

See [docs/QUICK_RELEASE.md](../docs/QUICK_RELEASE.md) for checklist.

## GitHub Actions Workflows

### CI Workflow (`.github/workflows/ci.yml`)

**Triggers:** Push to main/develop, pull requests to main/develop

**IMPORTANT:** When working as a Copilot agent, PR workflows may not auto-start. See [CRITICAL: PR Workflows May Not Auto-Start](#critical-pr-workflows-may-not-auto-start) below for details on checking and manually triggering workflows.

**Steps:**
1. Checkout code
2. Setup .NET 10.0 SDK
3. Restore NuGet packages
4. Build in Release configuration
5. Run unit tests (skip hardware tests)
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
6. Run all non-hardware tests
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

## Investigating GitHub Actions Workflows

When users **explicitly ask** you to investigate GitHub Actions workflows or CI/workflow failures, use the GitHub MCP tools described below. Do not claim you cannot access logs - you have these tools available.

### Available MCP Tools for GitHub Actions

1. **github-mcp-server-actions_list** - List workflows, runs, jobs, and artifacts
2. **github-mcp-server-actions_get** - Get workflow/run/job details and log download URLs
3. **github-mcp-server-actions_run_trigger** - Run, rerun, or cancel workflows

### CRITICAL: PR Workflows May Not Auto-Start

**When working as a GitHub Copilot coding agent creating or updating pull requests:**

- **PR workflows (`pull_request` triggers) may NOT automatically start** when you create or update the PR
- GitHub's security model often prevents workflows from running on PRs created by automation/bots without explicit approval
- **DO NOT wait indefinitely for PR workflows to start** - they likely won't start without intervention

**Required Actions:**

1. **Always check if workflows have started** after creating/updating a PR:
   - Use `github-mcp-server-actions_list` with `method: list_workflow_runs` and the PR's branch name
   - Check if any runs exist for your branch with `event: "pull_request"`
   - If no runs found, the workflow did not auto-start

2. **If workflows have NOT started, you MUST manually trigger them:**
   ```
   github-mcp-server-actions_run_trigger
   - method: run_workflow
   - owner: <repository_owner>
   - repo: <repository_name>
   - workflow_id: ci.yml
   - ref: <your_branch_name>
   - inputs: {}
   ```

3. **If workflows HAVE started** (triggered by a separate author or manual approval):
   - Proceed with normal investigation process
   - Check workflow status and results
   - Address any failures

**Summary:** Always verify workflow status, but expect that you'll need to manually trigger PR workflows yourself. Do not assume they will eventually start on their own.

### Standard Workflow Investigation Process

When asked to investigate workflow failures, follow this process:

1. **List Recent Workflow Runs:**
   ```
   github-mcp-server-actions_list
   - method: list_workflow_runs
   - owner: <repository_owner>
   - repo: <repository_name>
   - resource_id: ci.yml (or release.yml)
   - per_page: 1 (MUST be 1-3; note: paging may have bugs, not a perfect solution)
   - page: 1
   - workflow_runs_filter:
       branch: <branch_name> (REQUIRED when known)
       status: "completed" (or "in_progress", etc.)
   ```
   
   **CRITICAL - Prevent Context Overflow:** 
   - NEVER do broad searches - always use narrow filters
   - Always use `per_page: 1` for initial investigation (max 3)
   - ALWAYS specify `branch` in workflow_runs_filter when known
   - ALWAYS specify `status` filter when possible
   - Large responses WILL overrun your context window

2. **Get Workflow Run Details (Optional):**
   ```
   github-mcp-server-actions_get
   - method: get_workflow_run
   - owner: <repository_owner>
   - repo: <repository_name>
   - resource_id: <run_id from step 1>
   ```

3. **List Jobs:**
   ```
   github-mcp-server-actions_list
   - method: list_workflow_jobs
   - owner: <repository_owner>
   - repo: <repository_name>
   - resource_id: <run_id>
   ```

4. **Get Logs Download URL (run-level ZIP):**
   ```
   github-mcp-server-actions_get
   - method: get_workflow_run_logs_url
   - owner: <repository_owner>
   - repo: <repository_name>
   - resource_id: <run_id>
   ```
   Download the ZIP from `logs_url`, then open the job folders (e.g., `Build and Test/*.txt`, `license-compliance _ ORT License Compliance/*.txt`) to read step output and failures. Prefer this when `get_job_logs` is unavailable.

5. **Download and Extract Logs:**
   ```bash
   curl -s "<logs_url>" --output /tmp/workflow_logs.zip
   unzip -l /tmp/workflow_logs.zip
   unzip -o /tmp/workflow_logs.zip "job-name/step-name.txt"
   tail -100 "/tmp/job-name/step-name.txt"
   ```

### Common Investigation Scenarios

**Scenario 1: CI Failure Investigation**
1. List workflow runs (narrow filters: per_page: 1, branch, status)
2. List jobs to find failed job(s)
3. Download and extract logs
4. Analyze error messages
5. Propose fix

**Scenario 2: Check Build Status**
1. List workflow runs with status filter
2. Review most recent run conclusion

**Scenario 3: Test Failures**
1. List workflow runs (narrow filters)
2. Find failed runs
3. Download logs for test steps
4. Identify failing tests

**Scenario 4: Release Issues**
1. List workflow runs (narrow filters)
2. Verify build/test steps
3. Check logs for failures

### Workflow Filtering Options

Always use multiple filters to minimize response size:
- **branch:** Specific branch (HIGHLY RECOMMENDED)
- **status:** queued, in_progress, completed, requested, waiting
- **event:** push, pull_request, tag, etc.
- **actor:** Specific user

Example:
```
github-mcp-server-actions_list
- method: list_workflow_runs
- owner: <repository_owner>
- repo: <repository_name>
- resource_id: ci.yml
- per_page: 1
- workflow_runs_filter:
    status: "completed"
    branch: "<branch_name>"
```

### Triggering and Managing Workflows

**Run Manually:**
```
github-mcp-server-actions_run_trigger
- method: run_workflow
- owner: <repository_owner>
- repo: <repository_name>
- workflow_id: ci.yml
- ref: <branch_or_tag>
- inputs: {}
```

**Rerun Failed Workflow:**
```
github-mcp-server-actions_run_trigger
- method: rerun_workflow_run
- owner: <repository_owner>
- repo: <repository_name>
- run_id: <run_id>
```

**Rerun Only Failed Jobs:**
```
github-mcp-server-actions_run_trigger
- method: rerun_failed_jobs
- owner: <repository_owner>
- repo: <repository_name>
- run_id: <run_id>
```

**Cancel Running Workflow:**
```
github-mcp-server-actions_run_trigger
- method: cancel_workflow_run
- owner: <repository_owner>
- repo: <repository_name>
- run_id: <run_id>
```

### Best Practices

1. **Always use narrow filters** - NEVER do broad searches
   - Use `per_page: 1` (max 3; note: paging may have bugs)
   - ALWAYS specify `branch` when known
   - ALWAYS specify `status` when possible
   - Large responses WILL overrun context window

2. **Download logs as ZIP files**
   - Use `get_workflow_run_logs_url` for download URL
   - Use `curl` and `unzip` to extract logs
   - Use `tail -N` to view last N lines (errors at end)
   - Use `grep` for specific error patterns

3. **Analysis approach**
   - Check multiple jobs if parallel execution
   - Look for patterns (flaky tests, environment issues)
   - Provide specific error messages when proposing fixes
   - Never guess - always check actual logs

### Log Analysis Tips

Look for:
- Error messages and stack traces
- Failing test names
- Build errors (compilation, dependencies)
- Timing issues (timeouts)
- Authentication/permission errors
- Environment-specific issues

### Example Investigation Workflow

```
User: "The CI build is failing, can you check?"

1. List workflow runs (narrow filters):
   github-mcp-server-actions_list
   - method: list_workflow_runs
   - owner: <repository_owner>
   - repo: <repository_name>
   - resource_id: ci.yml
   - per_page: 1
   - workflow_runs_filter: {branch: "<branch_name>", status: "completed"}
   → run_id: <id>, conclusion: "failure"

2. List jobs:
   github-mcp-server-actions_list
   - method: list_workflow_jobs
   - resource_id: <run_id>
   → Failed job: "license-compliance / ORT License Compliance"

3. Get logs:
   github-mcp-server-actions_get
   - method: get_workflow_run_logs_url
   - resource_id: <run_id>
   
   curl -s "<url>" --output /tmp/logs.zip
   unzip -o /tmp/logs.zip "license-compliance _ ORT License Compliance/3_*.txt"
   tail -100 "/tmp/license-compliance _ ORT License Compliance/3_*.txt"
   → Error: file ".ort/policy/rules.kts" does not exist

4. Propose fix based on error
```

### Additional Resources

See **docs/guides/github-actions-investigation.md** for detailed examples.

## When Generating Code

### Audio-Related Code

- Use NAudio abstractions (IWaveIn, IWaveProvider)
- Handle device disconnection scenarios
- Document buffer sizes and latency implications
- Include error handling for WASAPI failures
- Test with multiple audio device configurations

### CLI Code

- Use System.CommandLine for argument definitions
- Add help text descriptions to all options
- Configure aliases (short forms like -m, -c) for common options
- Validate option combinations
- **Configurable Properties with Sensible Defaults:**
  - Always provide reasonable defaults for user-configurable options
  - Defaults should work for the common case (typically VB-Audio Virtual Cable devices)
  - Use `getDefaultValue: () => value` parameter when defining CLI options
  - Document what the default value is in help text and XML comments
  - Examples:
    ```csharp
    var cableRenderOption = new Option<string>(
        aliases: new[] { "-c", "--cable-render" },
        getDefaultValue: () => "CABLE Input (VB-Audio Virtual Cable)",
        description: "VB-Cable render device name for audio output (exact match). Default: 'CABLE Input (VB-Audio Virtual Cable)'.");
    
    var cableCaptureOption = new Option<string>(
        aliases: new[] { "--cable-capture" },
        getDefaultValue: () => "CABLE Output (VB-Audio Virtual Cable)",
        description: "VB-Cable capture device name for default microphone (exact match). Default: 'CABLE Output (VB-Audio Virtual Cable)'.");
    ```
  - This approach allows users to override defaults without breaking existing usage
  - Users only need to specify options when their setup differs from defaults
  - Keep option names short where possible (short flags like `-c`) for common options
  - Use long-form names (`--cable-capture`) for less common options
- Provide clear error messages for invalid input

### Test Code

- Use Moq for complex dependencies
- Mock only external dependencies (AudioDeviceManager, ILogger)
- Test public contract behavior
- Use descriptive test names: `ClassName_Method_Scenario_ExpectedResult`
- For hardware tests, use `[ConditionalHardwareTest]` attribute

### Documentation

- Update [CHANGELOG.md](../CHANGELOG.md) for all user-facing changes
- Create/update [docs/architecture/refactoring.md](../docs/architecture/refactoring.md) for significant architecture changes
- For major decisions, create a new ADR using [MADR format](../docs/adr/template.md)
- Keep [docs/guides/testing.md](../docs/guides/testing.md) in sync with test suite changes
- Update relevant docs in [docs/](../docs/) directory for workflow/process changes

## Regularly Updated Documentation

The following files should be reviewed/updated when making relevant changes:

| File | When to Update |
|------|----------------|
| [README.md](../README.md) | New features, setup changes, requirements |
| [docs/guides/testing.md](../docs/guides/testing.md) | New tests, test prerequisites, test execution |
| [CHANGELOG.md](../CHANGELOG.md) | **Every** release (Added/Fixed/Changed sections) |
| [docs/development/quick-release.md](../docs/development/quick-release.md) | Release process changes |
| [docs/development/versioning.md](../docs/development/versioning.md) | Version strategy changes |
| [docs/development/workflows.md](../docs/development/workflows.md) | GitHub Actions workflow changes |
| [docs/architecture/refactoring.md](../docs/architecture/refactoring.md) | Architecture decisions, major refactoring |
| [docs/adr/](../docs/adr/) | Significant technical decisions |
| [docs/guides/auto-switch.md](../docs/guides/auto-switch.md) | Auto-switch feature changes and testing |
| [docs/guides/daemon-mode.md](../docs/guides/daemon-mode.md) | Daemon mode feature changes |

## Documentation Anti-Patterns

**Point-in-Time Documentation (Avoid Creating):**
- Session-based summaries that document what was changed "today"
- Implementation notes that document the development process
- Fix summaries that track specific bug corrections

**Why Avoid:**
- These documents become outdated as soon as the next change is made
- They duplicate information better captured in CHANGELOG.md and git history
- They clutter the docs directory without providing lasting value
- They create maintenance burden as the project evolves

**Better Alternatives:**
- Use [CHANGELOG.md](../CHANGELOG.md) for user-facing changes (what changed and why)
- Use [docs/architecture/refactoring.md](../docs/architecture/refactoring.md) for significant code changes (why the change was necessary)
- Use [docs/adr/](../docs/adr/) for major architectural decisions (context, decision, consequences)
- Rely on git commit messages and pull request descriptions for development process history

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
- **System.CommandLine:** https://github.com/dotnet/command-line-api
- **Keep a Changelog:** https://keepachangelog.com/
- **Semantic Versioning:** https://semver.org/
- **MADR (ADR Format):** https://adr.github.io/madr/
- **VB-Audio Virtual Cable:** https://vb-audio.com/Cable/

## Architecture

### Components

- **Program.cs** - Entry point, initializes logging and application framework
- **Options.cs** - Command-line option definitions with validation
- **PassthroughApplication.cs** - Main orchestrator, handles application lifecycle
- **AudioDeviceManager.cs** - Audio device enumeration and discovery
- **PassthroughEngine.cs** - Core audio processing, WASAPI integration
- **ProcessAudioMonitor.cs** - Detects when external processes use audio device
- **WindowsDefaultMicrophoneManager.cs** - Manages Windows default microphone settings

### Continuous Mode (Default)

```
Microphone Device
    ↓ (WASAPI Capture)
PassthroughEngine
    ↓ (Frame buffering)
VB-Audio Cable Output
    ↓
PhoneLink (receives audio at full volume)
```

### Auto-Switch Mode (--auto-switch flag)

```
ProcessAudioMonitor (background, checks every 500ms)
    ↓
Detects PhoneLink using microphone
    ↓
PassthroughApplication orchestrates:
    ├─ WindowsDefaultMicrophoneManager switches to CABLE
    └─ PassthroughEngine starts passthrough
         ↓
    When call ends:
    ├─ PassthroughEngine stops
    └─ WindowsDefaultMicrophoneManager restores original
```

### ProcessAudioMonitor Details

- **Purpose:** Detect when other applications actively use the microphone
- **Method:** Monitors Windows Core Audio API session enumeration
- **Frequency:** Checks every 500ms
- **Usage:** `var monitor = new ProcessAudioMonitor(logger, deviceId);`
- **State:** Check `monitor.IsDeviceInUse` property
- **Lifecycle:** Runs on background thread, controlled via CancellationToken

### WindowsDefaultMicrophoneManager Details

- **Purpose:** Switch Windows default recording device
- **Methods:**
  - `SetDefaultMicrophone(deviceId)` - Saves original, switches to new device
  - `RestoreOriginalMicrophone()` - Restores original device
  - `GetDefaultMicrophone()` - Queries current default
- **Implementation:** Uses IPolicyConfig COM interface (ONLY correct way to set Windows defaults programmatically)
- **Platform:** Windows only (checks `OperatingSystem.IsWindows()`)
- **COM Interface:** PolicyConfigClient wraps IPolicyConfigVista for device management

### Auto-Switch Lifecycle

1. User runs: `MicPassthrough.exe --mic "MyMic" --auto-switch`
2. ProcessAudioMonitor starts background monitoring thread
3. When PhoneLink opens microphone:
   - Monitor detects active audio session
   - WindowsDefaultMicrophoneManager switches default to CABLE Output
   - PassthroughEngine starts capturing and routing audio
4. When PhoneLink closes microphone:
   - Monitor detects session closed
   - PassthroughEngine stops
   - WindowsDefaultMicrophoneManager restores original microphone
5. User presses ENTER to exit application
6. All resources cleaned up, original microphone restored

## Notes for AI Contributors

- This project was AI-generated with manual review. Maintain code quality standards.
- Follow established patterns in existing code (see [src/MicPassthrough/](../src/MicPassthrough/) for examples)
- Test all audio code paths with actual WASAPI devices when possible
- Keep documentation synchronized with code changes
- Use meaningful log messages for debugging audio issues
- Reference the [CHANGELOG.md](../CHANGELOG.md) format when documenting changes
- For major changes, consider creating an ADR in [docs/adr/](../docs/adr/)
