# CI/CD and Release Workflow Overview

This document visualizes the complete automated workflow for this project.

## Workflow Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                    Development Workflow                         │
└─────────────────────────────────────────────────────────────────┘

1. LOCAL DEVELOPMENT
   ├─ Clone repository
   ├─ Make code changes
   ├─ Run tests locally: dotnet test
   ├─ Commit: git commit -m "..."
   └─ Push: git push origin develop
                    ↓
2. CI WORKFLOW (.github/workflows/ci.yml)
   ├─ Triggers: push to main/develop OR pull request
   ├─ Runs on: Windows (for WASAPI)
   ├─ Steps:
   │  ├─ Checkout code
   │  ├─ Setup .NET 10.0
   │  ├─ Restore dependencies
   │  ├─ Build (Release)
   │  ├─ Run tests (11 unit tests)
   │  └─ Upload test artifacts
   ├─ Duration: ~30-60 seconds
   └─ Result: ✅ PASS/❌ FAIL shown in GitHub
                    ↓
3. CODE REVIEW & MERGE
   ├─ Create Pull Request to main
   ├─ CI runs automatically
   ├─ Code review
   └─ Merge to main when approved
                    ↓
4. RELEASE DECISION
   ├─ Version bump needed?
   ├─ YES: Continue to step 5
   └─ NO: Done for now
                    ↓
5. PREPARE RELEASE
   ├─ Update: src/MicPassthrough/MicPassthrough.csproj
   │  <Version>1.0.1</Version>
   ├─ Update: CHANGELOG.md
   ├─ Commit & Push
   └─ All changes on main branch
                    ↓
6. CREATE RELEASE TAG
   ├─ Local: git tag -a v1.0.1 -m "Release v1.0.1"
   ├─ Push: git push origin v1.0.1
   └─ Format: vX.Y.Z (matches v*.*.*)
                    ↓
7. RELEASE WORKFLOW (.github/workflows/release.yml)
   ├─ Triggers: tag push matching v*.*.*
   ├─ Runs on: Windows (for .NET)
   ├─ Steps:
   │  ├─ Checkout code
   │  ├─ Extract version from tag (v1.0.1 → 1.0.1)
   │  ├─ Setup .NET 10.0
   │  ├─ Restore dependencies
   │  ├─ Build (Release configuration)
   │  ├─ Run ALL tests (verify quality)
   │  ├─ Publish executable
   │  ├─ Create GitHub Release
   │  └─ Upload MicPassthrough.exe as asset
   ├─ Duration: ~1-2 minutes
   └─ Result: 🎉 Release published to Releases page
                    ↓
8. PUBLIC RELEASE
   ├─ GitHub Releases page updated
   ├─ MicPassthrough.exe available for download
   ├─ Release notes auto-populated with:
   │  ├─ Features from CHANGELOG
   │  ├─ Installation instructions
   │  ├─ System requirements
   │  └─ Usage examples
   └─ Users can download & install
```

## Two Workflows, Two Purposes

### CI Workflow (`ci.yml`)
- **When:** Every push to main/develop, every pull request
- **Purpose:** Verify code quality
- **Tests:** 11 unit tests (hardware tests skipped)
- **Artifacts:** Test results
- **Duration:** ~1 minute
- **Outcome:** Green ✅ or Red ❌ status check

### Release Workflow (`release.yml`)
- **When:** When you push a version tag (v1.0.0, v1.0.1, etc.)
- **Purpose:** Build and publish executable
- **Tests:** All 11 tests (verify release quality)
- **Artifacts:** GitHub Release + MicPassthrough.exe
- **Duration:** ~2 minutes
- **Outcome:** Release published with downloadable .exe

## Trigger Conditions

```
┌─────────────────────────────────────────┐
│  Event: Push to Repository              │
└─────────────────────────────────────────┘
         ↓                          ↓
    Normal Push              Tag Push (v*.*.*)
         ↓                          ↓
   CI Workflow                Release Workflow
  (Build & Test)          (Build, Test & Release)
         ↓                          ↓
    Status Check            GitHub Release
  ✅ Pass/❌ Fail         🎉 with .exe Download
```

## Version Management

The version flows through the system:

```
src/MicPassthrough/MicPassthrough.csproj
  <Version>1.0.1</Version>
         ↓
  Git tag: v1.0.1
         ↓
  Release workflow extracts: 1.0.1
         ↓
  GitHub Release: "Release v1.0.1"
         ↓
  MicPassthrough.exe embedded version: 1.0.1
         ↓
  Users download v1.0.1
```

## Example: Creating a v1.0.1 Release

```bash
# Step 1: Update .csproj (local)
# Edit src/MicPassthrough/MicPassthrough.csproj
# Change: <Version>1.0.1</Version>

# Step 2: Update CHANGELOG.md (local)
# Edit CHANGELOG.md
# Add v1.0.1 section

# Step 3: Commit & Push to main (local)
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.0.1"
git push origin main
     ↓
# Step 4: CI workflow runs automatically
# ✅ Tests pass
# Done: code is verified
     ↓
# Step 5: Create & push tag (local)
git tag -a v1.0.1 -m "Release version 1.0.1"
git push origin v1.0.1
     ↓
# Step 6: Release workflow runs automatically
# ✅ Build Release configuration
# ✅ All tests pass
# ✅ Publish executable
# ✅ Create GitHub Release
# 🎉 MicPassthrough.exe available for download
```

## Semantic Versioning

```
v 1 . 0 . 1
  │   │   └─ PATCH: Bug fixes (v1.0.0 → v1.0.1)
  │   └───── MINOR: New features (v1.0.0 → v1.1.0)
  └───────── MAJOR: Breaking changes (v1.0.0 → v2.0.0)

Current version: 1.0.0
Next patch release: 1.0.1 (bug fix)
Next minor release: 1.1.0 (new feature)
Next major release: 2.0.0 (breaking change)
```

## File Structure

```
.github/workflows/
├── ci.yml              # CI workflow (push/PR)
└── release.yml         # Release workflow (tags)

docs/
├── CI_CD.md            # CI/CD overview
├── VERSIONING.md       # Version strategy
├── RELEASE_GUIDE.md    # How to create releases
└── ...

src/MicPassthrough/
├── MicPassthrough.csproj  # Version source of truth
└── ...

CHANGELOG.md           # Release notes and history
```

## Status Checks

### On Pull Requests
- CI workflow runs
- Status shows: "CI / build-and-test"
- Must pass before merge to main

### On Push to Main
- CI workflow runs
- Status shows: "CI / build-and-test"
- Usually passes (already tested on PR)

### On Tag Push
- Release workflow runs
- Creates GitHub Release
- Users can download executable

## Monitoring Workflows

1. **GitHub Web:** Actions tab → See live workflow status
2. **Locally:** `git log --oneline` → See commits and tags
3. **Releases:** Releases page → See published versions

## Troubleshooting Workflows

| Issue | Solution |
|-------|----------|
| CI fails on PR | Check workflow logs, fix code, push fix |
| Release not created | Check tag format (v1.0.1 not 1.0.1) |
| Tests fail in release | Fix code, recommit, recreate tag |
| Release has no .exe | Check publish step in workflow logs |

## Key Points

✅ **CI runs automatically** on every push/PR (fast verification)
✅ **Release runs automatically** on every version tag (hands-off publishing)
✅ **Tests are quality gate** - release won't happen if tests fail
✅ **Version is single source** - .csproj is the source of truth
✅ **Releases are reproducible** - same code produces same .exe
✅ **No manual steps** - just create a tag, rest is automated

## Next Steps

- [Read release-guide.md](release-guide.md) to create your first release
- [Read versioning.md](versioning.md) for version strategy
- [Read ci-cd.md](ci-cd.md) for CI details
