# Settings reference and evidence

## Visible star count

The [linked author's experiment](https://www.reddit.com/r/EliteDangerous/comments/1wh6y9w/sky_is_a_lot_prettier_with_180k_stars_visible/) changes `GalaxyMap/Low|Medium|High/StarInstanceCount` to 180000. The author reports a denser flight sky with no observed flight FPS/loading penalty on their system, but lower galaxy-map FPS while moving quickly near the core. This is anecdotal, not a measurement on the user's RTX 3080 Ti / Quest 2 setup. Loading and VR frame times still need an A/B test.

Local Odyssey definitions inspected for this release specify 2000 at Low and Medium, and 4000 at High. The user's existing High override was already 5000. The app resolves the active tier using `GalaxyMapQuality`; it does not assume High or use `EnvironmentQuality`.

The effective Visible star count row is available without an existing override. Editing updates only StarInstanceCount in the selected tier, preserving other tiers, nebula settings, duplicate sections and unrelated customisations. Conflicting duplicate counts display as Ambiguous; an explicit edit assigns the chosen value to each selected-tier star-count node. Unknown selectors are unavailable, not guessed. Non-negative 32-bit integers are accepted as XML values; this is not a tested engine limit or a promise of safe performance at extreme counts. No value is applied automatically.

Suggested evaluation: fork a baseline, increase the count moderately, then compare the same sky position, galaxy-map pan and repeated jumps. Record VR frame times and loading durations; judge dense and sparse sky regions separately. Return to the baseline preset for rollback.

Reviewed 18 September 2026. Descriptions explain purpose, not measured performance gains. No running-game settings were changed to collect evidence.

## Primary local evidence

The installed Odyssey `GraphicsConfiguration.xml` provides ordered tiers and their `LocalisationName` labels. The app uses the definition snapshot saved with each preset, so later game updates do not silently reinterpret old presets using new definitions.

Examples: DOF has internal nodes Off / Low / Medium but display labels Off / Medium / High. Terrain includes Ultra+. Terrain LOD blending has Off / High / Ultra. Reflection quality has Low / High. These must not be replaced with a generic Low-to-Ultra list.

`OptionDefaults/*.fxcfg` corroborates raw selections: Ultra and VRUltra use AAMode=4; VRLow/VRMedium/VRHigh use 1. TextureFilterQuality progresses 0 / 2 / 3 / 4 through Low / Mid / High / Ultra. Newer Mid/High defaults use UpscalingQuality=2; VR defaults use 1. These files establish raw values, not all display labels.

## Mapping confidence

| Control | Mapping | Evidence / limitation |
| --- | --- | --- |
| Anti-aliasing | 0 Off, 1 FXAA, 4 SMAA | FXAA previously verified from the user's game save. SMAA is correlated with Ultra defaults and community configurations; not a fresh in-game toggle test. No invented modes 2 or 3. |
| Upscaling | 0 Normal, 1 AMD FidelityFX CAS, 2 AMD FSR 1.0 | Community configuration with upscaling disabled establishes 0; 1/2 labels inferred from game menu choices and shipped VR/flat presets. Still needs a controlled game-save comparison to independently verify those labels. |
| Filtering | 0 Trilinear, 1–4 Anisotropic 2×/4×/8×/16× | Inferred menu ordering, corroborated by shipped quality presets; not a fresh game-save test. |
| Texture quality | 0 Low, 1 Medium, 2 High | Legacy ordinal mapping; shipped files corroborate 1/2. |
| Window mode | 0 Windowed, 1 Fullscreen, 2 Borderless | Conventional Elite saved-mode mapping; not newly verified by changing display mode. |
| Feature quality | Ordered definition tiers | Local shipped definitions and localisation labels; no arbitrary upper bound. |
| Stereo output, adapter, monitor | Retain numeric value | Runtime/hardware-dependent mapping remains unresolved. No fabricated fixed device list. |

Unknown saved enum values stay available in their dropdown. Unknown engine parameters receive an explicit explanation that effect/range are unverified. Definitions and localisation labels are reference metadata, not editable quality controls. Raw data remains in the inspector. Slider-like settings remain numeric inputs rather than an incomplete list of suggested values.

## Research sources

- [Morbad's firsthand custom configuration and terrain-work observations](https://forums.frontier.co.uk/threads/custom-elite-dangerous-display-quality-settings-continued.433024/). Community evidence, not a current-build benchmark.
- [Firsthand Odyssey configuration with upscaling disabled](https://www.reddit.com/r/EliteDangerous/comments/pn87y0). Corroborates Normal=0; hardware-specific performance claims are not copied into the app.
- [EDConfig author's investigation of menu-to-XML mappings](https://wiki.herzbube.ch/index.php/EDConfig). Useful for field purposes and feature-tier indirection; older schema, not authoritative for current enum values.
- [Frontier Update 6 announcement](https://store.steampowered.com/news/posts/?appids=359320&enddate=1627998690&feed=steam_community_announcements). Introduces Ultra+ terrain for high-end GPUs.
- [AMD's FSR 1 documentation](https://gpuopen.com/fidelityfx-superresolution/). Spatial upscaling purpose and quality/performance trade-off; does not establish Elite's XML mode numbers.

Descriptions for common rendering concepts are concise explanations, not reproductions of game tooltip text. Comfort settings, screenshot settings and internal engine fields can behave differently across versions. The app does not claim to measure the active VR compositor, actual resolution, or performance.
