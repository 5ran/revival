#include "NavigationController.hpp"

NavigationController::NavigationController(QObject* parent) : QObject(parent) {}

QStringList NavigationController::sidebarItems() const { return sidebarItems_; }

QString NavigationController::currentPage() const { return currentPage_; }

void NavigationController::setCurrentPage(const QString& page) {
    if (currentPage_ == page) return;
    currentPage_ = page;
    emit currentPageChanged();
}

void NavigationController::navigate(const QString& page) {
    setCurrentPage(page);
}
