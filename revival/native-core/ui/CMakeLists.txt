set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)
set(CMAKE_AUTOUIC ON)

find_package(Qt6 6.5 COMPONENTS Quick Qml QuickControls2 QUIET)
if (NOT Qt6_FOUND)
    message(WARNING "Qt6 (Quick/Qml/QuickControls2) not found. Skipping openmacro_ui target.")
    return()
endif()

qt_standard_project_setup(REQUIRES 6.5)

qt_add_executable(openmacro_ui
    src/main.cpp
    src/backend/AppState.cpp
    src/backend/MacroController.cpp
    src/backend/SettingsController.cpp
    src/backend/AuthController.cpp
    src/backend/NavigationController.cpp
)

target_include_directories(openmacro_ui PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

target_compile_features(openmacro_ui PRIVATE cxx_std_20)

target_link_libraries(openmacro_ui
    PRIVATE
        Qt6::Quick
        Qt6::Qml
        Qt6::QuickControls2
        macro_rod
)

qt_add_qml_module(openmacro_ui
    URI OpenMacro.UI
    VERSION 1.0
    QML_FILES
        qml/Main.qml

        qml/components/Theme.qml
        qml/components/Tokens.qml
        qml/components/AppButton.qml
        qml/components/AppCard.qml
        qml/components/AppSidebarItem.qml
        qml/components/AppComboBox.qml
        qml/components/AppInput.qml
        qml/components/AppTextField.qml
        qml/components/AppSelect.qml
        qml/components/AppToggle.qml
        qml/components/AppDialog.qml
        qml/components/AppPageHeader.qml
        qml/components/AppSidebarButton.qml
        qml/components/AppTitleBar.qml
        qml/components/AppChip.qml
        qml/components/AppSlider.qml
        qml/components/AppSegmentedControl.qml
        qml/components2/GlassPanel.qml
        qml/components2/NavRailItem.qml
        qml/components2/PageHeader.qml
        qml/components2/StatusChip.qml
        qml/components2/IconRailButton.qml
        qml/components2/ModuleTile.qml
        qml/components2/CommandBar.qml
        qml/components2/InspectorSection.qml
        qml/components2/ActivityFeed.qml
        qml/components2/HudCompactView.qml

        qml/views2/CommandCenterView.qml
        qml/views2/FishingControlView.qml
        qml/views2/AddonsView.qml
        qml/views2/AutomationView.qml
        qml/views2/HuntDetectView.qml
        qml/views2/AccountView.qml
        qml/views2/SettingsView.qml

        qml/pages/BlankPage.qml
        qml/pages/LoginPage.qml
        qml/pages/LockedOutPage.qml
        qml/pages/ShellPage.qml
        qml/pages/DashboardPage.qml
        qml/pages/GeneralPage.qml
        qml/pages/FishingPage.qml
        qml/pages/FishingAddonsPage.qml
        qml/pages/OtherAutomationPage.qml
        qml/pages/AutoAnglerPage.qml
        qml/pages/EnchantPage.qml
        qml/pages/AppraisePage.qml
        qml/pages/TreasureAppraisePage.qml
        qml/pages/AutoSovPage.qml
        qml/pages/HuntDetectPage.qml
        qml/pages/AutoTotemPage.qml
        qml/pages/AccountPage.qml
        qml/pages/SettingsPage.qml
        qml/pages/CompactPage.qml
    RESOURCES
        qml/icons/general.svg
        qml/icons/fishing.svg
        qml/icons/addons.svg
        qml/icons/automation.svg
        qml/icons/hunt.svg
        qml/icons/account.svg
        qml/icons/settings.svg
)

if (WIN32)
    set_target_properties(openmacro_ui PROPERTIES WIN32_EXECUTABLE TRUE)
endif()



