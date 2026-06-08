import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import OpenMacro.UI

GlassPanel {
    id: root
    property string moduleName: "Module"
    property string moduleHint: "Runtime automation unit"
    property string statusText: "Idle"
    property bool enabledState: false
    property string metricAKey: "Signal"
    property string metricAValue: "--"
    property string metricBKey: "Success"
    property string metricBValue: "--"
    signal toggled(bool value)
    signal openDetails()

    implicitWidth: 280
    implicitHeight: 180
    cornerRadius: 16
    padding: 12

    ColumnLayout {
        anchors.fill: parent
        spacing: 12

        RowLayout {
            Layout.fillWidth: true
            Text {
                text: root.moduleName
                color: "#E3ECFA"
                font.pixelSize: 18
                font.bold: true
                Layout.fillWidth: true
                elide: Text.ElideRight
            }
            AppToggle { checked: root.enabledState; onToggled: root.toggled(checked) }
        }

        Rectangle {
            Layout.fillWidth: true
            Layout.preferredHeight: 78
            radius: 12
            gradient: Gradient {
                GradientStop { position: 0.0; color: "#0F1723" }
                GradientStop { position: 1.0; color: "#101A2A" }
            }
            border.width: 1
            border.color: "#2A3F59"

            ColumnLayout {
                anchors.fill: parent
                anchors.margins: 10
                spacing: 7
                Text { text: root.moduleHint; color: "#9BB1CC"; font.pixelSize: 12; elide: Text.ElideRight }
                RowLayout {
                    Layout.fillWidth: true
                    Text { text: root.metricAKey + ":"; color: "#7E96B3"; font.pixelSize: 11 }
                    Text { text: root.metricAValue; color: "#E2EBF9"; font.pixelSize: 12; font.bold: true }
                    Item { Layout.fillWidth: true }
                    Text { text: root.metricBKey + ":"; color: "#7E96B3"; font.pixelSize: 11 }
                    Text { text: root.metricBValue; color: "#E2EBF9"; font.pixelSize: 12; font.bold: true }
                }
            }
        }

        Item { Layout.fillHeight: true; Layout.minimumHeight: 8 }

        RowLayout {
            Layout.fillWidth: true
            StatusChip { label: root.statusText; state: root.enabledState ? "running" : "idle" }
            Item { Layout.fillWidth: true }
            AppButton { text: "Details"; variant: "ghost"; onClicked: root.openDetails() }
        }
    }
}

