#pragma once

#include <QObject>
#include <QString>

class AuthController : public QObject {
    Q_OBJECT
    Q_PROPERTY(QString loginMode READ loginMode WRITE setLoginMode NOTIFY loginModeChanged)
    Q_PROPERTY(bool updateAvailable READ updateAvailable WRITE setUpdateAvailable NOTIFY updateAvailableChanged)
    Q_PROPERTY(QString updateStatusText READ updateStatusText WRITE setUpdateStatusText NOTIFY updateStatusTextChanged)

    Q_PROPERTY(QString username READ username WRITE setUsername NOTIFY credentialsChanged)
    Q_PROPERTY(QString password READ password WRITE setPassword NOTIFY credentialsChanged)
    Q_PROPERTY(QString manualCode READ manualCode WRITE setManualCode NOTIFY manualCodeChanged)

    Q_PROPERTY(QString loginError READ loginError WRITE setLoginError NOTIFY loginErrorChanged)
    Q_PROPERTY(QString credentialError READ credentialError WRITE setCredentialError NOTIFY credentialErrorChanged)
    Q_PROPERTY(QString manualCodeError READ manualCodeError WRITE setManualCodeError NOTIFY manualCodeErrorChanged)

    Q_PROPERTY(QString profileName READ profileName WRITE setProfileName NOTIFY profileChanged)
    Q_PROPERTY(QString profileUsername READ profileUsername WRITE setProfileUsername NOTIFY profileChanged)

public:
    explicit AuthController(QObject* parent = nullptr);

    QString loginMode() const;
    void setLoginMode(const QString& value);

    bool updateAvailable() const;
    void setUpdateAvailable(bool value);

    QString updateStatusText() const;
    void setUpdateStatusText(const QString& value);

    QString username() const;
    void setUsername(const QString& value);

    QString password() const;
    void setPassword(const QString& value);

    QString manualCode() const;
    void setManualCode(const QString& value);

    QString loginError() const;
    void setLoginError(const QString& value);

    QString credentialError() const;
    void setCredentialError(const QString& value);

    QString manualCodeError() const;
    void setManualCodeError(const QString& value);

    QString profileName() const;
    void setProfileName(const QString& value);

    QString profileUsername() const;
    void setProfileUsername(const QString& value);

    Q_INVOKABLE void loginWithDiscord();
    Q_INVOKABLE void showCredentialLogin();
    Q_INVOKABLE void showManualCode();
    Q_INVOKABLE void showDiscordLogin();
    Q_INVOKABLE void submitManualCode();
    Q_INVOKABLE void submitCredentials();
    Q_INVOKABLE void saveUsername(const QString& value);
    Q_INVOKABLE void savePassword(const QString& currentValue, const QString& newValue, const QString& confirmValue);

signals:
    void loginModeChanged();
    void updateAvailableChanged();
    void updateStatusTextChanged();
    void credentialsChanged();
    void manualCodeChanged();
    void loginErrorChanged();
    void credentialErrorChanged();
    void manualCodeErrorChanged();
    void profileChanged();
    void authenticated();

private:
    QString loginMode_ = QStringLiteral("discord");
    bool updateAvailable_ = true;
    QString updateStatusText_ = QStringLiteral("A newer build is available.");

    QString username_;
    QString password_;
    QString manualCode_;

    QString loginError_;
    QString credentialError_;
    QString manualCodeError_;

    QString profileName_ = QStringLiteral("OpenMacro User");
    QString profileUsername_ = QStringLiteral("openmacro_user");
};
