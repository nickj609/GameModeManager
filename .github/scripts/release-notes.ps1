# Writes the release notes in the format of the releases up to v1.0.63:
#   [> [!IMPORTANT] when the config version changed]  ## Features / ## Bug Fixes / ## Misc  ## Installation  Wiki  Full Changelog
# The sections come from the merged pull request's description when it has "## " headings, else from the commit
# subjects since the previous release (feat: -> Features, fix: -> Bug Fixes, everything else -> Misc).
# Preview locally: .github\scripts\release-notes.ps1 -PreviousTag v1.0.63

param(
    [string]$Repo = "nickj609/GameModeManager",
    [string]$Sha = "HEAD",
    [string]$PreviousTag,
    [string]$OutFile,
    [string]$Owner = "nickj609"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

function Read-AtRef([string]$Ref, [string]$Path) {
    $text = & git -C $repoRoot show "${Ref}:$Path" 2>$null
    if ($LASTEXITCODE -ne 0) { return "" }
    return ($text -join "`n")
}

# $null when gh isn't signed in or the request fails: the notes then fall back to local git
function Get-GitHubJson([string]$Endpoint) {
    $previous = [Console]::OutputEncoding
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    try {
        $text = & gh api $Endpoint 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $text) { return $null }
        return (($text -join "`n") | ConvertFrom-Json)
    }
    catch { return $null }
    finally { [Console]::OutputEncoding = $previous }
}

function Get-ConfigVersion([string]$Text) {
    return [regex]::Match($Text, 'public int Version \{ get; set; \} = (\d+)').Groups[1].Value
}

# "feat: added x (#60)" -> "Added x (#60)"; someone else's commit -> "Added x (Thanks @them! #60)"
function Format-Subject([string]$Subject, [string]$Login) {
    $text = ($Subject -replace '^\s*[A-Za-z]+(\([^)]*\))?!?:\s*', '').Trim()
    if (-not $text) { return $null }
    $text = $text.Substring(0, 1).ToUpperInvariant() + $text.Substring(1)
    if ($Login -and $Login -ne $Owner -and $Login -notmatch '\[bot\]$') {
        $pr = [regex]::Match($text, '\s*\(#(\d+)\)\s*$')
        if ($pr.Success) { $text = $text.Substring(0, $pr.Index) + " (Thanks @$Login! #$($pr.Groups[1].Value))" }
        else { $text = "$text (Thanks @$Login!)" }
    }
    return "- $text"
}

$version = [regex]::Match((Read-AtRef $Sha "GameModeManager/Plugin.cs"), 'ModuleVersion => "([^"]+)"').Groups[1].Value
if (-not $version) { throw "No ModuleVersion in GameModeManager/Plugin.cs at $Sha" }

# The last published release, not the highest tag: the v1.0.7x tags are older test tags
if (-not $PreviousTag) {
    $PreviousTag = & gh release view --repo $Repo --json tagName --jq .tagName
    if ($LASTEXITCODE -ne 0 -or -not $PreviousTag) { throw "Could not read the latest release; pass -PreviousTag" }
}

# Sections: the pull request's description
$sections = ""
$prBody = ""
if (Get-Command gh -ErrorAction SilentlyContinue) {
    $pulls = Get-GitHubJson "repos/$Repo/commits/$(& git -C $repoRoot rev-parse $Sha)/pulls"
    if ($pulls -and @($pulls)[0].body) { $prBody = @($pulls)[0].body }
}
$prBody = [regex]::Replace($prBody, '(?s)<!--.*?-->', '').Replace("`r`n", "`n")
$prBody = [regex]::Replace($prBody, '(?ms)^## Installation\b.*\z', '').Trim()
if ($prBody -match '(?m)^## \S') {
    # Headings left empty from the pull request template don't go into the notes
    $sections = [regex]::Replace("$prBody`n", '(?m)^## [^\n]*\n\s*(?=^## |\z)', '').Trim()
}

# Sections: otherwise the commit subjects
if (-not $sections) {
    $commits = $null
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        $compare = Get-GitHubJson "repos/$Repo/compare/$PreviousTag...$(& git -C $repoRoot rev-parse $Sha)"
        if ($compare) {
            $commits = @($compare.commits | Where-Object { @($_.parents).Count -eq 1 } |
                ForEach-Object { "$($_.author.login)`t$(($_.commit.message -split "`n")[0])" })
        }
    }
    if (-not $commits) { $commits = & git -C $repoRoot log "$PreviousTag..$Sha" --no-merges --reverse --format="`t%s" }

    $groups = [ordered]@{ "Features" = @(); "Bug Fixes" = @(); "Misc" = @() }
    foreach ($commit in @($commits)) {
        $parts = "$commit" -split "`t", 2
        if ($parts.Count -lt 2) { continue }
        $line = Format-Subject $parts[1] $parts[0]
        if (-not $line) { continue }
        $group = if ($parts[1] -match '^\s*feat') { "Features" } elseif ($parts[1] -match '^\s*fix') { "Bug Fixes" } else { "Misc" }
        $groups[$group] += $line
    }
    $sections = (@($groups.Keys | Where-Object { $groups[$_].Count -gt 0 } | ForEach-Object { "## $_`n$($groups[$_] -join "`n")" }) -join "`n`n")
}

# A changed config version gets the same notice as v1.0.60 / v1.0.61, unless the description already has one
$important = ""
$configNow = Get-ConfigVersion (Read-AtRef $Sha "GameModeManager/Config.cs")
$configBefore = Get-ConfigVersion (Read-AtRef $PreviousTag "GameModeManager/Config.cs")
if ($configNow -and $configNow -ne $configBefore -and $sections -notmatch '\[!IMPORTANT\]') {
    $important = "> [!IMPORTANT]`n> - If moving from $PreviousTag or earlier, back up your ``GameModeManager.json`` configuration file and let the plugin generate the new version. This version has minor changes to the configuration file. `n>`n`n"
}

$notes = @"
$important$sections

## Installation
1. Extract and copy ``addons`` and ``cfg`` folders to ``/csgo/``.
2. Update each game mode configuration file (i.e. comp.cfg) to include ``css_gamemode <mode>``.
3. After the first run, update the configuration file ``GameModeManager.json``.

For more information on the configuration of these features, please see the updated [Wiki](https://github.com/$Repo/wiki).

**Full Changelog**: https://github.com/$Repo/compare/$PreviousTag...v$version
"@
$notes = $notes.Replace("`r`n", "`n").Trim() + "`n"

if ($OutFile) { [System.IO.File]::WriteAllText($OutFile, $notes, (New-Object System.Text.UTF8Encoding($false))) }
else { Write-Output $notes }
