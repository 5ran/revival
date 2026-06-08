import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    Rectangle { anchors.fill: parent; color: "#0E0E0E" }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 34
        spacing: 20

        Text {
            text: "Dashboard"
            color: "#F0F0F0"
            font.pixelSize: 24
            font.bold: true
        }

        AppCard {
            Layout.preferredWidth: 430
            Column {
                spacing: 8
                Text { text: "Overview"; color: "#666666"; font.pixelSize: 12 }
                Text { text: "Dashboard modules are being expanded."; color: "#F0F0F0"; font.pixelSize: 18; font.bold: true }
            }
        }
    }
}

