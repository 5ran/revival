import QtQuick
import QtQuick.Controls
import OpenMacro.UI

Switch {
    id: root
    implicitHeight: 30
    hoverEnabled: true

    indicator: Rectangle {
        implicitWidth: 52
        implicitHeight: 30
        radius: 15
        color: root.checked ? Tokens.accent : "#344155"
        Behavior on color { ColorAnimation { duration: 140 } }

        Rectangle {
            width: 24
            height: 24
            radius: 12
            y: 3
            x: root.checked ? parent.width - width - 3 : 3
            color: "#f2f6fc"
            Behavior on x { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }
        }
    }
}
