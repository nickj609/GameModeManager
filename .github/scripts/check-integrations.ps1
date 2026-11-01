# Fails when a bundled plugin no longer has a member GameModeManager reaches through reflection, so an update can't break it
# unnoticed. Update this list when GameModeManager starts using another one. CS2-SimpleAdmin isn't bundled, so the server repo's
# scripts/Apply-Updates.ps1 checks that one. Runs on Windows PowerShell 5.1 and PowerShell 7.

$repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$plugins = Join-Path $repo "GameModeManager\lib\csgo\addons\counterstrikesharp\plugins"
$failed = $false

$reflected = [ordered]@{
    "GG1MapChooser/GG1MapChooser.dll"         = @("_roundEndMap", "nominatedMaps", "players", "selectedMaps", "ProposedMaps", "_playedMaps", "_recentlyNominatedMaps", "RememberNominatedMaps", "MapsInVote", "AllowNominate", "MinPlayers", "MaxPlayers", "MapMenuCommand", "Nominate", "TryNominate", "BeginMapVote", "GetVoteMenuMode", "VotingTime", "ReloadMapcycle", "GetActiveMapPool", "GetRealClientCount", "ExtendMapInVote", "VoteDependsOnRoundWins", "_mapChooser", "GGMC_IsVoteInProgress", "MCAPICapability")
    "PMM_GG1MapChooser/PMM_GG1MapChooser.dll" = @("Instance", "Maps", "Title", "Badge")
}

foreach ($dll in $reflected.Keys) {
    $path = Join-Path $plugins $dll
    if (-not (Test-Path $path)) { Write-Host "::error::$dll isn't in the bundle"; $failed = $true; continue }
    $text = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($path))
    $missing = @($reflected[$dll] | Where-Object { $text.IndexOf($_, [StringComparison]::Ordinal) -lt 0 })
    if ($missing.Count -gt 0) { Write-Host "::error::$dll no longer has: $($missing -join ', ')"; $failed = $true }
    else { Write-Host "ok: $dll" }
}

if ($failed) { exit 1 }
