import QtQuick
import QtQuick.Layouts
import OpenMacro.UI

Item {
    Rectangle { anchors.fill: parent; color: "#0E0E0E" }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 34
        spacing: 12

        Text {
            text: "Account"
            color: "#F0F0F0"
            font.pixelSize: 24
            font.bold: true
        }

        AppCard {
            Layout.fillWidth: true
            RowLayout {
                anchors.fill: parent
                spacing: 12

                Rectangle {
                    width: 56
                    height: 56
                    radius: 28
                    color: "#1F2A2A"
                    border.color: "#2A2A2A"
                    border.width: 1

                    Text {
                        anchors.centerIn: parent
                        text: authController.profileName.length > 0 ? authController.profileName.charAt(0) : "U"
                        color: "#A9CFCB"
                        font.pixelSize: 18
                        font.bold: true
                    }
                }

                Column {
                    spacing: 4
                    Text { text: authController.profileName; color: "#F0F0F0"; font.pixelSize: 16; font.bold: true }
                    Text { text: authController.profileUsername; color: "#666666"; font.pixelSize: 12 }
                }
            }
        }

        AppCard {
            Layout.fillWidth: true
            Column {
                spacing: 8
                Text { text: "Username"; color: "#666666"; font.pixelSize: 12 }
                RowLayout {
                    width: parent.width
                    spacing: 10
                    AppInput {
                        id: usernameInput
                        Layout.fillWidth: true
                        text: authController.profileUsername
                    }
                    AppButton {
                        text: "Save"
                        onClicked: authController.saveUsername(usernameInput.text)
                    }
                }
            }
        }

        AppCard {
            Layout.fillWidth: true
            Column {
                spacing: 8
                Text { text: "Password"; color: "#666666"; font.pixelSize: 12 }
                AppInput { id: currentPwd; width: parent.width; placeholderText: "Current password"; echoMode: TextInput.Password }
                AppInput { id: newPwd; width: parent.width; placeholderText: "New password"; echoMode: TextInput.Password }
                AppInput { id: confirmPwd; width: parent.width; placeholderText: "Confirm new password"; echoMode: TextInput.Password }

                Row {
                    spacing: 10
                    AppButton {
                        text: "Update"
                        onClicked: authController.savePassword(currentPwd.text, newPwd.text, confirmPwd.text)
                    }
                    AppButton {
                        variant: "secondary"
                        text: "Cancel"
                        onClicked: {
                            currentPwd.text = ""
                            newPwd.text = ""
                            confirmPwd.text = ""
                        }
                    }
                }

                Text {
                    visible: authController.credentialError.length > 0
                    text: authController.credentialError
                    color: "#F87171"
                    font.pixelSize: 12
                }
            }
        }
    }
}

