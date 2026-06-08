using System.IO;
using System.Windows;
using Wpf.Ui.Appearance;

namespace OpenMacroSwift.Desktop;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        WriteStartupLog("app.start");
        DispatcherUnhandledException += (_, args) =>
        {
            WriteStartupLog($"dispatcher.exception {args.Exception}");
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            WriteStartupLog($"domain.exception {args.ExceptionObject}");
        };

        base.OnStartup(e);
        WriteStartupLog("app.after-base-startup");
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        WriteStartupLog("app.theme-applied");

        var window = new MainWindow
        {
            ShowActivated = true,
            Topmost = true,
            WindowState = WindowState.Normal,
        };
        WriteStartupLog("app.window-created");
        MainWindow = window;
        window.Show();
        WriteStartupLog("app.window-shown");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        WriteStartupLog($"app.exit code={e.ApplicationExitCode}");
        base.OnExit(e);
    }

    private static void WriteStartupLog(string message)
    {
        File.AppendAllText(
            Path.Combine(AppContext.BaseDirectory, "revival_startup_error.log"),
            $"[{DateTime.Now:O}] {message}\n");
    }
}

