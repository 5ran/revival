using System.Windows;
using Wpf.Ui.Appearance;

namespace OpenMacroSwift.Desktop;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
    }
}

