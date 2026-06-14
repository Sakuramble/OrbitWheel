$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap 256,256
$g = [System.Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$rect = New-Object System.Drawing.Rectangle 12,12,232,232
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect,([System.Drawing.Color]::FromArgb(55,216,255)),([System.Drawing.Color]::FromArgb(128,70,255)),45
$g.FillEllipse($brush,$rect)
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(245,255,255,255)),20
$pen.StartCap = $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawArc($pen,62,62,132,132,20,275)
$g.DrawLine($pen,134,128,185,77)
$g.FillEllipse([System.Drawing.Brushes]::White,116,110,36,36)
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream = [System.IO.File]::Create((Join-Path $PSScriptRoot 'OrbitWheel-Lite.ico'))
$icon.Save($stream)
$stream.Dispose()
$icon.Dispose()
$pen.Dispose()
$brush.Dispose()
$g.Dispose()
$bitmap.Dispose()
