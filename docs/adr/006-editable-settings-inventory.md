# ADR 006: Editing inventory values in the shared draft

Date: 2026-09-16
Status: Accepted before implementation.

All settings supports an Edit value action on each editable row plus double-click. Known quality and boolean values use labelled choices; numeric/text values expose exact units/raw representation and validate their existing type. Unknown mappings remain explicit. Edits address exact indexed XML leaf/attribute paths, preserving duplicate siblings and unrelated nodes, and flow into the same draft as Configure. Editing never applies files or writes a preset automatically. Save/update/fork and edited status remain visible across tabs.

Captured installed definitions are reference data and cannot be edited here. Older schemas remain identifiable; XML schema version attributes cannot be edited in this value editor. No guessed min/max or undocumented mode mappings are introduced. Changes that invalidate the active schema are rejected before accepting the draft. Tests cover duplicate paths, attributes, validation, transient load behavior where feasible and reversible deletion.
