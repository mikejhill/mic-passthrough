// ORT evaluator rules: allow only MIT-compatible licensing for the project output.
// See https://github.com/oss-review-toolkit/ort for rule DSL reference.

import org.ossreviewtoolkit.evaluator.LicenseView
import org.ossreviewtoolkit.evaluator.RuleSet
import org.ossreviewtoolkit.evaluator.RuleViolations
import org.ossreviewtoolkit.evaluator.licenseRuleSet

val allowedLicenses = setOf(
    "MIT"
)

val ruleViolations = RuleViolations()

ruleViolations += licenseRuleSet(licenseView = LicenseView.CONCLUDED_OR_DECLARED_AND_DETECTED) {
    licenseRule("OnlyAllowApprovedLicenses") {
        require { license !in allowedLicenses }
        error("License '$license' is not in the approved license allowlist: ${allowedLicenses.joinToString()}")
    }
}

RuleSet(ruleViolations)
