using System.ComponentModel;

namespace Client.ViewModels;

public sealed class AppraiseAutomationViewModel : ViewModelBase
{
    private readonly AppraiseViewModel _appraiseViewModel;
    private readonly TreasureAppraiseViewModel _treasureAppraiseViewModel;
    private object? _activeAutomation;

    public AppraiseAutomationViewModel(
        AppraiseViewModel appraiseViewModel,
        TreasureAppraiseViewModel treasureAppraiseViewModel)
    {
        _appraiseViewModel = appraiseViewModel;
        _treasureAppraiseViewModel = treasureAppraiseViewModel;

        _appraiseViewModel.PropertyChanged += HandleAppraisePropertyChanged;
        _treasureAppraiseViewModel.PropertyChanged += HandleTreasurePropertyChanged;
        UpdateActiveAutomation();
    }

    public bool AppraiseEnabled
    {
        get => _appraiseViewModel.AutoAppraiseEnabled;
        set
        {
            if (_appraiseViewModel.AutoAppraiseEnabled == value)
            {
                return;
            }

            _appraiseViewModel.AutoAppraiseEnabled = value;
            OnPropertyChanged(nameof(AppraiseEnabled));
            UpdateActiveAutomation();
        }
    }

    public bool TreasureAppraiseEnabled
    {
        get => _treasureAppraiseViewModel.AutoTreasureEnabled;
        set
        {
            if (_treasureAppraiseViewModel.AutoTreasureEnabled == value)
            {
                return;
            }

            _treasureAppraiseViewModel.AutoTreasureEnabled = value;
            OnPropertyChanged(nameof(TreasureAppraiseEnabled));
            UpdateActiveAutomation();
        }
    }

    public bool HasActiveAutomation => _activeAutomation is not null;

    public bool ShowAutomationTabs => !HasActiveAutomation;

    public object? ActiveAutomation => _activeAutomation;

    public AppraiseViewModel AppraiseAutomation => _appraiseViewModel;

    public TreasureAppraiseViewModel TreasureAppraiseAutomation => _treasureAppraiseViewModel;

    public string ActiveAutomationTitle => _activeAutomation switch
    {
        AppraiseViewModel => "Appraise",
        TreasureAppraiseViewModel => "Treasure Appraise",
        _ => "Appraise",
    };

    private void HandleAppraisePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppraiseViewModel.AutoAppraiseEnabled))
        {
            OnPropertyChanged(nameof(AppraiseEnabled));
            UpdateActiveAutomation();
        }
    }

    private void HandleTreasurePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TreasureAppraiseViewModel.AutoTreasureEnabled))
        {
            OnPropertyChanged(nameof(TreasureAppraiseEnabled));
            UpdateActiveAutomation();
        }
    }

    private void UpdateActiveAutomation()
    {
        object? next = null;
        if (_appraiseViewModel.AutoAppraiseEnabled)
        {
            next = _appraiseViewModel;
        }
        else if (_treasureAppraiseViewModel.AutoTreasureEnabled)
        {
            next = _treasureAppraiseViewModel;
        }

        if (ReferenceEquals(_activeAutomation, next))
        {
            return;
        }

        _activeAutomation = next;
        OnPropertyChanged(nameof(ActiveAutomation));
        OnPropertyChanged(nameof(HasActiveAutomation));
        OnPropertyChanged(nameof(ShowAutomationTabs));
        OnPropertyChanged(nameof(ActiveAutomationTitle));
    }
}
