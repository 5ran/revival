import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import OpenMacro.UI

Item {
    Rectangle {
        anchors.fill: parent
        color: "#0E0E0E"
    }

    Rectangle {
        anchors.left: parent.left
        width: 420
        height: parent.height
        color: "#161616"

        Column {
            anchors.left: parent.left
            anchors.leftMargin: 48
            anchors.bottom: parent.bottom
            anchors.bottomMargin: 52
            spacing: 8

            Text {
                text: "OpenMacro"
                color: "#F0F0F0"
                font.pixelSize: 32
                font.bold: true
            }

            Text {
                text: "Swift"
                color: "#A9CFCB"
                font.pixelSize: 32
                font.bold: true
            }

            Text {
                text: "The best Automation\nfor Fisch."
                color: "#666666"
                font.pixelSize: 13
                lineHeight: 20
                topPadding: 10
            }
        }
    }

    Item {
        anchors.left: parent.left
        anchors.leftMargin: 420
        width: parent.width - 420
        height: parent.height

        Rectangle {
            visible: authController.updateAvailable
            anchors.top: parent.top
            anchors.left: parent.left
            anchors.right: parent.right
            height: 40
            color: "#0F1D1C"
            border.color: "#2A2A2A"
            border.width: 1

            RowLayout {
                anchors.fill: parent
                anchors.leftMargin: 24
                anchors.rightMargin: 24
                spacing: 14

                Text {
                    text: "Update ready"
                    color: "#F0F0F0"
                    font.pixelSize: 13
                    font.bold: true
                }

                Text {
                    Layout.fillWidth: true
                    text: authController.updateStatusText
                    color: "#666666"
                    font.pixelSize: 12
                }

                AppButton {
                    text: "Restart"
                }
            }
        }

        Column {
            id: pane
            width: 300
            spacing: 10
            anchors.left: parent.left
            anchors.leftMargin: 64
            anchors.verticalCenter: parent.verticalCenter

            Text {
                text: "Sign in"
                color: "#F0F0F0"
                font.pixelSize: 22
                font.bold: true
            }

            Text {
                text: authController.loginMode === "manual"
                      ? "Copy the browser callback code and paste it below."
                      : (authController.loginMode === "credentials"
                         ? "Enter your credentials."
                         : "Fast, with Discord.")
                color: "#666666"
                font.pixelSize: 12
                wrapMode: Text.WordWrap
            }

            Item {
                width: parent.width
                height: 7

                Rectangle {
                    anchors.bottom: parent.bottom
                    width: parent.width
                    height: 1
                    color: "#2A2A2A"
                }
            }

            AppButton {
                visible: authController.loginMode === "discord"
                width: 300
                text: "Continue with Discord"
                onClicked: authController.loginWithDiscord()
            }

            AppButton {
                visible: authController.loginMode === "discord"
                width: 300
                variant: "secondary"
                text: "Skip sign in"
                onClicked: appState.rootRoute = "Shell"
            }

            AppButton {
                visible: authController.loginMode === "discord"
                width: 300
                variant: "secondary"
                text: "Sign in with username"
                onClicked: authController.showCredentialLogin()
            }

            AppButton {
                visible: authController.loginMode === "discord"
                width: 300
                variant: "ghost"
                text: "Have a code? Paste it"
                onClicked: authController.showManualCode()
            }

            AppButton {
                visible: authController.loginMode !== "discord"
                variant: "ghost"
                text: "? Back"
                onClicked: authController.showDiscordLogin()
            }

            AppInput {
                visible: authController.loginMode === "credentials"
                width: 300
                placeholderText: "Username"
                text: authController.username
                onTextChanged: authController.username = text
            }

            AppInput {
                visible: authController.loginMode === "credentials"
                width: 300
                placeholderText: "Password"
                echoMode: TextInput.Password
                text: authController.password
                onTextChanged: authController.password = text
            }

            AppButton {
                visible: authController.loginMode === "credentials"
                width: 300
                text: "Sign in"
                onClicked: authController.submitCredentials()
            }

            AppInput {
                visible: authController.loginMode === "manual"
                width: 300
                placeholderText: "Paste code here"
                text: authController.manualCode
                onTextChanged: authController.manualCode = text
            }

            AppButton {
                visible: authController.loginMode === "manual"
                width: 300
                text: "Finish sign in"
                onClicked: authController.submitManualCode()
            }

            Text {
                visible: authController.loginError.length > 0
                text: authController.loginError
                color: "#F87171"
                font.pixelSize: 12
            }

            Text {
                visible: authController.credentialError.length > 0
                text: authController.credentialError
                color: "#F87171"
                font.pixelSize: 12
            }

            Text {
                visible: authController.manualCodeError.length > 0
                text: authController.manualCodeError
                color: "#F87171"
                font.pixelSize: 12
            }
        }
    }
}

