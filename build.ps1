$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /codepage:65001 /target:winexe /optimize+ `
    /win32icon:"$root\OrbitWheel-Lite.ico" `
    /out:"$dist\OrbitWheel-Preview.exe" `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Web.Extensions.dll `
    /resource:"$root\assets\system-icons-sheet.png",OrbitWheel.SystemIcons `
    "$root\OrbitWheelLite.cs"

if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }

Copy-Item "$root\README.md" "$dist\README.md" -Force
Copy-Item "$root\PREVIEW.md" "$dist\PREVIEW.md" -Force
Copy-Item "$root\LICENSE" "$dist\LICENSE" -Force
Write-Host "Built: $dist\OrbitWheel-Preview.exe"
