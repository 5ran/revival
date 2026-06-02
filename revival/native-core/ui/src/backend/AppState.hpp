#pragma once

#include <QObject>
#include <QString>

class AppState : public QObject {
    Q_OBJECT
    Q_PROPERTY(QString appTitle READ appTitle CONSTANT)
    Q_PROPERTY(int windowWidth READ windowWidth CONSTANT)
    Q_PROPERTY(int windowHeight READ windowHeight CONSTANT)
    Q_PROPERTY(bool topMost READ topMost WRITE setTopMost NOTIFY topMostChanged)
    Q_PROPERTY(bool compactMode READ compactMode WRITE setCompactMode NOTIFY compactModeChanged)
    Q_PROPERTY(QString rootRoute READ rootRoute WRITE setRootRoute NOTIFY rootRouteChanged)
    Q_PROPERTY(bool updateAvailable READ updateAvailable WRITE setUpdateAvailable NOTIFY updateAvailableChanged)
    Q_PROPERTY(QString updateStatusText READ updateStatusText WRITE setUpdateStatusText NOTIFY updateStatusTextChanged)

public:
    explicit AppState(QObject* parent = nullptr);

    QString appTitle() const;
    int windowWidth() const;
    int windowHeight() const;

    bool topMost() const;
    void setTopMost(bool value);

    bool compactMode() const;
    void setCompactMode(bool value);

    QString rootRoute() const;
    void setRootRoute(const QString& value);

    bool updateAvailable() const;
    void setUpdateAvailable(bool value);

    QString updateStatusText() const;
    void setUpdateStatusText(const QString& value);

    Q_INVOKABLE void requestMinimize();
    Q_INVOKABLE void requestClose();

signals:
    void topMostChanged();
    void compactModeChanged();
    void rootRouteChanged();
    void updateAvailableChanged();
    void updateStatusTextChanged();

    void minimizeRequested();
    void closeRequested();

private:
    bool topMost_ = true;
    bool compactMode_ = false;
    QString rootRoute_ = QStringLiteral("Login");
    bool updateAvailable_ = false;
    QString updateStatusText_ = QStringLiteral("You're up to date");
};
