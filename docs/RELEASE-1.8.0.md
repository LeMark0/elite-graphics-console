# 1.8.0 — Description column and visible star count

- Settings now have three columns: Setting, Value, Description. The technical inspector and revision notes expand below the table, leaving room for all three columns at the minimum window size.
- Adds an editable Visible star count under Planets / Galaxy, even when the value is inherited from game definitions. It follows GalaxyMapQuality and updates only that tier's StarInstanceCount.
- Includes effective star count in resolved preset comparisons. Reports missing selectors and conflicting overrides explicitly; retains existing nebula, HUD and dormant-tier customisations.
- Documents the linked Reddit experiment and its limits. The author's 180,000 count is not applied automatically or advertised as free of performance cost.

Validation: 54 core tests and 37 UI checks passed. Verified three-column layout at minimum width, inherited count display, invalid input, selected-tier editing, saved revisions, duplicate override handling and unchanged live game files during UI editing.

No game graphics settings are applied by installing this update.
