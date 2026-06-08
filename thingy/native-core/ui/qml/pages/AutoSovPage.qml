import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    implicitHeight: 180

    ColumnLayout {
        anchors.fill: parent
        spacing: 12

        AppCard {
            Layout.fillWidth: true
            RowLayout {
                anchors.fill: parent
                spacing: 10

                Column {
                    spacing: 4
                    Text { text: "Min %"; color: "#666666"; font.pixelSize: 12 }
                    AppInput {
                        width: 170
                        text: Number(macroController.sovMinPercent).toString()
                        onEditingFinished: macroController.sovMinPercent = Number(text)
                    }
                }

                Column {
                    spacing: 4
                    Text { text: "Max %"; color: "#666666"; font.pixelSize: 12 }
                    AppInput {
                        width: 170
                        text: Number(macroController.sovMaxPercent).toString()
                        onEditingFinished: macroController.sovMaxPercent = Number(text)
                    }
                }
            }
        }

        AppCard {
            Layout.fillWidth: true
            Column {
                spacing: 8
                Text { text: "Status"; color: "#666666"; font.pixelSize: 12 }
                Text { text: macroController.sovStatusText; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true }
            }
        }
    }
}

