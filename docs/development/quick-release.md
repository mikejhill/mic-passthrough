# Quick Release Checklist

Use this checklist when creating a release.

## Pre-Release (Local Development)

- [ ] All features complete and tested locally
- [ ] All tests pass: `dotnet test`
- [ ] Code is committed and pushed to main
- [ ] Pull request merged (if applicable)

## Create Release

### Step 1: Update Version (2 files)

**File 1:** `src/MicPassthrough/MicPassthrough.csproj`
```xml
<Version>1.0.1</Version>  <!-- Change this -->
```

**File 2:** `CHANGELOG.md`
```markdown
## [1.0.1] - 2025-12-31

### Added
- List new features

### Fixed
- List bug fixes

### Changed
- List breaking changes
```

### Step 2: Commit Changes

```bash
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.0.1"
git push origin main
```

### Step 3: Create & Push Tag

```bash
git tag -a v1.0.1 -m "Release version 1.0.1"
git push origin v1.0.1
```

### Step 4: Monitor GitHub Actions

Visit: https://github.com/mikejhill/mic-passthrough/actions

Wait for Release workflow to complete (1-2 minutes).

### Step 5: Verify Release

Visit: https://github.com/mikejhill/mic-passthrough/releases

- [ ] New release v1.0.1 is there
- [ ] Release notes are populated
- [ ] MicPassthrough.exe is downloadable
- [ ] All tests passed (shown in Actions)

## Post-Release

- [ ] Announce release (Discord, Twitter, etc.)
- [ ] Test downloaded .exe works
- [ ] Monitor for issues/feedback
- [ ] Start planning next release

## Troubleshooting

**Release didn't appear?**
- Check tag format: must be `v1.0.1` (with 'v')
- Check Actions tab for workflow errors
- Verify .exe was published in workflow

**Tests failed during release?**
- Don't worry, release won't be created
- Fix the failing tests
- Retry: delete tag, recreate, push again

**Made a mistake in release notes?**
- Edit directly on GitHub Releases page
- No need to recreate release

## Version Format Reference

```
v MAJOR . MINOR . PATCH
v 1    . 0    . 0     (Initial release)
v 1    . 0    . 1     (Bug fix)
v 1    . 1    . 0     (New feature)
v 2    . 0    . 0     (Breaking change)
```

## Common Release Scenarios

### Bug Fix Release (v1.0.0 → v1.0.1)
```bash
# Update .csproj: <Version>1.0.1</Version>
# Add to CHANGELOG: ### Fixed section
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.0.1"
git push
git tag -a v1.0.1 -m "Release version 1.0.1"
git push origin v1.0.1
```

### Feature Release (v1.0.0 → v1.1.0)
```bash
# Update .csproj: <Version>1.1.0</Version>
# Add to CHANGELOG: ### Added section
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 1.1.0"
git push
git tag -a v1.1.0 -m "Release version 1.1.0"
git push origin v1.1.0
```

### Breaking Change Release (v1.0.0 → v2.0.0)
```bash
# Update .csproj: <Version>2.0.0</Version>
# Add to CHANGELOG: ### Changed section + migration notes
git add src/MicPassthrough/MicPassthrough.csproj CHANGELOG.md
git commit -m "chore: Bump version to 2.0.0"
git push
git tag -a v2.0.0 -m "Release version 2.0.0

BREAKING: API changed
See migration guide in release notes"
git push origin v2.0.0
```

## Estimated Time

- Update files: **2 minutes**
- Commit & push: **1 minute**
- GitHub Actions build: **1-2 minutes**
- **Total: ~5 minutes** (hands-off after tag push)

---

Need help? See:
- [release-guide.md](release-guide.md) - Detailed step-by-step
- [versioning.md](versioning.md) - Version strategy
- [workflows.md](workflows.md) - How workflows work
