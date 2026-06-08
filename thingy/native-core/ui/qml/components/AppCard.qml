import QtQuick
import OpenMacro.UI

Rectangle {
    id: root
    default property alias contentData: contentItem.data
    property int padding: Tokens.space2

    color: Tokens.surface
    radius: Tokens.radiusLg

    Item {
        id: contentItem
        anchors.fill: parent
        anchors.margins: root.padding
    }
}
