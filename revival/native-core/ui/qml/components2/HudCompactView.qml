import QtQuick
import QtQuick.Layouts

GlassPanel {
    radius: 16
    padding: 14

    ColumnLayout {
        anchors.fill: parent
        spacing: 8

        Text { text: "Compact HUD"; color: "#E6EEFA"; font.pixelSize: 20; font.bold: true }
        RowLayout {
            StatusChip { label: macroController.runStateText; state: macroController.macroRunning ? "running" : "idle" }
            StatusChip { label: macroController.compactPhase; state: "connected" }
            StatusChip { label: macroController.runTimeText; state: "waiting" }
        }
        Text { text: macroController.compactStatusMessage; color: "#AEC2DC"; font.pixelSize: 13 }
        Rectangle {
            Layout.fillWidth: true
            Layout.preferredHeight: 18
            radius: 9
            color: "#0C1118"
            border.width: 1
            border.color: "#36465D"
            Rectangle {
                anchors.verticalCenter: parent.verticalCenter
                x: macroController.playerbarLeft * parent.width / 300
                width: macroController.playerbarWidth * parent.width / 300
                height: 8
                radius: 4
                color: "#7DD3FC"
            }
            Rectangle {
                anchors.verticalCenter: parent.verticalCenter
                x: macroController.fishMarkerLeft * parent.width / 300
                width: 10
                height: 10
                radius: 5
                color: "#F59E0B"
            }
        }
    }
}

