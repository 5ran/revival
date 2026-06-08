#pragma once

#include <QObject>
#include <QString>
#include <QStringList>

class NavigationController : public QObject {
    Q_OBJECT
    Q_PROPERTY(QStringList sidebarItems READ sidebarItems CONSTANT)
    Q_PROPERTY(QString currentPage READ currentPage WRITE setCurrentPage NOTIFY currentPageChanged)

public:
    explicit NavigationController(QObject* parent = nullptr);

    QStringList sidebarItems() const;

    QString currentPage() const;
    void setCurrentPage(const QString& page);

    Q_INVOKABLE void navigate(const QString& page);

signals:
    void currentPageChanged();

private:
    QStringList sidebarItems_ {
        QStringLiteral("General"),
        QStringLiteral("Fishing"),
        QStringLiteral("Addons"),
        QStringLiteral("Automation"),
        QStringLiteral("Hunt Detect"),
        QStringLiteral("Account"),
        QStringLiteral("Settings")
    };
    QString currentPage_ = QStringLiteral("General");
};
