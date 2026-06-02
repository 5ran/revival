import QtQuick
import QtQuick.Layouts

Item {
    Rectangle {
        anchors.fill: parent
        color: "#0E0E0E"
    }

    ColumnLayout {
        anchors.centerIn: parent
        spacing: 12

        Rectangle {
            width: 180
            height: 120
            radius: 12
            color: "#161616"
            border.color: "#2A2A2A"
            border.width: 1
            Layout.alignment: Qt.AlignHCenter
        }

        Text {
            text: "Locked Out"
            color: "#F0F0F0"
            font.pixelSize: 24
            font.bold: true
            Layout.alignment: Qt.AlignHCenter
        }

        Text {
            text: "Your account currently does not have access."
            color: "#666666"
            font.pixelSize: 12
            Layout.alignment: Qt.AlignHCenter
        }
    }
}

