import QtQuick
import QtQuick.Controls
import OpenMacro.UI

ComboBox {
    id: root
    implicitHeight: 44
    font.pixelSize: Tokens.textMd
    hoverEnabled: true

    delegate: ItemDelegate {
        width: ListView.view.width
        hoverEnabled: true
        contentItem: Text {
            text: modelData
            color: Tokens.text
            verticalAlignment: Text.AlignVCenter
            leftPadding: 12
            font.pixelSize: Tokens.textMd
        }
        background: Rectangle {
            color: highlighted ? Tokens.surfaceHover : Tokens.surface
            Behavior on color { ColorAnimation { duration: 120 } }
        }
    }

    contentItem: Text {
        leftPadding: 14
        rightPadding: 34
        text: root.displayText
        color: Tokens.text
        verticalAlignment: Text.AlignVCenter
        font: root.font
        elide: Text.ElideRight
    }

    indicator: Canvas {
        x: root.width - width - 14
        y: (root.height - height) / 2
        width: 10
        height: 6
        contextType: "2d"
        onPaint: {
            context.reset()
            context.moveTo(0, 0)
            context.lineTo(width, 0)
            context.lineTo(width / 2, height)
            context.closePath()
            context.fillStyle = Tokens.textMuted
            context.fill()
        }
    }

    background: Rectangle {
        radius: Tokens.radiusMd
        color: root.pressed ? "#26364d" : (root.hovered ? Tokens.surfaceHover : Tokens.surfaceRaised)
        Behavior on color { ColorAnimation { duration: 140 } }
    }

    popup: Popup {
        y: root.height + 6
        width: root.width
        padding: 0
        contentItem: ListView {
            clip: true
            implicitHeight: Math.min(contentHeight, 260)
            model: root.popup.visible ? root.delegateModel : null
        }
        background: Rectangle {
            radius: Tokens.radiusMd
            color: Tokens.surface
        }
    }
}
