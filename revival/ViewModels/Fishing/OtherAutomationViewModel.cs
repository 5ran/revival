using System.ComponentModel;

namespace Client.ViewModels;

public sealed class OtherAutomationViewModel : ViewModelBase
{
    private readonly AutoAnglerViewModel _autoAnglerViewModel;
    private readonly EnchantViewModel _enchantViewModel;
    private readonly AppraiseViewModel _appraiseViewModel;
    private readonly TreasureAppraiseViewModel _treasureAppraiseViewModel;
    private readonly TraderViewModel _traderViewModel = new();
    private readonly CurrentlyTradingViewModel _currentlyTradingViewModel = new();
    private object? _activeAutomation;

    public OtherAutomationViewModel(
        AutoAnglerViewModel autoAnglerViewModel,
        EnchantViewModel enchantViewModel,
        AppraiseViewModel appraiseViewModel,
        TreasureAppraiseViewModel treasureAppraiseViewModel)
    {
        _autoAnglerViewModel = autoAnglerViewModel;
        _enchantViewModel = enchantViewModel;
        _appraiseViewModel = appraiseViewModel;
        _treasureAppraiseViewModel = treasureAppraiseViewModel;

        _autoAnglerViewModel.PropertyChanged += HandleAutoAnglerPropertyChanged;
        _enchantViewModel.PropertyChanged += HandleEnchantPropertyChanged;
        _appraiseViewModel.PropertyChanged += HandleAppraisePropertyChanged;
        _treasureAppraiseViewModel.PropertyChanged += HandleTreasurePropertyChanged;
        _traderViewModel.PropertyChanged += HandleTraderPropertyChanged;
        _currentlyTradingViewModel.PropertyChanged += HandleCurrentlyTradingPropertyChanged;
        UpdateActiveAutomation();
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
            UpdateActiveAutomation();
        }
    }

    public bool EnchantEnabled
    {
        get => _enchantViewModel.AutoEnchantEnabled;
        set
        {
            if (_enchantViewModel.AutoEnchantEnabled == value)
            {
                return;
            }

            _enchantViewModel.AutoEnchantEnabled = value;
            OnPropertyChanged(nameof(EnchantEnabled));
            UpdateActiveAutomation();
        }
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

    public bool TraderEnabled
    {
        get => _traderViewModel.TraderEnabled;
        set
        {
            Client.Services.AppLog.Info("TraderMode", $"TraderEnabled set request -> {value} (current={_traderViewModel.TraderEnabled})");
            if (_traderViewModel.TraderEnabled == value)
            {
                Client.Services.AppLog.Info("TraderMode", "TraderEnabled unchanged; skipping.");
                return;
            }

            _traderViewModel.TraderEnabled = value;
            OnPropertyChanged(nameof(TraderEnabled));
            Client.Services.AppLog.Info("TraderMode", $"TraderEnabled now={_traderViewModel.TraderEnabled}; activeBefore={_activeAutomation?.GetType().Name ?? "none"}");
            UpdateActiveAutomation();
            Client.Services.AppLog.Info("TraderMode", $"TraderEnabled complete; activeAfter={_activeAutomation?.GetType().Name ?? "none"}");
        }
    }

    public TraderViewModel Trader => _traderViewModel;

    public bool CurrentlyTradingEnabled
    {
        get => _currentlyTradingViewModel.Enabled;
        set
        {
            Client.Services.AppLog.Info("TraderMode", $"CurrentlyTradingEnabled set request -> {value} (current={_currentlyTradingViewModel.Enabled})");
            if (_currentlyTradingViewModel.Enabled == value)
            {
                Client.Services.AppLog.Info("TraderMode", "CurrentlyTradingEnabled unchanged; skipping.");
                return;
            }

            _currentlyTradingViewModel.Enabled = value;
            OnPropertyChanged(nameof(CurrentlyTradingEnabled));
            Client.Services.AppLog.Info("TraderMode", $"CurrentlyTradingEnabled now={_currentlyTradingViewModel.Enabled}; activeBefore={_activeAutomation?.GetType().Name ?? "none"}");
            UpdateActiveAutomation();
            Client.Services.AppLog.Info("TraderMode", $"CurrentlyTradingEnabled complete; activeAfter={_activeAutomation?.GetType().Name ?? "none"}");
        }
    }

    public CurrentlyTradingViewModel CurrentlyTrading => _currentlyTradingViewModel;

    public bool HasActiveAutomation => _activeAutomation is not null;

    public bool ShowAutomationTabs => !HasActiveAutomation;

    public object? ActiveAutomation => _activeAutomation;

    public string ActiveAutomationTitle => _activeAutomation switch
    {
        AutoAnglerViewModel => "Other Automation - Angler",
        EnchantViewModel => "Other Automation - Enchant",
        AppraiseViewModel => "Other Automation - Appraise",
        TreasureAppraiseViewModel => "Other Automation - Treasure Appraise",
        TraderViewModel => "Other Automation - Trader",
        CurrentlyTradingViewModel => "Other Automation - Currently Trading",
        _ => "Other Automation",
    };

    private void HandleAutoAnglerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AutoAnglerViewModel.AutoAnglerEnabled))
        {
            OnPropertyChanged(nameof(AutoAnglerEnabled));
            UpdateActiveAutomation();
        }
    }

    private void HandleEnchantPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EnchantViewModel.AutoEnchantEnabled))
        {
            OnPropertyChanged(nameof(EnchantEnabled));
            UpdateActiveAutomation();
        }
    }

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

    private void HandleTraderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TraderViewModel.TraderEnabled))
        {
            OnPropertyChanged(nameof(TraderEnabled));
            UpdateActiveAutomation();
        }
    }

    private void HandleCurrentlyTradingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CurrentlyTradingViewModel.Enabled))
        {
            OnPropertyChanged(nameof(CurrentlyTradingEnabled));
            UpdateActiveAutomation();
        }
    }

    private void UpdateActiveAutomation()
    {
        object? next = null;
        Client.Services.AppLog.Info(
            "TraderMode",
            $"UpdateActiveAutomation enter trader={_traderViewModel.TraderEnabled} angler={_autoAnglerViewModel.AutoAnglerEnabled} enchant={_enchantViewModel.AutoEnchantEnabled} appraise={_appraiseViewModel.AutoAppraiseEnabled} treasure={_treasureAppraiseViewModel.AutoTreasureEnabled}");
        if (_autoAnglerViewModel.AutoAnglerEnabled)
        {
            next = _autoAnglerViewModel;
        }
        else if (_enchantViewModel.AutoEnchantEnabled)
        {
            next = _enchantViewModel;
        }
        else if (_appraiseViewModel.AutoAppraiseEnabled)
        {
            next = _appraiseViewModel;
        }
        else if (_treasureAppraiseViewModel.AutoTreasureEnabled)
        {
            next = _treasureAppraiseViewModel;
        }
        else if (_traderViewModel.TraderEnabled)
        {
            next = _traderViewModel;
        }
        else if (_currentlyTradingViewModel.Enabled)
        {
            next = _currentlyTradingViewModel;
        }

        if (ReferenceEquals(_activeAutomation, next))
        {
            Client.Services.AppLog.Info("TraderMode", $"UpdateActiveAutomation no-op active={_activeAutomation?.GetType().Name ?? "none"}");
            return;
        }

        _activeAutomation = next;
        Client.Services.AppLog.Info("TraderMode", $"UpdateActiveAutomation new active={_activeAutomation?.GetType().Name ?? "none"}");
        OnPropertyChanged(nameof(ActiveAutomation));
        OnPropertyChanged(nameof(HasActiveAutomation));
        OnPropertyChanged(nameof(ShowAutomationTabs));
        OnPropertyChanged(nameof(ActiveAutomationTitle));
    }
}
