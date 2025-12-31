# Versioning and Releases

This document explains how versioning and releases work in this project.

## Semantic Versioning

This project follows [Semantic Versioning 2.0.0](https://semver.org/):

```
v MAJOR . MINOR . PATCH
  1    .   0    .   0

- MAJOR: Breaking changes to the API or CLI
- MINOR: New features (backwards compatible)
- PATCH: Bug fixes (backwards compatible)
```

### Examples

- `v1.0.0` → First release
- `v1.1.0` → Added new feature (--enable-monitor)
- `v1.0.1` → Fixed bug in audio buffering
- `v2.0.0` → Breaking change (different CLI interface)

## Release Process

### Step 1: Update Version in .csproj

Edit `src/MicPassthrough/MicPassthrough.csproj`:

```xml
<Version>1.0.1</Version>
```

This version is automatically embedded in the executable.

### Step 2: Update CHANGELOG.md

Add your changes under `[Unreleased]` section, then create a new version section:

```markdown
## [1.0.1] - 2025-12-31

### Added
- New feature description

### Fixed
- Bug fix description

### Changed
- Behavior change description
```

### Step 3: Commit Changes

```bash
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.0.1"
git push
```

### Step 4: Create Version Tag

```bash
# Create an annotated tag
git tag -a v1.0.1 -m "Release version 1.0.1"

# Push the tag to GitHub
git push origin v1.0.1
```

### Step 5: GitHub Actions Automatically

When you push a tag matching `v*.*.*`:

1. ✅ GitHub Actions detects the tag
2. ✅ Extracts version: `1.0.1`
3. ✅ Runs all tests to ensure quality
4. ✅ Builds executable in Release mode
5. ✅ Creates GitHub Release with notes
6. ✅ Uploads `MicPassthrough.exe` as downloadable asset

### Result

Your release appears on the [Releases page](https://github.com/mikejhill/mic-passthrough/releases) with:
- Release name: `Release v1.0.1`
- Description: Pre-filled with installation instructions
- Asset: `MicPassthrough.exe` (ready to download)

## Automated Release Workflow

**File:** `.github/workflows/release.yml`

Triggers on: `push` with tags matching `v*.*.*`

Steps:
1. Checkout code
2. Extract version from git tag
3. Setup .NET SDK
4. Restore dependencies
5. Build in Release configuration
6. Run all tests (verify nothing broke)
7. Publish executable
8. Create GitHub Release
9. Upload MicPassthrough.exe

### Release Notes

The release is automatically populated with:
- Version number
- Link to CHANGELOG.md
- Installation instructions
- System requirements
- Download instructions

You can edit the release notes on GitHub after creation if needed.

## Versioning Strategy

### PATCH Version (x.y.Z)

**When:** Bug fixes, internal improvements

Examples:
- Fixed audio buffer underruns
- Improved error messages
- Fixed WASAPI device enumeration edge case

**Increment:** v1.0.0 → v1.0.1

### MINOR Version (x.Y.z)

**When:** New features (backwards compatible)

Examples:
- Added --enable-monitor flag
- Added --prebuffer-frames option
- Added verbose logging

**Increment:** v1.0.0 → v1.1.0

### MAJOR Version (X.y.z)

**When:** Breaking changes

Examples:
- Change CLI argument names
- Change audio routing behavior
- Deprecate old options

**Increment:** v1.0.0 → v2.0.0

## Quick Reference

### Create a Release

```bash
# 1. Edit version in .csproj
# 2. Update CHANGELOG.md
# 3. Commit
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.0.1"

# 4. Create tag
git tag -a v1.0.1 -m "Release version 1.0.1"

# 5. Push
git push origin main
git push origin v1.0.1
```

### View Release History

```bash
# List all tags
git tag -l

# Show details of a tag
git show v1.0.1

# View on GitHub
# https://github.com/mikejhill/mic-passthrough/releases
```

## FAQ

**Q: What if I push a tag but tests fail?**
A: The release workflow stops at the test step. No release is created. Fix the issue, amend the tag, and try again.

**Q: Can I create a release manually?**
A: Yes, GitHub allows manual release creation, but using git tags is recommended for consistency.

**Q: What if I make a mistake in the version?**
A: Delete the tag locally and on GitHub:
```bash
git tag -d v1.0.1              # Delete locally
git push origin :v1.0.1         # Delete on GitHub
```

**Q: Should the version match what's in the .csproj?**
A: Yes! Always update the .csproj version before creating the tag. The workflow extracts the version from the git tag, so keep them in sync.

**Q: Can I release from branches other than main?**
A: Yes, the release workflow triggers on any tag matching `v*.*.*`, regardless of branch. However, best practice is to only release from `main`.

## Continuous Integration

The separate CI workflow (`.github/workflows/ci.yml`) runs on:
- Every push to `main` or `develop`
- Every pull request

The Release workflow runs on:
- Every tag push matching `v*.*.*`

This means you can verify code quality with CI before creating a release tag.
