import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import OpenMacro.UI

Item {
    implicitHeight: 520

    ColumnLayout {
        anchors.fill: parent
        spacing: 12

        RowLayout {
            spacing: 12

            ColumnLayout {
                Layout.preferredWidth: 320
                Layout.fillHeight: true
                spacing: 12

                AppCard {
                    Layout.fillWidth: true
                    Column {
                        spacing: 12

                        Row {
                            spacing: 10
                            AppToggle {
                                checked: macroController.autoAppraiseEnabled
                                onToggled: macroController.autoAppraiseEnabled = checked
                            }
                            Text { text: "Auto Appraise"; color: "#F0F0F0"; font.pixelSize: 13 }
                        }

                        AppSegmentedControl {
                            width: parent.width
                            options: ["Gamepass", "Normal"]
                            selectedIndex: macroController.appraiseMode === "Normal" ? 1 : 0
                            onOptionSelected: macroController.appraiseMode = options[index]
                        }

                        Item {
                            width: parent.width
                            height: 36

                            RowLayout {
                                anchors.fill: parent
                                visible: macroController.appraiseMode === "Gamepass"
                                Text { text: "Slow"; color: "#666666"; font.pixelSize: 11 }
                                AppSlider {
                                    Layout.fillWidth: true
                                    value: macroController.gamepassSpeed
                                    onMoved: macroController.gamepassSpeed = value
                                }
                                Text { text: "Fast"; color: "#666666"; font.pixelSize: 11 }
                            }

                            AppButton {
                                visible: macroController.appraiseMode === "Normal"
                                anchors.fill: parent
                                variant: "secondary"
                                text: "Use Cursor Position"
                                onClicked: macroController.useCursorPosition()
                            }
                        }
                    }
                }

                AppCard {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    Column {
                        spacing: 10
                        Text { text: "Required Traits"; color: "#666666"; font.pixelSize: 12; font.bold: true }

                        GridLayout {
                            columns: 3
                            columnSpacing: 10
                            rowSpacing: 8

                            CheckBox { text: "Shiny"; checked: macroController.requireShiny; onToggled: macroController.requireShiny = checked }
                            CheckBox { text: "Sparkling"; checked: macroController.requireSparkling; onToggled: macroController.requireSparkling = checked }
                            CheckBox { text: "Tiny"; checked: macroController.requireTiny; onToggled: macroController.requireTiny = checked }
                            CheckBox { text: "Small"; checked: macroController.requireSmall; onToggled: macroController.requireSmall = checked }
                            CheckBox { text: "Big"; checked: macroController.requireBig; onToggled: macroController.requireBig = checked }
                            CheckBox { text: "Giant"; checked: macroController.requireGiant; onToggled: macroController.requireGiant = checked }
                        }
                    }
                }
            }

            AppCard {
                Layout.fillWidth: true
                Layout.fillHeight: true

                ColumnLayout {
                    anchors.fill: parent
                    spacing: 10

                    Text { text: "Base Mutations"; color: "#666666"; font.pixelSize: 12; font.bold: true }

                    AppInput {
                        Layout.fillWidth: true
                        placeholderText: "Search mutations..."
                        text: macroController.mutationSearchText
                        onTextChanged: macroController.mutationSearchText = text
                    }

                    ScrollView {
                        Layout.fillWidth: true
                        Layout.fillHeight: true

                        Column {
                            width: parent.width
                            spacing: 6

                            Repeater {
                                model: macroController.mutationOptions
                                delegate: Item {
                                    width: parent.width
                                    height: 36
                                    property bool localSelected: Boolean(modelData.selected)

                                    Rectangle {
                                        anchors.fill: parent
                                        radius: 6
                                        border.width: 1
                                        border.color: localSelected ? "#2A2A2A" : "transparent"
                                        color: localSelected ? "#1A1A1A" : "transparent"
                                    }

                                    Row {
                                        anchors.fill: parent
                                        anchors.leftMargin: 10
                                        anchors.rightMargin: 10
                                        spacing: 8

                                        CheckBox {
                                            checked: parent.parent.localSelected
                                            onToggled: parent.parent.localSelected = checked
                                        }

                                        Text {
                                            anchors.verticalCenter: parent.verticalCenter
                                            text: modelData.name
                                            color: "#F0F0F0"
                                            font.pixelSize: 13
                                        }
                                    }

                                    MouseArea {
                                        anchors.fill: parent
                                        onClicked: parent.localSelected = !parent.localSelected
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

