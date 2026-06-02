import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    Rectangle { anchors.fill: parent; color: "#0E0E0E" }

    Flickable {
        anchors.fill: parent
        contentWidth: width
        contentHeight: content.implicitHeight + 34
        clip: true

        ColumnLayout {
            id: content
            width: parent.width - 68
            x: 34
            y: 34
            spacing: 12

            Text {
                text: macroController.activeAutomationTitle
                color: "#F0F0F0"
                font.pixelSize: 24
                font.bold: true
            }

            Repeater {
                model: [
                    { label: "Auto Angler", key: "Auto Angler" },
                    { label: "Enchant", key: "Enchant" },
                    { label: "Appraise", key: "Appraise" },
                    { label: "Treasure Appraise", key: "Treasure Appraise" }
                ]

                delegate: AppCard {
                    Layout.fillWidth: true
                    RowLayout {
                        anchors.fill: parent
                        Text { Layout.fillWidth: true; text: modelData.label; color: "#F0F0F0"; font.pixelSize: 14; font.bold: true }
                        AppToggle {
                            checked: modelData.key === "Auto Angler" ? macroController.autoAnglerEnabled
                                      : modelData.key === "Enchant" ? macroController.enchantEnabled
                                      : modelData.key === "Appraise" ? macroController.appraiseEnabled
                                      : macroController.treasureAppraiseEnabled
                            onToggled: {
                                if (modelData.key === "Auto Angler") macroController.autoAnglerEnabled = checked
                                else if (modelData.key === "Enchant") macroController.enchantEnabled = checked
                                else if (modelData.key === "Appraise") macroController.appraiseEnabled = checked
                                else macroController.treasureAppraiseEnabled = checked
                            }
                        }
                        AppButton {
                            variant: "secondary"
                            text: "Open"
                            implicitWidth: 70
                            onClicked: macroController.selectedAutomation = modelData.key
                        }
                    }
                }
            }

            Loader {
                Layout.fillWidth: true
                sourceComponent: {
                    if (macroController.selectedAutomation === "Auto Angler") return autoAngler
                    if (macroController.selectedAutomation === "Enchant") return enchant
                    if (macroController.selectedAutomation === "Appraise") return appraise
                    if (macroController.selectedAutomation === "Treasure Appraise") return treasure
                    return autoAngler
                }
            }

            Component { id: autoAngler; AutoAnglerPage { Layout.fillWidth: true } }
            Component { id: enchant; EnchantPage { Layout.fillWidth: true } }
            Component { id: appraise; AppraisePage { Layout.fillWidth: true } }
            Component { id: treasure; TreasureAppraisePage { Layout.fillWidth: true } }
        }
    }
}

