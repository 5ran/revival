using System;
using System.Reflection;
using Client.Services;

namespace Client.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    public SettingsViewModel()
    {
        InterfaceSoundService.SettingsChanged += OnInterfaceSoundSettingsChanged;
        ClientSettings = new SettingsClientViewModel();
    }

    public SettingsClientViewModel ClientSettings { get; }

    public string AppVersionText { get; } = ResolveAppVersionText();

    public bool InterfaceSoundsEnabled
    {
        get => InterfaceSoundService.Enabled;
        set => InterfaceSoundService.SetEnabled(value);
    }

    public double InterfaceSoundVolume
    {
        get => InterfaceSoundService.Volume;
        set => InterfaceSoundService.SetVolume((int)Math.Round(value));
    }

    // Called by ShellViewModel when navigating to Settings so a freshly-entered
    // page always lands on the main view, never the previously-opened sub-page.
    public void ResetToMainView()
    {
        // Settings now renders as a single integrated page, so there is no
        // sub-view state to reset when re-entering Edit.
    }

    private void OnInterfaceSoundSettingsChanged()
    {
        OnPropertyChanged(nameof(InterfaceSoundsEnabled));
        OnPropertyChanged(nameof(InterfaceSoundVolume));
    }

    private static string ResolveAppVersionText()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            var trimmed = plus >= 0 ? informational[..plus] : informational;
            return $"v{trimmed}";
        }

        var version = assembly.GetName().Version;
        return version is null ? string.Empty : $"v{version.ToString(3)}";
    }
}
