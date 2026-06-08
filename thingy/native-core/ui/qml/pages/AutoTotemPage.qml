import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    implicitHeight: 290

    ColumnLayout {
        anchors.fill: parent
        spacing: 12

        AppCard {
            Layout.fillWidth: true

            Column {
                spacing: 10

                Text { text: "Totem"; color: "#666666"; font.pixelSize: 12 }
                AppSelect {
                    width: 230
                    model: macroController.totemOptions
                    currentIndex: macroController.totemOptions.indexOf(macroController.selectedTotem)
                    onActivated: macroController.selectedTotem = currentText
                }

                Text { text: "Shiny / Sparkling / Mutation"; color: "#666666"; font.pixelSize: 12 }
                RowLayout {
                    spacing: 10
                    AppCard {
                        Layout.fillWidth: true
                        Row {
                            spacing: 8
                            AppToggle { checked: macroController.useShinyTotem; onToggled: macroController.useShinyTotem = checked }
                            Text { text: "Use Shiny Totem"; color: "#F0F0F0"; font.pixelSize: 13 }
                        }
                    }
                    AppCard {
                        Layout.fillWidth: true
                        Row {
                            spacing: 8
                            AppToggle { checked: macroController.useSparklingTotem; onToggled: macroController.useSparklingTotem = checked }
                            Text { text: "Use Sparkling Totem"; color: "#F0F0F0"; font.pixelSize: 13 }
                        }
                    }
                    AppCard {
                        Layout.fillWidth: true
                        Row {
                            spacing: 8
                            AppToggle { checked: macroController.useMutationTotem; onToggled: macroController.useMutationTotem = checked }
                            Text { text: "Use Mutation Totem"; color: "#F0F0F0"; font.pixelSize: 13 }
                        }
                    }
                }

                RowLayout {
                    spacing: 10
                    Text { text: "Cycle Lock"; color: "#666666"; font.pixelSize: 12 }
                    Text {
                        visible: !macroController.timePreferenceEditable
                        text: "Locked by Totem"
                        color: "#666666"
                        font.pixelSize: 11
                    }
                }

                RowLayout {
                    spacing: 10
                    AppCard {
                        Layout.fillWidth: true
                        Row {
                            spacing: 8
                            AppToggle { enabled: macroController.timePreferenceEditable; checked: macroController.stayDay; onToggled: macroController.stayDay = checked }
                            Text { text: "Stay Day"; color: "#F0F0F0"; font.pixelSize: 13 }
                        }
                    }
                    AppCard {
                        Layout.fillWidth: true
                        Row {
                            spacing: 8
                            AppToggle { enabled: macroController.timePreferenceEditable; checked: macroController.stayNight; onToggled: macroController.stayNight = checked }
                            Text { text: "Stay Night"; color: "#F0F0F0"; font.pixelSize: 13 }
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
                Text { text: macroController.totemStatusText; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true; wrapMode: Text.WordWrap }
            }
        }
    }
}

