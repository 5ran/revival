#pragma once

#include <QObject>
#include <QString>

class SettingsController : public QObject {
    Q_OBJECT
    Q_PROPERTY(QString selectedTheme READ selectedTheme WRITE setSelectedTheme NOTIFY selectedThemeChanged)
    Q_PROPERTY(QString backgroundColor READ backgroundColor WRITE setBackgroundColor NOTIFY customColorsChanged)
    Q_PROPERTY(QString surfaceColor READ surfaceColor WRITE setSurfaceColor NOTIFY customColorsChanged)
    Q_PROPERTY(QString borderColor READ borderColor WRITE setBorderColor NOTIFY customColorsChanged)
    Q_PROPERTY(QString accentColor READ accentColor WRITE setAccentColor NOTIFY customColorsChanged)
    Q_PROPERTY(QString textColor READ textColor WRITE setTextColor NOTIFY customColorsChanged)

public:
    explicit SettingsController(QObject* parent = nullptr);

    QString selectedTheme() const;
    void setSelectedTheme(const QString& value);

    QString backgroundColor() const;
    void setBackgroundColor(const QString& value);

    QString surfaceColor() const;
    void setSurfaceColor(const QString& value);

    QString borderColor() const;
    void setBorderColor(const QString& value);

    QString accentColor() const;
    void setAccentColor(const QString& value);

    QString textColor() const;
    void setTextColor(const QString& value);

    Q_INVOKABLE void selectTheme(const QString& themeName);

signals:
    void selectedThemeChanged();
    void customColorsChanged();

private:
    QString selectedTheme_ = QStringLiteral("Dark");
    QString backgroundColor_ = QStringLiteral("#0E0E0E");
    QString surfaceColor_ = QStringLiteral("#161616");
    QString borderColor_ = QStringLiteral("#2A2A2A");
    QString accentColor_ = QStringLiteral("#A9CFCB");
    QString textColor_ = QStringLiteral("#F0F0F0");
};
