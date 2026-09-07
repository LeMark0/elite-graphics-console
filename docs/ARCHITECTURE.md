# Architecture

`EliteGraphics.Core` is a dependency-free .NET 8 library. `EliteGraphics.App` is a Windows WPF shell. `EliteGraphics.Tests` is an executable test harness using generated temporary fixtures; it never targets the real graphics directory.

## Profiles

A revision directory contains raw `Graphics` files, `Definitions.xml` from the installed game and `revision.json` with parent revision, build, mode and hashes. New revisions have unique IDs. Baseline capture preserves the original bytes; editing serializes only affected XML files and retains unknown elements. Managed files are top-level `.xml`, `.fxcfg` and `.start` files. Basename validation rejects traversal and alternate streams; linked files, XML DTDs and unexpectedly large documents are rejected.

Effective planet/background resolution follows Environment quality's installed feature mapping. Duplicate override values are retained; conflicting selected values are ambiguous. The latest numeric Custom schema is an inference and explicitly labelled. Migrating a historical preset copies known old values into a clone of current settings, retaining fields introduced by the current schema.

## Apply/recovery

Apply checks process state, a fresh preview fingerprint, installed build/definition hashes and Custom schema. It writes a durable transaction record and original backups before staging replacements. Each destination file is replaced through a temporary sibling; all final bytes are verified. Failure attempts rollback. If recovery cannot safely finish, its Prepared journal remains for a later recovery action.

Restore verifies backup hashes and accepts only recorded before/after states of affected live files. It pauses on other external changes. Each transaction is scoped to its recorded target directory. Other files are not mirrored away. Process checks and fingerprints reduce races with Elite/managers; they do not lock out arbitrary external applications. Users should close Elite and other profile managers during apply/restore.

## Benchmarks

Each run has metadata, a flushed GPU CSV and optional copied screenshots/PresentMon CSV. NVIDIA's existing `nvidia-smi` process supplies read-only counters from GPU index 0 once per second. Missing counters stay null. Hotkeys are registered for the lifetime of the WPF window. There is no injection, overlay interception, runtime switching, network upload or background service.

VR performance evidence comes from manual headset/VD observations alongside GPU trends. PresentMon import is FLAT-only and isolates one Elite swapchain. Comparison links results to recorded revision IDs and actual saved-file hashes.

## Release

`build.ps1 -Publish` tests, builds and publishes a self-contained x64 package. Runtime/native extraction is handled by .NET single-file deployment. Packaging includes runtime legal notices, documentation and a per-user install script. Application data is outside installation and source folders. Updating binaries leaves profiles and benchmark history in place.

Use GitHub CLI for source/release operations. Release notes should identify checks actually completed and distinguish telemetry validation from an in-game benchmark. Do not commit user snapshots, game definitions, audit reports, captures, local paths/settings or credentials.
