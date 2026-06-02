import QtQuick
import QtQuick.Controls
import OpenMacro.UI

Dialog {
    id: root
    modal: true
    standardButtons: Dialog.NoButton
    closePolicy: Popup.CloseOnEscape | Popup.CloseOnPressOutside

    Overlay.modal: Rectangle { color: "#70000000" }

    background: Rectangle {
        color: Tokens.surface
        radius: Tokens.radiusLg
    }

    enter: Transition {
        ParallelAnimation {
            NumberAnimation { property: "opacity"; from: 0; to: 1; duration: 160 }
            NumberAnimation { property: "scale"; from: 0.98; to: 1.0; duration: 160; easing.type: Easing.OutCubic }
        }
    }
    exit: Transition {
        NumberAnimation { property: "opacity"; to: 0; duration: 120 }
    }
}
