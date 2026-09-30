# 016 — Public documentation privacy and runtime servicing

Status: Accepted, 2026-09-30. Recorded before implementation.

Prioritise removing personal hardware/configuration observations from current public
documentation and stop assuming a legacy import folder on new installations. Preserve
explicitly saved local paths and existing libraries. Keep project attribution and
repository links; they are intentional public metadata. Keep the screenshot supplied
for the README. Do not rewrite Git history or silently replace old release assets.
Document that historical commits and published packages retain their previous content.

Move all four projects from .NET 8 to .NET 10 LTS, using stable SDK 10.0.401 and runtime
10.0.12 verified against Microsoft's release metadata. Pin the SDK with global.json,
update build guidance and the workflow template, and retain self-contained packaging
with corresponding runtime legal notices. Verify the downloaded SDK's SHA-512 before
using it. No external NuGet packages are currently referenced.

Add a public-file privacy check to packaging, covering tracked text, leaked local paths
and personal-data archive entries. Keep audit output under ignored artifacts. This is
a targeted hygiene check, not a general secret scanner or proof of complete anonymity.
Repository protections/CI activation remain a separate milestone.
