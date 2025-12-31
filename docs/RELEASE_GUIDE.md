# Release Guide

This project has a fully automated release workflow. Here's how to create a release.

## Quick Start

**To create a release for version 1.0.1:**

```bash
# 1. Make sure your changes are committed
git status

# 2. Create the tag
git tag -a v1.0.1 -m "Release version 1.0.1"

# 3. Push the tag
git push origin v1.0.1
```

That's it! GitHub Actions will:
- ✅ Build the executable
- ✅ Run all tests
- ✅ Create a GitHub Release
- ✅ Upload MicPassthrough.exe

**View your release at:** https://github.com/mikejhill/mic-passthrough/releases

## Step-by-Step Guide

### 1. Plan Your Release

Decide on the new version number using [Semantic Versioning](https://semver.org/):

- **PATCH** (v1.0.1): Bug fixes, small improvements
- **MINOR** (v1.1.0): New features, backwards compatible
- **MAJOR** (v2.0.0): Breaking changes

### 2. Update Version in Code

Edit `src/MicPassthrough/MicPassthrough.csproj`:

```xml
<!-- Change this line -->
<Version>1.0.1</Version>
```

### 3. Update CHANGELOG.md

Add your changes under the `[Unreleased]` section, then create a new version heading:

```markdown
## [1.0.1] - 2025-12-31

### Added
- New feature X

### Fixed  
- Bug fix Y

### Changed
- Behavior change Z
```

Move everything from `[Unreleased]` to the new version section.

### 4. Commit Your Changes

```bash
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.0.1"
git push origin main
```

### 5. Create the Git Tag

```bash
# Create annotated tag with message
git tag -a v1.0.1 -m "Release version 1.0.1"

# Or with detailed message
git tag -a v1.0.1 -m "Release version 1.0.1

- Fixed audio buffer underruns
- Improved device enumeration
- Updated documentation"
```

### 6. Push the Tag

```bash
git push origin v1.0.1
```

### 7. Watch GitHub Actions

Go to https://github.com/mikejhill/mic-passthrough/actions and watch the Release workflow run:

1. **Extract version** - Gets v1.0.1 from tag ✓
2. **Setup .NET** - Installs SDK ✓
3. **Restore** - Gets dependencies ✓
4. **Build** - Compiles Release build ✓
5. **Test** - Runs all 11 tests ✓
6. **Publish** - Creates executable ✓
7. **Release** - Creates GitHub Release ✓

### 8. Download & Share

Go to https://github.com/mikejhill/mic-passthrough/releases

Your new release will be there with:
- ✅ Release name: "Release v1.0.1"
- ✅ Description: Installation instructions
- ✅ Asset: MicPassthrough.exe (ready to download)

## Release Workflow Details

**File:** `.github/workflows/release.yml`

**Triggers on:** Any tag matching `v*.*.*`

**Does:**
1. Checks out your code
2. Extracts version from tag (v1.0.1 → 1.0.1)
3. Builds in Release configuration
4. Runs full test suite (11 tests)
5. Publishes .NET executable
6. Creates GitHub Release with pre-filled instructions
7. Uploads MicPassthrough.exe as downloadable asset

**If tests fail:** Release is NOT created. Fix the issue and try again.

## Common Tasks

### Create a Bug Fix Release

```bash
# You're at v1.0.0, found a bug
# Fix the bug, commit...

# Update version to v1.0.1
# Edit: src/MicPassthrough/MicPassthrough.csproj
<Version>1.0.1</Version>

# Update changelog
# Edit: CHANGELOG.md
## [1.0.1] - 2025-12-31
### Fixed
- Fixed audio buffer issue

# Commit
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.0.1"
git push

# Create release
git tag -a v1.0.1 -m "Release version 1.0.1"
git push origin v1.0.1

# ✅ GitHub Actions creates release automatically
```

### Create a Feature Release

```bash
# You're at v1.0.0, added new features
# Features are committed...

# Update version to v1.1.0 (MINOR bump)
# Edit: src/MicPassthrough/MicPassthrough.csproj
<Version>1.1.0</Version>

# Update changelog
# Edit: CHANGELOG.md
## [1.1.0] - 2025-12-31
### Added
- New --monitor flag for audio monitoring
- Support for multiple input devices

# Commit
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.1.0"
git push

# Create release
git tag -a v1.1.0 -m "Release version 1.1.0"
git push origin v1.1.0

# ✅ GitHub Actions creates release automatically
```

### Create a Breaking Change Release

```bash
# You're at v1.0.0, changed CLI interface
# Changes are committed...

# Update version to v2.0.0 (MAJOR bump)
# Edit: src/MicPassthrough/MicPassthrough.csproj
<Version>2.0.0</Version>

# Update changelog with migration notes
# Edit: CHANGELOG.md
## [2.0.0] - 2025-12-31
### Changed
- CLI argument renamed from --mic to --microphone
- Configuration file format updated

### Added
- Migration guide from v1.x to v2.x

# Commit
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 2.0.0"
git push

# Create release
git tag -a v2.0.0 -m "Release version 2.0.0

BREAKING: CLI interface has changed
See migration guide in release notes"
git push origin v2.0.0

# ✅ GitHub Actions creates release automatically
```

## Troubleshooting

### Tests failed, release not created

✓ Expected behavior. Tests verify code quality before release.

**Fix:** Look at the test output, fix the issue, commit, and retry.

### I pushed a tag but it didn't trigger

Check that:
- Tag matches `v*.*.*` format (e.g., `v1.0.1`, not `1.0.1`)
- Tag was pushed: `git push origin v1.0.1`
- GitHub Actions is enabled in your repository

### I made a mistake in the tag

Delete and recreate:

```bash
# Delete locally
git tag -d v1.0.1

# Delete on GitHub
git push origin :refs/tags/v1.0.1

# Create correct tag
git tag -a v1.0.1 -m "Release version 1.0.1"
git push origin v1.0.1
```

### Release exists but executable is missing

The publish step failed. Check the workflow logs for errors. Common causes:
- .csproj is misconfigured
- Missing dependencies
- Path issues

## What Gets Released

The release contains:
- **MicPassthrough.exe** - Self-contained executable
- **Release Notes** - Pre-filled with features and installation instructions
- **CHANGELOG Link** - Points to full changelog on GitHub

Users can download the .exe directly from the Releases page and run it.

## Next Steps

Once you've created a release:

1. **Announce it** - Share on social media, Discord, forums
2. **Update website** - Link to the new version
3. **Monitor issues** - Watch for user feedback
4. **Plan next release** - Work on features for v1.0.2 or v1.1.0

Happy releasing! 🚀
