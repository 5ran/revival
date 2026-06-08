import QtQuick
import QtQuick.Controls

Rectangle {
    id: root
    property var options: []
    property int selectedIndex: 0
    signal optionSelected(int index)

    cornerRadius: 6
    color: "#161616"
    border.width: 1
    border.color: "#2A2A2A"
    implicitHeight: 38

    Row {
        anchors.fill: parent
        anchors.margins: 2
        spacing: 2

        Repeater {
            model: root.options

            delegate: AppButton {
                required property int index
                required property string modelData

                variant: index === root.selectedIndex ? "primary" : "ghost"
                cornerRadius: 4
                text: modelData
                implicitHeight: 34
                implicitWidth: Math.max(110, contentItem.implicitWidth + 24)
                onClicked: {
                    root.selectedIndex = index
                    root.optionSelected(index)
                }
            }
        }
    }
}

