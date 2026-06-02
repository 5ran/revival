#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QQmlContext>
#include <QQuickStyle>

#include "backend/AppState.hpp"
#include "backend/AuthController.hpp"
#include "backend/MacroController.hpp"
#include "backend/NavigationController.hpp"
#include "backend/SettingsController.hpp"

int main(int argc, char* argv[]) {
    QGuiApplication app(argc, argv);
    QQuickStyle::setStyle(QStringLiteral("Fusion"));

    QQmlApplicationEngine engine;

    AppState appState;
    NavigationController navigationController;
    MacroController macroController;
    SettingsController settingsController;
    AuthController authController;

    QObject::connect(&authController, &AuthController::authenticated, [&]() {
        appState.setRootRoute(QStringLiteral("Shell"));
    });

    engine.rootContext()->setContextProperty(QStringLiteral("appState"), &appState);
    engine.rootContext()->setContextProperty(QStringLiteral("navigationController"), &navigationController);
    engine.rootContext()->setContextProperty(QStringLiteral("macroController"), &macroController);
    engine.rootContext()->setContextProperty(QStringLiteral("settingsController"), &settingsController);
    engine.rootContext()->setContextProperty(QStringLiteral("authController"), &authController);

    engine.loadFromModule("OpenMacro.UI", "Main");
    if (engine.rootObjects().isEmpty()) {
        return -1;
    }

    return app.exec();
}
