# ADR 004: Explicit game-settings capture and preset apply status

Date: 2026-09-16
Status: Accepted before implementation

Refresh live only checks saved game files; it does not load them into the editor. Rename it Check applied status, and make it preserve the selected preset and unsaved draft. Add Load current game settings in the library navigation. It opens the existing creation workflow with current settings selected and explicit Load as new preset wording, requiring a name. This creates and selects an exact snapshot without overwriting the selected preset or applying files. The dialog explains that Elite must be closed and only saved disk settings are read.

Move Apply preset to the selected preset header, beside a prominent APPLIED / NOT APPLIED / STATUS UNKNOWN badge. Applied means the selected saved revision exactly matches managed game files, not that a running game has loaded those files. State explains this distinction and shows when last checked. Dirty edits say UNSAVED CHANGES with a separate statement of whether the saved revision matches; apply is disabled until Update/Fork. Missing/unreadable files clear previous applied status. Refresh status on activation, selection, apply and restore, without changing editor contents. Existing apply review, game-closed checks and backups remain.
