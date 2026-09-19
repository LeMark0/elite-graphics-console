# 1.9.0 — Review save and apply changes

- Shows a before/after review before updating, forking, saving current settings, creating, importing or migrating a preset, including saving from the unsaved-changes dialog.
- Save reviews identify the source snapshot, list graphics and metadata changes, and explain that saving does not apply game settings. Unchanged copies explicitly report no graphics file changes.
- Apply reviews compare the selected preset with freshly read game files. Friendly labels accompany exact raw values and paths; new and removed files include their XML contents as individual setting rows. Existing rollback and stale-file protection remain in place.
- Cancelling a review preserves the draft and does not save or apply changes.
- Restores descriptions and technical details to the right sidebar, with a compact Setting / Value table. Description search and visible star count remain available.

Validation: core tests and UI workflow checks, including save/apply cancellation, read-only review tables, saving while leaving, and added/removed XML fields. Reviewed screenshots of the dialogs and minimum-width sidebar layout. Installation does not change game settings.
