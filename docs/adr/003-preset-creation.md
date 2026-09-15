# ADR 003: Sidebar plus creates presets from explicit starting points

Date: 2026-09-16
Status: Accepted before implementation

The sidebar + opens a creation dialog offering current saved game settings, installed default quality presets, or saved presets. Creation requires a name and does not apply anything. Current settings are read with Elite closed and checked for change before creation. Saved presets are copied byte-for-byte with their captured definitions/build; the source is unchanged.

Default choices are discovered from the installation's OptionDefaults/*.fxcfg rather than hardcoded. They seed a Custom snapshot using the current schema and display/headset configuration. Fields supplied by the default replace matching current quality/general fields; current-version fields absent from the template remain inherited and are disclosed. HUD colour overrides are preserved; other XML graphics overrides are excluded from the new default-based copy so old 4096/shadow overrides do not silently defeat the starting point. The original files, source presets, mod DLLs and live settings remain untouched. Default VR/flat selection does not switch the user's runtime or display mode; the dialog states that current display/headset settings are retained.
