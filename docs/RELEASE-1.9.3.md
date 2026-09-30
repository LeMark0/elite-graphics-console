# 1.9.3 — Public-file cleanup and .NET servicing (prerelease)

- Removed personal hardware and prior configuration observations from current docs.
- Removed the assumed desktop legacy-import path for new installations; existing
  saved paths and libraries remain unchanged.
- Moved all projects to .NET 10 LTS, SDK 10.0.401, with bundled runtime 10.0.12.
  The SDK is pinned, its download checksum verified, and runtime notices are packaged.
- Added targeted public-file and package checks, privacy guidance and a runtime
  servicing procedure. No third-party NuGet packages are referenced.

Validation: 68 core tests and 43 WPF UI checks passed. The published runtime
configuration includes .NET and Windows Desktop 10.0.12. Package advisory queries
found no package dependencies to assess. A synthetic profile archive was rejected
by the privacy check; targeted Windows user-path scans found no matches in reachable
Git history. These checks are not a comprehensive secret scan.

Historical commits and previous releases still retain their original documentation
and binaries. This release does not rewrite history, update previous assets, change
game/VR/driver settings, or activate repository CI/protections. It remains unsigned
and a prerelease while the remaining public-readiness milestones are addressed.
