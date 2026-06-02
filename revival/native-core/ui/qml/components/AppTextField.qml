import QtQuick
import QtQuick.Controls
import OpenMacro.UI

TextField {
    id: root
    implicitHeight: 44
    color: Tokens.text
    placeholderTextColor: Tokens.textMuted
    selectionColor: Tokens.accent
    selectedTextColor: "#0f161f"
    font.pixelSize: Tokens.textMd

    background: Rectangle {
        radius: Tokens.radiusMd
        color: root.activeFocus ? "#23334a" : Tokens.surfaceRaised
        Behavior on color { ColorAnimation { duration: 140 } }
    }
}
