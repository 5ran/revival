import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    implicitHeight: 220

    ColumnLayout {
        anchors.fill: parent
        spacing: 12

        AppCard {
            Layout.fillWidth: true
            Column {
                spacing: 10
                Row {
                    spacing: 10
                    AppToggle {
                        checked: macroController.autoTreasureEnabled
                        onToggled: macroController.autoTreasureEnabled = checked
                    }
                    Text { text: "Auto Treasure"; color: "#F0F0F0"; font.pixelSize: 13 }
                }

                RowLayout {
                    spacing: 10
                    Column {
                        spacing: 4
                        Text { text: "Click Delay (sec)"; color: "#666666"; font.pixelSize: 12 }
                        AppInput {
                            width: 160
                            text: Number(macroController.treasureClickDelaySeconds).toString()
                            onEditingFinished: macroController.treasureClickDelaySeconds = Number(text)
                        }
                    }
                    Column {
                        spacing: 4
                        Text { text: "Minimum Multi"; color: "#666666"; font.pixelSize: 12 }
                        AppInput {
                            width: 160
                            text: Number(macroController.treasureMinimumMulti).toString()
                            onEditingFinished: macroController.treasureMinimumMulti = Number(text)
                        }
                    }
                }
            }
        }

        AppCard {
            Layout.fillWidth: true
            Column {
                spacing: 8
                Text { text: "Status"; color: "#666666"; font.pixelSize: 12 }
                Text { text: macroController.treasureStatusText; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true }
            }
        }
    }
}

