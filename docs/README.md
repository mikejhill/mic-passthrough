# Documentation

This directory contains all technical documentation for the MicPassthrough project.

## Structure

### `/guides`
User-facing guides and feature documentation.

- [testing.md](guides/testing.md) - Test suite documentation and hardware test setup
- [daemon-mode.md](guides/daemon-mode.md) - Daemon mode with system tray UI
- [auto-switch.md](guides/auto-switch.md) - Auto-switch mode improvements and call detection

### `/development`
Developer processes, workflows, and release management.

- [ci-cd.md](development/ci-cd.md) - CI/CD configuration details
- [workflows.md](development/workflows.md) - GitHub Actions workflow diagrams
- [quick-release.md](development/quick-release.md) - 1-page release checklist
- [release-guide.md](development/release-guide.md) - Complete release walkthrough
- [versioning.md](development/versioning.md) - Semantic versioning strategy
- [license-compliance.md](development/license-compliance.md) - OSS license compliance and dependency checking

### `/architecture`
Technical documentation about the system design, code organization, and implementation details.

- [refactoring.md](architecture/refactoring.md) - History of architecture decisions and refactoring notes

### `/adr`
Architecture Decision Records (ADRs) document significant architectural decisions made throughout the project lifecycle.

This project uses the **MADR (Markdown Any Decision Records)** template format, a widely-adopted standard based on Michael Nygard's original ADR concept.

**Official Resources:**
- [MADR Template Documentation](https://adr.github.io/madr/) - Modern ADR template format
- [ADR GitHub Organization](https://adr.github.io/) - Community standards and tools
- [Original ADR Concept](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) by Michael Nygard

**Format Overview:**
```markdown
# [Title]

* Status: [proposed | rejected | accepted | deprecated | superseded by ADR-XXXX]
* Date: YYYY-MM-DD

## Context and Problem Statement
[Describe the context and problem statement]

## Decision Drivers
* [driver 1]
* [driver 2]

## Considered Options
* [option 1]
* [option 2]

## Decision Outcome
Chosen option: "[option X]", because [justification]
```

To create a new ADR:
1. Copy `adr/template.md` to `adr/XXXX-descriptive-title.md` (use sequential numbering)
2. Fill in all sections following the MADR format
3. Submit for review

## Contributing

When adding new documentation:
- Place project overview in the root README.md
- Place user guides and feature docs in `/guides`
- Place developer processes and CI/CD docs in `/development`
- Place technical/architectural docs in `/architecture`
- Document significant decisions as ADRs in `/adr`
- Keep docs up-to-date with code changes
