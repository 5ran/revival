using System;
using System.Reflection;
using System.Windows.Input;
using Client.Services;

namespace Client.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private ViewModelBase? _currentSubView;

    public SettingsViewModel()
    {
        InterfaceSoundService.SettingsChanged += OnInterfaceSoundSettingsChanged;
        NavigateToClientCommand = new RelayCommand(_ =>
        {
            CurrentSubView = new SettingsClientViewModel(ShowMainView);
            return System.Threading.Tasks.Task.CompletedTask;
        });
    }

    public ViewModelBase? CurrentSubView
    {
        get => _currentSubView;
        private set
        {
            if (_currentSubView is IDisposable old)
                old.Dispose();
            SetProperty(ref _currentSubView, value);
            OnPropertyChanged(nameof(IsMainViewVisible));
        }
    }

    public bool IsMainViewVisible => _currentSubView is null;

    public ICommand NavigateToClientCommand { get; }

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
        CurrentSubView = null;
    }

    private void ShowMainView()
    {
        CurrentSubView = null;
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
