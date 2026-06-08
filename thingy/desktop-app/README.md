# OpenMacro Swift WPF Frontend

This is the separate C# WPF frontend for the native C++ OpenMacro core.

## Stack

- .NET 9 WPF
- MVVM via `CommunityToolkit.Mvvm`
- `WPF-UI` Fluent window/titlebar primitives
- Fluent symbol icons
- Named pipes for IPC with the native C++ process

## Build

From the repo root:

```powershell
dotnet restore .\OpenMacroSwift.Wpf\OpenMacroSwift.Wpf.csproj
dotnet build .\OpenMacroSwift.Wpf\OpenMacroSwift.Wpf.csproj -c Release
```

Build the native IPC host:

```powershell
cmake --build .\cpp-macro-port\build-qt-vcpkg-short --config Release --target openmacro_core_ipc
```

## Run

```powershell
dotnet run --project .\OpenMacroSwift.Wpf\OpenMacroSwift.Wpf.csproj -c Release
```

The WPF process launches `openmacro_core_ipc.exe` when available. The current native IPC host supports:

- `StartMacro`
- `StopMacro`
- `UpdateSetting`
- `GetStats`
- `GetStatus`
- `RebindHotkey`

The C++ host intentionally contains TODO markers where the real native macro runtime entrypoints should be wired in. Macro logic remains in C++.

## Structure

```text
OpenMacroSwift.Wpf/
  Models/
  Services/
  ViewModels/
  Converters/
  Docs/
  App.xaml
  MainWindow.xaml
cpp-macro-port/
  src/ipc/openmacro_core_ipc.cpp
```
