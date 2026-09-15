# ADR 002: Selected preset owns the editor and dirty actions

Date: 2026-09-16
Status: Accepted before implementation

Remove Save current as a top-level tab. Clicking a sidebar preset shows its configuration surface and its values. Changes belong to that selected preset's draft. Update this preset and Fork this preset live on the editor surface; update is enabled only for valid dirty, unprotected current revisions. Fork uses the draft, not the saved source. A discard action restores saved values. Leaving a dirty editor, including closing or refreshing, requires an explicit discard decision, avoiding silent loss. Advanced XML edits return to this same draft rather than saving through a separate workflow.

Settings inventory follows the selected preset/draft; the persistent Current files toggle is removed because it competes with selection. Comparison remains a separate multi-preset view. Apply continues to require a saved revision.
