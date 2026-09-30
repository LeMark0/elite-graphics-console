param([string]$PackagePath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$violations = [Collections.Generic.List[string]]::new()
# Deliberately generic: do not add real usernames or personal values to this check.
$localPath = '(?i)[A-Z]:[\\/]Users[\\/](?!Public(?:[\\/]|$)|Default(?:[\\/]|$))[^\\/\s"<>]+'
$personalObservation = '(?i)the user''s (?:existing|game save|RTX|Quest)'
function Check-Text([string]$name, [string]$value) {
    if ($value -match $localPath -or $value -match $personalObservation) {
        $violations.Add("Personal path or observation in $name")
    }
}
Push-Location $repo
try {
    $files = & git ls-files --cached --others --exclude-standard
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate repository files.' }
    foreach ($name in ($files | Sort-Object -Unique)) {
        if ($name -match '(^|/)(profiles|benchmarks|transactions|local-data)/|(^|/)settings\.json$|PUBLIC-READINESS-AUDIT') {
            $violations.Add("Private data-like repository path: $name")
        }
        if ([IO.Path]::GetExtension($name) -in @('.md','.cs','.xaml','.csproj','.props','.json','.yml','.yaml','.ps1','.txt')) {
            Check-Text $name ([IO.File]::ReadAllText((Join-Path $repo $name)))
        }
    }
    if ($PackagePath) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
        try {
            foreach ($entry in $archive.Entries) {
                $name = $entry.FullName.Replace('\','/')
                if ($name -match '(^|/)(profiles|benchmarks|transactions|local-data)/|(^|/)settings\.json$|PUBLIC-READINESS-AUDIT|\.(csv|log|xml|fxcfg|start|pdb)$') {
                    $violations.Add("Private data-like package entry: $name")
                }
                if ([IO.Path]::GetExtension($name) -in @('.md','.ps1','.txt','.json','.yml')) {
                    $reader = [IO.StreamReader]::new($entry.Open())
                    try { Check-Text $name $reader.ReadToEnd() } finally { $reader.Dispose() }
                }
            }
        } finally { $archive.Dispose() }
    }
    if ($violations.Count) { throw ($violations -join [Environment]::NewLine) }
    Write-Output 'Public-file checks passed (targeted paths, observations and data-like entries; not a full secret scan).'
} finally { Pop-Location }
