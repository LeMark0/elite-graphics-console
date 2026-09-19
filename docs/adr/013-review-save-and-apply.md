# 013 — Review before saving and applying

Status: Accepted, 2026-09-19. Recorded before implementation.

Show a shared before/after review before every user-facing preset save: update, fork, save current, creation and save while leaving. Compare updates/forks with their loaded source revision, current workspaces with their captured files, and default-based creation with the captured game files. Include preset metadata changes and explicitly describe unchanged copies. Cancellation preserves the draft and writes no revision.

Apply continues to compare against freshly read game files and retain transaction/staleness checks. Use readable setting names and values alongside exact file paths and raw values. Include leaves of newly added/removed files, not merely file presence. Saving affects the library; applying affects game files. State that distinction in the review.
