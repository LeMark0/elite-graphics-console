# 1.7.0 — Setting choices and explanations

- Adds SMAA to anti-aliasing, plus named upscaling, texture-filtering and window-mode choices. Preserves unknown saved values.
- Uses every quality tier from each preset's definition snapshot, including depth of field, HUD colour and Ultra+ terrain. Reads game localisation labels rather than displaying internal tier names.
- Adds searchable descriptions under settings in the table, with a wider value column for long option names.
- Displays localisation identifiers such as `$QUALITY_ULTRA$` as read-only readable labels. Raw XML remains intact and available in the inspector.
- Documents evidence, inferred mappings and unresolved engine fields in `SETTING-REFERENCE.md`; decision recorded in ADR 010 before implementation.

Validation: 50 core tests and 33 UI checks, including SMAA persistence, unknown-mode retention, token preservation, description search and minimum-width layout. Synthetic UI fixtures verify that editing does not write live game files.

This update does not apply any graphics settings. Some enum labels remain inferred; the inspector and reference document identify those cases. Undocumented engine values remain available with an explicit uncertainty description.
