param([switch]$Publish, [string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & $DotnetPath run --project tests/EliteGraphics.Tests/EliteGraphics.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
    & $DotnetPath build src/EliteGraphics.App/EliteGraphics.App.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'App build failed.' }
    if ($Publish) {
        $version = ([xml](Get-Content Directory.Build.props -Raw)).Project.PropertyGroup.Version
        $artifactRoot = Join-Path $PSScriptRoot 'artifacts'
        $stage = Join-Path $artifactRoot ('stage-' + [guid]::NewGuid().ToString('N'))
        $package = Join-Path $stage "EliteGraphicsConsole-$version-win-x64"
        New-Item -ItemType Directory -Path $package -Force | Out-Null
        & $DotnetPath publish src/EliteGraphics.App/EliteGraphics.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $package
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
        Copy-Item -LiteralPath README.md, THIRD-PARTY-NOTICES.md, Install.ps1 -Destination $package
        Copy-Item -LiteralPath docs -Destination $package -Recurse
        # Read exact restored pack versions/locations, including portable SDK NuGet caches.
        $assets = Get-Content src/EliteGraphics.App/obj/project.assets.json -Raw | ConvertFrom-Json
        $noticeCount = 0
        foreach ($packName in @('Microsoft.NETCore.App.Runtime.win-x64','Microsoft.WindowsDesktop.App.Runtime.win-x64')) {
            $download = $assets.project.frameworks.PSObject.Properties.Value.downloadDependencies | Where-Object { $_.name -eq $packName } | Select-Object -First 1
            if (-not $download) { throw "Cannot identify runtime pack $packName" }
            $packVersion = $download.version.Trim('[',']').Split(',')[0].Trim()
            $packFound = $false
            foreach ($cache in $assets.packageFolders.PSObject.Properties.Name) {
                $pack = Join-Path $cache ($packName.ToLowerInvariant() + '/' + $packVersion)
                if (-not (Test-Path -LiteralPath $pack)) { continue }
                foreach ($notice in @('LICENSE','LICENSE.TXT','THIRD-PARTY-NOTICES.TXT')) {
                    $source = Join-Path $pack $notice
                    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $package "$packName-$notice"); $noticeCount++; $packFound = $true }
                }
                if ($packFound) { break }
            }
            if (-not $packFound) { throw "Runtime legal notices missing for $packName $packVersion" }
        }
        if ($noticeCount -lt 2) { throw 'Runtime legal notices were not found in SDK packs; inspect the SDK before packaging.' }
        $zip = Join-Path $artifactRoot "EliteGraphicsConsole-$version-win-x64.zip"
        Compress-Archive -LiteralPath $package -DestinationPath $zip -Force
        $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
        "$hash  $([IO.Path]::GetFileName($zip))" | Set-Content -LiteralPath "$zip.sha256" -Encoding ascii
        Write-Output "Package: $zip"
        Write-Output "SHA256: $hash"
    }
} finally { Pop-Location }
