# 011 — Description column and effective star count

Status: Accepted, 2026-09-18. Recorded before implementation.

- Move explanations to the third settings-table column: Setting / Value / Description. Keep descriptions wrapped and searchable. Move the technical inspector below the table to give all three columns useful space at the existing minimum window width.
- Expose `GalaxyMap/<selected tier>/StarInstanceCount` as Visible star count, even when no override exists. Resolve the tier from GalaxyMapQuality and the preset's saved definitions, not EnvironmentQuality. Show source/tier and report unresolved or conflicting values explicitly.
- Accept non-negative 32-bit integer counts; this is serialization validation, not a claim that all values are supported or performant. Edit only that field at the selected tier, retaining other tiers, nebula settings, HUD customisations and unrelated XML. Do not automatically apply the Reddit author's 180,000 value.
- Include the effective count in resolved preset comparisons. Test inherited values, override creation, duplicates, validation, preservation, and UI draft/save behaviour.

Evidence: the user's linked Reddit author's configuration and local shipped GalaxyMap definitions. Performance reports are anecdotal, not a guarantee for Quest 2 VR. See `SETTING-REFERENCE.md` for details.
