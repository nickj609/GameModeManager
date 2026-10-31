# Builds GameModeManager and packs the release zip: GameModeManager_v<version>.zip with addons/ and cfg/ at its root.
# Used by .github/workflows/release.yml and by the server repo's bisect/test-build.ps1, so a test build is the same
# package as a release. Prints the zip's path as its last line. Runs on Windows PowerShell 5.1 and PowerShell 7.

param(
    [string]$OutDir = (Join-Path $PSScriptRoot "..\..\bin\package"),
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

# The plugin and its NuGet contract are published under one version
$pluginCs = Get-Content (Join-Path $repo "GameModeManager\Plugin.cs") -Raw
$version = [regex]::Match($pluginCs, 'ModuleVersion => "([^"]+)"').Groups[1].Value
$sharedCsproj = Get-Content (Join-Path $repo "GameModeManager.Shared\GameModeManager.Shared.csproj") -Raw
$sharedVersion = [regex]::Match($sharedCsproj, '<Version>([^<]+)</Version>').Groups[1].Value
if (-not $version) { throw "No ModuleVersion found in GameModeManager\Plugin.cs" }
if ($version -ne $sharedVersion) { throw "Plugin.cs is version $version but GameModeManager.Shared.csproj is $sharedVersion" }

# A clean Public folder: files of earlier builds must not end up in the package
$public = Join-Path $repo "bin\$Configuration\net10.0\Public"
if (Test-Path $public) { Remove-Item $public -Recurse -Force }

& dotnet build (Join-Path $repo "GameModeManager.sln") -c $Configuration --nologo -v quiet | Out-Host
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$zipPath = Join-Path (Resolve-Path $OutDir).Path "GameModeManager_v$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

# Only what the release notes say to copy. Entries use forward slashes and folders get their own entry, like the
# releases up to v1.0.63.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$publicRoot = (Resolve-Path $public).Path.TrimEnd('\', '/')
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($top in "addons", "cfg") {
        $topPath = Join-Path $publicRoot $top
        if (-not (Test-Path $topPath)) { throw "$top is missing from the build output" }
        [void]$zip.CreateEntry("$top/")
        foreach ($item in Get-ChildItem $topPath -Recurse | Sort-Object FullName) {
            $entry = $item.FullName.Substring($publicRoot.Length + 1).Replace('\', '/')
            if ($item.PSIsContainer) { [void]$zip.CreateEntry("$entry/") }
            else { [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $item.FullName, $entry) }
        }
    }
}
finally { $zip.Dispose() }

Write-Output $zipPath
