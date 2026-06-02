import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    implicitHeight: 340

    ColumnLayout {
        anchors.fill: parent
        spacing: 12

        AppCard {
            Layout.fillWidth: true
            Column {
                spacing: 12

                Row {
                    spacing: 10
                    AppToggle {
                        checked: macroController.autoEnchantEnabled
                        onToggled: macroController.autoEnchantEnabled = checked
                    }
                    Text { text: "Auto Enchant"; color: "#F0F0F0"; font.pixelSize: 13 }
                }

                AppSegmentedControl {
                    options: ["Gamepass", "Normal"]
                    selectedIndex: macroController.enchantMode === "Normal" ? 1 : 0
                    onOptionSelected: macroController.enchantMode = options[index]
                }

                RowLayout {
                    spacing: 10
                    ColumnLayout {
                        Layout.fillWidth: true
                        spacing: 6
                        Text { text: "Target Enchant"; color: "#666666"; font.pixelSize: 12 }
                        AppInput {
                            Layout.fillWidth: true
                            placeholderText: "Search"
                            text: macroController.targetSearchText
                            onTextChanged: macroController.targetSearchText = text
                        }
                        AppSelect {
                            Layout.fillWidth: true
                            model: macroController.targetEnchants
                            currentIndex: macroController.targetEnchants.indexOf(macroController.selectedTargetEnchant)
                            onActivated: macroController.selectedTargetEnchant = currentText
                        }
                    }

                    Column {
                        spacing: 6
                        AppInput {
                            width: 120
                            placeholderText: "New"
                            text: macroController.newEnchantText
                            onTextChanged: macroController.newEnchantText = text
                        }
                        AppButton {
                            width: 120
                            text: "Add"
                            onClicked: macroController.addEnchant()
                        }
                    }
                }
            }
        }

        RowLayout {
            spacing: 12
            AppCard {
                Layout.fillWidth: true
                Column {
                    spacing: 8
                    Text { text: "Rod"; color: "#666666"; font.pixelSize: 12 }
                    Text {
                        text: macroController.enchantRodText
                        color: "#F0F0F0"
                        font.pixelSize: 18
                        font.bold: true
                        wrapMode: Text.WordWrap
                    }
                }
            }
            AppCard {
                Layout.fillWidth: true
                Column {
                    spacing: 8
                    Text { text: "Current Enchant"; color: "#666666"; font.pixelSize: 12 }
                    Text {
                        text: macroController.currentEnchantText
                        color: "#F0F0F0"
                        font.pixelSize: 18
                        font.bold: true
                        wrapMode: Text.WordWrap
                    }
                }
            }
            AppCard {
                Layout.fillWidth: true
                Column {
                    spacing: 8
                    Text { text: "Status"; color: "#666666"; font.pixelSize: 12 }
                    Text {
                        text: macroController.enchantStatusText
                        color: "#F0F0F0"
                        font.pixelSize: 18
                        font.bold: true
                        wrapMode: Text.WordWrap
                    }
                }
            }
        }
    }
}

