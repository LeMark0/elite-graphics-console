# Dependencies and servicing

Verified 30 September 2026 against Microsoft's release metadata:

| Component | Pinned version | Purpose |
|---|---|---|
| .NET SDK | 10.0.401 | Build tool, selected by global.json |
| .NET / Windows Desktop runtime | 10.0.12 | Included in the Windows x64 package |
| Third-party NuGet packages | None | Projects reference only each other and framework components |

.NET 10 is an LTS release supported until 14 November 2028 according to the
[Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy).
Check the [release index](https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json)
and [10.0 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)
when preparing each release and after monthly security servicing announcements.

Update global.json and RuntimeFrameworkVersion in Directory.Build.props together,
run core and WPF UI checks, then publish a new version with its checksum and matching
runtime legal notices. A self-contained app requires a new package to receive runtime
fixes; installing a shared runtime does not update an existing app package. SDK downloads
used for this migration were checked against Microsoft's SHA-512 release metadata.

Run `dotnet list <project.csproj> package --include-transitive --vulnerable` to query
package advisories. Framework runtime servicing is checked separately in Microsoft's
metadata; an empty package audit does not certify the bundled runtime or application.

NVIDIA tools and VR software are optional locally installed integrations, not bundled
dependencies. This project does not upgrade drivers, Elite Dangerous or VR runtimes.
The GitHub Actions file under docs is still an inactive template, not active CI.
