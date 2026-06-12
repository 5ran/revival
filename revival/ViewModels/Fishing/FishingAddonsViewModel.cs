using System.ComponentModel;
using System.Threading.Tasks;

namespace Client.ViewModels;

public sealed class FishingAddonsViewModel : ViewModelBase
{
    private readonly FishingViewModel _fishingViewModel;
    private readonly AutoTotemViewModel _autoTotemViewModel;
    private readonly AutoSovereignRechargeViewModel _autoSovereignRechargeViewModel;

    public GeneralViewModel General { get; private set; } = null!;

    public void AttachGeneral(GeneralViewModel general)
    {
        General = general;
        OnPropertyChanged(nameof(General));
    }

    public FishingAddonsViewModel(
        FishingViewModel fishingViewModel,
        AutoTotemViewModel autoTotemViewModel,
        AutoSovereignRechargeViewModel autoSovereignRechargeViewModel)
    {
        _fishingViewModel = fishingViewModel;
        _autoTotemViewModel = autoTotemViewModel;
        _autoSovereignRechargeViewModel = autoSovereignRechargeViewModel;

        _fishingViewModel.PropertyChanged += HandleFishingPropertyChanged;
        _autoTotemViewModel.PropertyChanged += HandleTotemPropertyChanged;
        _autoSovereignRechargeViewModel.PropertyChanged += HandleSovereignRechargePropertyChanged;
    }

    public bool AutoAquariumEnabled
    {
        get => _fishingViewModel.AutoAquariumEnabled;
        set
        {
            if (_fishingViewModel.AutoAquariumEnabled == value)
            {
                return;
            }

            _fishingViewModel.AutoAquariumEnabled = value;
            OnPropertyChanged(nameof(AutoAquariumEnabled));
        }
    }

    public double AutoAquariumCycleDelayMinutes
    {
        get => _fishingViewModel.AutoAquariumCycleDelayMinutes;
        set
        {
            if (_fishingViewModel.AutoAquariumCycleDelayMinutes == value)
            {
                return;
            }

            _fishingViewModel.AutoAquariumCycleDelayMinutes = value;
            OnPropertyChanged(nameof(AutoAquariumCycleDelayMinutes));
        }
    }

    public string AutoAquariumStatusText => _fishingViewModel.AquariumStatusText;

    public bool AutoTotemEnabled
    {
        get => _autoTotemViewModel.AutoTotemEnabled;
        set
        {
            if (_autoTotemViewModel.AutoTotemEnabled == value)
            {
                return;
            }

            _autoTotemViewModel.AutoTotemEnabled = value;
            OnPropertyChanged(nameof(AutoTotemEnabled));
        }
    }

    public AutoTotemViewModel AutoTotem => _autoTotemViewModel;

    public bool AutoSovereignRechargeEnabled
    {
        get => _autoSovereignRechargeViewModel.Enabled;
        set
        {
            if (_autoSovereignRechargeViewModel.Enabled == value)
            {
                return;
            }

            _autoSovereignRechargeViewModel.Enabled = value;
            OnPropertyChanged(nameof(AutoSovereignRechargeEnabled));
        }
    }

    public AutoSovereignRechargeViewModel AutoSovereignRecharge => _autoSovereignRechargeViewModel;

    public string ActiveAddonTitle => "Fishing Settings";

    private void HandleFishingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FishingViewModel.AutoAquariumEnabled))
        {
            OnPropertyChanged(nameof(AutoAquariumEnabled));
        }
        else if (e.PropertyName is nameof(FishingViewModel.AutoAquariumCycleDelayMinutes))
        {
            OnPropertyChanged(nameof(AutoAquariumCycleDelayMinutes));
        }
        else if (e.PropertyName is nameof(FishingViewModel.AquariumStatusText))
        {
            OnPropertyChanged(nameof(AutoAquariumStatusText));
        }
    }

    private void HandleTotemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AutoTotemViewModel.AutoTotemEnabled))
        {
            OnPropertyChanged(nameof(AutoTotemEnabled));
        }
    }

    private void HandleSovereignRechargePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AutoSovereignRechargeViewModel.Enabled))
        {
            OnPropertyChanged(nameof(AutoSovereignRechargeEnabled));
        }
    }

}
