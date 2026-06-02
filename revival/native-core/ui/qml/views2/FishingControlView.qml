import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    ColumnLayout {
        anchors.fill: parent
        spacing: 16

        AppPageHeader { title: "Fishing"; subtitle: "Tracking and cast control"; Layout.fillWidth: true }

        RowLayout {
            Layout.fillWidth: true
            spacing: 16
            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 120
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    Text { text: "Tracking Method"; color: "#8F9FB3"; font.pixelSize: 13 }
                    AppSelect { Layout.fillWidth: true; model: macroController.trackerOptions; currentIndex: macroController.trackerOptions.indexOf(macroController.selectedTracker); onActivated: macroController.selectedTracker = currentText }
                }
            }
            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 120
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    Text { text: "Casting Method"; color: "#8F9FB3"; font.pixelSize: 13 }
                    AppSelect { Layout.fillWidth: true; model: macroController.castingModes; currentIndex: macroController.castingModes.indexOf(macroController.selectedCastingMode); onActivated: macroController.selectedCastingMode = currentText }
                }
            }
        }

        RowLayout {
            Layout.fillWidth: true
            spacing: 16
            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 220
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    Text { text: "Status"; color: "#F0F5FC"; font.pixelSize: 20; font.bold: true }
                    Repeater {
                        model: macroController.fishingStatusItems
                        delegate: RowLayout {
                            Layout.fillWidth: true
                            Text { text: modelData.key; color: "#8F9FB3"; Layout.preferredWidth: 150 }
                            Text { text: modelData.value; color: "#EAF1FB"; font.bold: true }
                        }
                    }
                }
            }
            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 220
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    Text { text: "Statistics"; color: "#F0F5FC"; font.pixelSize: 20; font.bold: true }
                    Repeater {
                        model: macroController.fishingStatsItems
                        delegate: RowLayout {
                            Layout.fillWidth: true
                            Text { text: modelData.key; color: "#8F9FB3"; Layout.preferredWidth: 120 }
                            Text { text: modelData.value; color: "#EAF1FB"; font.bold: true }
                        }
                    }
                }
            }
        }

        GlassPanel {
            visible: macroController.masterlineEquipped
            Layout.fillWidth: true
            Layout.preferredHeight: 92
            padding: 20
            ColumnLayout {
                anchors.fill: parent
                Text { text: "Masterline Rods"; color: "#8F9FB3"; font.pixelSize: 13 }
                Text { text: macroController.masterlineRodsText; color: "#F0F5FC"; font.pixelSize: 19; font.bold: true }
            }
        }

        Item { Layout.fillHeight: true }
    }
}


