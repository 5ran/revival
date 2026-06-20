using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Client.ViewModels;

public sealed class OtherAutomationViewModel : ViewModelBase
{
    private readonly EnchantViewModel _enchantViewModel;
    private object? _activeAutomation;

    public OtherAutomationViewModel(EnchantViewModel enchantViewModel)
    {
        _enchantViewModel = enchantViewModel;
        NavigateBackToSystemsCommand = new RelayCommand(_ =>
        {
            NavigateBackToSystemsAction?.Invoke();
            return Task.CompletedTask;
        });

        _enchantViewModel.PropertyChanged += HandleEnchantPropertyChanged;
        UpdateActiveAutomation();
    }

    public Action? NavigateBackToSystemsAction { get; set; }

    public RelayCommand NavigateBackToSystemsCommand { get; }

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

    public bool HasActiveAutomation => _activeAutomation is not null;

    public bool ShowAutomationTabs => !HasActiveAutomation;

    public object? ActiveAutomation => _activeAutomation;

    public EnchantViewModel EnchantAutomation => _enchantViewModel;

    public string ActiveAutomationTitle => _activeAutomation switch
    {
        EnchantViewModel => "Auto Enchant",
        _ => "Auto Enchant",
    };

    private void HandleEnchantPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EnchantViewModel.AutoEnchantEnabled))
        {
            OnPropertyChanged(nameof(EnchantEnabled));
            UpdateActiveAutomation();
        }
    }

    private void UpdateActiveAutomation()
    {
        object? next = null;
        if (_enchantViewModel.AutoEnchantEnabled)
        {
            next = _enchantViewModel;
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
