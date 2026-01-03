/*
 * Copyright (C) 2024 MicPassthrough Contributors
 *
 * Licensed under the MIT License.
 *
 * ORT Evaluator Rules for License Compliance
 * 
 * This file defines the license policy for the MicPassthrough project.
 * The project is MIT-licensed and requires all dependencies to use
 * MIT-compatible (permissive) licenses.
 * 
 * Approach: WHITELIST-BASED
 * Only licenses explicitly listed in allowedLicenses are permitted.
 * This ensures high confidence that incompatible licenses are not missed.
 * 
 * Reference: https://oss-review-toolkit.org/ort/docs/configuration/evaluator-rules
 */

// WHITELIST: MIT-compatible permissive licenses
// These licenses allow usage in MIT-licensed projects without restrictions
val allowedLicenses = setOf(
    // MIT and variants
    "MIT",
    "MIT-0",
    
    // Apache family
    "Apache-2.0",
    
    // BSD family  
    "BSD-2-Clause",
    "BSD-3-Clause",
    "BSD-3-Clause-Clear",
    "0BSD",
    
    // Other permissive licenses
    "ISC",
    "Unlicense",
    "CC0-1.0",
    
    // Boost Software License
    "BSL-1.0",
    
    // zlib/libpng
    "Zlib",
    
    // PostgreSQL License
    "PostgreSQL",
    
    // Python Software Foundation License
    "Python-2.0",
    
    // Common permissive licenses
    "MS-PL"  // Microsoft Public License
)

// BLACKLIST: Known incompatible copyleft licenses
// This list is maintained separately for cross-reference validation.
// These licenses MUST NOT appear in the whitelist above.
val disallowedLicenses = setOf(
    // GPL family (strong copyleft - requires derivative works to use same license)
    "GPL-1.0", "GPL-1.0-only", "GPL-1.0-or-later", "GPL-1.0+",
    "GPL-2.0", "GPL-2.0-only", "GPL-2.0-or-later", "GPL-2.0+",
    "GPL-3.0", "GPL-3.0-only", "GPL-3.0-or-later", "GPL-3.0+",
    
    // AGPL family (strong copyleft with network clause)
    "AGPL-1.0", "AGPL-1.0-only", "AGPL-1.0-or-later",
    "AGPL-3.0", "AGPL-3.0-only", "AGPL-3.0-or-later",
    
    // LGPL family (weak copyleft)
    "LGPL-2.0", "LGPL-2.0-only", "LGPL-2.0-or-later", "LGPL-2.0+",
    "LGPL-2.1", "LGPL-2.1-only", "LGPL-2.1-or-later", "LGPL-2.1+",
    "LGPL-3.0", "LGPL-3.0-only", "LGPL-3.0-or-later", "LGPL-3.0+",
    
    // Mozilla Public License (weak copyleft)
    "MPL-1.0", "MPL-1.1", "MPL-2.0",
    
    // Eclipse Public License (weak copyleft)
    "EPL-1.0", "EPL-2.0",
    
    // Common Development and Distribution License (weak copyleft)
    "CDDL-1.0", "CDDL-1.1",
    
    // Open Software License
    "OSL-1.0", "OSL-2.0", "OSL-2.1", "OSL-3.0",
    
    // European Union Public License
    "EUPL-1.0", "EUPL-1.1", "EUPL-1.2",
    
    // Creative Commons ShareAlike (copyleft)
    "CC-BY-SA-1.0", "CC-BY-SA-2.0", "CC-BY-SA-2.5", "CC-BY-SA-3.0", "CC-BY-SA-4.0"
)

// VALIDATION: Ensure no overlap between whitelist and blacklist
val overlap = allowedLicenses.intersect(disallowedLicenses)
if (overlap.isNotEmpty()) {
    throw IllegalStateException(
        "Configuration error: The following licenses appear in both allowedLicenses and disallowedLicenses: $overlap"
    )
}

// Helper function for fix instructions
fun PackageRule.howToFixLicenseIssue(license: String, isKnownCopyleft: Boolean): String {
    return if (isKnownCopyleft) {
        """
        Package uses COPYLEFT license '$license' which is INCOMPATIBLE with MIT.
        This license requires derivative works to also use '$license', which conflicts with MIT licensing.
        
        REQUIRED ACTIONS:
        1. Find an alternative package with a permissive license (MIT, Apache-2.0, BSD, etc.)
        2. Remove this dependency if not essential
        3. Consult legal counsel if this dependency is critical
        
        Allowed licenses: ${allowedLicenses.sorted().joinToString(", ")}
        """.trimIndent()
    } else {
        """
        Package uses license '$license' which is NOT in the approved whitelist.
        This license has not been reviewed for compatibility with MIT.
        
        REQUIRED ACTIONS:
        1. Verify this license is permissive and compatible with MIT
        2. If compatible, add it to allowedLicenses in .ort/policy/rules.kts
        3. If incompatible, find an alternative package
        4. Consult legal counsel if uncertain
        
        Allowed licenses: ${allowedLicenses.sorted().joinToString(", ")}
        Known incompatible licenses: ${disallowedLicenses.sorted().take(10).joinToString(", ")}...
        """.trimIndent()
    }
}

// RULE SET: Enforce whitelist-based license compliance
ruleSet {
    // Rule: Only allow whitelisted licenses
    packageRule("REQUIRE_WHITELISTED_LICENSE") {
        require {
            // Apply to all packages
            true
        }
        
        licenseRule("LICENSE_MUST_BE_ALLOWED", LicenseView.CONCLUDED_OR_DECLARED_AND_DETECTED) {
            require {
                // WHITELIST CHECK: License must NOT be in the allowed list (triggers error)
                license !in allowedLicenses
            }
            
            // Determine if this is a known copyleft license
            val isKnownCopyleft = license in disallowedLicenses
            
            error(
                "License '$license' is not whitelisted for package '${pkg.id.toCoordinates()}'.",
                howToFixLicenseIssue(license, isKnownCopyleft)
            )
        }
    }
}
