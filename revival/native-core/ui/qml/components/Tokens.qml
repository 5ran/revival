pragma Singleton
import QtQuick

QtObject {
    readonly property color bg: "#0c1117"
    readonly property color surface: "#121a24"
    readonly property color surfaceRaised: "#162131"
    readonly property color surfaceHover: "#1c2a3f"
    readonly property color accent: "#8ab4a8"
    readonly property color accentHover: "#9ac6b9"
    readonly property color accentPressed: "#77a397"
    readonly property color text: "#e9eef5"
    readonly property color textMuted: "#94a3b8"
    readonly property color success: "#33a06f"
    readonly property color warning: "#b9924a"
    readonly property color danger: "#c55f68"

    readonly property int radiusSm: 8
    readonly property int radiusMd: 12
    readonly property int radiusLg: 18

    readonly property int space1: 8
    readonly property int space2: 16
    readonly property int space3: 24

    readonly property int textSm: 12
    readonly property int textMd: 14
    readonly property int textLg: 18
    readonly property int textXl: 24
}
