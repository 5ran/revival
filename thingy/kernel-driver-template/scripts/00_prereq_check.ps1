param()

$ErrorActionPreference = "Stop"

function Test-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Write-Host "== KMDF Template Prerequisite Check =="
Write-Host "Repo: $PSScriptRoot\.."

$admin = Test-Admin
Write-Host ("Admin shell: " + ($(if ($admin) { "YES" } else { "NO" })))

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    Write-Warning "vswhere not found. Install Visual Studio 2022."
    exit 1
}

$vsPath = & $vswhere -latest -products * -property installationPath
if (-not $vsPath) {
    Write-Warning "Visual Studio installation not found."
    exit 1
}

Write-Host "Visual Studio: $vsPath"

$msbuild = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    Write-Warning "MSBuild not found at expected path: $msbuild"
    exit 1
}
Write-Host "MSBuild: $msbuild"

$wdkRoots = @(
    "C:\Program Files (x86)\Windows Kits\10\build",
    "C:\Program Files (x86)\Windows Kits\10\Include"
)

foreach ($p in $wdkRoots) {
    if (-not (Test-Path $p)) {
        Write-Warning "WDK component path missing: $p"
        exit 1
    }
}

Write-Host "WDK paths: OK"
Write-Host "Prerequisite check passed."
