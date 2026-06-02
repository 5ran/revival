import QtQuick
import QtQuick.Controls
import OpenMacro.UI

Button {
    id: root
    property string variant: "primary" // primary | secondary | ghost
    property int cornerRadius: Tokens.radiusMd

    implicitHeight: 44
    implicitWidth: 132
    font.pixelSize: Tokens.textMd
    hoverEnabled: true

    background: Rectangle {
        radius: root.cornerRadius
        color: {
            if (!root.enabled) return "#2a3446"
            if (root.variant === "ghost") return root.hovered ? Tokens.surfaceHover : "transparent"
            if (root.variant === "secondary") return root.down ? "#223246" : (root.hovered ? "#283a52" : Tokens.surfaceRaised)
            return root.down ? Tokens.accentPressed : (root.hovered ? Tokens.accentHover : Tokens.accent)
        }
        Behavior on color { ColorAnimation { duration: 140 } }
    }

    contentItem: Text {
        text: root.text
        color: root.variant === "primary" ? "#0f161f" : Tokens.text
        horizontalAlignment: Text.AlignHCenter
        verticalAlignment: Text.AlignVCenter
        font: root.font
        elide: Text.ElideRight
    }

    scale: root.down ? 0.985 : (root.hovered ? 1.01 : 1.0)
    Behavior on scale { NumberAnimation { duration: 130; easing.type: Easing.OutCubic } }
}
