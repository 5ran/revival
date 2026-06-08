import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    property var modules: ["Auto Angler", "Enchant", "Appraise", "Treasure Appraise"]

    RowLayout {
        anchors.fill: parent
        spacing: 16

        ColumnLayout {
            Layout.preferredWidth: 260
            Layout.fillHeight: true
            spacing: 8
            AppPageHeader { title: "Automation"; subtitle: "Module controls"; Layout.fillWidth: true }

            Repeater {
                model: modules
                delegate: AppButton {
                    Layout.fillWidth: true
                    text: modelData
                    variant: macroController.selectedAutomation === modelData ? "primary" : "secondary"
                    onClicked: macroController.selectedAutomation = modelData
                }
            }
            Item { Layout.fillHeight: true }
        }

        GlassPanel {
            Layout.fillWidth: true
            Layout.fillHeight: true
            padding: 20

            ColumnLayout {
                anchors.fill: parent
                spacing: 14
                Text { text: macroController.selectedAutomation; color: "#F0F5FC"; font.pixelSize: 26; font.bold: true }

                Loader {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    sourceComponent: {
                        if (macroController.selectedAutomation === "Auto Angler") return angler
                        if (macroController.selectedAutomation === "Enchant") return enchant
                        if (macroController.selectedAutomation === "Appraise") return appraise
                        return treasure
                    }
                }
            }
        }
    }

    Component {
        id: angler
        ColumnLayout {
            spacing: 12
            RowLayout {
                AppToggle { checked: macroController.autoAnglerEnabled; onToggled: macroController.autoAnglerEnabled = checked }
                Text { text: "Enable Auto Angler"; color: "#D7E2F2" }
            }
            AppButton { text: "Use Cursor"; variant: "secondary"; onClicked: macroController.useCursorPosition() }
            Text { text: "Current Fish: " + macroController.currentFishText; color: "#A8B8CE" }
            Text { text: "Status: " + macroController.autoAnglerStatusText; color: "#A8B8CE" }
        }
    }

    Component {
        id: enchant
        ColumnLayout {
            spacing: 12
            RowLayout {
                AppToggle { checked: macroController.autoEnchantEnabled; onToggled: macroController.autoEnchantEnabled = checked }
                Text { text: "Enable Auto Enchant"; color: "#D7E2F2" }
            }
            AppSelect { Layout.fillWidth: true; model: ["Gamepass", "Normal"]; currentIndex: macroController.enchantMode === "Normal" ? 1 : 0; onActivated: macroController.enchantMode = currentText }
            AppInput { Layout.fillWidth: true; placeholderText: "Search enchant"; text: macroController.targetSearchText; onTextChanged: macroController.targetSearchText = text }
            AppSelect { Layout.fillWidth: true; model: macroController.targetEnchants; currentIndex: macroController.targetEnchants.indexOf(macroController.selectedTargetEnchant); onActivated: macroController.selectedTargetEnchant = currentText }
            RowLayout {
                AppInput { Layout.fillWidth: true; placeholderText: "New enchant"; text: macroController.newEnchantText; onTextChanged: macroController.newEnchantText = text }
                AppButton { text: "Add"; onClicked: macroController.addEnchant() }
            }
        }
    }

    Component {
        id: appraise
        ColumnLayout {
            spacing: 12
            RowLayout {
                AppToggle { checked: macroController.autoAppraiseEnabled; onToggled: macroController.autoAppraiseEnabled = checked }
                Text { text: "Enable Auto Appraise"; color: "#D7E2F2" }
            }
            AppSelect { Layout.fillWidth: true; model: ["Gamepass", "Normal"]; currentIndex: macroController.appraiseMode === "Normal" ? 1 : 0; onActivated: macroController.appraiseMode = currentText }
            Text { text: "Gamepass Speed"; color: "#8F9FB3" }
            AppSlider { Layout.fillWidth: true; from: 0; to: 1; value: macroController.gamepassSpeed; onValueChanged: macroController.gamepassSpeed = value }
        }
    }

    Component {
        id: treasure
        ColumnLayout {
            spacing: 12
            RowLayout {
                AppToggle { checked: macroController.autoTreasureEnabled; onToggled: macroController.autoTreasureEnabled = checked }
                Text { text: "Enable Treasure Appraise"; color: "#D7E2F2" }
            }
            Text { text: "Click Delay (s)"; color: "#8F9FB3" }
            AppSlider { Layout.fillWidth: true; from: 0; to: 2; value: macroController.treasureClickDelaySeconds; onValueChanged: macroController.treasureClickDelaySeconds = value }
            Text { text: "Minimum Multi"; color: "#8F9FB3" }
            AppSlider { Layout.fillWidth: true; from: 1; to: 5; value: macroController.treasureMinimumMulti; onValueChanged: macroController.treasureMinimumMulti = value }
        }
    }
}


