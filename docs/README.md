# Documentation

This directory contains all technical documentation for the MicPassthrough project.

## Structure

### `/architecture`
Technical documentation about the system design, code organization, and implementation details.

- [REFACTORING.md](architecture/REFACTORING.md) - History of architecture decisions and refactoring notes

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
- Place user-facing documentation in the root README.md
- Place technical/architectural docs in `/architecture`
- Document significant decisions as ADRs in `/adr`
- Keep docs up-to-date with code changes
