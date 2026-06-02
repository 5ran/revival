import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    implicitHeight: 240

    ColumnLayout {
        anchors.fill: parent
        spacing: 12

        AppCard {
            Layout.fillWidth: true
            Column {
                spacing: 12
                Row {
                    spacing: 10
                    AppToggle {
                        checked: macroController.autoAnglerEnabled
                        onToggled: macroController.autoAnglerEnabled = checked
                    }
                    Text { text: "Auto Angler"; color: "#F0F0F0"; font.pixelSize: 13 }
                }
                AppButton {
                    text: "Use Cursor"
                    variant: "secondary"
                    onClicked: macroController.useCursorPosition()
                }
            }
        }

        RowLayout {
            spacing: 12
            AppCard {
                Layout.fillWidth: true
                Column {
                    spacing: 8
                    Text { text: "Current Fish"; color: "#666666"; font.pixelSize: 12 }
                    Text { text: macroController.currentFishText; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true }
                }
            }
            AppCard {
                Layout.fillWidth: true
                Column {
                    spacing: 8
                    Text { text: "Status"; color: "#666666"; font.pixelSize: 12 }
                    Text { text: macroController.autoAnglerStatusText; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true }
                }
            }
        }
    }
}

