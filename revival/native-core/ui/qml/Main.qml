import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import OpenMacro.UI

ApplicationWindow {
    id: root
    width: 1240
    height: 780
    minimumWidth: 1120
    minimumHeight: 700
    visible: true
    color: Tokens.bg
    title: "OpenMacro Swift"
    flags: Qt.FramelessWindowHint | Qt.Window | (appState.topMost ? Qt.WindowStaysOnTopHint : 0)

    property var navIcons: {
        "General": "qrc:/qt/qml/OpenMacro/UI/qml/icons/general.svg",
        "Fishing": "qrc:/qt/qml/OpenMacro/UI/qml/icons/fishing.svg",
        "Addons": "qrc:/qt/qml/OpenMacro/UI/qml/icons/addons.svg",
        "Automation": "qrc:/qt/qml/OpenMacro/UI/qml/icons/automation.svg",
        "Hunt Detect": "qrc:/qt/qml/OpenMacro/UI/qml/icons/hunt.svg",
        "Account": "qrc:/qt/qml/OpenMacro/UI/qml/icons/account.svg",
        "Settings": "qrc:/qt/qml/OpenMacro/UI/qml/icons/settings.svg"
    }
    property string pendingPage: navigationController.currentPage

    function pageComponent(page) {
        switch (page) {
        case "Fishing": return fishing
        case "Addons": return addons
        case "Automation": return automation
        case "Hunt Detect": return hunt
        case "Account": return account
        case "Settings": return settings
        default: return general
        }
    }

    Connections {
        target: appState
        function onMinimizeRequested() { root.showMinimized() }
        function onCloseRequested() { Qt.quit() }
    }

    Rectangle { anchors.fill: parent; color: Tokens.bg }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 16
        spacing: 10

        CommandBar {
            Layout.fillWidth: true
            Layout.preferredHeight: 64
            runtimeText: macroController.runTimeText
            stateText: macroController.runStateText
            hotkeyText: macroController.hotkeyText
            running: macroController.macroRunning
            updateText: appState.updateStatusText
            onStartStopClicked: macroController.toggleMacro()
            onMinimizeClicked: appState.requestMinimize()
            onCloseClicked: appState.requestClose()
        }

        RowLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            spacing: 10

            AppCard {
                Layout.preferredWidth: 228
                Layout.fillHeight: true
                padding: 14

                ColumnLayout {
                    anchors.fill: parent
                    spacing: 8

                    Text { text: "OpenMacro"; color: Tokens.text; font.pixelSize: 20; font.bold: true }
                    Text { text: "Swift"; color: Tokens.textMuted; font.pixelSize: 13 }

                    Repeater {
                        model: navigationController.sidebarItems
                        delegate: AppSidebarItem {
                            Layout.fillWidth: true
                            text: modelData
                            iconSource: root.navIcons[modelData]
                            active: navigationController.currentPage === modelData
                            onClicked: navigationController.navigate(modelData)
                        }
                    }
                    Item { Layout.fillHeight: true }
                }
            }

            AppCard {
                Layout.fillWidth: true
                Layout.fillHeight: true
                padding: 22

                Item {
                    anchors.fill: parent
                    Loader {
                        id: pageLoader
                        anchors.fill: parent
                        opacity: 1.0
                        x: 0
                        sourceComponent: root.pageComponent(navigationController.currentPage)
                        Behavior on opacity { NumberAnimation { duration: 150 } }
                        Behavior on x { NumberAnimation { duration: 160; easing.type: Easing.OutCubic } }
                    }

                    Connections {
                        target: navigationController
                        function onCurrentPageChanged() {
                            root.pendingPage = navigationController.currentPage
                            pageLoader.opacity = 0.0
                            pageLoader.x = 18
                            swapTimer.restart()
                        }
                    }

                    Timer {
                        id: swapTimer
                        interval: 70
                        repeat: false
                        onTriggered: {
                            pageLoader.sourceComponent = root.pageComponent(root.pendingPage)
                            pageLoader.opacity = 1.0
                            pageLoader.x = 0
                        }
                    }
                }
            }
        }

        AppCard {
            Layout.fillWidth: true
            Layout.preferredHeight: 78
            padding: 8
            ActivityFeed { anchors.fill: parent; entries: macroController.activityFeed }
        }
    }

    Loader {
        anchors.fill: parent
        visible: macroController.macroRunning && appState.compactMode
        sourceComponent: compactHud
        z: 99
    }

    Component { id: general; CommandCenterView {} }
    Component { id: fishing; FishingControlView {} }
    Component { id: addons; AddonsView {} }
    Component { id: automation; AutomationView {} }
    Component { id: hunt; HuntDetectView {} }
    Component { id: account; AccountView {} }
    Component { id: settings; SettingsView {} }
    Component { id: compactHud; HudCompactView {} }
}
