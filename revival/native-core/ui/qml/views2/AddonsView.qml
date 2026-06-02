import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    property bool showAquarium: false
    property bool showTotem: false
    property bool showSov: false
    property bool showHunt: false

    ColumnLayout {
        anchors.fill: parent
        spacing: 16

        AppPageHeader { title: "Addons"; subtitle: "Fishing support modules"; Layout.fillWidth: true }

        Flickable {
            Layout.fillWidth: true
            Layout.fillHeight: true
            contentWidth: width
            contentHeight: addonColumn.implicitHeight
            clip: true

            ColumnLayout {
                id: addonColumn
                width: parent.width
                spacing: 12

                GlassPanel {
                    Layout.fillWidth: true
                    padding: 18
                    ColumnLayout {
                        anchors.fill: parent
                        spacing: 10
                        RowLayout {
                            Layout.fillWidth: true
                            Text { text: "Auto Aquarium"; color: "#F0F5FC"; font.pixelSize: 18; font.bold: true }
                            Text { text: "Cycle timing and sequence"; color: "#8F9FB3"; font.pixelSize: 13 }
                            Item { Layout.fillWidth: true }
                            AppToggle { checked: macroController.autoAquariumEnabled; onToggled: macroController.autoAquariumEnabled = checked }
                            AppButton { text: showAquarium ? "Hide" : "Expand"; variant: "ghost"; onClicked: showAquarium = !showAquarium }
                        }
                        ColumnLayout {
                            visible: showAquarium
                            spacing: 8
                            Text { text: "Cycle Delay (minutes)"; color: "#8F9FB3" }
                            AppSlider { Layout.fillWidth: true; from: 1; to: 60; value: macroController.aquariumCycleDelayMinutes; onValueChanged: macroController.aquariumCycleDelayMinutes = value }
                        }
                    }
                }

                GlassPanel {
                    Layout.fillWidth: true
                    padding: 18
                    ColumnLayout {
                        anchors.fill: parent
                        spacing: 10
                        RowLayout {
                            Layout.fillWidth: true
                            Text { text: "Auto Totem"; color: "#F0F5FC"; font.pixelSize: 18; font.bold: true }
                            Text { text: "Totem and cycle preferences"; color: "#8F9FB3"; font.pixelSize: 13 }
                            Item { Layout.fillWidth: true }
                            AppToggle { checked: macroController.autoTotemEnabled; onToggled: macroController.autoTotemEnabled = checked }
                            AppButton { text: showTotem ? "Hide" : "Expand"; variant: "ghost"; onClicked: showTotem = !showTotem }
                        }
                        ColumnLayout {
                            visible: showTotem
                            spacing: 8
                            AppSelect { Layout.fillWidth: true; model: macroController.totemOptions; currentIndex: macroController.totemOptions.indexOf(macroController.selectedTotem); onActivated: macroController.selectedTotem = currentText }
                            RowLayout {
                                AppToggle { checked: macroController.useShinyTotem; onToggled: macroController.useShinyTotem = checked }
                                Text { text: "Use Shiny Totem"; color: "#C9D4E4" }
                            }
                            RowLayout {
                                AppToggle { checked: macroController.useSparklingTotem; onToggled: macroController.useSparklingTotem = checked }
                                Text { text: "Use Sparkling Totem"; color: "#C9D4E4" }
                            }
                            RowLayout {
                                AppToggle { checked: macroController.useMutationTotem; onToggled: macroController.useMutationTotem = checked }
                                Text { text: "Use Mutation Totem"; color: "#C9D4E4" }
                            }
                        }
                    }
                }

                GlassPanel {
                    Layout.fillWidth: true
                    padding: 18
                    ColumnLayout {
                        anchors.fill: parent
                        spacing: 10
                        RowLayout {
                            Layout.fillWidth: true
                            Text { text: "Auto Sovereign Recharge"; color: "#F0F5FC"; font.pixelSize: 18; font.bold: true }
                            Text { text: "Battery threshold controls"; color: "#8F9FB3"; font.pixelSize: 13 }
                            Item { Layout.fillWidth: true }
                            AppToggle { checked: macroController.autoSovEnabled; onToggled: macroController.autoSovEnabled = checked }
                            AppButton { text: showSov ? "Hide" : "Expand"; variant: "ghost"; onClicked: showSov = !showSov }
                        }
                        ColumnLayout {
                            visible: showSov
                            spacing: 8
                            Text { text: "Min %"; color: "#8F9FB3" }
                            AppSlider { Layout.fillWidth: true; from: 0; to: 100; value: macroController.sovMinPercent; onValueChanged: macroController.sovMinPercent = value }
                            Text { text: "Max %"; color: "#8F9FB3" }
                            AppSlider { Layout.fillWidth: true; from: 0; to: 100; value: macroController.sovMaxPercent; onValueChanged: macroController.sovMaxPercent = value }
                        }
                    }
                }

                GlassPanel {
                    Layout.fillWidth: true
                    padding: 18
                    ColumnLayout {
                        anchors.fill: parent
                        spacing: 10
                        RowLayout {
                            Layout.fillWidth: true
                            Text { text: "Hunt Detect"; color: "#F0F5FC"; font.pixelSize: 18; font.bold: true }
                            Text { text: "Target monitor and alerts"; color: "#8F9FB3"; font.pixelSize: 13 }
                            Item { Layout.fillWidth: true }
                            AppToggle { checked: macroController.huntDetectEnabled; onToggled: macroController.huntDetectEnabled = checked }
                            AppButton { text: showHunt ? "Hide" : "Expand"; variant: "ghost"; onClicked: showHunt = !showHunt }
                        }
                        ColumnLayout {
                            visible: showHunt
                            spacing: 8
                            AppInput { Layout.fillWidth: true; placeholderText: "Discord webhook"; text: macroController.discordWebhook; onTextChanged: macroController.discordWebhook = text }
                        }
                    }
                }
            }
        }
    }
}


