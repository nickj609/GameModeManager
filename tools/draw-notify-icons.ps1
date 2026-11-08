# Draws the notification icons in addon\images: PanoramaMenuManager's tick, "!" and cross, same size, place and colours,
# but each one shape. PMM draws them from rotated 2px bars, and its tick's two bars don't meet. build-menu-addon.ps1 puts
# them in the addon and menu_theme.css shows them instead of the bars. Drawn at 4x (64px for PMM's 16px box), so they stay
# sharp. Runs on Windows PowerShell 5.1.
#
#   .\draw-notify-icons.ps1

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$out = Join-Path (Split-Path -Parent $PSScriptRoot) "addon\images"
New-Item -ItemType Directory -Force -Path $out | Out-Null
$scale = 4

# Points in PMM's 16px box, from its bars: where each bar's ends land after its rotation (ui.css, .ntf-ok-*, .ntf-warn-*, .ntf-err-*)
function Draw([string]$name, [string]$color, [scriptblock]$shape) {
    $bitmap = New-Object Drawing.Bitmap (16 * $scale), (16 * $scale), ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([Drawing.Color]::Transparent)
    $brush = New-Object Drawing.SolidBrush ([Drawing.ColorTranslator]::FromHtml($color))
    $pen = New-Object Drawing.Pen $brush.Color, (2 * $scale)
    $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    & $shape $g $pen $brush
    $bitmap.Save((Join-Path $out "$name.png"), [Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bitmap.Dispose()
    Write-Host "  addon\images\$name.png"
}

function P([double]$x, [double]$y) { New-Object Drawing.PointF ([float]($x * $scale)), ([float]($y * $scale)) }

# Tick: the short bar's top end, the corner both bars meet at, the long bar's top end
Draw "notify_ok" "#57e08a" { param($g, $pen) $g.DrawLines($pen, [Drawing.PointF[]]@((P 1.9 8.9), (P 6.1 13.1), (P 13.9 4.4))) }

# "!": the 2x10 stem from the top, and the 3px dot at the bottom
Draw "notify_warn" "#ffcf5c" {
    param($g, $pen, $brush)
    $g.DrawLine($pen, (P 8 1), (P 8 9))
    $g.FillEllipse($brush, [float](6.5 * $scale), [float](13 * $scale), [float](3 * $scale), [float](3 * $scale))
}

# Cross: two 14px bars through the middle at 45 degrees
Draw "notify_err" "#ff5f6d" { param($g, $pen) $g.DrawLine($pen, (P 3.05 3.05), (P 12.95 12.95)); $g.DrawLine($pen, (P 12.95 3.05), (P 3.05 12.95)) }

Write-Host "[OK] Notification icons drawn" -ForegroundColor Green
