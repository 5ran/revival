import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    id: root
    property string title: "Page"
    property string subtitle: ""
    implicitHeight: subtitle.length > 0 ? 64 : 40

    ColumnLayout {
        anchors.fill: parent
        spacing: 4
        Text {
            text: root.title
            color: Tokens.text
            font.pixelSize: Tokens.textXl
            font.bold: true
        }
        Text {
            visible: root.subtitle.length > 0
            text: root.subtitle
            color: Tokens.textMuted
            font.pixelSize: Tokens.textMd
        }
    }
}
