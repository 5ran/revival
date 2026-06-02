import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import OpenMacro.UI

Button {
    id: root
    property bool active: false
    property string iconSource: ""

    implicitHeight: 44
    implicitWidth: 196
    hoverEnabled: true

    background: Rectangle {
        radius: Tokens.radiusMd
        color: root.active ? "#22364e" : (root.hovered ? Tokens.surfaceHover : "transparent")
        Behavior on color { ColorAnimation { duration: 140 } }
    }

    contentItem: RowLayout {
        spacing: 10
        Image {
            source: root.iconSource
            width: 18
            height: 18
            fillMode: Image.PreserveAspectFit
            opacity: root.active ? 1.0 : 0.85
            Behavior on opacity { NumberAnimation { duration: 130 } }
        }
        Text {
            text: root.text
            color: root.active ? Tokens.text : Tokens.textMuted
            font.pixelSize: Tokens.textMd
            font.bold: root.active
            elide: Text.ElideRight
        }
        Item { Layout.fillWidth: true }
    }

    scale: root.hovered ? 1.01 : 1.0
    Behavior on scale { NumberAnimation { duration: 130 } }
}
