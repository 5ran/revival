import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    Rectangle { anchors.fill: parent; color: "#0E0E0E" }

    GridLayout {
        anchors.fill: parent
        anchors.margins: 14
        columns: 1
        rowSpacing: 8

        RowLayout {
            Layout.fillWidth: true
            spacing: 10

            Text {
                text: macroController.compactMacroName
                color: "#F0F0F0"
                font.pixelSize: 13
                font.bold: true
            }

            AppChip {
                text: macroController.compactPhase
                chipColor: "#161616"
                borderColor: "#2A2A2A"
                textColor: "#A9CFCB"
            }

            Item { Layout.fillWidth: true }

            Text {
                text: macroController.compactRuntime
                color: "#F0F0F0"
                font.pixelSize: 11
                font.family: "Cascadia Mono"
            }
        }

        Text {
            Layout.fillWidth: true
            text: macroController.compactStatusMessage
            color: "#666666"
            font.pixelSize: 11
            elide: Text.ElideRight
        }

        Rectangle {
            Layout.fillWidth: true
            Layout.preferredHeight: 24
            color: "#161616"
            border.color: "#2A2A2A"
            border.width: 1
            radius: 3

            Rectangle {
                x: macroController.playerbarLeft
                y: 5
                width: macroController.playerbarWidth
                height: 14
                radius: 2
                color: "#A9CFCB"
                opacity: 0.75
            }

            Rectangle {
                x: macroController.fishMarkerLeft
                y: 6
                width: 12
                height: 12
                radius: 6
                color: "#F0F0F0"
                border.color: "#0E0E0E"
                border.width: 2
            }
        }

        RowLayout {
            Layout.fillWidth: true
            Text { text: "Caught"; color: "#666666"; font.pixelSize: 11 }
            Text { text: macroController.compactCaught; color: "#F0F0F0"; font.pixelSize: 13; font.bold: true }
            Item { Layout.fillWidth: true }
            Text { text: "Lost"; color: "#666666"; font.pixelSize: 11 }
            Text { text: macroController.compactLost; color: "#F0F0F0"; font.pixelSize: 12 }
            Item { Layout.fillWidth: true }
            Text { text: "SR"; color: "#666666"; font.pixelSize: 11 }
            Text { text: macroController.compactSr; color: "#F0F0F0"; font.pixelSize: 12 }
        }

        RowLayout {
            Layout.fillWidth: true
            ColumnLayout {
                Layout.fillWidth: true
                Text { text: "Totem: " + macroController.selectedTotem; color: "#F0F0F0"; font.pixelSize: 12 }
                Text { text: macroController.compactAquariumStatus; color: "#666666"; font.pixelSize: 11 }
            }

            ColumnLayout {
                Layout.fillWidth: true
                RowLayout {
                    Text { text: "World"; color: "#666666"; font.pixelSize: 11 }
                    Text { text: macroController.compactWeather; color: "#F0F0F0"; font.pixelSize: 12 }
                    Text { text: "?"; color: "#666666"; font.pixelSize: 11 }
                    Text { text: macroController.compactCycle; color: "#F0F0F0"; font.pixelSize: 12 }
                }

                RowLayout {
                    spacing: 6
                    Rectangle { visible: macroController.shinySurge; width: 8; height: 8; radius: 4; color: "#FFD27F" }
                    Rectangle { visible: macroController.sparklingSurge; width: 8; height: 8; radius: 4; color: "#9ED1FF" }
                    Rectangle { visible: macroController.mutationSurge; width: 8; height: 8; radius: 4; color: "#C99EFF" }
                    Text { text: (macroController.shinySurge || macroController.sparklingSurge || macroController.mutationSurge) ? "surge" : ""; color: "#666666"; font.pixelSize: 11 }
                }
            }
        }

        Item { Layout.fillHeight: true }

        RowLayout {
            Layout.fillWidth: true
            Text { text: macroController.compactOffsetsVersion; color: "#666666"; font.pixelSize: 11; elide: Text.ElideRight; Layout.fillWidth: true }
            AppChip { text: macroController.hotkeyText }
        }
    }
}

