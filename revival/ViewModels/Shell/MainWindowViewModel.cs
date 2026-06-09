using System;
using System.Threading.Tasks;
using Avalonia.Input;

namespace Client.ViewModels;

/// <summary>
/// Hosts the current top-level view model for the main window.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ShellViewModel _shellViewModel;
    private object _currentView;

    /// <summary>
    /// Creates the main window view model.
    /// </summary>
    public MainWindowViewModel()
    {
        _shellViewModel = new ShellViewModel();
        _currentView = _shellViewModel;
        _shellViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.IsCompactMode))
            {
                OnPropertyChanged(nameof(IsCompactMode));
            }
        };
    }

    /// <summary>
    /// Gets the currently displayed view model.
    /// </summary>
    public object CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    /// <summary>
    /// Initializes the shell-only app startup.
    /// </summary>
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public bool HandleKey(Key key)
    {
        return CurrentView is ShellViewModel shell && shell.HandleKey(key);
    }

    public Key StartStopHotkey => _shellViewModel.StartStopHotkey;

    /// <summary>
    /// True while the shell is in compact mode (any macro is running). Drives
    /// MainWindow's size — bound there so the window shrinks to a focused
    /// layout while running and restores when stopped.
    /// </summary>
    public bool IsCompactMode => CurrentView is ShellViewModel shell && shell.IsCompactMode;

    public Task ToggleMacroAsync()
    {
        return CurrentView is ShellViewModel ? _shellViewModel.ToggleMacroAsync() : Task.CompletedTask;
    }

    public Task ToggleMacroFromHotkeyAsync()
    {
        return CurrentView is ShellViewModel ? _shellViewModel.ToggleMacroFromHotkeyAsync() : Task.CompletedTask;
    }

}
