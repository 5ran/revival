param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

function Test-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    throw "Run this script in an elevated PowerShell window."
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$stage = Join-Path $root "out\$Configuration-$Platform"
$inf = Join-Path $stage "KmdfHelloIoctl.inf"

if (-not (Test-Path $inf)) {
    throw "Staged INF not found: $inf. Run 20_stage_artifacts.ps1 first."
}

Write-Host "Installing INF via pnputil..."
pnputil /add-driver $inf /install
if ($LASTEXITCODE -ne 0) { throw "pnputil failed." }

Write-Host "Starting service KmdfHelloIoctl..."
sc.exe start KmdfHelloIoctl | Out-Host

Write-Host "Querying service state..."
sc.exe query KmdfHelloIoctl | Out-Host
