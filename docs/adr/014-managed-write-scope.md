# 014 — Explicit managed-file write scope

Status: Accepted, 2026-09-26. Recorded before implementation.

The first public-readiness milestone restricts Apply and Restore to Settings.xml,
DisplaySettings.xml, GraphicsConfigurationOverride.xml, StartPreset.start and
versioned Custom.<major>.<minor>.fxcfg files. Capture continues preserving other
supported snapshot files. An unknown file may remain in a snapshot unchanged,
but applying a new or changed unknown file must fail before creating a transaction.
Unknown destination files absent from a snapshot are preserved.

Transaction manifests contain only managed files. Restore validates every manifest
entry before writing; older transactions containing unknown files fail closed with
an explanation rather than restoring those files. Unknown XML nodes inside managed
files, including HUD colour and graphics overrides, retain existing byte-preserving
behaviour.

This deliberately small milestone does not establish directory identity, eliminate
linked-parent paths, or serialize transactions between libraries. Those remain
release blockers, tracked in the public-readiness milestones. Ship as a prerelease.
