# Builds the client Workshop addon that PanoramaMenuManager and its bridges need, from their release zips.
# Creates the addon in CS2's csgo_addons folders, copies the panorama files, adds the MODE badge color and compiles.
# Safe to run again: it replaces only the panorama files below. Publishing stays manual, in Workshop Tools.
# The theme is in GameModeManager's addon folder. The map pictures, vote title tokens and plugin versions come from the server repo.
#
#   .\build-menu-addon.ps1
#   .\build-menu-addon.ps1 -AddonName cs2_modded_menus -ZipFolder "$env:USERPROFILE\Downloads"
#   .\build-menu-addon.ps1 -ServerRepo ..\..\cs2-modded-bisect-server

[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9_]+$')]
    [string]$AddonName = "cs2_modded_menus",
    [string]$ZipFolder = (Join-Path $env:USERPROFILE "Downloads"),
    [string]$CS2Path,
    [string]$ServerRepo = (Join-Path $PSScriptRoot "..\..\cs2-modded-bisect-server")
)

$ErrorActionPreference = "Stop"
# The theme and footers in GameModeManager, the pictures and tokens in the server repo
$addonDir = Join-Path (Split-Path -Parent $PSScriptRoot) "addon"
if (-not (Test-Path (Join-Path $ServerRepo "scripts\Visuals-Common.ps1"))) { throw "No server repo at $ServerRepo. Pass -ServerRepo." }
$ServerRepo = (Resolve-Path $ServerRepo).Path
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Define the zips to take panorama files from (newest match wins)
$zipPatterns = @("MenuManagerCS2-*.zip", "PMM_GG1MapChooser-*.zip", "PMM_SimpleAdmin-*.zip")
$zipPrefix = "Content-addonmanager/panorama/"

# Define the files the addon must end up with
$sourceFiles = @(
    "panorama/layout/custom_game/menu_ui.xml",
    "panorama/layout/custom_game/pmm_mapvote.xml",
    "panorama/layout/custom_game/pmm_admin.xml",
    "panorama/styles/custom_game/ui.css",
    "panorama/styles/custom_game/pmm_mapvote.css",
    "panorama/styles/custom_game/pmm_mapvote_maps.css",
    "panorama/styles/custom_game/pmm_admin.css"
)
$modeBadge = ".bd-mode { background-color: #e0a030; }"

function Stop-WithError {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    exit 1
}

function Find-CS2 {
    $steam = (Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue).SteamPath
    if (-not $steam) { return $null }

    $libraries = @($steam)
    $vdf = Join-Path $steam "steamapps\libraryfolders.vdf"
    if (Test-Path $vdf) {
        foreach ($match in (Select-String -Path $vdf -Pattern '"path"\s+"([^"]+)"')) {
            $libraries += $match.Matches[0].Groups[1].Value -replace '\\\\', '\'
        }
    }

    foreach ($library in $libraries) {
        $candidate = Join-Path $library "steamapps\common\Counter-Strike Global Offensive"
        if (Test-Path (Join-Path $candidate "game\bin\win64\resourcecompiler.exe")) { return $candidate }
    }
    return $null
}

if (-not $CS2Path) { $CS2Path = Find-CS2 }
if (-not $CS2Path) { Stop-WithError "CS2 not found. Pass -CS2Path, and tick Counter-Strike 2 Workshop Tools under CS2 > Properties > DLC in Steam." }
$compiler = Join-Path $CS2Path "game\bin\win64\resourcecompiler.exe"
if (-not (Test-Path $compiler)) { Stop-WithError "resourcecompiler.exe not found. Tick Counter-Strike 2 Workshop Tools under CS2 > Properties > DLC in Steam." }
if (Get-Process cs2 -ErrorAction SilentlyContinue) { Stop-WithError "CS2 or Workshop Tools is running. Close it and run this again." }

$contentDir = Join-Path $CS2Path "content\csgo_addons\$AddonName"
$gameDir = Join-Path $CS2Path "game\csgo_addons\$AddonName"
Write-Host "Addon: $contentDir" -ForegroundColor Cyan

# Create the addon, the same two folders Workshop Tools' Create New Addon makes
New-Item -ItemType Directory -Force -Path $contentDir, $gameDir | Out-Null
$addonInfo = Join-Path $gameDir "addoninfo.txt"
if (-not (Test-Path $addonInfo) -or (Select-String -LiteralPath $addonInfo -Pattern '"IsTemplate"' -Quiet)) {
    # An addon marked IsTemplate is hidden from Workshop Tools' addon list
    Set-Content -LiteralPath $addonInfo -Value "`"AddonInfo`"`r`n{`r`n}" -Encoding Ascii
}

# Copy the panorama files out of each zip. Some zips store paths with backslashes, so read the entries directly.
$contentRoot = [IO.Path]::GetFullPath($contentDir).TrimEnd('\') + '\'
foreach ($pattern in $zipPatterns) {
    $zip = Get-ChildItem -Path $ZipFolder -Filter $pattern -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $zip) { Stop-WithError "No $pattern in $ZipFolder. Download it (see docs) or pass -ZipFolder." }

    $copied = 0
    $archive = [IO.Compression.ZipFile]::OpenRead($zip.FullName)
    try {
        foreach ($entry in $archive.Entries) {
            $name = $entry.FullName -replace '\\', '/'
            if (-not $name.StartsWith($zipPrefix) -or $name.EndsWith('/')) { continue }

            $relative = $name.Substring("Content-addonmanager/".Length)
            $target = [IO.Path]::GetFullPath((Join-Path $contentDir $relative))
            if (-not $target.StartsWith($contentRoot, [StringComparison]::OrdinalIgnoreCase)) { Stop-WithError "Unsafe path in $($zip.Name): $name" }

            New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
            $copied++
        }
    }
    finally {
        $archive.Dispose()
    }
    if ($copied -eq 0) { Stop-WithError "$($zip.Name) has no files under $zipPrefix" }
    Write-Host "  $($zip.Name): $copied file(s)"
}

foreach ($file in $sourceFiles) {
    if (-not (Test-Path (Join-Path $contentDir $file))) { Stop-WithError "Missing after copying: $file" }
}

# Apply the theme from addon\menu_theme.css over PMM's menu. The zips were just copied, so this always starts clean.
$uiCss = Join-Path $contentDir "panorama\styles\custom_game\ui.css"
$menuXml = Join-Path $contentDir "panorama\layout\custom_game\menu_ui.xml"
$utf8 = New-Object Text.UTF8Encoding($false)
$css = [IO.File]::ReadAllText($uiCss) -replace '(?m)^@define accent: [^;]+;', '@define accent: #f5a623;'

# PMM's Mono look from its constructor (genesis-cs.space/menuconstructor), with the orange accent above for the side bars,
# the focus bar and switches. These are the only lines Mono changes in PMM 1.3.01's ui.css: if PMM changes one, the build stops.
$mono = [ordered]@{
    '@define accent-soft: rgba(96, 128, 255, 0.40);' = '@define accent-soft: rgba(255, 255, 255, 0.16);'
    '@define frame: rgba(140, 170, 210, 0.22);' = '@define frame: rgba(255, 255, 255, 0.14);'
    '@define btn-bg: rgba(255, 255, 255, 0.05);' = '@define btn-bg: rgba(255, 255, 255, 0.012);'
    '@define btn-bg-hover: rgba(255, 255, 255, 0.11);' = '@define btn-bg-hover: rgba(255, 255, 255, 0.028);'
    '@define btn-bg-active: rgba(255, 255, 255, 0.18);' = '@define btn-bg-active: rgba(255, 255, 255, 0.045);'
    '@define label-color: #8b8f94;' = '@define label-color: #9a9a9e;'
    'from( rgba(22, 30, 42, 0.82) ), to( rgba(13, 19, 28, 0.86) )' = 'from( rgba(18, 18, 20, 0.92) ), to( rgba(8, 8, 10, 0.95) )'
    'background-image: url("s2r://panorama/images/backgrounds/bluedots_large_png.vtex");' = 'background-image: none;'
    'background-img-opacity: 0.03;' = 'background-img-opacity: 0;'
    'wash-color: #9fb0be;' = 'wash-color: #c8c8cc;'
    'background-color: #9fb0be;' = 'background-color: #c8c8cc;'
    'wash-color: #8b929a;' = 'wash-color: #9a9a9e;'
    'color: #cdd8e4;' = 'color: #ececec;'
    'color: #8ea2b4;' = 'color: #b0b0b4;'
}
foreach ($line in $mono.Keys) {
    if (-not $css.Contains($line)) { Stop-WithError "PMM's ui.css has changed: $line not found. Compare it with the constructor's Mono ui.css." }
    $css = $css.Replace($line, $mono[$line])
}
[IO.File]::WriteAllText($uiCss, $css + [IO.File]::ReadAllText((Join-Path $addonDir "menu_theme.css")), $utf8)

$xml = [IO.File]::ReadAllText($menuXml)

# The WASD and number key footers. PMM's hint can only be plain text, so these are fixed keycaps: keep WASD in step with ButtonsConfig
# in MenuManagerCore.json. The theme shows each one instead of the hint, in its own style only.
$hintLine = '<Label id="utils-hint" class="utils-hint stratum-medium-tf" text="{s:hint}" />'
if (-not $xml.Contains($hintLine)) { Stop-WithError "menu_ui.xml has changed: $hintLine not found" }
$footer = New-Object Text.StringBuilder
[void]$footer.AppendLine($hintLine)
# Each group: its word, what goes between its keys, then the keys
$keyFooters = [ordered]@{
    "gmm-keys-wasd"   = @(@("Move", "/", "W", "S"), @("Select", "/", "D", "E"), @("Back", "", "A"), @("Close", "", "R"))
    "gmm-keys-digits" = @(@("Select", ([string][char]0x2013), "1", "6"), @("Back", "", "8"), @("Next", "", "9"), @("Close", "", "0"))
}
foreach ($style in $keyFooters.Keys) {
    [void]$footer.AppendLine("`t`t`t`t`t<Panel class=`"gmm-keys $style`">")
    foreach ($group in $keyFooters[$style]) {
        $keys = @($group | Select-Object -Skip 2)
        foreach ($key in $keys) {
            if ($key -ne $keys[0]) { [void]$footer.AppendLine("`t`t`t`t`t`t<Label class=`"gmm-key-or stratum-bold-tf`" text=`"$($group[1])`" />") }
            $class = if ($group[0] -eq "Close") { "gmm-key gmm-key-close" } else { "gmm-key" }
            [void]$footer.AppendLine("`t`t`t`t`t`t<Label class=`"$class stratum-bold-tf`" text=`"$key`" />")
        }
        $nameClass = if ($group[0] -eq "Close") { "gmm-key-name gmm-key-name-last" } else { "gmm-key-name" }
        [void]$footer.AppendLine("`t`t`t`t`t`t<Label class=`"$nameClass stratum-medium-tf`" text=`"$($group[0])`" />")
    }
    [void]$footer.AppendLine("`t`t`t`t`t</Panel>")
}
$xml = $xml.Replace($hintLine, $footer.ToString().TrimEnd())

# PMM's title blends additively, which leaves dark text almost invisible on the orange band
$titleClass = 'class="utils-title additive stratum-bold-tf"'
if (-not $xml.Contains($titleClass)) { Stop-WithError "menu_ui.xml has changed: $titleClass not found" }
$xml = $xml.Replace($titleClass, 'class="utils-title stratum-bold-tf"')

# The divider under the title goes, because the band replaces it. The one above the page counter stays.
$titleDivider = '<Panel class="utils-divider" />(\s*<Panel class="utils-buttons">)'
if ($xml -notmatch $titleDivider) { Stop-WithError "menu_ui.xml has changed: the divider above utils-buttons not found" }
$xml = [regex]::Replace($xml, $titleDivider, '<Panel class="utils-divider gmm-title-divider" />$1')

[IO.File]::WriteAllText($menuXml, $xml, $utf8)
Write-Host "  Applied the menu theme"

# Carry the vote title tokens to players, for Votes.NativeTitleToken. Valve's uploader only packs resource/overviews, so this
# needs CS2 Workshop Manager, which reads publish_rules.txt. Compiling doesn't touch it, so it goes straight in the game folder.
$tokens = Join-Path $ServerRepo "game\csgo\resource\platform_english.txt"
New-Item -ItemType Directory -Force -Path (Join-Path $gameDir "resource") | Out-Null
Copy-Item -LiteralPath $tokens -Destination (Join-Path $gameDir "resource\platform_english.txt") -Force
[IO.File]::WriteAllText((Join-Path $contentDir "publish_rules.txt"), "`"publish_rules`"`r`n{`r`n`t`"include`"`t`"resource/platform_english.txt`"`r`n}`r`n", $utf8)
Write-Host "  Added the vote title tokens"

# Add the pictures from the server repo's bisect\build-menu-images.ps1: map files, then a CSS class for each map and mode, as the bridge names them.
# The compiler only takes a PNG through a file:// reference in CSS, and builds it along with that stylesheet.
$imageFiles = @()

# GameModeManager's own pictures (addon\images, from tools\draw-notify-icons.ps1): the notification icons menu_theme.css uses
$ownImages = Join-Path $PSScriptRoot "..\addon\images"
$ownTarget = Join-Path $contentDir "panorama\images\custom_game\gmm"
New-Item -ItemType Directory -Force -Path $ownTarget | Out-Null
foreach ($picture in Get-ChildItem -LiteralPath $ownImages -Filter "*.png") {
    Copy-Item -LiteralPath $picture.FullName -Destination (Join-Path $ownTarget $picture.Name) -Force
    $imageFiles += "panorama/images/custom_game/gmm/$($picture.Name)"
}
Write-Host "  Added $($imageFiles.Count) notification icon(s)"
$imagesDir = Join-Path $env:USERPROFILE "Documents\CS2\addon-images"
$manifestPath = Join-Path $imagesDir "images.json"
if (Test-Path $manifestPath) {
    $images = [IO.File]::ReadAllText($manifestPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
    $targetDir = Join-Path $contentDir "panorama\images\custom_game\maps"
    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    $classes = New-Object Text.StringBuilder
    foreach ($map in $images.maps.PSObject.Properties) {
        Copy-Item -LiteralPath (Join-Path $imagesDir "maps\$($map.Value)") -Destination (Join-Path $targetDir $map.Value) -Force
        $imageFiles += "panorama/images/custom_game/maps/$($map.Value)"
        $name = [IO.Path]::GetFileNameWithoutExtension($map.Value)
        [void]$classes.AppendLine(".mi-$name { background-image: url(`"file://{images}/custom_game/maps/$($map.Value)`"); }")
    }
    foreach ($mode in $images.modes.PSObject.Properties) {
        $class = -join ($mode.Name.Trim().ToLowerInvariant().ToCharArray() | ForEach-Object { if ($_ -match '[a-z0-9_-]') { $_ } else { '_' } })
        $url = if ($mode.Value -like 's2r://*') { $mode.Value } else { "file://{images}/custom_game/$($mode.Value)" }
        [void]$classes.AppendLine(".mi-$class { background-image: url(`"$url`"); }")
    }
    [IO.File]::AppendAllText((Join-Path $contentDir "panorama\styles\custom_game\pmm_mapvote_maps.css"), "`r`n" + $classes.ToString(), $utf8)
    Write-Host "  Added $(@($images.maps.PSObject.Properties).Count) map picture(s)"
}

# BEGIN vote footers
# Keycaps on the vote, nominate and settings panels, for the WASD and number key styles, from what each panel's text hint says.
# The vote panel gets them in mouse mode too, for voting with !1 to !9 in chat.
# The hint stays in the file: it's the "done" message after voting. The bridge isn't changed, only the addon's copy of its layout.
$voteXmlPath = Join-Path $contentDir "panorama\layout\custom_game\pmm_mapvote.xml"
$voteXml = [IO.File]::ReadAllText($voteXmlPath)
$dash = [string][char]0x2013
$voteFooters = [ordered]@{
    "mv_hint_click"  = @("click", ,@("Vote", $dash, "!1", "!9"))   # one group: the comma keeps it a group
    "mv_hint_wasd"   = @("wasd", @("Move", "/", "W", "S"), @("Vote", "", "E"), @("Mode", "", "R"))
    "mv_hint_digits" = @("digits", @("Vote", $dash, "1", "9"), @("Mode", "", "0"))
    "nm_hint_wasd"   = @("wasd", @("Move", "/", "W", "S"), @("Page", "/", "A", "D"), @("Nominate", "", "E"), @("Close", "", "R"))
    "nm_hint_digits" = @("digits", @("Nominate", $dash, "1", "6"), @("Page", "/", "8", "9"), @("Close", "", "0"))
    "st_hint_wasd"   = @("wasd", @("Move", "/", "W", "S"), @("Choose", "", "E"), @("Back", "", "R"))
    "st_hint_digits" = @("digits", @("Choose", $dash, "1", "5"), @("Back", "", "0"))
}
foreach ($hint in $voteFooters.Keys) {
    $spec = @($voteFooters[$hint])
    $line = "<Label class=`"mv-hint mv-hint-$($spec[0]) stratum-medium-tf`" text=`"{s:$hint}`" />"
    if (-not $voteXml.Contains($line)) { Stop-WithError "pmm_mapvote.xml has changed: $line not found" }
    $keys = New-Object Text.StringBuilder
    [void]$keys.AppendLine($line)
    [void]$keys.AppendLine("`t`t`t`t`t<Panel class=`"gmm-vote-keys gmm-vote-keys-$($spec[0])`" hittest=`"false`">")
    foreach ($group in ($spec | Select-Object -Skip 1)) {
        $caps = @($group | Select-Object -Skip 2)
        foreach ($cap in $caps) {
            if ($cap -ne $caps[0]) { [void]$keys.AppendLine("`t`t`t`t`t`t<Label class=`"gmm-vote-key-or stratum-bold-tf`" text=`"$($group[1])`" />") }
            $class = if ($group[0] -eq "Close") { "gmm-vote-key gmm-vote-key-close" } else { "gmm-vote-key" }
            [void]$keys.AppendLine("`t`t`t`t`t`t<Label class=`"$class stratum-bold-tf`" text=`"$cap`" />")
        }
        [void]$keys.AppendLine("`t`t`t`t`t`t<Label class=`"gmm-vote-key-name stratum-medium-tf`" text=`"$($group[0])`" />")
    }
    [void]$keys.Append("`t`t`t`t`t</Panel>")
    $voteXml = $voteXml.Replace($line, $keys.ToString())
}
[IO.File]::WriteAllText($voteXmlPath, $voteXml, (New-Object Text.UTF8Encoding($false)))
[IO.File]::AppendAllText((Join-Path $contentDir "panorama\styles\custom_game\pmm_mapvote.css"), [IO.File]::ReadAllText((Join-Path $addonDir "vote_footer.css")), (New-Object Text.UTF8Encoding($false)))
# The admin panel gets Mono's background and border too, from addon\admin_theme.css
[IO.File]::AppendAllText((Join-Path $contentDir "panorama\styles\custom_game\pmm_admin.css"), [IO.File]::ReadAllText((Join-Path $addonDir "admin_theme.css")), (New-Object Text.UTF8Encoding($false)))
Write-Host "  Added the vote panel footers"
# END vote footers

# Add the MODE badge color used by GameModeManager's mode entries in the vote
$mapsCss = Join-Path $contentDir "panorama\styles\custom_game\pmm_mapvote_maps.css"
if ((Get-Content -LiteralPath $mapsCss -Raw) -notmatch [regex]::Escape(".bd-mode")) {
    [IO.File]::AppendAllText($mapsCss, "`r`n$modeBadge`r`n", (New-Object Text.UTF8Encoding($false)))
    Write-Host "  Added the MODE badge color"
}

# Compile, then check each compiled file was really written: the compiler's exit code alone isn't trusted
Write-Host "Compiling..." -ForegroundColor Cyan
$started = (Get-Date).AddSeconds(-2)
$fileList = [IO.Path]::GetTempFileName()
try {
    $sourceFiles | ForEach-Object { (Join-Path $contentDir $_) -replace '/', '\' } | Set-Content -LiteralPath $fileList -Encoding Ascii
    $ErrorActionPreference = "Continue"
    $output = & $compiler -nop4 -f -filelist $fileList 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
}
finally {
    Remove-Item -LiteralPath $fileList -ErrorAction SilentlyContinue
}

$missing = @()
foreach ($file in ($sourceFiles + $imageFiles)) {
    $compiledName = (($file -replace '\.xml$', '.vxml_c') -replace '\.css$', '.vcss_c') -replace '\.png$', '_png.vtex_c'
    $compiled = Get-Item -LiteralPath (Join-Path $gameDir $compiledName) -ErrorAction SilentlyContinue
    if (-not $compiled -or $compiled.LastWriteTime -lt $started) { $missing += $compiledName }
}
if ($missing.Count -gt 0 -or $exitCode -ne 0) {
    $output | Select-Object -Last 30 | ForEach-Object { Write-Host "  $_" }
    Stop-WithError "Compile failed (exit code $exitCode). Not written: $($missing -join ', ')"
}

. (Join-Path $ServerRepo "scripts\Visuals-Common.ps1")
Write-Stamp $script:AddonStamp (Get-AddonInputsHash $ServerRepo)
Write-Host "[OK] Addon '$AddonName' is built: $($sourceFiles.Count) files and $($imageFiles.Count) pictures compiled to $gameDir" -ForegroundColor Green
Write-Host ""
Write-Host "Next, publish it by hand:" -ForegroundColor Cyan
Write-Host "  1. In Steam, launch CS2 with Launch Counter-Strike 2 Workshop Tools and open '$AddonName'."
Write-Host "  2. Use the tools' Workshop upload, then set visibility to Unlisted on the item's Steam page."
Write-Host "  3. Add the ID from the item's URL to mm_extra_addons in game\csgo\cfg\multiaddonmanager\multiaddonmanager.cfg."
