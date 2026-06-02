import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    ColumnLayout {
        anchors.fill: parent
        spacing: 16

        AppPageHeader { title: "Hunt Detect"; subtitle: "Target list and webhook notifications"; Layout.fillWidth: true }

        RowLayout {
            Layout.fillWidth: true
            spacing: 16

            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 120
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    Text { text: "Discord Webhook"; color: "#8F9FB3"; font.pixelSize: 13 }
                    AppInput { Layout.fillWidth: true; placeholderText: "https://discord.com/api/webhooks/..."; text: macroController.discordWebhook; onTextChanged: macroController.discordWebhook = text }
                }
            }

            GlassPanel {
                Layout.fillWidth: true
                Layout.preferredHeight: 120
                padding: 20
                ColumnLayout {
                    anchors.fill: parent
                    Text { text: "Search Targets"; color: "#8F9FB3"; font.pixelSize: 13 }
                    AppInput { Layout.fillWidth: true; placeholderText: "Search hunt names"; text: macroController.huntSearchText; onTextChanged: macroController.huntSearchText = text }
                }
            }
        }

        GlassPanel {
            Layout.fillWidth: true
            Layout.fillHeight: true
            padding: 20

            ColumnLayout {
                anchors.fill: parent
                spacing: 12

                RowLayout {
                    Layout.fillWidth: true
                    AppInput { Layout.fillWidth: true; placeholderText: "New hunt target"; text: macroController.newHuntTarget; onTextChanged: macroController.newHuntTarget = text }
                    AppButton { text: "Add"; onClicked: macroController.addHuntTarget() }
                    AppButton { text: "Unselect all"; variant: "secondary"; onClicked: macroController.unselectAllHuntTargets() }
                }

                Flickable {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    contentWidth: width
                    contentHeight: targetWrap.implicitHeight
                    clip: true

                    Flow {
                        id: targetWrap
                        width: parent.width
                        spacing: 10

                        Repeater {
                            model: macroController.availableHuntTargets
                            delegate: AppButton {
                                text: modelData.name + (modelData.selected ? " ?" : "")
                                variant: modelData.selected ? "primary" : "secondary"
                                onClicked: macroController.toggleHuntTarget(modelData.name)
                            }
                        }
                    }
                }

                Text { text: "Selected"; color: "#8F9FB3"; font.pixelSize: 13 }
                Flow {
                    Layout.fillWidth: true
                    spacing: 8
                    Repeater {
                        model: macroController.selectedHuntTargets
                        delegate: Rectangle {
                            height: 30
                            radius: 15
                            color: "#24374E"
                            border.color: "#2E4B6C"
                            border.width: 1
                            width: Math.max(90, chipLabel.paintedWidth + 28)
                            Text { id: chipLabel; anchors.centerIn: parent; text: modelData; color: "#E7EFFB"; font.pixelSize: 12 }
                            MouseArea { anchors.fill: parent; onClicked: macroController.removeSelectedHuntTarget(modelData) }
                        }
                    }
                }
            }
        }
    }
}


