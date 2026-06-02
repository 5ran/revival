import QtQuick
import QtQuick.Layouts

Item {
    id: root
    property string title: "Title"
    property string subtitle: ""
    implicitHeight: subtitle.length > 0 ? 56 : 40

    ColumnLayout {
        anchors.fill: parent
        spacing: 2
        Text { text: root.title; color: "#F1F5FB"; font.pixelSize: 28; font.bold: true }
        Text { visible: root.subtitle.length > 0; text: root.subtitle; color: "#8C99AD"; font.pixelSize: 13 }
    }
}

