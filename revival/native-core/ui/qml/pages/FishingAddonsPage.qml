import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    Rectangle { anchors.fill: parent; color: "#0E0E0E" }

    Flickable {
        anchors.fill: parent
        contentWidth: width
        contentHeight: content.implicitHeight + 34
        clip: true

        ColumnLayout {
            id: content
            width: parent.width - 68
            x: 34
            y: 34
            spacing: 12

            Text {
                text: macroController.activeAddonTitle
                color: "#F0F0F0"
                font.pixelSize: 24
                font.bold: true
            }

            AppCard {
                Layout.fillWidth: true
                RowLayout {
                    anchors.fill: parent
                    Text { Layout.fillWidth: true; text: "Auto Aquarium"; color: "#F0F0F0"; font.pixelSize: 14; font.bold: true }
                    AppToggle { checked: macroController.autoAquariumEnabled; onToggled: macroController.autoAquariumEnabled = checked }
                    AppButton { text: macroController.autoAquariumExpanded ? "Hide" : "Show"; implicitWidth: 70; onClicked: macroController.autoAquariumExpanded = !macroController.autoAquariumExpanded }
                }
            }

            AppCard {
                visible: macroController.autoAquariumExpanded
                Layout.fillWidth: true
                RowLayout {
                    anchors.fill: parent
                    Column {
                        spacing: 4
                        Text { text: "Cycle Delay (min)"; color: "#666666"; font.pixelSize: 12 }
                        AppInput {
                            width: 170
                            text: Number(macroController.aquariumCycleDelayMinutes).toFixed(0)
                            onEditingFinished: macroController.aquariumCycleDelayMinutes = Number(text)
                        }
                    }
                    Text {
                        text: "Time between aquarium runs."
                        color: "#666666"
                        font.pixelSize: 12
                        Layout.fillWidth: true
                        wrapMode: Text.WordWrap
                    }
                }
            }

            AppCard {
                Layout.fillWidth: true
                RowLayout {
                    anchors.fill: parent
                    Text { Layout.fillWidth: true; text: "Auto Totem - Leave roblox chat open"; color: "#F0F0F0"; font.pixelSize: 14; font.bold: true }
                    AppToggle { checked: macroController.autoTotemEnabled; onToggled: macroController.autoTotemEnabled = checked }
                    AppButton { text: macroController.autoTotemExpanded ? "Hide" : "Show"; implicitWidth: 70; onClicked: macroController.autoTotemExpanded = !macroController.autoTotemExpanded }
                }
            }

            AutoTotemPage {
                visible: macroController.autoTotemExpanded
                implicitHeight: visible ? 280 : 0
                Layout.fillWidth: true
            }

            AppCard {
                Layout.fillWidth: true
                RowLayout {
                    anchors.fill: parent
                    Text { Layout.fillWidth: true; text: "Auto Sovereign Recharge - Gamepass enchant required"; color: "#F0F0F0"; font.pixelSize: 14; font.bold: true }
                    AppToggle { checked: macroController.autoSovEnabled; onToggled: macroController.autoSovEnabled = checked }
                    AppButton { text: macroController.autoSovExpanded ? "Hide" : "Show"; implicitWidth: 70; onClicked: macroController.autoSovExpanded = !macroController.autoSovExpanded }
                }
            }

            AutoSovPage {
                visible: macroController.autoSovExpanded
                implicitHeight: visible ? 180 : 0
                Layout.fillWidth: true
            }

            AppCard {
                Layout.fillWidth: true
                RowLayout {
                    anchors.fill: parent
                    Text { Layout.fillWidth: true; text: "Hunt Detect - Leave roblox chat open"; color: "#F0F0F0"; font.pixelSize: 14; font.bold: true }
                    AppToggle { checked: macroController.huntDetectEnabled; onToggled: macroController.huntDetectEnabled = checked }
                    AppButton { text: macroController.huntDetectExpanded ? "Hide" : "Show"; implicitWidth: 70; onClicked: macroController.huntDetectExpanded = !macroController.huntDetectExpanded }
                }
            }

            HuntDetectPage {
                visible: macroController.huntDetectExpanded
                implicitHeight: visible ? 420 : 0
                Layout.fillWidth: true
            }
        }
    }
}

