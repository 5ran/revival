using System.ComponentModel;
using System.Threading.Tasks;

namespace Client.ViewModels;

public sealed class FishingAddonsViewModel : ViewModelBase
{
    private readonly AutoTotemViewModel _autoTotemViewModel;
    private readonly AutoSovereignRechargeViewModel _autoSovereignRechargeViewModel;
    private readonly HuntDetectViewModel _huntDetectViewModel;
    private bool _isAutoTotemExpanded;
    private bool _isAutoSovereignRechargeExpanded;
    private bool _isHuntDetectExpanded;

    public FishingAddonsViewModel(
        AutoTotemViewModel autoTotemViewModel,
        AutoSovereignRechargeViewModel autoSovereignRechargeViewModel,
        HuntDetectViewModel? huntDetectViewModel = null)
    {
        _autoTotemViewModel = autoTotemViewModel;
        _autoSovereignRechargeViewModel = autoSovereignRechargeViewModel;
        _huntDetectViewModel = huntDetectViewModel ?? new HuntDetectViewModel();
        _isAutoTotemExpanded = false;
        _isAutoSovereignRechargeExpanded = false;
        _isHuntDetectExpanded = false;

        ToggleAutoTotemExpandedCommand = new RelayCommand(_ =>
        {
            if (IsAutoTotemExpanded)
            {
                IsAutoTotemExpanded = false;
            }
            else
            {
                IsAutoTotemExpanded = true;
                IsAutoSovereignRechargeExpanded = false;
                IsHuntDetectExpanded = false;
            }

            return Task.CompletedTask;
        });

        ToggleAutoSovereignRechargeExpandedCommand = new RelayCommand(_ =>
        {
            if (IsAutoSovereignRechargeExpanded)
            {
                IsAutoSovereignRechargeExpanded = false;
            }
            else
            {
                IsAutoSovereignRechargeExpanded = true;
                IsAutoTotemExpanded = false;
                IsHuntDetectExpanded = false;
            }

            return Task.CompletedTask;
        });

        ToggleHuntDetectExpandedCommand = new RelayCommand(_ =>
        {
            if (IsHuntDetectExpanded)
            {
                IsHuntDetectExpanded = false;
            }
            else
            {
                IsHuntDetectExpanded = true;
                IsAutoTotemExpanded = false;
                IsAutoSovereignRechargeExpanded = false;
            }

            return Task.CompletedTask;
        });

        _autoTotemViewModel.PropertyChanged += HandleTotemPropertyChanged;
        _autoSovereignRechargeViewModel.PropertyChanged += HandleSovereignRechargePropertyChanged;
        _huntDetectViewModel.PropertyChanged += HandleHuntDetectPropertyChanged;
    }

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

    public bool HuntDetectEnabled
    {
        get => _huntDetectViewModel.Enabled;
        set
        {
            if (_huntDetectViewModel.Enabled == value)
            {
                return;
            }

            _huntDetectViewModel.Enabled = value;
            OnPropertyChanged(nameof(HuntDetectEnabled));
        }
    }

    public HuntDetectViewModel HuntDetect => _huntDetectViewModel;

    public bool IsAutoTotemExpanded
    {
        get => _isAutoTotemExpanded;
        set
        {
            if (!SetProperty(ref _isAutoTotemExpanded, value))
            {
                return;
            }

            RaiseAddonVisibilityStateChanged();
            OnPropertyChanged(nameof(AutoTotemExpandGlyph));
        }
    }

    public bool IsAutoSovereignRechargeExpanded
    {
        get => _isAutoSovereignRechargeExpanded;
        set
        {
            if (!SetProperty(ref _isAutoSovereignRechargeExpanded, value))
            {
                return;
            }

            RaiseAddonVisibilityStateChanged();
            OnPropertyChanged(nameof(AutoSovereignRechargeExpandGlyph));
        }
    }

    public bool IsHuntDetectExpanded
    {
        get => _isHuntDetectExpanded;
        set
        {
            if (!SetProperty(ref _isHuntDetectExpanded, value))
            {
                return;
            }

            RaiseAddonVisibilityStateChanged();
            OnPropertyChanged(nameof(HuntDetectExpandGlyph));
        }
    }

    public bool HasOpenAddon => IsAutoTotemExpanded || IsAutoSovereignRechargeExpanded || IsHuntDetectExpanded;

    public bool ShowAutoTotemSection => IsAutoTotemExpanded || !HasOpenAddon;
    public bool ShowAutoSovereignRechargeSection => IsAutoSovereignRechargeExpanded || !HasOpenAddon;
    public bool ShowHuntDetectSection => IsHuntDetectExpanded || !HasOpenAddon;

    public string AutoTotemExpandGlyph => IsAutoTotemExpanded ? "Hide" : "Open";
    public string AutoSovereignRechargeExpandGlyph => IsAutoSovereignRechargeExpanded ? "Hide" : "Open";
    public string HuntDetectExpandGlyph => IsHuntDetectExpanded ? "Hide" : "Open";

    public string ActiveAddonTitle => IsAutoTotemExpanded
            ? "Fishing Add-ons - Auto Totem"
            : IsAutoSovereignRechargeExpanded
                ? "Fishing Add-ons - Auto Sovereign Recharge"
                : IsHuntDetectExpanded
                    ? "Fishing Add-ons - Hunt Detect"
            : "Fishing Add-ons";

    public RelayCommand ToggleAutoTotemExpandedCommand { get; }
    public RelayCommand ToggleAutoSovereignRechargeExpandedCommand { get; }
    public RelayCommand ToggleHuntDetectExpandedCommand { get; }

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

    private void HandleHuntDetectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(HuntDetectViewModel.Enabled))
        {
            OnPropertyChanged(nameof(HuntDetectEnabled));
        }
    }

    private void RaiseAddonVisibilityStateChanged()
    {
        OnPropertyChanged(nameof(HasOpenAddon));
        OnPropertyChanged(nameof(ShowAutoTotemSection));
        OnPropertyChanged(nameof(ShowAutoSovereignRechargeSection));
        OnPropertyChanged(nameof(ShowHuntDetectSection));
        OnPropertyChanged(nameof(ActiveAddonTitle));
    }
}
