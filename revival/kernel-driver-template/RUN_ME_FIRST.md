# KMDF Hello IOCTL - Run Order

Use a VM snapshot first. Run elevated PowerShell for install/remove steps.

## 1) Prerequisites
```powershell
cd C:\Users\ethan\source\repos\Gamething\OpenMacro-Client\kmdf-template\scripts
.\00_prereq_check.ps1
```

## 2) Build
```powershell
.\10_build.ps1 -Configuration Debug -Platform x64
```

## 3) Stage artifacts
```powershell
.\20_stage_artifacts.ps1 -Configuration Debug -Platform x64
```

## 4) Install + start driver (Admin PowerShell)
```powershell
.\30_install_start.ps1 -Configuration Debug -Platform x64
```

## 5) Run user-mode client
```powershell
.\40_run_client.ps1 -Configuration Debug -Platform x64
```

Expected output is a ping response with matching nonce.

## 6) Stop + remove driver package (Admin PowerShell)
```powershell
.\50_stop_remove.ps1
```

## Notes
- If build fails: ensure Visual Studio 2022 + WDK are installed.
- If install fails due signing: use test-signing in a VM only.
- Artifacts are staged in:
  - `kmdf-template\out\Debug-x64`
