# 1.9.4 — Apply displayed settings (prerelease)

The visible header button is now **Apply**. It applies the current editor values from
a selected preset or loaded game settings, including unsaved edits. No preset or
revision is created automatically, and the source preset remains unchanged.

Apply still shows a before/after review. Cancellation preserves the draft. After
success the header shows APPLIED, while an asterisk continues to identify edits not
saved to the preset library. Update, Fork and Save as preset remain optional separate
actions. Discarding app edits does not undo game changes; use Restore previous apply.

Matching values disable redundant Apply. Invalid/pending edits and historical presets
remain blocked. Game-closed, schema/build, drift, transaction locking and rollback
checks still apply. Existing benchmark capture requires a matching saved preset.

Validation: 68 core tests and 56 WPF UI checks, including applying an edited preset
and an unselected current-settings workspace, cancellation, no new revisions, source
preset preservation and rollback. All game writes in testing use isolated fixtures.
This remains an unsigned prerelease with other public-readiness work outstanding.
