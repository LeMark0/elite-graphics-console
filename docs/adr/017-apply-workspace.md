# 017 — Apply the current workspace without saving a preset

Status: Accepted, 2026-09-30. Recorded before implementation.

The persistent header action is Apply. It applies the validated editor draft from
either a selected preset or loaded current settings. A saved preset is optional.
Compare the draft with freshly read game files in the existing review; cancellation
keeps the draft. Retain game-closed, schema/build, drift, target-lock and rollback checks.

After Apply, retain the selected preset/workspace and all unsaved library edits.
Do not create or update a revision. Applied status compares the draft to saved game
files independently of whether the draft is saved as a preset. Matching values disable
Apply; invalid/pending edits and historical presets remain blocked. Saving, forking,
discarding and leaving remain separate actions; discarding never undoes an Apply.
