using System.ComponentModel;

namespace Client.ViewModels;

public sealed class AnglerAutomationViewModel : ViewModelBase
{
    private readonly AutoAnglerViewModel _autoAnglerViewModel;

    public AnglerAutomationViewModel(AutoAnglerViewModel autoAnglerViewModel)
    {
        _autoAnglerViewModel = autoAnglerViewModel;
        _autoAnglerViewModel.PropertyChanged += HandleAutoAnglerPropertyChanged;
    }

    public bool AutoAnglerEnabled
    {
        get => _autoAnglerViewModel.AutoAnglerEnabled;
        set
        {
            if (_autoAnglerViewModel.AutoAnglerEnabled == value)
            {
                return;
            }

            _autoAnglerViewModel.AutoAnglerEnabled = value;
            OnPropertyChanged(nameof(AutoAnglerEnabled));
            OnPropertyChanged(nameof(HasActiveAutomation));
            OnPropertyChanged(nameof(ShowAutomationTab));
        }
    }

    public bool HasActiveAutomation => _autoAnglerViewModel.AutoAnglerEnabled;

    public bool ShowAutomationTab => !HasActiveAutomation;

    public AutoAnglerViewModel ActiveAutomation => _autoAnglerViewModel;

    private void HandleAutoAnglerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AutoAnglerViewModel.AutoAnglerEnabled))
        {
            OnPropertyChanged(nameof(AutoAnglerEnabled));
            OnPropertyChanged(nameof(HasActiveAutomation));
            OnPropertyChanged(nameof(ShowAutomationTab));
        }
    }
}
