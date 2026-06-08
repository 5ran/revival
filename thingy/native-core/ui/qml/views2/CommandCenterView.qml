import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    ColumnLayout {
        anchors.fill: parent
        spacing: 16

        AppPageHeader { title: "General"; subtitle: "Core runtime overview"; Layout.fillWidth: true }

        RowLayout {
            Layout.fillWidth: true
            spacing: 16

            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 168
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    spacing: 8
                    Text { text: "Status"; color: "#8F9FB3"; font.pixelSize: 13 }
                    Text { text: macroController.runStateText; color: "#F0F5FC"; font.pixelSize: 34; font.bold: true }
                    StatusChip { label: macroController.macroRunning ? "Running" : "Stopped"; state: macroController.macroRunning ? "running" : "idle" }
                }
            }

            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 168
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    spacing: 8
                    Text { text: "Hotkey"; color: "#8F9FB3"; font.pixelSize: 13 }
                    Text { text: macroController.hotkeyText; color: "#F0F5FC"; font.pixelSize: 34; font.bold: true }
                    AppButton { text: "Rebind Hotkey"; variant: "secondary"; onClicked: macroController.rebindHotkey() }
                }
            }

            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 168
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    spacing: 8
                    Text { text: "Rod Slot"; color: "#8F9FB3"; font.pixelSize: 13 }
                    Text { text: macroController.selectedRodSlot; color: "#F0F5FC"; font.pixelSize: 34; font.bold: true }
                    AppSelect { Layout.fillWidth: true; model: macroController.rodSlots; currentIndex: macroController.rodSlots.indexOf(macroController.selectedRodSlot); onActivated: macroController.selectedRodSlot = currentText }
                }
            }
        }

        GlassPanel {
            Layout.fillWidth: true
            Layout.fillHeight: true
            padding: 20
            ColumnLayout {
                anchors.fill: parent
                spacing: 12
                Text { text: "Quick Runtime Controls"; color: "#F0F5FC"; font.pixelSize: 20; font.bold: true }
                RowLayout {
                    spacing: 12
                    AppButton { text: macroController.macroRunning ? "Stop Macro" : "Start Macro"; onClicked: macroController.toggleMacro() }
                    AppSelect { model: macroController.trackerOptions; currentIndex: macroController.trackerOptions.indexOf(macroController.selectedTracker); onActivated: macroController.selectedTracker = currentText }
                    AppSelect { model: macroController.castingModes; currentIndex: macroController.castingModes.indexOf(macroController.selectedCastingMode); onActivated: macroController.selectedCastingMode = currentText }
                }
            }
        }
    }
}


