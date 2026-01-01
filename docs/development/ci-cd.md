# GitHub Actions CI/CD

This project includes automated CI/CD workflows via GitHub Actions.

## CI Workflow

**File:** `.github/workflows/ci.yml`

Runs on every push to `main` and `develop` branches, and on all pull requests targeting these branches.

### What It Does

1. **Checkout** - Pulls the latest code
2. **Setup .NET SDK** - Installs .NET 10.0 runtime
3. **Restore** - Downloads NuGet dependencies (NAudio, Moq, xUnit, etc.)
4. **Build** - Compiles in Release mode
5. **Test** - Runs 11 unit tests (hardware tests automatically skipped)
6. **Upload Artifacts** - Archives test results for 30 days

### Platform

- **OS:** Windows (required for WASAPI audio APIs)
- **Concurrency:** Single job, sequentially

### Test Behavior in CI

Unit tests run in CI:
- ✅ 6 CLI parsing tests
- ✅ 5 unit tests with Moq mocks
- ⏸️ 4 hardware integration tests (skipped - no `RUN_HARDWARE_TESTS`)

The hardware tests are skipped because:
- CI environment has no audio hardware
- `ConditionalHardwareTestAttribute` detects missing environment variable
- Ensures fast, reliable CI without device dependencies

### Viewing Results

1. Go to **Actions** tab on GitHub
2. Select the **CI** workflow
3. Click on any run to see detailed logs
4. Download **test-results** artifact to see xUnit reports

### Local Replication

To run the exact same tests locally:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --verbosity normal
```

To also run hardware tests locally:

```bash
# PowerShell
$env:RUN_HARDWARE_TESTS = "1"
dotnet test --configuration Release --verbosity normal

# Bash/Zsh
export RUN_HARDWARE_TESTS=1
dotnet test --configuration Release --verbosity normal
```

## Future Enhancements

Potential additions to the workflow:

- Code coverage reporting (Coverlet + Codecov)
- SonarQube static analysis
- Release builds and publish to NuGet
- Docker image building
- Deployment to GitHub releases
