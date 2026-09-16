# ADR 005: Unsaved current-game workspace and reversible preset deletion

Date: 2026-09-16
Status: Accepted before implementation; supersedes ADR 004 capture behavior.

Load current game settings reads a stable snapshot into an unsaved workspace, clears sidebar selection and creates no library revision. It can be saved immediately or after edits using Save as preset. Configure, All settings and Advanced XML share this draft. An asterisk and Unsaved changes label distinguish modifications from the captured snapshot. Applying, exporting, history and benchmark recording require a saved preset. Leaving modified work requires confirmation. Loading requires Elite closed; captured files and definitions are checked for drift.

Delete preset is on the preset surface and requires confirmation naming the preset and revision count. Delete removes the entire logical preset from the visible library/comparison via a local tombstone, retaining its immutable revisions for benchmark references and recovery. No game files or derivative presets are removed. Protected baselines may also be deleted after this explicit confirmation. The dialog states that files currently applied to the game stay unchanged and describes retained recovery data.
