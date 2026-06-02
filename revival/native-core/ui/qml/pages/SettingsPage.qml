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
            text: "Settings"
            color: "#F0F0F0"
            font.pixelSize: 24
            font.bold: true
        }

        AppCard {
            Layout.fillWidth: true

            Column {
                spacing: 12

                Text { text: "Theme"; color: "#F0F0F0"; font.pixelSize: 13; font.bold: true }

                Row {
                    spacing: 8
                    Repeater {
                        model: ["Dark", "Light", "Slate", "Pink", "Custom"]
                        delegate: AppButton {
                            required property string modelData
                            variant: settingsController.selectedTheme === modelData ? "primary" : "secondary"
                            text: modelData
                            implicitWidth: 110
                            onClicked: settingsController.selectTheme(modelData)
                        }
                    }
                }

                Row {
                    spacing: 10
                    AppToggle {
                        checked: appState.topMost
                        onToggled: appState.topMost = checked
                    }
                    Text {
                        text: "Topmost window"
                        color: "#F0F0F0"
                        font.pixelSize: 13
                    }
                }
            }
        }

        AppCard {
            visible: settingsController.selectedTheme === "Custom"
            Layout.fillWidth: true

            GridLayout {
                columns: 3
                columnSpacing: 12
                rowSpacing: 8

                Text { text: "Background"; color: "#666666"; font.pixelSize: 12 }
                Rectangle { width: 20; height: 20; radius: 4; color: settingsController.backgroundColor; border.color: "#2A2A2A"; border.width: 1 }
                AppInput { text: settingsController.backgroundColor; onTextChanged: settingsController.backgroundColor = text }

                Text { text: "Surface"; color: "#666666"; font.pixelSize: 12 }
                Rectangle { width: 20; height: 20; radius: 4; color: settingsController.surfaceColor; border.color: "#2A2A2A"; border.width: 1 }
                AppInput { text: settingsController.surfaceColor; onTextChanged: settingsController.surfaceColor = text }

                Text { text: "Border"; color: "#666666"; font.pixelSize: 12 }
                Rectangle { width: 20; height: 20; radius: 4; color: settingsController.borderColor; border.color: "#2A2A2A"; border.width: 1 }
                AppInput { text: settingsController.borderColor; onTextChanged: settingsController.borderColor = text }

                Text { text: "Accent"; color: "#666666"; font.pixelSize: 12 }
                Rectangle { width: 20; height: 20; radius: 4; color: settingsController.accentColor; border.color: "#2A2A2A"; border.width: 1 }
                AppInput { text: settingsController.accentColor; onTextChanged: settingsController.accentColor = text }

                Text { text: "Text"; color: "#666666"; font.pixelSize: 12 }
                Rectangle { width: 20; height: 20; radius: 4; color: settingsController.textColor; border.color: "#2A2A2A"; border.width: 1 }
                AppInput { text: settingsController.textColor; onTextChanged: settingsController.textColor = text }
            }
        }
    }
}

