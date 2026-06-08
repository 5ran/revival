import QtQuick
import QtQuick.Layouts

Item {
    Rectangle {
        anchors.fill: parent
        color: "#0E0E0E"
    }

    ColumnLayout {
        anchors.centerIn: parent
        spacing: 10

        Text {
            text: "OpenMacro"
            color: "#F0F0F0"
            font.pixelSize: 28
            font.bold: true
            Layout.alignment: Qt.AlignHCenter
        }

        Text {
            text: "Restoring session"
            color: "#666666"
            font.pixelSize: 13
            Layout.alignment: Qt.AlignHCenter
        }
    }
}

