# Comparing baseline and hi-res

The purpose is to determine whether the combined 4096 planet/background candidate improves clarity while retaining the baseline's smoothness. No performance improvement is claimed before measurement.

## Keep these conditions fixed

Record the game build, location/body, ship/camera orientation and lighting, Virtual Desktop quality, actual per-eye resolution if available, headset refresh rate, SSW setting and observed state, codec, bitrate, sharpening and network conditions. Keep the same driver and OpenComposite/VDXR installation. The Windows OpenXR registry entry is evidence of registration, not proof of the runtime actually used by a running game.

Do not change the external rendering pipeline between A and B. Restart Elite after applying each revision. Close unrelated GPU-heavy applications consistently. Use comparable online/scene conditions; repeat if they differ materially.

## Repeatable flight

1. **Space view:** stationary ship, same heading and galaxy background, 60 seconds.
2. **Orbital approach:** same planet, start distance, approach angle and speed, about 120 seconds. Mark texture arrival and visible hitches separately.
3. **Planet view:** stationary at a fixed altitude and heading, 60 seconds. Compare surface detail and distant edges in the headset.

Use the same route with baseline A and candidate B, then reverse the order B/A. Aim for at least three usable runs per revision. Record the first approach after restart separately from warmed repeat passes. Do not clear driver caches just to force a cold test.

## Recording

Select the revision matching live files. Enter the scenario and runtime settings in **Benchmarks**. Start capture with `Ctrl+Shift+B`, mark each phase with `Ctrl+Shift+M`, then stop with `Ctrl+Shift+B`. Checkpoints have elapsed seconds for matching observations to GPU samples. Finish the run notes and attach comparison screenshots if useful.

At each checkpoint, note VD's displayed game FPS/latency, SSW state and any hitches. Do not equate a reported display refresh rate with native game FPS when SSW generates frames. The refresh budget is `1000 / Hz` milliseconds per displayed frame (for example 13.89 ms at 72 Hz); a GPU utilisation percentage alone cannot establish whether that budget was met.

The CSV contains one-second **whole-GPU** memory/load/power/clock/temperature samples. Under Windows these counters are not a process-specific resident-memory audit. They show trends and approximate headroom, and miss brief spikes or individual stutters. The run explicitly records whether Elite was running at capture start; it does not continuously validate every live graphics/runtime setting.

For FLAT play, an optional [PresentMon](https://github.com/GameTechDev/PresentMon) capture can be imported. Export one Elite process/swapchain with `Application` or `ProcessName` and `MsBetweenPresents` or `FrameTime` in milliseconds. The app filters `EliteDangerous64.exe`, rejects multiple swapchains and calculates nearest-rank P95/P99. The original CSV is kept with the run. These are app presentation intervals, not proof of final displayed frame delivery. VR mirror captures cannot establish headset performance and are rejected as VR data.

## Decision

Look for reproducibly sharper planetary detail or background stars, without additional texture-arrival pauses, sustained load increases that reduce headroom, more SSW/reprojection, or worse headset smoothness. Record any tradeoff explicitly. Comparing equal formats/mip counts, 4096 has 16 times the texels of 1024, 4 times those of 2048, and 2.56 times those of 2560; that is not an estimate of total game VRAM or FPS cost. Actual allocations, faces, compression and streaming determine the measured cost.

If B regresses, exit Elite normally and restore the preceding apply, or apply the protected baseline after reviewing its diff. Keep the run records even when a candidate is rejected. A visually acceptable improvement becomes a tested preference only after the headset comparison.
