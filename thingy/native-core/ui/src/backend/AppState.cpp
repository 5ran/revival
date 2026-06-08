#include "AppState.hpp"

AppState::AppState(QObject* parent) : QObject(parent) {}

QString AppState::appTitle() const { return QStringLiteral("OpenMacro Swift"); }
int AppState::windowWidth() const { return 900; }
int AppState::windowHeight() const { return 600; }

bool AppState::topMost() const { return topMost_; }
void AppState::setTopMost(bool value) {
    if (topMost_ == value) return;
    topMost_ = value;
    emit topMostChanged();
}

bool AppState::compactMode() const { return compactMode_; }
void AppState::setCompactMode(bool value) {
    if (compactMode_ == value) return;
    compactMode_ = value;
    emit compactModeChanged();
}

QString AppState::rootRoute() const { return rootRoute_; }
void AppState::setRootRoute(const QString& value) {
    if (rootRoute_ == value) return;
    rootRoute_ = value;
    emit rootRouteChanged();
}

bool AppState::updateAvailable() const { return updateAvailable_; }
void AppState::setUpdateAvailable(bool value) {
    if (updateAvailable_ == value) return;
    updateAvailable_ = value;
    emit updateAvailableChanged();
}

QString AppState::updateStatusText() const { return updateStatusText_; }
void AppState::setUpdateStatusText(const QString& value) {
    if (updateStatusText_ == value) return;
    updateStatusText_ = value;
    emit updateStatusTextChanged();
}

void AppState::requestMinimize() { emit minimizeRequested(); }
void AppState::requestClose() { emit closeRequested(); }
