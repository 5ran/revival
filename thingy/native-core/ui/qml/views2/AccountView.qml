import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    ColumnLayout {
        anchors.fill: parent
        spacing: 16
        AppPageHeader { title: "Account"; subtitle: "Profile and sign-in"; Layout.fillWidth: true }

        GlassPanel {
            Layout.fillWidth: true
            Layout.preferredHeight: 240
            padding: 20
            ColumnLayout {
                anchors.fill: parent
                spacing: 12
                Text { text: "Account Management"; color: "#F0F5FC"; font.pixelSize: 22; font.bold: true }
                Text { text: "Use login flow or bypass during local testing."; color: "#8F9FB3" }
                RowLayout {
                    spacing: 10
                    AppButton { text: "Go to Login"; onClicked: appState.rootRoute = "Login" }
                    AppButton { text: "Skip Sign In"; variant: "secondary"; onClicked: appState.rootRoute = "Shell" }
                }
            }
        }
        Item { Layout.fillHeight: true }
    }
}


