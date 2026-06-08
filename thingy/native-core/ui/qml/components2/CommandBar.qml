import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

AppCard {
    id: root
    property string runtimeText: "00:00:00"
    property string stateText: "Idle"
    property string hotkeyText: "F6"
    property string updateText: "Up to date"
    property bool running: false
    signal startStopClicked()
    signal minimizeClicked()
    signal closeClicked()

    padding: 14

    RowLayout {
        anchors.fill: parent
        spacing: 12

        Text { text: "OpenMacro Swift"; color: Tokens.text; font.pixelSize: 22; font.bold: true; Layout.preferredWidth: 240 }
        StatusChip { label: root.stateText; state: root.running ? "running" : "idle" }
        AppChip { text: "Runtime " + root.runtimeText }
        Text { text: "Hotkey " + root.hotkeyText; color: Tokens.textMuted; font.pixelSize: 13 }
        Text { text: root.updateText; color: Tokens.textMuted; font.pixelSize: 13 }

        Item { Layout.fillWidth: true }

        AppButton { text: root.running ? "Stop" : "Start"; onClicked: root.startStopClicked() }
        AppButton { text: "-"; variant: "ghost"; implicitWidth: 40; onClicked: root.minimizeClicked() }
        AppButton { text: "X"; variant: "ghost"; implicitWidth: 40; onClicked: root.closeClicked() }
    }
}
