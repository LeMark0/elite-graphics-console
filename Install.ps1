# Run from an extracted release. No administrator access or game-setting writes.
$ErrorActionPreference = 'Stop'
$sourceExe = Join-Path $PSScriptRoot 'EliteGraphicsConsole.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) { throw 'Extract the release ZIP first and run its Install.ps1.' }
if (Get-Process -Name EliteGraphicsConsole -ErrorAction SilentlyContinue) { throw 'Close Elite Graphics Console before installing/updating.' }
$target = Join-Path $env:LOCALAPPDATA 'Programs\Elite Graphics Console'
if ([IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\') -eq [IO.Path]::GetFullPath($target).TrimEnd('\')) { throw 'Run the installer from the extracted release folder.' }
New-Item -ItemType Directory -Path $target -Force | Out-Null
Get-ChildItem -LiteralPath $PSScriptRoot | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force }
$desktop = [Environment]::GetFolderPath('DesktopDirectory')
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path $desktop 'Elite Graphics Console.lnk'))
$shortcut.TargetPath = Join-Path $target 'EliteGraphicsConsole.exe'
$shortcut.WorkingDirectory = $target
$shortcut.IconLocation = $shortcut.TargetPath + ',0'
$shortcut.Description = 'Versioned Elite Dangerous VR and flat-screen graphics profiles'
$shortcut.Save()
Write-Output "Installed: $target"
Write-Output 'Desktop shortcut created. Graphics settings and your local profile library were preserved.'
