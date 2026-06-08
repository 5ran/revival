#include "AuthController.hpp"

AuthController::AuthController(QObject* parent) : QObject(parent) {}

QString AuthController::loginMode() const { return loginMode_; }
void AuthController::setLoginMode(const QString& value) {
    if (loginMode_ == value) return;
    loginMode_ = value;
    emit loginModeChanged();
}

bool AuthController::updateAvailable() const { return updateAvailable_; }
void AuthController::setUpdateAvailable(bool value) {
    if (updateAvailable_ == value) return;
    updateAvailable_ = value;
    emit updateAvailableChanged();
}

QString AuthController::updateStatusText() const { return updateStatusText_; }
void AuthController::setUpdateStatusText(const QString& value) {
    if (updateStatusText_ == value) return;
    updateStatusText_ = value;
    emit updateStatusTextChanged();
}

QString AuthController::username() const { return username_; }
void AuthController::setUsername(const QString& value) {
    if (username_ == value) return;
    username_ = value;
    emit credentialsChanged();
}

QString AuthController::password() const { return password_; }
void AuthController::setPassword(const QString& value) {
    if (password_ == value) return;
    password_ = value;
    emit credentialsChanged();
}

QString AuthController::manualCode() const { return manualCode_; }
void AuthController::setManualCode(const QString& value) {
    if (manualCode_ == value) return;
    manualCode_ = value;
    emit manualCodeChanged();
}

QString AuthController::loginError() const { return loginError_; }
void AuthController::setLoginError(const QString& value) {
    if (loginError_ == value) return;
    loginError_ = value;
    emit loginErrorChanged();
}

QString AuthController::credentialError() const { return credentialError_; }
void AuthController::setCredentialError(const QString& value) {
    if (credentialError_ == value) return;
    credentialError_ = value;
    emit credentialErrorChanged();
}

QString AuthController::manualCodeError() const { return manualCodeError_; }
void AuthController::setManualCodeError(const QString& value) {
    if (manualCodeError_ == value) return;
    manualCodeError_ = value;
    emit manualCodeErrorChanged();
}

QString AuthController::profileName() const { return profileName_; }
void AuthController::setProfileName(const QString& value) {
    if (profileName_ == value) return;
    profileName_ = value;
    emit profileChanged();
}

QString AuthController::profileUsername() const { return profileUsername_; }
void AuthController::setProfileUsername(const QString& value) {
    if (profileUsername_ == value) return;
    profileUsername_ = value;
    emit profileChanged();
}

void AuthController::loginWithDiscord() {
    // TODO: connect to real OAuth browser flow + callback handling.
    setLoginError(QString());
    emit authenticated();
}

void AuthController::showCredentialLogin() { setLoginMode(QStringLiteral("credentials")); }
void AuthController::showManualCode() { setLoginMode(QStringLiteral("manual")); }
void AuthController::showDiscordLogin() { setLoginMode(QStringLiteral("discord")); }

void AuthController::submitManualCode() {
    // TODO: validate code against backend API, then hydrate session.
    if (manualCode_.trimmed().isEmpty()) {
        setManualCodeError(QStringLiteral("Paste a code first."));
        return;
    }

    setManualCodeError(QString());
    emit authenticated();
}

void AuthController::submitCredentials() {
    // TODO: validate credentials via backend API, then hydrate session.
    if (username_.trimmed().isEmpty() || password_.trimmed().isEmpty()) {
        setCredentialError(QStringLiteral("Username and password are required."));
        return;
    }

    setCredentialError(QString());
    emit authenticated();
}

void AuthController::saveUsername(const QString& value) {
    // TODO: persist username update through account API.
    if (value.trimmed().isEmpty()) {
        setCredentialError(QStringLiteral("Username cannot be empty."));
        return;
    }

    setCredentialError(QString());
    setProfileUsername(value.trimmed());
}

void AuthController::savePassword(const QString& currentValue, const QString& newValue, const QString& confirmValue) {
    // TODO: persist password update through account API.
    Q_UNUSED(currentValue);

    if (newValue.isEmpty() || confirmValue.isEmpty()) {
        setCredentialError(QStringLiteral("Fill all password fields."));
        return;
    }

    if (newValue != confirmValue) {
        setCredentialError(QStringLiteral("Passwords do not match."));
        return;
    }

    setCredentialError(QStringLiteral("Password updated (stubbed)."));
}
