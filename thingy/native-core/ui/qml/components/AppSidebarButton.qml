import QtQuick
import QtQuick.Controls
import OpenMacro.UI

Button {
    id: root
    property bool selected: false
    implicitHeight: 40
    leftPadding: 12
    rightPadding: 12
    font.pixelSize: 13
    font.weight: Font.Medium

    background: Rectangle {
        radius: 8
        color: root.selected ? Tokens.surfaceHover : (root.hovered ? Tokens.surfaceRaised : "transparent")
        Behavior on color { ColorAnimation { duration: 130 } }
    }

    contentItem: Text {
        text: root.text
        color: root.selected ? Tokens.text : Tokens.textMuted
        verticalAlignment: Text.AlignVCenter
        horizontalAlignment: Text.AlignLeft
        font: root.font
        elide: Text.ElideRight
    }
}
