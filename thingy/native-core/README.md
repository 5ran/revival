# C++ Macro Port

Incremental C++ port of macro logic from `soruce/Services/Fishing`.

Current scope:
- Step 1 complete: `RodKind` + `RodClassifier`

## New Command Center UI (Fresh Qt Design)

This repository now includes a completely fresh Qt Quick/QML desktop UI for the macro app.
It does not mirror the old Avalonia page structure. The layout is a new control-center model:

- Top command bar (state, runtime, hotkey, update status, start/stop)
- Compact icon rail navigation
- Main dashboard/module workspace
- Right inspector panel for selected module and quick controls
- Bottom live activity feed strip
- Optional compact HUD mode while running

### Runtime Integration Points

The UI is wired to existing C++ bridge controllers, with explicit TODO hooks for production runtime wiring:

- `ui/src/backend/MacroController.cpp`
  - `toggleMacro()` - connect to real start/stop dispatcher
  - `rebindHotkey()` - connect to native hotkey capture
  - `useCursorPosition()` - connect to in-game cursor sampling
  - `openModuleDetails()` - map module tiles to real settings models
  - `pushActivity()` - feed with real runtime events/logs

### Fresh UI File Structure

```text
ui/
  src/
    main.cpp
    backend/
      AppState.hpp/.cpp
      MacroController.hpp/.cpp
      SettingsController.hpp/.cpp
      AuthController.hpp/.cpp
      NavigationController.hpp/.cpp
  qml/
    Main.qml
    components2/
      GlassPanel.qml
      StatusChip.qml
      IconRailButton.qml
      ModuleTile.qml
      CommandBar.qml
      InspectorSection.qml
      ActivityFeed.qml
      HudCompactView.qml
    views2/
      CommandCenterView.qml
      FishingControlView.qml
      AddonsView.qml
      AutomationView.qml
      HuntDetectView.qml
      AccountView.qml
      SettingsView.qml
```

## Qt 6 Modern UI (QML)

A complete new UI layer was added under `cpp-macro-port/ui` using:
- C++20
- Qt 6 (`Qt Quick` + `QML`)
- CMake (`Qt6::Quick`, `qt_add_qml_module`)
- backend bridge classes exposed to QML

Existing macro logic in `include/` + `src/` remains intact and is not removed.

### UI Architecture

- QML component system:
  - `AppButton.qml`
  - `AppCard.qml`
  - `AppInput.qml`
  - `AppSelect.qml`
  - `AppToggle.qml`
  - `AppSidebarButton.qml`
  - `AppTitleBar.qml`
  - `AppChip.qml`
  - `AppSlider.qml`
  - `AppSegmentedControl.qml`
- C++ bridge classes:
  - `AppState`
  - `MacroController`
  - `SettingsController`
  - `AuthController`
  - `NavigationController`

### Required Installs

1. Visual Studio 2022 Build Tools or Visual Studio 2022 with C++ workload.
2. CMake 3.20+.
3. Qt 6.5+ (Quick, QML, Quick Controls).
   - If missing, install via Qt Online Installer (GUI or CLI mode).
4. Optional: vcpkg (manifest file included as `vcpkg.json`).

### Configure (Qt installed normally)

From `cpp-macro-port/`:

```powershell
cmake -S . -B build-qt -DOPENMACRO_BUILD_QT_UI=ON
```

If Qt is not auto-discovered, pass `CMAKE_PREFIX_PATH`:

```powershell
cmake -S . -B build-qt `
  -DOPENMACRO_BUILD_QT_UI=ON `
  -DCMAKE_PREFIX_PATH=\"C:/Qt/6.7.2/msvc2022_64\"
```

### Configure (vcpkg manifest mode)

If using vcpkg toolchain:

```powershell
cmake -S . -B build-qt-vcpkg `
  -DOPENMACRO_BUILD_QT_UI=ON `
  -DCMAKE_TOOLCHAIN_FILE=<path-to-vcpkg>/scripts/buildsystems/vcpkg.cmake
```

Notes:
- `vcpkg.json` is provided for manifest mode dependency resolution.
- If your local vcpkg requires baseline refresh, run:
  - `vcpkg x-update-baseline --add-initial-baseline`

### Build

```powershell
cmake --build build-qt --config Release
```

### Run

```powershell
./build-qt/Release/openmacro_ui.exe
```

## File Layout (UI)

```text
cpp-macro-port/
  ui/
    CMakeLists.txt
    src/
      main.cpp
      backend/
        AppState.hpp/.cpp
        MacroController.hpp/.cpp
        SettingsController.hpp/.cpp
        AuthController.hpp/.cpp
        NavigationController.hpp/.cpp
    qml/
      Main.qml
      components/
        Theme.qml
        AppButton.qml
        AppCard.qml
        AppInput.qml
        AppSelect.qml
        AppToggle.qml
        AppSidebarButton.qml
        AppTitleBar.qml
        AppChip.qml
        AppSlider.qml
        AppSegmentedControl.qml
      pages/
        BlankPage.qml
        LoginPage.qml
        LockedOutPage.qml
        ShellPage.qml
        DashboardPage.qml
        GeneralPage.qml
        FishingPage.qml
        FishingAddonsPage.qml
        OtherAutomationPage.qml
        AutoAnglerPage.qml
        EnchantPage.qml
        AppraisePage.qml
        TreasureAppraisePage.qml
        AutoSovPage.qml
        HuntDetectPage.qml
        AutoTotemPage.qml
        AccountPage.qml
        SettingsPage.qml
        CompactPage.qml
```
