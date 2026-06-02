import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

Button {
    id: root
    property bool active: false
    property string glyph: ""

    implicitWidth: 176
    implicitHeight: 44

    background: Rectangle {
        radius: 10
        color: root.active ? "#233247" : (root.hovered ? "#1A232F" : "transparent")
    }

    contentItem: RowLayout {
        spacing: 10
        Rectangle {
            width: 22
            height: 22
            radius: 6
            color: root.active ? "#3A587A" : "#26374A"
            Text {
                anchors.centerIn: parent
                text: root.glyph
                color: "#E8EDF5"
                font.pixelSize: 10
                font.bold: true
            }
        }

        Text {
            text: root.text
            color: root.active ? "#F2F6FC" : "#AEB8C8"
            font.pixelSize: 14
            font.bold: root.active
            elide: Text.ElideRight
        }
        Item { Layout.fillWidth: true }
    }
}

