# Puts the text to paste into Discord for a new release in the run's summary. Runs on Windows PowerShell 5.1 and PowerShell 7.

param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Repo,
    [Parameter(Mandatory)][string]$NotesFile
)

$title = "**GameModeManager v$Version**`n"
$url = "`n`nhttps://github.com/$Repo/releases/tag/v$Version"

# Discord doesn't draw HTML tags, images or GitHub's alert labels, and the release page has the install steps
$notes = (Get-Content $NotesFile -Raw -Encoding UTF8) -replace '(?i)</?(details|summary|br)\s*/?>', '' -replace '!\[[^\]]*\]\([^)]*\)', ''
$notes = (($notes -replace '\r\n', "`n") -replace '(?m)^> \[!\w+\]\n', '' -replace '(?s)\n## Installation\n.*', '' -replace '\n{3,}', "`n`n").Trim()

# A typed Discord message can be 2000 characters
$limit = 2000 - $title.Length - $url.Length
if ($notes.Length -gt $limit) {
    $cut = $notes.Substring(0, $limit - 4)
    $notes = $cut.Substring(0, [Math]::Max($cut.LastIndexOf("`n"), 0)).TrimEnd() + "`n..."
}

# A longer fence than the notes' own code blocks
$fence = '````'
$summary = "## Discord post`nPaste this into the channel:`n`n$fence" + "text`n$title$notes$url`n$fence"
if ($env:GITHUB_STEP_SUMMARY) { Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $summary -Encoding UTF8 }
else { Write-Host $summary }
