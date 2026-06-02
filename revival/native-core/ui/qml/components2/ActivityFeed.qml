import QtQuick
import QtQuick.Controls
import OpenMacro.UI

Item {
    id: root
    property var entries: []

    ListView {
        anchors.fill: parent
        model: root.entries
        spacing: 6
        clip: true
        delegate: Text {
            width: ListView.view.width
            text: modelData
            color: "#95a2b7"
            font.pixelSize: 12
            elide: Text.ElideRight
        }
    }
}


