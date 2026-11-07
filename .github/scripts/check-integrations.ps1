# Fails when a bundled plugin no longer has a member GameModeManager reaches through reflection, so an update can't break it
# unnoticed. The lists are in GameModeManager/Core/ReflectedMembers.cs. CS2-SimpleAdmin isn't bundled, so the server repo's
# scripts/Apply-Updates.ps1 checks that one. Runs on Windows PowerShell 5.1 and PowerShell 7.

$repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$plugins = Join-Path $repo "GameModeManager\lib\csgo\addons\counterstrikesharp\plugins"
$source = Get-Content (Join-Path $repo "GameModeManager\Core\ReflectedMembers.cs") -Raw
$failed = $false

$reflected = [ordered]@{
    "GG1MapChooser/GG1MapChooser.dll"         = "GG1MapChooser"
    "PMM_GG1MapChooser/PMM_GG1MapChooser.dll" = "PMM_GG1MapChooser"
}

foreach ($dll in $reflected.Keys) {
    $list = [regex]::Match($source, "string\[\] $($reflected[$dll])\s*=\s*\{(.*?)\}", 'Singleline')
    if (-not $list.Success) { Write-Host "::error::No list for $dll in ReflectedMembers.cs"; $failed = $true; continue }
    $members = @([regex]::Matches($list.Groups[1].Value, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value })

    $path = Join-Path $plugins $dll
    if (-not (Test-Path $path)) { Write-Host "::error::$dll isn't in the bundle"; $failed = $true; continue }
    $text = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($path))
    $missing = @($members | Where-Object { $text.IndexOf($_, [StringComparison]::Ordinal) -lt 0 })
    if ($missing.Count -gt 0) { Write-Host "::error::$dll no longer has: $($missing -join ', ')"; $failed = $true }
    else { Write-Host "ok: $dll ($($members.Count) members)" }
}

if ($failed) { exit 1 }
