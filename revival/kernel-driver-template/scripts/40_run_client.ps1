param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$stage = Join-Path $root "out\$Configuration-$Platform"
$exe = Join-Path $stage "KmdfHelloIoctlClient.exe"

if (-not (Test-Path $exe)) {
    throw "Client exe not found: $exe. Ensure build and staging completed."
}

Write-Host "Running: $exe"
& $exe
if ($LASTEXITCODE -ne 0) {
    throw "Client returned exit code $LASTEXITCODE"
}
