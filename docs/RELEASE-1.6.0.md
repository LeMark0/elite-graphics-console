# Elite Graphics Console 1.6.0

The settings editor now uses the reviewed Elite station-terminal layout: warm black surfaces, orange selected rows, compact controls and vector triangles.

- One searchable, categorized Settings surface replaces Configure and All settings.
- Edit choices and raw numeric/text values inline. Enter or valid focus loss commits; Escape cancels. Invalid pending input blocks save/apply and survives refresh.
- The inspector shows saved and draft values, source, XML path and reset. Planet and galaxy rows edit the effective tier while retaining other overrides and HUD colours.
- Load current remains transient. Dirty presets expose Update, Fork and Discard; Apply and saved-file status remain in the header. Maintenance actions move to More and Library tools.
- Comparison, performance and dialogs share the theme. Apply review shows Current game and Selected preset columns.
- Fractional multipliers remain editable when the previous value was serialized as an integer.

Validation: 45 isolated core tests and 20 WPF workflow checks; rendered settings at 1440 × 940 and 1120 × 760, comparison and apply review. The WPF tests exercise real window controls and draft services in a synthetic local library. Desktop input automation failed with unavailable input geometry, so a complete physical keyboard/mouse and multi-DPI sweep remains unverified. No game settings were applied during release validation. Animations remain deferred.

Run `./build.ps1 -Publish -UiTests` on Windows to repeat the release checks. Test fixtures and rendered captures stay outside version control. The packaged app retains the existing managed-file apply safeguards, immutable revision history and rollback snapshots.
