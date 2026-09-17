# ADR 009: Explicit actions when leaving unsaved changes

Date: 2026-09-17
Status: Accepted — recorded before implementation

Replace the native Yes/No discard prompt with a terminal-styled dialog. Identify the edited workspace, state that only unsaved app edits would be lost and that game files are unchanged, and offer Keep editing, Discard changes and continue, and a contextual save-and-continue action. Current settings save as a named preset; an editable latest preset updates in place; protected or earlier revisions fork. A name field belongs inside the dialog when creating a preset. Escape and closing the dialog keep editing. Invalid pending input disables saving with an explanation; failed saves leave the dialog and draft intact. Continuing occurs only after a successful save or explicit discard.
