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
            text: "Fishing"
            color: "#F0F0F0"
            font.pixelSize: 24
            font.bold: true
        }

        GridLayout {
            columns: 2
            columnSpacing: 12
            rowSpacing: 12

            Text { text: "Tracking Method"; color: "#F0F0F0"; font.pixelSize: 14; font.bold: true }
            AppSelect {
                Layout.preferredWidth: 180
                model: macroController.trackerOptions
                currentIndex: macroController.trackerOptions.indexOf(macroController.selectedTracker)
                onActivated: macroController.selectedTracker = currentText
            }

            Text { text: "Casting"; color: "#F0F0F0"; font.pixelSize: 14; font.bold: true }
            AppSelect {
                Layout.preferredWidth: 180
                model: macroController.castingModes
                currentIndex: macroController.castingModes.indexOf(macroController.selectedCastingMode)
                onActivated: macroController.selectedCastingMode = currentText
            }
        }

        RowLayout {
            spacing: 12

            AppCard {
                Layout.preferredWidth: 430
                Column {
                    spacing: 6
                    Repeater {
                        model: macroController.fishingStatusItems
                        delegate: Row {
                            spacing: 8
                            Text { text: modelData.key; color: "#666666"; font.pixelSize: 13; font.bold: true }
                            Text { text: modelData.value; color: "#F0F0F0"; font.pixelSize: 13; font.bold: true }
                        }
                    }
                }
            }

            AppCard {
                Layout.preferredWidth: 220
                Column {
                    spacing: 6
                    Repeater {
                        model: macroController.fishingStatsItems
                        delegate: Row {
                            spacing: 8
                            Text { text: modelData.key; color: "#666666"; font.pixelSize: 13; font.bold: true }
                            Text { text: modelData.value; color: "#F0F0F0"; font.pixelSize: 13; font.bold: true }
                        }
                    }
                }
            }
        }

        Row {
            visible: macroController.masterlineEquipped
            spacing: 10
            Text { text: "Masterline Rods"; color: "#666666"; font.pixelSize: 13; font.bold: true }
            Text { text: macroController.masterlineRodsText; color: "#F0F0F0"; font.pixelSize: 13; font.bold: true }
        }
    }
}

