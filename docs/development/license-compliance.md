# License Compliance

This document describes the automated license compliance process for the MicPassthrough project using the OSS Review Toolkit (ORT).

## Overview

MicPassthrough is licensed under the **MIT License**, one of the most permissive open-source licenses. To maintain compliance and avoid legal issues, all upstream dependencies must use licenses that are compatible with MIT.

The project uses **OSS Review Toolkit (ORT)** to automatically scan all NuGet dependencies and enforce a **whitelist-based** license policy.

## What is License Compatibility?

License compatibility refers to whether two licenses can be used together in the same project. Some licenses, particularly **copyleft licenses** like GPL, require that any project using GPL-licensed code must also be licensed under GPL. This creates incompatibility with MIT.

### MIT-Compatible Licenses (Whitelist)

The following licenses are **explicitly whitelisted** and can be used:

- **MIT** and MIT-0
- **Apache-2.0**
- **BSD-2-Clause**, BSD-3-Clause, BSD-3-Clause-Clear, 0BSD
- **ISC**
- **Unlicense**
- **CC0-1.0** (Creative Commons Zero)
- **BSL-1.0** (Boost Software License)
- **Zlib**
- **PostgreSQL License**
- **Python-2.0** (Python Software Foundation License)
- **MS-PL** (Microsoft Public License)

These are all **permissive licenses** that allow usage in MIT-licensed projects without restrictions.

### Incompatible Licenses (Blacklist Reference)

The following licenses are **NOT compatible** with MIT and will cause CI to fail:

- **GPL family** (GPL-1.0, GPL-2.0, GPL-3.0) - Strong copyleft
- **AGPL family** (AGPL-1.0, AGPL-3.0) - Strong copyleft with network clause
- **LGPL family** (LGPL-2.0, LGPL-2.1, LGPL-3.0) - Weak copyleft
- **MPL** (Mozilla Public License 1.0, 1.1, 2.0) - Weak copyleft
- **EPL** (Eclipse Public License 1.0, 2.0) - Weak copyleft
- **CDDL** (Common Development and Distribution License 1.0, 1.1) - Weak copyleft
- **OSL** (Open Software License 1.0-3.0)
- **EUPL** (European Union Public License 1.0-1.2)
- **CC-BY-SA** (Creative Commons ShareAlike - copyleft)

**Copyleft licenses** require that derivative works also be licensed under the same license, which conflicts with MIT's permissive nature.

## Automated License Checking with ORT

### How It Works

The ORT workflow (`.github/workflows/ort-license.yml`) automatically:

1. **Analyzes all NuGet dependencies** (direct and transitive) using ORT's analyzer
2. **Extracts license information** from package metadata, LICENSE files, and source code
3. **Evaluates against policy rules** defined in `.ort/policy/rules.kts`
4. **Enforces the whitelist** - only explicitly allowed licenses pass
5. **Fails the build** if any dependency uses a non-whitelisted license
6. **Generates comprehensive reports** in multiple formats (CycloneDX, SPDX, WebApp)
7. **Uploads results as CI artifacts** for review

### Whitelist-Based Approach

**Why whitelist instead of blacklist?**

- **High confidence**: Only explicitly approved licenses are allowed
- **Prevents oversights**: New/unknown licenses must be manually reviewed before approval
- **Clear policy**: Easy to understand what licenses are acceptable
- **Cross-validation**: Blacklist is maintained separately to ensure no conflicts

The whitelist is defined in `.ort/policy/rules.kts` in the `allowedLicenses` set.

### CI Workflow

Every commit and pull request to `main` or `develop` branches automatically triggers:

1. **ORT Analysis** - Scans all NuGet packages
2. **License Evaluation** - Checks against whitelist policy
3. **Report Generation** - Creates CycloneDX SBOM, SPDX documents, and HTML reports
4. **Build Status** - Passes or fails based on policy violations
5. **Artifact Upload** - Results available for 90 days

## Configuring License Policy

### Adding a New Allowed License

If a dependency uses a permissive license not in the whitelist:

1. **Verify the license is permissive** and compatible with MIT
   - Check https://opensource.org/licenses
   - Consult legal guidance if uncertain

2. **Edit `.ort/policy/rules.kts`**:
   ```kotlin
   val allowedLicenses = setOf(
       "MIT",
       "Apache-2.0",
       // ... existing licenses ...
       "NewLicense-1.0"  // Add here with SPDX identifier
   )
   ```

3. **Document the change** in this file under the whitelist section

4. **Commit and push** - CI will validate the updated policy

### Cross-Validation

The rules file maintains both a whitelist (`allowedLicenses`) and a blacklist (`disallowedLicenses`) for validation:

- **Startup validation**: ORT checks that no license appears in both lists
- **Prevents configuration errors**: Ensures policy consistency
- **Clear separation**: Explicitly documents incompatible licenses

## Adding New Dependencies

When adding a new NuGet package dependency:

1. **Add the dependency** to the .csproj file
2. **Run locally** (optional): `dotnet restore` to fetch the package
3. **Commit and push** - ORT will automatically scan the new dependency
4. **If CI fails**:
   - Check the ORT results artifact to see which license was detected
   - If the license is permissive and MIT-compatible, add it to the whitelist
   - If the license is copyleft or incompatible, find an alternative package

### If a Dependency Uses an Incompatible License

If you need a dependency that uses an incompatible license:

1. **Find an alternative** with a compatible license
2. **Implement the functionality yourself** (if simple)
3. **Discuss with maintainers** to determine if:
   - The project can switch to MIT license
   - Dynamic linking is possible (for LGPL - requires legal consultation)
   - The license is actually compatible (some licenses have exceptions)

**NEVER bypass the license check.** This creates legal liability for the project and all users.

## Local Testing

### Running ORT Locally

To test license compliance before pushing:

```bash
# Install ORT (requires JDK 11+)
# See https://oss-review-toolkit.org/ort/docs/getting-started/installation

# Run ORT analyzer
ort analyze -i . -o .ort/local

# Run ORT evaluator with local rules
ort evaluate -i .ort/local/analyzer-result.yml \
  -o .ort/local \
  --rules-file .ort/policy/rules.kts

# Generate reports
ort report -i .ort/local/evaluation-result.yml \
  -o .ort/local/reports \
  -f WebApp,CycloneDx
```

### Quick Dependency Check

```powershell
# List all NuGet packages
dotnet list src/MicPassthrough/MicPassthrough.csproj package --include-transitive

# Check NuGet.org for license information
# Visit https://www.nuget.org/packages/{PackageName}
```

## Troubleshooting

### CI Fails with "License not in whitelist"

1. **Check the ORT results artifact** uploaded by CI
2. **Find the specific package and license** in the evaluation report
3. **Verify the license** on NuGet.org and the package's repository
4. **If MIT-compatible**: Add to whitelist in `.ort/policy/rules.kts`
5. **If incompatible**: Find an alternative dependency

### Unknown or Unlicensed Package

If ORT cannot detect a license:

1. **Check the package manually** on NuGet.org and GitHub
2. **Review the LICENSE file** in the package's repository
3. **Contact the package maintainer** if license is missing
4. **Find an alternative** if license cannot be verified

### ORT Configuration Issues

If ORT fails to run or produces errors:

1. **Check ORT version** - Ensure using compatible version (v1)
2. **Review ORT logs** in CI workflow output
3. **Validate rules.kts syntax** - Kotlin syntax errors will fail evaluation
4. **Consult ORT documentation** - https://oss-review-toolkit.org/ort/docs/

## Current Dependencies and Their Licenses

All current dependencies are MIT-licensed and whitelisted:

| Package | Version | License | Verified |
|---------|---------|---------|----------|
| NAudio | 2.2.1 | MIT | ✅ |
| System.CommandLine | 2.0.0-beta4 | MIT | ✅ |
| Microsoft.Extensions.Logging | 9.0.0 | MIT | ✅ |
| Microsoft.Extensions.Logging.Console | 9.0.0 | MIT | ✅ |
| System.Drawing.Common | 8.0.0 | MIT | ✅ |

All transitive dependencies are also MIT-licensed as they come from Microsoft and the .NET ecosystem.

## ORT Configuration Files

### `.ort/policy/rules.kts`

Kotlin-based policy rules that define:
- `allowedLicenses` - Whitelist of permitted licenses (SPDX identifiers)
- `disallowedLicenses` - Blacklist for cross-reference validation
- Policy rules that enforce the whitelist
- Error messages with remediation guidance

### `.github/workflows/ort-license.yml`

GitHub Actions workflow that:
- Runs ORT analyzer on NuGet packages
- Evaluates results against policy rules
- Generates CycloneDX and SPDX reports
- Uploads results as artifacts
- Comments on pull requests with compliance status

## References

- [OSS Review Toolkit Documentation](https://oss-review-toolkit.org/ort/docs/)
- [ORT Evaluator Rules](https://oss-review-toolkit.org/ort/docs/configuration/evaluator-rules)
- [MIT License](https://opensource.org/licenses/MIT)
- [OSI Approved Licenses](https://opensource.org/licenses/category)
- [License Compatibility](https://en.wikipedia.org/wiki/License_compatibility)
- [SPDX License List](https://spdx.org/licenses/)
- [CycloneDX SBOM Standard](https://cyclonedx.org/)
- [Understanding FOSS License Compatibility](https://www.gnu.org/licenses/license-compatibility.en.html)
