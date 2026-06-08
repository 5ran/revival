param()

$ErrorActionPreference = "Continue"

function Test-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    throw "Run this script in an elevated PowerShell window."
}

Write-Host "Stopping service KmdfHelloIoctl..."
sc.exe stop KmdfHelloIoctl | Out-Host
Start-Sleep -Seconds 1
sc.exe query KmdfHelloIoctl | Out-Host

Write-Host "Removing driver package candidates (oem*.inf with KmdfHelloIoctl)..."
$drivers = pnputil /enum-drivers
$published = @()
for ($i = 0; $i -lt $drivers.Length; $i++) {
    if ($drivers[$i] -match "^Published Name:\s+(oem\d+\.inf)$") {
        $name = $Matches[1]
        $block = ($drivers[$i..([Math]::Min($i + 15, $drivers.Length - 1))] -join "`n")
        if ($block -match "KmdfHelloIoctl") {
            $published += $name
        }
    }
}

foreach ($inf in $published | Select-Object -Unique) {
    Write-Host "Deleting $inf"
    pnputil /delete-driver $inf /uninstall /force | Out-Host
}

Write-Host "Done."
