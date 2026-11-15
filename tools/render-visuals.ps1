# Draws the Workshop page's screenshots and thumbnail, and the same pictures for GameModeManager's README, from addon\mockups with
# Microsoft Edge. Map pictures come from the server repo's bisect\build-menu-images.ps1. Nothing is committed or uploaded.
#
#   .\render-visuals.ps1

[CmdletBinding()]
param(
    [string]$WorkshopDir = (Join-Path $PSScriptRoot "..\..\docs\workshop-addon"),
    [string]$ReadmeDir = (Join-Path $PSScriptRoot "..\docs\images"),
    [string]$ServerRepo = (Join-Path $PSScriptRoot "..\..\cs2-modded-bisect-server"),
    [string]$Edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path (Join-Path $ServerRepo "scripts\Visuals-Common.ps1"))) { throw "No server repo at $ServerRepo. Pass -ServerRepo." }
$repo = (Resolve-Path $ServerRepo).Path
$mockups = Join-Path (Split-Path -Parent $PSScriptRoot) "addon\mockups"
. (Join-Path $repo "scripts\Visuals-Common.ps1")

if (-not (Test-Path $Edge)) { throw "Microsoft Edge not found: $Edge" }
$maps = (Join-Path $script:VisualsDir "maps") -replace '\\', '/'
$icons = (Join-Path (Split-Path -Parent $PSScriptRoot) "addon\images") -replace '\\', '/'   # the notification icons the mockups show
if (-not (Test-Path $maps)) { throw "No map pictures in $maps. Run the server repo's bisect\build-menu-images.ps1 first." }

# Each picture: the page and scene, then where it goes
$pictures = @(
    @("scenes.html#wasd", "screenshots\1_wasd.png", "menu-wasd.png"),
    @("scenes.html#mouse", "screenshots\2_mouse.png", "menu-mouse.png"),
    @("scenes.html#digits", "screenshots\3_digits.png", "menu-digits.png"),
    @("scenes.html#csgo", "screenshots\4_csgo.png", "menu-csgo.png"),
    @("scenes.html#vote", "screenshots\5_vote.png", "vote.png"),
    @("scenes.html#admin", "screenshots\6_admin.png", "admin.png"),
    @("scenes.html#rotation", "screenshots\7_rotation.png", "admin-rotation.png"),
    @("scenes.html#rounds", "screenshots\8_rounds.png", "admin-rounds.png"),
    @("scenes.html#native", "", "vote-native.png"),   # the game's own vote panel, not the addon's: README only
    @("thumbnail.html", "thumbnail.png", "")
)

$work = Join-Path ([IO.Path]::GetTempPath()) ("cs2-render-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $work | Out-Null
try {
    foreach ($page in @("scenes.html", "thumbnail.html")) {
        $html = [IO.File]::ReadAllText((Join-Path $mockups $page), [Text.Encoding]::UTF8).Replace("__MAPS__", "file:///$maps/").Replace("__ICONS__", "file:///$icons/")
        [IO.File]::WriteAllText((Join-Path $work $page), $html, (New-Object Text.UTF8Encoding($false)))
    }

    foreach ($picture in $pictures) {
        $output = Join-Path $work "out.png"
        Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
        $url = "file:///" + ($work -replace '\\', '/') + "/" + $picture[0]
        $process = Start-Process -FilePath $Edge -PassThru -Wait -WindowStyle Hidden -ArgumentList "--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files", "--user-data-dir=`"$work\edge`"", "--window-size=1280,720", "--virtual-time-budget=4000", "--screenshot=`"$output`"", "`"$url`""
        if (-not (Test-Path $output)) { throw "Edge didn't draw $($picture[0]) (exit code $($process.ExitCode))" }
        if ($picture[1]) { Copy-Item -LiteralPath $output -Destination (Join-Path $WorkshopDir $picture[1]) -Force }
        if ($picture[2]) { Copy-Item -LiteralPath $output -Destination (Join-Path $ReadmeDir $picture[2]) -Force }
        Write-Host "  $(if ($picture[1]) { $picture[1] } else { "README: $($picture[2])" })"
    }
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Stamp $script:RenderStamp (Get-VisualsInputsHash $repo)
Write-Host "[OK] $($pictures.Count) pictures drawn. Commit GameModeManager's docs\images, and upload the changed ones on the Workshop page." -ForegroundColor Green
