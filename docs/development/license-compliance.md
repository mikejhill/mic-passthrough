# License Compliance

This document describes the license compliance process for the MicPassthrough project.

## Overview

MicPassthrough is licensed under the **MIT License**, one of the most permissive open-source licenses. To maintain compliance and avoid legal issues, all upstream dependencies must use licenses that are compatible with MIT.

## What is License Compatibility?

License compatibility refers to whether two licenses can be used together in the same project. Some licenses, particularly **copyleft licenses** like GPL, require that any project using GPL-licensed code must also be licensed under GPL. This creates incompatibility with MIT.

### MIT-Compatible Licenses (Permissive)

The following licenses are compatible with MIT and can be used:

- **MIT** and MIT-0
- **Apache-2.0**
- **BSD-2-Clause**, BSD-3-Clause, BSD-3-Clause-Clear
- **ISC**
- **0BSD** (BSD Zero Clause)
- **Unlicense**
- **CC0-1.0** (Creative Commons Zero)
- **BSL-1.0** (Boost Software License)
- **Zlib**
- **PostgreSQL License**

These are all **permissive licenses** that allow usage in MIT-licensed projects without restrictions.

### Incompatible Licenses (Copyleft)

The following licenses are **NOT compatible** with MIT and will cause CI to fail:

- **GPL family** (GPL-1.0, GPL-2.0, GPL-3.0) - Strong copyleft
- **AGPL family** (AGPL-1.0, AGPL-3.0) - Strong copyleft with network clause
- **LGPL family** (LGPL-2.0, LGPL-2.1, LGPL-3.0) - Weak copyleft
- **MPL** (Mozilla Public License 1.0, 1.1, 2.0) - Weak copyleft
- **EPL** (Eclipse Public License 1.0, 2.0) - Weak copyleft
- **CDDL** (Common Development and Distribution License 1.0, 1.1) - Weak copyleft
- **OSL-3.0** (Open Software License)
- **EUPL** (European Union Public License 1.1, 1.2)

**Copyleft licenses** require that derivative works also be licensed under the same license, which conflicts with MIT's permissive nature.

## Automated License Checking

### CI Workflow

The project uses the `.github/workflows/license-compliance.yml` workflow to automatically check license compliance on every commit and pull request. This workflow:

1. **Lists all NuGet dependencies** (direct and transitive) using `dotnet list package`
2. **Documents known dependency licenses** with references to source repositories
3. **Generates a license compliance report** with current status
4. **Validates against known copyleft patterns** to catch GPL, LGPL, MPL, EPL, CDDL, etc.
5. **Uploads the compliance report** as a CI artifact for review

### Current Approach

The current implementation uses a **documented verification** approach rather than automated parsing because:

1. **NuGet metadata reliability**: Not all packages properly declare their license in machine-readable format
2. **Simplicity**: All current dependencies are Microsoft/ecosystem packages with well-known MIT licenses
3. **Maintainability**: Manual verification at dependency addition time is more reliable than parsing
4. **Transparency**: Clear documentation of what licenses are used and why they're compatible

For this project's small, stable dependency set, this approach provides excellent compliance assurance without complex tooling dependencies.

## How to Run License Checks Locally

### Check Current Dependencies

```powershell
# Navigate to the project root
cd /path/to/mic-passthrough

# List all packages including transitive dependencies
dotnet list src/MicPassthrough/MicPassthrough.csproj package --include-transitive

# Check for vulnerable packages (includes license warnings)
dotnet list src/MicPassthrough/MicPassthrough.csproj package --vulnerable
```

### Verify License Information

For each dependency, verify its license:

1. **Visit NuGet.org**: Search for the package (e.g., `NAudio`)
2. **Check License field**: Should show "MIT" or link to license
3. **Visit source repository**: Verify LICENSE file matches
4. **Cross-reference with allowlist**: Ensure license is in the MIT-compatible list above

### Generate Local Report

The CI workflow generates a markdown report. To preview locally:

```bash
# Run the workflow steps manually
dotnet restore
dotnet list src/MicPassthrough/MicPassthrough.csproj package --include-transitive > packages.txt

# Review the output
cat packages.txt
```

## Adding New Dependencies

When adding a new NuGet package dependency:

1. **Check the package's license** on NuGet.org or the project's GitHub repository
2. **Verify it's MIT-compatible** using the list above
3. **Add the dependency** to the .csproj file
4. **Run `dotnet list package`** to verify it appears correctly
5. **Update the license documentation** in this file if it's a new license type
6. **Commit and push** - CI will validate the dependency list

### If a Dependency Uses an Incompatible License

If you need a dependency that uses an incompatible license:

1. **Find an alternative** with a compatible license
2. **Implement the functionality yourself** (if simple)
3. **Discuss with maintainers** to determine if:
   - The project can switch to MIT license
   - Dynamic linking is possible (for LGPL - consult legal advice)
   - The license is actually compatible (some licenses have exceptions)

**NEVER ignore or bypass the license check.** This creates legal liability for the project and all users.

## Updating the License Policy

If you need to update the license compliance workflow:

1. Edit `.github/workflows/license-compliance.yml`
2. Update the known dependency licenses in the "Check for GPL/copyleft licenses" step
3. Add new dependencies to the "Generate license documentation" step
4. Document any license policy changes in this file
5. Get approval from the project maintainer before merging

## Troubleshooting

### CI Reports Incorrect License Information

The current workflow uses documented verification. If a dependency changes its license:

1. Check the package's current license on NuGet.org and GitHub
2. If the license is still MIT-compatible, update the workflow to reflect the current license
3. If the license is no longer compatible, find an alternative dependency

### Adding a New MIT-Compatible License

If a dependency uses a permissive license not listed in this document (e.g., "Fair License", "NCSA"):

1. Research the license to confirm it's permissive and MIT-compatible
2. Add it to the "MIT-Compatible Licenses" list in this document
3. Document why it's compatible (reference: https://opensource.org/licenses or legal guidance)
4. Update the workflow documentation if needed

### Package Shows "License URL" Instead of License ID

Some NuGet packages only provide a license URL instead of an SPDX identifier:

1. Visit the license URL
2. Identify the actual license (often MIT, Apache-2.0, or BSD)
3. Verify it's in the compatible list
4. Document the package and its license in this file

## Current Dependencies and Their Licenses

As of the latest check, all current dependencies are MIT-licensed:

| Package | Version | License |
|---------|---------|---------|
| NAudio | 2.2.1 | MIT |
| System.CommandLine | 2.0.0-beta4 | MIT |
| Microsoft.Extensions.Logging | 9.0.0 | MIT |
| Microsoft.Extensions.Logging.Console | 9.0.0 | MIT |
| System.Drawing.Common | 8.0.0 | MIT |

All transitive dependencies are also MIT-licensed as they come from Microsoft and the .NET ecosystem, which primarily uses MIT.

## References

- [MIT License](https://opensource.org/licenses/MIT)
- [OSI Approved Licenses](https://opensource.org/licenses/category)
- [License Compatibility Matrix](https://en.wikipedia.org/wiki/License_compatibility)
- [Understanding FOSS License Compatibility](https://www.gnu.org/licenses/license-compatibility.en.html)
- [NuGet Package License Metadata](https://learn.microsoft.com/en-us/nuget/reference/nuspec#license)
- [.NET CLI Package Commands](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-list-package)
