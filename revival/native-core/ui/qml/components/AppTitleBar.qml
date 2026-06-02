import QtQuick
import QtQuick.Controls
import OpenMacro.UI

Rectangle {
    id: root
    required property var window
    signal minimizeClicked()
    signal closeClicked()

    height: 34
    color: Tokens.surface

    MouseArea {
        anchors.fill: parent
        acceptedButtons: Qt.LeftButton
        onPressed: {
            if (root.window && root.window.startSystemMove) root.window.startSystemMove()
        }
    }

    Text {
        text: appState.appTitle
        color: Tokens.text
        font.pixelSize: 11
        anchors.verticalCenter: parent.verticalCenter
        anchors.left: parent.left
        anchors.leftMargin: 14
        z: 1
    }

    Row {
        spacing: 0
        anchors.right: parent.right
        anchors.verticalCenter: parent.verticalCenter
        z: 1

        AppButton { variant: "ghost"; cornerRadius: 0; implicitWidth: 34; implicitHeight: 34; text: "-"; onClicked: root.minimizeClicked() }
        AppButton { variant: "ghost"; cornerRadius: 0; implicitWidth: 34; implicitHeight: 34; text: "X"; onClicked: root.closeClicked() }
    }
}
