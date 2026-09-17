# Redesign plan: Elite graphics terminal

Date: 2026-09-16
Status: Proposed — planning only. No application changes or release in this task.

## Direction

Replicate the visual and interaction language of Elite's functional screens. Use the flat station-services composition from Photo 4 as the desktop foundation, the cockpit tables and selectors in Photos 1–3 for everyday editing, and the hierarchy of Photos 5–7 for navigation. Coriolis is a secondary reference for translating Elite's visual language into a dense desktop tool.

The target is a graphics terminal: square amber controls, dark warm surfaces, aligned rows, strong selected states, thin separators and compact information groups. Preserve the existing configuration engine and preset behavior. Animations come after the static interface and workflows are settled.

## Reference map

| Reference | Observed pattern | Proposed use |
| --- | --- | --- |
| Photo 1: Modules | Full-width orange selected row; aligned column labels and values; small chevrons; thin row separators | Settings table, preset comparison, keyboard focus and active row |
| Photo 2: Functions | Label/value pairs; ON/OFF states; stepped brightness control; adjacent contextual information | Inline enum/toggle controls, segmented sliders with exact numeric entry, setting inspector |
| Photo 3: Navigation | Horizontal selected tabs; compact filter tools; narrow status/icon column; consistent numeric alignment | Global tabs, settings filters, row status markers, compact scrollbars |
| Photo 4: Station services | Flat list/detail composition; restrained white headings; orange rules; dark warm rows; fixed actions | Main desktop layout, preset browser, detail pane and confirmation dialogs |
| Photo 5: Mission board | Persistent entity list; selected entity content and metadata alongside | Preset library paired with working preset and revision information; use composition only because image is low resolution |
| Photo 6: Options | Category glyphs, strong selected surface, clear title/description hierarchy | Category identity and empty-state choices; do not make routine editing traverse a tile landing page |
| Photo 7: Ship panel | Consistent icon vocabulary, compact status area, grouped functions and reserved secondary colours | Section icons and status hierarchy; secondary cyan used sparingly |
| Coriolis shipyard | Dense aligned table, warm alternating rows, compact filter strip, orange headers | Comparison and library density |
| Coriolis Sidewinder editor | Object title/actions above grouped numeric information; orange group bars; occasional cyan category accent | Working-preset header and benchmark summaries |

Web references inspected: https://coriolis.io/ and https://coriolis.io/outfit/sidewinder (2026-09-16). Screenshot references are the seven user attachments; exact colours/font identities cannot be established from these compressed and perspective-distorted images. Values below are proposed tokens, not claimed samples from game source assets.

## Main composition

At the initial 1440 × 900 design size:

- Top strip: GRAPHICS | COMPARE | PERFORMANCE | LIBRARY; small application/settings access at the far right. Selected tab has an orange fill with dark text, thin orange baseline underneath.
- Left preset rail, approximately 240–270 DIPs: Load current game settings; All / VR / Flat with + aligned on the same row; compact preset rows. Each row has name, mode, revision and applied/protected indicators. No tall cards.
- Working header: preset name or CURRENT GAME SETTINGS, asterisk and change count when edited; origin/revision underneath; applied status and Apply at the right. Library actions sit below the title with clear separation from Apply.
- Main editor: approximately 60–70% of the remaining width for grouped setting rows; 260–320 DIPs for a selected-setting inspector when space permits. Inspector collapses into a details drawer on narrower windows.
- Compact bottom strip: contextual help, saved-file check time and keyboard hints. It does not repeat all header actions.

Suggested schematic:

```text
 GRAPHICS               COMPARE        PERFORMANCE        LIBRARY       SETTINGS
 ───────────────────────────────────────────────────────────────────────────────
 LOAD CURRENT           VR HMD 1.0 FXAA + 4096 *      2 CHANGES    [APPLY PRESET]
 ALL   VR   FLAT    +    Saved revision: APPLIED · Draft: NOT APPLIED
                        [UPDATE] [FORK] [DISCARD]                  [MORE ▾]
 Preset library         ────────────────────────────────────────────────────────
 ───────────────        SEARCH…   CATEGORY: ALL   [CHANGED ONLY] [ADVANCED]
 VR Baseline      ◇     SETTING                  VALUE             DETAILS
 VR FXAA + 4096   ✓     VR / RENDERING                             HMD QUALITY
 Flat High              HMD image quality        ◀ 1.00× ▶        Explanation
                        Anti-aliasing            ◀ FXAA  ▶        Saved: 1.25×
                        Supersampling            ◀ 1.00× ▶        Draft: 1.00×
                        PLANETS / BACKGROUND                      Source
                        Planet texture           ◀ 4096 px ▶      Exact XML path
                        Galaxy background        ◀ 4096 px ▶      [RESET VALUE]
 ───────────────────────────────────────────────────────────────────────────────
 Elite closed · Saved files checked 21:12              Enter: edit · Esc: cancel
```

Schematic indicators and values illustrate layout, not measured performance or a proposed graphics preset.

## Visual primitives

| Token | Initial proposal |
| --- | --- |
| Canvas | #080A0B, near black |
| Base panel / alternate row | #17110B / #261707, warm near-black browns |
| Orange | #FF8C00 for selected surfaces and primary controls |
| Secondary orange | #D97A16 for labels; use only where contrast passes |
| Main text / secondary text | #ECE5D9 / #B8AA98 |
| Informational accent | #32BBDD, reserved for a distinct semantic purpose |
| Error | #FF6655 plus icon and explicit text |
| Shape | Square edges, 1 DIP separators; very limited corner cuts at major frames |
| Spacing | 4 / 8 / 12 / 16 / 24 DIP scale |
| Row height | 32 DIP default; 40 DIP comfortable option |
| Typography | Compact sans-serif, uppercase navigation/group headings; readable mixed-case explanations and exact user-entered names; aligned/tabular numerals |
| Icons | Consistent 16/20/24 DIP vector strokes; category glyphs, arrows, plus, check, lock, fork, history and delete |

Font selection remains a prototype decision: compare available redistributable condensed faces against Segoe UI, especially at 125–200% Windows scaling. Do not assume the game's exact font is available. Draw app-specific vector icons following the reference geometry; avoid substituting emoji. Use labels/tooltips for ambiguous icons.

Selected rows use solid orange and dark text. Hover uses a subtler warm fill. Keyboard focus gets a distinct outline independent of selection. Applied, dirty, protected and unavailable states have explicit text or symbols, not colour alone. Reserve enough contrast for normal text; target 4.5:1 normal text and 3:1 large text/control boundaries where applicable.

Flat desktop projection keeps text sharp. Cockpit perspective, moving scenery, bright text bloom and strong scanlines are not part of the first version. A subtle static surface texture can be evaluated after legibility is verified. Photos 1–3 provide structure and hierarchy rather than literal camera distortion.

## Controls and interaction

Unify Configure and All settings into one SETTINGS surface with category filters: Display, VR & scaling, Anti-aliasing & image processing, Planets & galaxy, Terrain, Lighting & shadows, Effects, HUD, and Advanced/other. Every stored setting remains discoverable. Selected categories do not alter configuration. Unknown fields stay searchable in Advanced with raw names, types where known and source information.

- Quality enums: left/right chevrons around a named value, plus click-to-open list for direct selection. Only verified mappings get friendly names; unknown modes remain editable and explicitly unmapped.
- Booleans: ON/OFF selector in the value column.
- Numeric values: inline numeric input with unit and step controls. Sliders only for verified bounded ranges; exact entry remains available. Percentage display converts explicitly to/from raw fractions.
- Texture sizes: discrete labelled values with px unit.
- Text and unusual XML values: inline text where practical; multiline structured content opens the inspector editor.
- Selecting a row reveals description, saved/captured versus draft value, source, XML path, inference caveats and Reset value. Technical columns leave the default table but remain available in Advanced and comparison.
- Edits commit to the draft on Enter or valid focus loss. Esc cancels the active cell. Invalid input stays visible with a local message, blocks save/apply and is never silently clamped. Switching presets with invalid or dirty edits prompts appropriately.
- Arrow keys navigate rows; Tab enters controls; changing a value requires focus within its control. Wheel scrolling must not accidentally change a selector.

## State and action contract

| Working state | Sidebar selection | Header and actions |
| --- | --- | --- |
| Current files loaded, unchanged | None | CURRENT GAME SETTINGS / CAPTURED; Save as preset; Reload current |
| Current files edited | None | CURRENT GAME SETTINGS * / N CHANGES; Save as preset; Discard |
| Saved preset, matches disk | Selected preset | APPLIED (saved files); Apply disabled; Fork; history/more |
| Saved preset, differs from disk | Selected preset | NOT APPLIED; Apply; Fork; history/more |
| Saved preset edited | Selected preset retained | Name * / N CHANGES; state of saved revision shown separately; Update, Fork, Discard; Apply disabled |
| Protected preset edited | Selected preset retained | PROTECTED / N CHANGES; Fork and Discard; Update unavailable with reason |
| Files unavailable or stale | Retain editor | STATUS UNKNOWN or RECHECK NEEDED; never retain a misleading green/applied signal |

Apply remains in the header on every surface. Saving and applying stay separate operations. Delete is in a labelled More menu to reduce accidental clicks, with the existing explicit confirmation and retained revisions. Initial apply review presents Setting / Current game / Selected preset, followed by confirmation. Loading current creates no revision and does not silently replace a saved preset. Runtime claims must continue to distinguish saved files, inferred settings and measured observations.

## Other surfaces

- Compare: Photos 1 and 4 plus Coriolis table alignment. Settings down rows, selected presets across columns. Freeze labels, align numeric values, keep Changed only and baseline-column choice obvious. Mark differences with symbols/text in addition to colour. Sources expand on demand.
- Performance: compact run list and paired detail pane; measured charts and numeric summaries; recording state unmistakable. Separate GPU telemetry, imported frame timing and subjective headset notes. No decorative gauges implying unmeasured performance.
- Library: revision history, import/export, deleted-preset recovery and migration. Preserve a quick preset rail while making infrequent maintenance actions secondary.
- Dialogs: square panels, orange title rule, concise content, explicit action labels and keyboard focus. Creation offers the three starting points as compact choices. Delete explains the actual recovery behavior.

## Delivery sequence and review gates

1. Static design prototype: one representative settings screen, full component/state sheet, current-state draft, saved dirty preset, apply review and comparison. Review at 1440 × 900 and the current minimum window size, plus 100/125/150/200% scaling. Settle typography and density here.
2. WPF resource dictionaries and templates: palette, type, rows, tabs, buttons, selectors, inputs, scrollbars, icons, focus/error states. Keep configuration IO and revision persistence unchanged.
3. Replace app composition and unify settings editor. Introduce a shared working-state/view-model boundary so current snapshot, selected revision and draft are distinct objects. Both friendly and raw editing must use the same validated draft.
4. Restyle comparison, performance, creation/history/apply/delete dialogs. Make recovery available through Library; retain benchmark and revision IDs.
5. Workflow and visual acceptance pass, then package the release. Animation is a separate later pass: short selection/content transitions, subtle panel reveal and recording feedback, all disableable/reduced-motion aware. Never delay actions or add continuous decorative motion.

Before each implementation step, accept or revise its proposed ADR. Planning documents are not implementation approval. No new runtime, game settings, preset values or release changes are part of this plan.

## Acceptance criteria

- Familiar Elite visual language is evident in rows, selection, controls and information hierarchy, beyond an orange recolour.
- At least 12 ordinary settings rows are visible at 1440 × 900 with the main header and actions accessible.
- Routine editing uses inline controls without a modal per value.
- Load current, edit, save, update, fork, compare and apply are distinguishable without reading technical XML.
- Applied/draft/current/protected states remain unambiguous with colour removed.
- Known settings use names/units; unknown and duplicate XML values remain available and accurately targeted.
- Keyboard-only editing, invalid input recovery, dirty navigation, empty/deleted libraries, long preset names and mixed game-build comparisons pass.
- Existing snapshots, HUD overrides, benchmark links, apply backups and game-closed protections survive unchanged.
- Visual fidelity and input behavior are checked in the packaged WPF app, not only a browser mockup.
