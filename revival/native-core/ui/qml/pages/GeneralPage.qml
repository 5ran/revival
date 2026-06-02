import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    Rectangle { anchors.fill: parent; color: "#0E0E0E" }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 34
        spacing: 16

        Text {
            text: "General"
            color: "#F0F0F0"
            font.pixelSize: 24
            font.bold: true
        }

        AppCard {
            Layout.preferredWidth: 430
            GridLayout {
                columns: 2
                columnSpacing: 20
                rowSpacing: 14

                Column {
                    spacing: 4
                    Text { text: macroController.activeMacroText; color: "#666666"; font.pixelSize: 12 }
                    Text { text: macroController.runStateText; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true }
                    Text { text: macroController.runTimeText; color: "#F0F0F0"; font.pixelSize: 12; font.family: "Cascadia Mono" }
                }

                AppButton {
                    text: macroController.macroRunning ? "Stop" : "Start"
                    onClicked: {
                        macroController.toggleMacro()
                        appState.compactMode = macroController.macroRunning
                    }
                }

                Rectangle { Layout.columnSpan: 2; height: 1; color: "#2A2A2A"; Layout.fillWidth: true }

                Column {
                    spacing: 4
                    Text { text: "Hotkey"; color: "#666666"; font.pixelSize: 12 }
                    Text { text: macroController.hotkeyText; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true }
                }

                AppButton {
                    text: "Rebind"
                    variant: "secondary"
                    onClicked: macroController.rebindHotkey()
                }

                Rectangle { Layout.columnSpan: 2; height: 1; color: "#2A2A2A"; Layout.fillWidth: true }

                Column {
                    spacing: 4
                    Text { text: "Rod Location"; color: "#666666"; font.pixelSize: 12 }
                    Text { text: macroController.selectedRodSlot; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true }
                }

                AppSelect {
                    model: macroController.rodSlots
                    currentIndex: macroController.rodSlots.indexOf(macroController.selectedRodSlot)
                    onActivated: macroController.selectedRodSlot = currentText
                }
            }
        }
    }
}

