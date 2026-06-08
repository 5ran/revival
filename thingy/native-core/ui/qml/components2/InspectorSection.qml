import QtQuick
import QtQuick.Layouts

GlassPanel {
    id: root
    property string title: "Section"
    property string value: "-"
    implicitHeight: 66
    cornerRadius: 14
    padding: 10

    ColumnLayout {
        anchors.fill: parent
        spacing: 4
        Text { text: root.title; color: "#88A1BE"; font.pixelSize: 11 }
        Text { text: root.value; color: "#E6EEFA"; font.pixelSize: 14; font.bold: true; wrapMode: Text.WordWrap }
    }
}

