import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    Rectangle { anchors.fill: parent; color: "#0E0E0E" }

    CompactPage {
        anchors.fill: parent
        visible: appState.compactMode
    }

    RowLayout {
        anchors.fill: parent
        visible: !appState.compactMode
        spacing: 0

        Rectangle {
            Layout.preferredWidth: 168
            Layout.fillHeight: true
            color: "#0E0E0E"
            border.color: "#2A2A2A"
            border.width: 1

            Column {
                anchors.fill: parent
                anchors.margins: 8
                spacing: 4

                Repeater {
                    model: navigationController.sidebarItems
                    delegate: AppSidebarButton {
                        required property string modelData
                        width: parent.width
                        text: modelData
                        selected: navigationController.currentPage === modelData
                        onClicked: navigationController.navigate(modelData)
                    }
                }
            }
        }

        ColumnLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            spacing: 0

            Rectangle {
                visible: appState.updateAvailable
                Layout.fillWidth: true
                Layout.preferredHeight: 40
                color: "#0F1D1C"
                border.color: "#2A2A2A"
                border.width: 1

                RowLayout {
                    anchors.fill: parent
                    anchors.leftMargin: 24
                    anchors.rightMargin: 24
                    spacing: 14

                    Text { text: "Update ready"; color: "#F0F0F0"; font.pixelSize: 13; font.bold: true }
                    Text { Layout.fillWidth: true; text: appState.updateStatusText; color: "#666666"; font.pixelSize: 12 }
                    AppButton { text: "Restart" }
                }
            }

            Loader {
                Layout.fillWidth: true
                Layout.fillHeight: true
                sourceComponent: {
                    if (navigationController.currentPage === "General") return general
                    if (navigationController.currentPage === "Fishing") return fishing
                    if (navigationController.currentPage === "Fishing Addons") return fishingAddons
                    if (navigationController.currentPage === "Other Automation") return otherAutomation
                    if (navigationController.currentPage === "Account") return account
                    if (navigationController.currentPage === "Settings") return settings
                    return general
                }
            }

            Component { id: general; GeneralPage {} }
            Component { id: fishing; FishingPage {} }
            Component { id: fishingAddons; FishingAddonsPage {} }
            Component { id: otherAutomation; OtherAutomationPage {} }
            Component { id: account; AccountPage {} }
            Component { id: settings; SettingsPage {} }
        }
    }
}

