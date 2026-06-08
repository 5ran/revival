import QtQuick
import QtQuick.Controls

Button {
    id: root
    property bool active: false
    property string glyph: ""
    implicitWidth: 56
    implicitHeight: 56

    background: Rectangle {
        radius: 16
        gradient: Gradient {
            GradientStop { position: 0.0; color: root.active ? "#324D73" : (root.hovered ? "#1E2D42" : "#152133") }
            GradientStop { position: 1.0; color: root.active ? "#2A3F5B" : (root.hovered ? "#1A2538" : "#121D2D") }
        }
        border.width: 1
        border.color: root.active ? "#6484B0" : "#314863"
    }

    contentItem: Text {
        anchors.centerIn: parent
        text: root.glyph.length > 0 ? root.glyph : root.text.charAt(0).toUpperCase()
        color: "#E5EEFB"
        font.pixelSize: 15
        font.bold: true
    }

    ToolTip.visible: hovered
    ToolTip.text: root.text
}

