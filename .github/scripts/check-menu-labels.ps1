# Checks that the translated texts GameModeManager shows as menu rows fit on one row.
# PanoramaMenuManager draws rows in capitals and cuts them off with "..." at about 25 characters, so a row text longer than
# that is unreadable ("PAUSE AT NEXT ROUND (CANC..."). Titles wrap onto two lines and chat messages have no limit, so only
# row texts are checked.
#
# A row text is a translation key that the source:
#   - names on the same line as a menu row call (MenuActions.Add, AddOption, AddSubMenu, AddMenuOption, AddToggle,
#     AddSelect)
#   - puts in a question's choices: (_localizer.Localize("key"), ...)
#   - returns from a method whose name ends in Label (like MatchPause.PauseLabel)
#   - starts a row built in code: new("key", ...)
#   - is in the $extra list below (built through a variable or an interpolated key)
# Run from the repository root:  .github/scripts/check-menu-labels.ps1 [-MaxLength 24]
param([int]$MaxLength = 24)

$ErrorActionPreference = "Stop"
$root = Join-Path $PSScriptRoot "..\.."
$source = Join-Path $root "src\GameModeManager"
$langFolder = Join-Path $source "lang"

$english = Get-Content (Join-Path $langFolder "en.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$known = @($english.PSObject.Properties.Name)

$rowCall = '(MenuActions\.Add|AddOption|AddSubMenu|AddMenuOption|AddToggle|AddSelect)\('
$choice = '\(\s*_localizer\.Localize\("([a-z0-9.\-]+)"\)\s*,'

# Row texts the source builds in ways the patterns don't see (a variable, an interpolated key)
$extra = @("setting.cheats-yes", "setting.cheats-no", "menu.enable", "menu.disable", "matchzy.ready-all", "matchzy.pause", "matchzy.unpause")

$keys = @{}
foreach ($key in $extra) { $keys[$key] = "(listed in this script)" }
foreach ($file in Get-ChildItem $source -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }) {
    $inLabelMethod = $false
    $depth = 0
    foreach ($line in Get-Content $file.FullName -Encoding UTF8) {
        if ($line -match $rowCall) {
            # Every known key on the line, so a choice between two ("a" : "b") counts too
            foreach ($m in [regex]::Matches($line, '"([a-z0-9.-]+)"')) {
                if ($known -contains $m.Groups[1].Value) { $keys[$m.Groups[1].Value] = $file.Name }
            }
        }
        foreach ($m in [regex]::Matches($line, $choice)) { $keys[$m.Groups[1].Value] = $file.Name }
        # A row model built in code: new("key", ...) (the MatchZy page)
        foreach ($m in [regex]::Matches($line, 'new\(\s*"([a-z0-9.-]+)"')) {
            if ($known -contains $m.Groups[1].Value) { $keys[$m.Groups[1].Value] = $file.Name }
        }

        # A *Label() method: every known key in it is a row text
        if (-not $inLabelMethod -and $line -match '\b\w+Label\s*\(\s*\)\s*$') { $inLabelMethod = $true; $depth = 0; continue }
        if ($inLabelMethod) {
            foreach ($m in [regex]::Matches($line, '"([a-z0-9.\-]+)"')) {
                if ($known -contains $m.Groups[1].Value) { $keys[$m.Groups[1].Value] = $file.Name }
            }
            $depth += ([regex]::Matches($line, '\{')).Count - ([regex]::Matches($line, '\}')).Count
            if ($depth -le 0 -and $line -match '\}') { $inLabelMethod = $false }
        }
    }
}

$problems = @()
foreach ($langFile in Get-ChildItem $langFolder -Filter *.json) {
    $texts = Get-Content $langFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($key in $keys.Keys | Sort-Object) {
        $text = $texts.$key
        if ($null -eq $text) { continue }
        # A {0} in a row is filled with something short, like a round number
        $shown = $text -replace '\{\d+\}', '00'
        if ($shown.Length -gt $MaxLength) {
            $problems += "{0,-8} {1,-28} {2,3}  {3}" -f $langFile.BaseName, $key, $shown.Length, $text
        }
    }
}

Write-Host "Menu row texts checked: $($keys.Count) keys in $((Get-ChildItem $langFolder -Filter *.json).Count) languages, at most $MaxLength characters"
if ($problems.Count -gt 0) {
    Write-Host "Too long for a menu row:" -ForegroundColor Red
    $problems | ForEach-Object { Write-Host "  $_" }
    exit 1
}
Write-Host "All fit." -ForegroundColor Green
