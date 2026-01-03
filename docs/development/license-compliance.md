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

The project uses the `.github/workflows/ort-license.yml` workflow to automatically check license compliance on every commit and pull request. This workflow:

1. **Scans all NuGet dependencies** (direct and transitive)
2. **Generates a license report** with details of each dependency's license
3. **Checks for incompatible licenses** against the copyleft blocklist
4. **Fails the build** if any incompatible license is detected
5. **Uploads the license report** as a CI artifact for manual review

### Tools Used

The workflow uses **dotnet-project-licenses**, a .NET-specific tool that:
- Extracts license information from NuGet packages
- Generates JSON and text reports
- Includes license text for each dependency

## How to Run License Checks Locally

### Prerequisites

Install the dotnet-project-licenses tool globally:

```powershell
dotnet tool install --global dotnet-project-licenses
```

### Generate License Report

```powershell
# Navigate to the project root
cd /path/to/mic-passthrough

# Generate license report
dotnet-project-licenses --input src/MicPassthrough/MicPassthrough.csproj `
  --output-directory license-report `
  --export-license-texts `
  --json
```

This creates a `license-report/` directory with:
- `licenses.json` - Machine-readable license data
- `licenses.txt` - Human-readable license summary
- Individual license text files for each dependency

### Manual Review

Open `license-report/licenses.json` and check the `LicenseInformationOrigin` field for each package. Ensure all licenses are in the "MIT-Compatible" list above.

## Adding New Dependencies

When adding a new NuGet package dependency:

1. **Check the package's license** on NuGet.org or the project's GitHub repository
2. **Verify it's MIT-compatible** using the list above
3. **Add the dependency** to the .csproj file
4. **Run license check locally** to confirm compatibility
5. **Commit and push** - CI will run the automated check

### If a Dependency Uses an Incompatible License

If you need a dependency that uses an incompatible license:

1. **Find an alternative** with a compatible license
2. **Implement the functionality yourself** (if simple)
3. **Discuss with maintainers** to determine if:
   - The project can switch to MIT license
   - Dynamic linking is possible (for LGPL)
   - The license is actually compatible (some licenses have exceptions)

**NEVER ignore or bypass the license check.** This creates legal liability for the project and all users.

## Updating the License Policy

If you need to add a new compatible license to the allowlist or update the copyleft blocklist:

1. Edit `.github/workflows/ort-license.yml`
2. Add the license identifier to the appropriate array in the "Check for incompatible licenses" step
3. Document the change in this file
4. Get approval from the project maintainer before merging

## Troubleshooting

### CI Fails with "Found incompatible license"

1. Check the CI logs to identify which dependency triggered the failure
2. Review the license report artifact uploaded by CI
3. Find the specific package and its license
4. Either:
   - Replace the dependency with a compatible alternative
   - Remove the dependency if it's not essential
   - Contact the dependency maintainer to request a license change

### License Report Shows Unknown License

If a package shows an unknown or missing license:

1. Check the package's NuGet page and GitHub repository manually
2. If the license is actually compatible, update the workflow to recognize it
3. If the license is missing or proprietary, find an alternative

### False Positives

If the CI incorrectly flags a compatible license:

1. Check if the license uses a variant identifier (e.g., "MIT-Modern" vs "MIT")
2. Add the variant to the workflow configuration
3. Document the variant in this file

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
- [dotnet-project-licenses GitHub](https://github.com/tomchavakis/nuget-license)
- [Understanding FOSS License Compatibility](https://www.gnu.org/licenses/license-compatibility.en.html)
