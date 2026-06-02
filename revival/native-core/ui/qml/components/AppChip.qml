import QtQuick
import OpenMacro.UI

Rectangle {
    id: root
    property alias text: label.text
    property color chipColor: Tokens.surfaceRaised
    property color textColor: Tokens.textMuted

    radius: 10
    color: chipColor
    implicitHeight: 24
    implicitWidth: label.implicitWidth + 14

    Text {
        id: label
        anchors.centerIn: parent
        color: root.textColor
        font.pixelSize: 11
        font.family: "Cascadia Mono"
    }
}
