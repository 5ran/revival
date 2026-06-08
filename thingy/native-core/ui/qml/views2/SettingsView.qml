import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    ColumnLayout {
        anchors.fill: parent
        spacing: 16

        AppPageHeader { title: "Settings"; subtitle: "Theme, colors, and account"; Layout.fillWidth: true }

        RowLayout {
            Layout.fillWidth: true
            spacing: 16

            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 220
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    spacing: 10
                    Text { text: "Theme"; color: "#F0F5FC"; font.pixelSize: 20; font.bold: true }
                    AppSelect { Layout.fillWidth: true; model: ["Dark", "Light", "Slate", "Pink", "Custom"] }
                    RowLayout {
                        AppToggle { checked: appState.topMost; onToggled: appState.topMost = checked }
                        Text { text: "Always on top"; color: "#C9D4E4" }
                    }
                    RowLayout {
                        AppToggle { checked: appState.compactMode; onToggled: appState.compactMode = checked }
                        Text { text: "Compact mode while running"; color: "#C9D4E4" }
                    }
                }
            }

            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 220
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    spacing: 10
                    Text { text: "Color Settings"; color: "#F0F5FC"; font.pixelSize: 20; font.bold: true }
                    AppInput { Layout.fillWidth: true; placeholderText: "Background #" }
                    AppInput { Layout.fillWidth: true; placeholderText: "Surface #" }
                    AppInput { Layout.fillWidth: true; placeholderText: "Accent #" }
                    AppInput { Layout.fillWidth: true; placeholderText: "Text #" }
                }
            }
        }

        GlassPanel {
            Layout.fillWidth: true
            Layout.preferredHeight: 180
            padding: 20
            ColumnLayout {
                anchors.fill: parent
                spacing: 10
                Text { text: "Account"; color: "#F0F5FC"; font.pixelSize: 20; font.bold: true }
                Text { text: "Authentication bridge is stubbed until runtime service wiring is connected."; color: "#8F9FB3"; wrapMode: Text.WordWrap }
                AppButton { text: "Open Login"; variant: "secondary"; onClicked: appState.rootRoute = "Login" }
            }
        }

        Item { Layout.fillHeight: true }
    }
}


