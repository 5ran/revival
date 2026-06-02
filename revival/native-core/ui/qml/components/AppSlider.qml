import QtQuick
import QtQuick.Controls
import OpenMacro.UI

Slider {
    id: root
    from: 0
    to: 1

    background: Rectangle {
        x: root.leftPadding
        y: root.topPadding + root.availableHeight / 2 - height / 2
        width: root.availableWidth
        height: 6
        radius: 3
        color: "#27313f"

        Rectangle {
            width: root.visualPosition * parent.width
            height: parent.height
            radius: 3
            color: "#7ca4d8"
            Behavior on width { NumberAnimation { duration: 140 } }
        }
    }

    handle: Rectangle {
        x: root.leftPadding + root.visualPosition * (root.availableWidth - width)
        y: root.topPadding + root.availableHeight / 2 - height / 2
        implicitWidth: 16
        implicitHeight: 16
        radius: 8
        color: root.pressed ? "#b8cbe5" : "#d7e3f3"
        scale: root.pressed ? 0.94 : 1.0
        Behavior on scale { NumberAnimation { duration: 120 } }
    }
}


