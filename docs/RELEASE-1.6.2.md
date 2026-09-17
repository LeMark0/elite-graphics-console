# Elite Graphics Console 1.6.2

- Replace the ambiguous Yes/No unsaved-changes prompt with Keep editing, Discard changes and continue, and a contextual save action. Current settings can be named and saved directly; editable presets update; protected/earlier revisions fork. Escape or closing the dialog preserves edits. Invalid input blocks saving with an explanation.
- Center the preset plus as a vector icon and align its height with All / VR / Flat.
- Replace remaining native menu popup borders, blue selection fills, separators, checkboxes and dropdown-option chrome with terminal templates. Keep visible keyboard focus.

Validation: 45 core tests and 28 WPF checks, including saving before leaving, preserving cancelled/invalid drafts, menu contents, and both scrolling axes. Inspected rendered menu, unsaved dialog and settings controls. Full physical-input/multi-DPI coverage remains limited as noted in 1.6.0. No game settings are applied by the update.
