# 010 — Setting choices and inline explanations

Status: Accepted, 2026-09-18

The settings inventory must remain complete without presenting every engine field as a documented game control. The previous choice heuristic omitted SMAA and depth of field, and displayed internal tier names instead of localisation labels.

Before implementation, we decide to:

- Generate feature choices from every local definition tier; prefer recognised localisation labels over internal names. Use explicit, non-contiguous mappings for independent modes. Retain unknown saved values without guessing a label or discarding them.
- Show an explanation beneath each setting name, searchable with the setting. Describe familiar game controls; explicitly identify undocumented engine fields when evidence is insufficient.
- Display localisation metadata as readable, read-only labels. Preserve its exact XML value and show raw data in the inspector. Merely viewing a preset must never rewrite it.
- Document evidence and limitations in `docs/SETTING-REFERENCE.md`. Shipped files establish raw values; some game labels remain community-correlated rather than verified by changing the running game.

This adds row height but keeps the value controls aligned and avoids an extra horizontal description column. No game or runtime settings are applied by this change.
