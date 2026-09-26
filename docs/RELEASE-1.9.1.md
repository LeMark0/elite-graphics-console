# 1.9.1 — Managed-file safety (prerelease)

Apply previously treated any captured XML, FXCFG or START file as writable. It now
writes only Settings.xml, DisplaySettings.xml, GraphicsConfigurationOverride.xml,
StartPreset.start and versioned Custom quality files. Captures still preserve other
supported files, but attempts to apply a new or changed unknown file fail before
any transaction is created. Unknown files absent from a preset remain untouched.

Transactions exclude unknown files; rollback preserves external changes to those
files. Legacy rollback manifests containing unknown files are refused before any
file is restored. Keep those backups for manual inspection. Unknown nodes and HUD
customisations within managed files retain the existing preservation behaviour.

Applied-status comparisons still compare full captures, so an unknown destination
file absent from an older preset can leave it showing different after Apply. Capture
the current settings as a new revision to align the snapshots; do not remove the file.

Validation: 61 core regression tests and the existing 43 UI checks. Tests use isolated
fixtures; no live game settings are changed. The package is unsigned.

This addresses only milestone 1. Target-directory validation, linked-parent handling,
concurrent writers and other public-readiness findings remain unresolved. Do not
treat this prerelease as completion of the safety audit.
