param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$outDir = Join-Path $root "out\$Configuration-$Platform"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sys = Get-ChildItem -Path $root -Recurse -Filter "KmdfHelloIoctl.sys" |
    Where-Object { $_.FullName -match "\\$Platform\\$Configuration\\" } |
    Select-Object -First 1

if (-not $sys) {
    throw "Driver sys not found. Run 10_build.ps1 first."
}

$inf = Join-Path $root "driver\KmdfHelloIoctl.inf"
if (-not (Test-Path $inf)) {
    throw "INF not found: $inf"
}

Copy-Item $sys.FullName (Join-Path $outDir "KmdfHelloIoctl.sys") -Force
Copy-Item $inf (Join-Path $outDir "KmdfHelloIoctl.inf") -Force

$clientExe = Get-ChildItem -Path $root -Recurse -Filter "KmdfHelloIoctlClient.exe" |
    Where-Object { $_.FullName -match "\\$Platform\\$Configuration\\" } |
    Select-Object -First 1

if ($clientExe) {
    Copy-Item $clientExe.FullName (Join-Path $outDir "KmdfHelloIoctlClient.exe") -Force
}

Write-Host "Staged to: $outDir"
Get-ChildItem $outDir | Select-Object Name, Length
