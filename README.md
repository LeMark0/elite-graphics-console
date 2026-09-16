# Elite Graphics Console

A Windows desktop graphics profile manager for Elite Dangerous Odyssey, with a cockpit-inspired interface and a local performance notebook. Designed for VR through Virtual Desktop / OpenComposite / VDXR and for flat-screen play. SteamVR is not required.

## Install and start

Download the Windows x64 ZIP from this private repository's [releases](https://github.com/LeMark0/elite-graphics-console/releases), extract it, and run `EliteGraphicsConsole.exe`. The package includes its .NET runtime; no SDK or administrator access is required. The executable is not code-signed.

To install under your user account and create a desktop shortcut, run `Install.ps1` from the extracted folder. Installation does not change Elite's graphics files.

Close Elite normally before the first launch. The app captures the existing graphics files byte for byte as a protected baseline, prepares a separate 4096 planet + galaxy candidate, and imports the old batch switcher's VR/FLAT folders when present. It never runs the batch file. Check **Paths** if installation discovery needs correcting.

## Everyday workflow

1. Select a revision. Its cards show texture values resolved from the saved quality selector, installed definitions and existing XML overrides.
2. Edit the main controls or use **Advanced XML editor**. Save creates a new revision; the baseline remains intact.
3. Open **Compare** and select two or more presets. Each selected revision has its own column; amber cells differ from the first column. **Differences only** hides equal settings. The resolved view shows friendly quality labels and inferred effective planet/background values; **Raw XML / all overrides** includes older `.fxcfg` files and indexed duplicate elements. Collapse the preset picker for more table space. Selections are remembered. Formatting-only changes are omitted from this table; the apply preview still reports them.
4. With Elite closed, choose **Review & apply selected** and inspect the final diff. Verified backups are retained for **Restore previous apply**.
5. Launch Elite through your usual route. This app does not launch a VR runtime or modify driver/mod configuration.
6. Record matching benchmark runs before deciding whether a candidate improves the experience.

The hi-res candidate is **untested until flown**. It changes only planet/background texture sizes at the tiers selected by the saved environment quality. Existing dormant overrides and other custom values remain. Higher texture sizes may improve distant detail but do not fix render resolution, compression, terrain LOD or texture-streaming delays. Galaxy background changes are not assumed to be free of memory/loading cost.

## Performance recording

**Benchmarks** records one whole-GPU NVIDIA sample per second: memory used, utilisation, temperature, graphics clock and power. Runs retain the selected revision ID, actual graphics file hashes, game build, hardware, timestamps, checkpoints and notes. `Ctrl+Shift+B` starts/stops a run; `Ctrl+Shift+M` adds a checkpoint. Buttons work if another app has reserved those hotkeys.

Record Virtual Desktop quality, refresh rate, SSW mode/state, codec, bitrate, overlay readings and visual observations manually. GPU samples are not per-frame timings, process-specific VRAM, headset FPS or continuous SSW measurements. A run started without Elite is labelled as a telemetry check. Optional PresentMon CSV import is limited to FLAT runs; it reports Elite presentation intervals, average FPS, P95/P99 and intervals over 50 ms. See [the test protocol](docs/PERFORMANCE.md).

## Data and limits

- Data lives in `%LOCALAPPDATA%\EliteGraphicsConsole`: `profiles`, `benchmarks`, `transactions`, and local `settings.json`. Back up this whole folder. None of it is automatically uploaded to GitHub.
- Revisions are immutable through the app and checked with SHA-256. These checks detect file changes, not malicious alteration of both data and metadata.
- The highest saved Custom schema is the **inferred** active preset, not live engine verification. Conflicting duplicate texture overrides are reported as ambiguous. Checkerboard precedence remains unverified when files disagree.
- v1 edits Custom presets. Saved built-in presets need conversion to Custom in Elite before capture. Older imported snapshots require migration onto current settings before apply.
- Apply is blocked while Elite runs, when previewed files drift, or when the captured game build/definitions no longer match. Following a game update, capture or migrate a new revision. Capturing definitions helps reproduce interpretation; it does not freeze the game executable or runtime.
- XML customisations are retained in snapshots. EDHM and other DLL/mod folders are outside the app's write scope and are not backed up. A separate manager can still overwrite game files later; refresh/capture those changes deliberately. Avoid running the old batch switcher alongside this app.
- Normal profile apply replaces only its managed graphics files and does not mirror/delete unrelated destination files. Rollback can remove a file only when that exact transaction created it. This is recoverable per-file replacement, not a filesystem-wide atomic transaction.
- Closing during capture saves the run. An interrupted apply retains a recovery journal. If external edits conflict with restoration, recovery stops to preserve them.

To use an isolated library for development: `EliteGraphicsConsole.exe --data-dir C:\some\test-library`. Set **Paths** to test copies before testing Apply.

## Build

On Windows with .NET SDK 8.0.424 or a newer 8.0 servicing SDK:

```powershell
./build.ps1
./build.ps1 -Publish
```

The first command runs the isolated core tests and builds WPF. `-Publish` also makes a self-contained Windows x64 ZIP and SHA-256 file in `artifacts`. `-DotnetPath` accepts a portable SDK executable. The included GitHub Actions template runs the same checks on Windows when enabled; uploading it requires GitHub CLI workflow permission. See [architecture](docs/ARCHITECTURE.md) for maintenance details.

An independent personal tool, not affiliated with Frontier Developments, Meta or NVIDIA.

### All settings (1.2)

The **All settings** tab lists every saved graphics value from the inferred active Custom schema, Settings.xml, DisplaySettings.xml and all XML overrides. Search by friendly name, raw field, value or source. The view follows the selected preset and its current draft. Configure and Advanced XML edit that draft; Update or Fork saves it.

Optional switches include older schemas and the revision's shipped definition snapshot (current installed definitions when inspecting current files). Exact indexed XML paths preserve duplicates and unknown/new fields. Quality names are inferred from saved definitions where supported; raw enum values remain visible. Conflicting checkerboard values are shown separately without guessing precedence. Available resolutions/refresh rates and unsaved menu changes cannot be established from saved files. This covers persisted graphics controls, not an independently verified menu-by-menu inventory of every game build.

### Read current settings and save a named preset (1.3)

Use the sidebar **+** to create a named preset from current saved game settings, an installed game default, or a saved preset. Current/default capture requires Elite to be closed. Default quality values inherit current display/headset mode, HUD and unspecified newer fields; other XML graphics overrides are excluded from the new copy. Creation does not apply settings.

Version 1.3.1 shows readable quality names, On/Off, multipliers, percentages and units in All settings and Save current. Raw XML values remain in a separate column. Unknown modes are explicit; available quality names are inferred from definition snapshots and shipped presets. FXAA mode 1 was identified from the user's in-game selection.

Version 1.4: selecting a preset opens its settings. **Update this preset** saves changes under the same identity while retaining revision history; **Fork this preset** creates a separate preset from the draft. Protected baselines are fork-only. Unsaved edits prompt before leaving. **History** opens earlier revisions. Design decisions are recorded in docs/adr/001 through 003.

Version 1.4.1: **Load current game settings…** creates and selects a named snapshot of saved game settings. **Check applied status** checks disk without discarding edits. The preset header shows Apply and APPLIED/NOT APPLIED (exact saved-file match); a running game may still have older settings in memory. Unsaved drafts are labelled separately. See ADR 004.
