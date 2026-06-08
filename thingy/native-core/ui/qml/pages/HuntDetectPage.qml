import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import OpenMacro.UI

Item {
    implicitHeight: 430

    RowLayout {
        anchors.fill: parent
        spacing: 12

        AppCard {
            Layout.fillWidth: true
            Layout.fillHeight: true

            ColumnLayout {
                anchors.fill: parent
                spacing: 8

                Text { text: "Discord Webhook"; color: "#666666"; font.pixelSize: 12 }
                AppInput {
                    Layout.fillWidth: true
                    placeholderText: "https://discord.com/api/webhooks/..."
                    text: macroController.discordWebhook
                    onTextChanged: macroController.discordWebhook = text
                }

                RowLayout {
                    Layout.fillWidth: true
                    spacing: 8
                    AppInput {
                        Layout.fillWidth: true
                        placeholderText: "New hunt name"
                        text: macroController.newHuntTarget
                        onTextChanged: macroController.newHuntTarget = text
                    }
                    AppButton {
                        text: "Add"
                        implicitWidth: 96
                        onClicked: macroController.addHuntTarget()
                    }
                }

                RowLayout {
                    Layout.fillWidth: true
                    spacing: 8
                    AppInput {
                        Layout.fillWidth: true
                        placeholderText: "Search targets"
                        text: macroController.huntSearchText
                        onTextChanged: macroController.huntSearchText = text
                    }
                    AppButton {
                        text: "Unselect all"
                        variant: "secondary"
                        implicitWidth: 96
                        onClicked: macroController.unselectAllHuntTargets()
                    }
                }

                ScrollView {
                    Layout.fillWidth: true
                    Layout.fillHeight: true

                    GridLayout {
                        width: parent.width
                        columns: 3
                        columnSpacing: 8
                        rowSpacing: 8

                        Repeater {
                            model: macroController.availableHuntTargets
                            delegate: AppButton {
                                required property var modelData
                                Layout.fillWidth: true
                                implicitHeight: 34
                                variant: modelData.selected ? "primary" : "secondary"
                                text: modelData.name
                                visible: macroController.huntSearchText.length === 0 || modelData.name.toLowerCase().indexOf(macroController.huntSearchText.toLowerCase()) !== -1
                                onClicked: macroController.toggleHuntTarget(modelData.name)
                            }
                        }
                    }
                }
            }
        }

        AppCard {
            Layout.preferredWidth: 240
            Layout.fillHeight: true

            ColumnLayout {
                anchors.fill: parent
                spacing: 10

                Text {
                    text: "Selected Targets"
                    color: "#F0F0F0"
                    font.pixelSize: 14
                    font.bold: true
                }

                Text {
                    visible: macroController.selectedHuntTargets.length === 0
                    text: "None Selected"
                    color: "#666666"
                    font.pixelSize: 12
                }

                ScrollView {
                    visible: macroController.selectedHuntTargets.length > 0
                    Layout.fillWidth: true
                    Layout.fillHeight: true

                    Column {
                        width: parent.width
                        spacing: 6

                        Repeater {
                            model: macroController.selectedHuntTargets
                            delegate: Rectangle {
                                required property string modelData
                                width: parent.width
                                height: 34
                                color: "#161616"
                                border.color: "#2A2A2A"
                                border.width: 1
                                radius: 6

                                Row {
                                    anchors.fill: parent
                                    anchors.leftMargin: 10
                                    anchors.rightMargin: 10
                                    spacing: 8

                                    Text {
                                        anchors.verticalCenter: parent.verticalCenter
                                        text: modelData
                                        color: "#F0F0F0"
                                        font.pixelSize: 12
                                    }

                                    Item { width: 1; height: 1 }

                                    AppButton {
                                        anchors.verticalCenter: parent.verticalCenter
                                        variant: "ghost"
                                        implicitWidth: 24
                                        implicitHeight: 24
                                        text: "?"
                                        onClicked: macroController.removeSelectedHuntTarget(modelData)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

