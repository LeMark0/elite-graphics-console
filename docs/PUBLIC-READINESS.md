# Public-readiness milestones

Each iteration records its decision before implementation, adds relevant regression
checks, and ships independently. A prerelease is not a claim that all audit findings
are resolved. Existing public visibility is retained.

| Order | Milestone | Exit criteria |
|---|---|---|
| 1 | Managed-file write scope | Unknown snapshot files cannot be applied/restored; customisations survive |
| 2 | Target and transaction safety | Validated targets, linked-path rejection, target-wide Apply/Restore/recovery locking, concurrency regression tests |
| 3 | Repository and release gates | Security protections, history scan, Windows CI, artifact checks, support/security guidance; owner chooses license |
| 4 | General-user setup | Explicit installation/baseline selection, optional legacy imports and enhancements, useful empty states |
| 5 | Distribution and servicing | Supported LTS runtime, manifest-based installer, documented update and rollback process |
| 6 | Portability and resilience | Portable presets, bounded inputs, isolated corrupt records, hardware-neutral optional telemetry and private support exports |

Keep broad promotion on hold until the safety and release gates pass. License,
signing budget and the scope of public preset sharing remain owner decisions.
