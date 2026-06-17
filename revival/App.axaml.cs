using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Client.Services;

namespace Client;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                var userSettings = new UserSettingsStore().Load();
                var theme = !string.IsNullOrWhiteSpace(userSettings.Theme) &&
                            Enum.TryParse<AppTheme>(userSettings.Theme, true, out var parsedTheme)
                    ? parsedTheme
                    : AppSettingsService.Load().Theme;
                ThemeService.Apply(theme);
                _ = new OffsetsService();

                AppLog.Info("App", "Creating MainWindow.");
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
                AppLog.Info("App", "MainWindow shown.");
            }
            catch (Exception ex)
            {
                AppLog.Error("App", "Failed to create/show MainWindow.", ex);
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
