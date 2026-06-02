#include "SettingsController.hpp"

SettingsController::SettingsController(QObject* parent) : QObject(parent) {}

QString SettingsController::selectedTheme() const { return selectedTheme_; }
void SettingsController::setSelectedTheme(const QString& value) {
    if (selectedTheme_ == value) return;
    selectedTheme_ = value;
    emit selectedThemeChanged();
}

QString SettingsController::backgroundColor() const { return backgroundColor_; }
void SettingsController::setBackgroundColor(const QString& value) {
    if (backgroundColor_ == value) return;
    backgroundColor_ = value;
    emit customColorsChanged();
}

QString SettingsController::surfaceColor() const { return surfaceColor_; }
void SettingsController::setSurfaceColor(const QString& value) {
    if (surfaceColor_ == value) return;
    surfaceColor_ = value;
    emit customColorsChanged();
}

QString SettingsController::borderColor() const { return borderColor_; }
void SettingsController::setBorderColor(const QString& value) {
    if (borderColor_ == value) return;
    borderColor_ = value;
    emit customColorsChanged();
}

QString SettingsController::accentColor() const { return accentColor_; }
void SettingsController::setAccentColor(const QString& value) {
    if (accentColor_ == value) return;
    accentColor_ = value;
    emit customColorsChanged();
}

QString SettingsController::textColor() const { return textColor_; }
void SettingsController::setTextColor(const QString& value) {
    if (textColor_ == value) return;
    textColor_ = value;
    emit customColorsChanged();
}

void SettingsController::selectTheme(const QString& themeName) {
    setSelectedTheme(themeName);

    if (themeName == QStringLiteral("Dark")) {
        setBackgroundColor(QStringLiteral("#0E0E0E"));
        setSurfaceColor(QStringLiteral("#161616"));
        setBorderColor(QStringLiteral("#2A2A2A"));
        setAccentColor(QStringLiteral("#A9CFCB"));
        setTextColor(QStringLiteral("#F0F0F0"));
        return;
    }

    if (themeName == QStringLiteral("Light")) {
        setBackgroundColor(QStringLiteral("#F5F6F7"));
        setSurfaceColor(QStringLiteral("#FFFFFF"));
        setBorderColor(QStringLiteral("#D9DDE1"));
        setAccentColor(QStringLiteral("#4F7C78"));
        setTextColor(QStringLiteral("#111418"));
        return;
    }

    if (themeName == QStringLiteral("Slate")) {
        setBackgroundColor(QStringLiteral("#11151A"));
        setSurfaceColor(QStringLiteral("#171D24"));
        setBorderColor(QStringLiteral("#28313B"));
        setAccentColor(QStringLiteral("#8FB6C8"));
        setTextColor(QStringLiteral("#E7EDF2"));
        return;
    }

    if (themeName == QStringLiteral("Pink")) {
        setBackgroundColor(QStringLiteral("#151115"));
        setSurfaceColor(QStringLiteral("#1E1820"));
        setBorderColor(QStringLiteral("#3A2A3F"));
        setAccentColor(QStringLiteral("#D49BBF"));
        setTextColor(QStringLiteral("#F4EAF1"));
        return;
    }
}
