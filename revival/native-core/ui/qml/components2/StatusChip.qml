import QtQuick
import OpenMacro.UI

Rectangle {
    property string label: "Idle"
    property string state: "idle"
    implicitWidth: 96
    implicitHeight: 30
    radius: 15
    color: state === "running" ? "#2d8a63" : state === "error" ? "#b65a63" : state === "waiting" ? "#b1843f" : "#313e52"

    Behavior on color { ColorAnimation { duration: 140 } }

    Text {
        anchors.centerIn: parent
        text: parent.label
        color: "#f3f7fd"
        font.pixelSize: 12
        font.bold: true
    }
}


