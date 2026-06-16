using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Client.Services;
using Client.Services.Fishing;

namespace Client.ViewModels;

public sealed class GeneralViewModel : ViewModelBase
{
    private const int StopDrainIntervalMs = 90;
    private const int StopDrainMaxMs = 5000;
    private const int StopHotkeyLatchMs = 1800;
    private readonly FishingViewModel _fishing;
    private readonly EnchantViewModel _enchant;
    private readonly AppraiseViewModel _appraise;
    private readonly TreasureAppraiseViewModel _treasureAppraise;
    private readonly AutoAnglerViewModel _autoAngler;
    private readonly FishingAddonsViewModel _fishingAddons;
    private Key _startStopHotkey = Key.F3;
    private bool _isRebindingHotkey;
    private int _selectedRodSlot = 1;
    private int _toggleInProgress;
    private int _stopDrainInProgress;
    private long _stopHotkeyLatchUntilTick;
    private DateTimeOffset? _macroStartedAt;
    private bool _lastObservedRunning;

    public GeneralViewModel(
        FishingViewModel fishing,
        EnchantViewModel enchant,
        AppraiseViewModel appraise,
        TreasureAppraiseViewModel treasureAppraise,
        AutoAnglerViewModel autoAngler,
        FishingAddonsViewModel fishingAddons)
    {
        _fishing = fishing;
        _enchant = enchant;
        _appraise = appraise;
        _treasureAppraise = treasureAppraise;
        _autoAngler = autoAngler;
        _fishingAddons = fishingAddons;
        RodSlots = new ObservableCollection<int>([1, 2, 3, 4, 5, 6, 7, 8, 9]);
        SelectedRodSlot = HotbarSlotSettings.RodSlot;
        ToggleMacroCommand = new RelayCommand(_ => ToggleMacroAsync());
        RebindHotkeyCommand = new RelayCommand(_ => BeginHotkeyRebindAsync());
        _fishing.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(FishingViewModel.IsRunning) or
                nameof(FishingViewModel.StatusMessage))
            {
                RaiseFishingStateChanged();
            }
        };
        _enchant.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(EnchantViewModel.IsRunning) or
                nameof(EnchantViewModel.StatusText) or
                nameof(EnchantViewModel.AutoEnchantEnabled))
            {
                RaiseFishingStateChanged();
            }
        };
        _appraise.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AppraiseViewModel.IsRunning) or
                nameof(AppraiseViewModel.StatusText) or
                nameof(AppraiseViewModel.AutoAppraiseEnabled))
            {
                RaiseFishingStateChanged();
            }
        };
        _treasureAppraise.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TreasureAppraiseViewModel.IsRunning) or
                nameof(TreasureAppraiseViewModel.StatusText) or
                nameof(TreasureAppraiseViewModel.AutoTreasureEnabled))
            {
                RaiseFishingStateChanged();
            }
        };
        _autoAngler.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AutoAnglerViewModel.IsRunning) or
                nameof(AutoAnglerViewModel.StatusText) or
                nameof(AutoAnglerViewModel.AutoAnglerEnabled))
            {
                RaiseFishingStateChanged();
            }
        };
    }

    public bool IsRunning => _enchant.AutoEnchantEnabled
        ? _enchant.IsRunning
        : _appraise.AutoAppraiseEnabled
            ? _appraise.IsRunning
            : _treasureAppraise.AutoTreasureEnabled
                ? _treasureAppraise.IsRunning
                : _autoAngler.AutoAnglerEnabled
                    ? _autoAngler.IsRunning
                    : _fishing.IsRunning;

    public string StatusMessage => _enchant.AutoEnchantEnabled
        ? _enchant.StatusText
        : _appraise.AutoAppraiseEnabled
            ? _appraise.StatusText
            : _treasureAppraise.AutoTreasureEnabled
                ? _treasureAppraise.StatusText
                : _autoAngler.AutoAnglerEnabled
                    ? _autoAngler.StatusText
                    : _fishing.StatusMessage;

    public string RunStateText => IsRunning ? "Running" : "Stopped";

    /// <summary>
    /// Timestamp when the macro most recently transitioned from stopped → running.
    /// Null while stopped. Used by the compact-mode view to render session duration.
    /// </summary>
    public DateTimeOffset? MacroStartedAt
    {
        get => _macroStartedAt;
        private set => SetProperty(ref _macroStartedAt, value);
    }

    public string StartStopButtonText => IsRunning ? "Stop" : "Start";

    public string ActiveMacroText => _enchant.AutoEnchantEnabled
        ? "Auto Enchant"
        : _appraise.AutoAppraiseEnabled
            ? "Appraise"
            : _treasureAppraise.AutoTreasureEnabled
                ? "Treasure Appraise"
                : _autoAngler.AutoAnglerEnabled
                    ? "Auto Angler"
                    : "Fishing";

    public string HotkeyText => StartStopHotkey.ToString();

    public string RebindButtonText => IsRebindingHotkey ? "Press a key" : "Rebind";

    public ObservableCollection<int> RodSlots { get; }

    public FishingViewModel Fishing => _fishing;

    public FishingAddonsViewModel FishingAddons => _fishingAddons;

    public int SelectedRodSlot
    {
        get => _selectedRodSlot;
        set
        {
            var clamped = Math.Clamp(value, 1, 9);
            if (!SetProperty(ref _selectedRodSlot, clamped))
            {
                return;
            }

            HotbarSlotSettings.RodSlot = clamped;
            OnPropertyChanged(nameof(RodSlotText));
        }
    }

    public string RodSlotText
    {
        get => SelectedRodSlot.ToString();
        set
        {
            if (int.TryParse(value, out var slot))
            {
                SelectedRodSlot = Math.Clamp(slot, 1, 9);
            }
        }
    }

    public Key StartStopHotkey
    {
        get => _startStopHotkey;
        set
        {
            if (SetProperty(ref _startStopHotkey, value))
            {
                OnPropertyChanged(nameof(HotkeyText));
            }
        }
    }

    public bool IsRebindingHotkey
    {
        get => _isRebindingHotkey;
        private set
        {
            if (SetProperty(ref _isRebindingHotkey, value))
            {
                OnPropertyChanged(nameof(RebindButtonText));
            }
        }
    }

    public RelayCommand ToggleMacroCommand { get; }

    public RelayCommand RebindHotkeyCommand { get; }

    public bool HandleKey(Key key)
    {
        if (key == Key.None)
        {
            return false;
        }

        if (IsRebindingHotkey)
        {
            StartStopHotkey = key;
            IsRebindingHotkey = false;
            return true;
        }

        return false;
    }

    public async Task ToggleMacroAsync()
    {
        AppLog.Info("MacroToggle", $"ToggleMacroAsync ENTER running={IsRunning} active={ActiveMacroText} stopDrain={Volatile.Read(ref _stopDrainInProgress)}");
        if (Interlocked.Exchange(ref _toggleInProgress, 1) == 1)
        {
            AppLog.Info("MacroToggle", "ToggleMacroAsync suppressed: toggle already in progress.");
            return;
        }
        try
        {
            if (ShouldForceStopOnToggle())
            {
                AppLog.Info("MacroToggle", "ToggleMacroAsync forcing stop drain.");
                await DrainStopUntilStoppedAsync();
                return;
            }

            if (Volatile.Read(ref _stopDrainInProgress) == 1)
            {
                AppLog.Info("MacroToggle", "ToggleMacroAsync suppressed: stop drain already active.");
                return;
            }

            if (_enchant.AutoEnchantEnabled)
            {
                AppLog.Info("MacroToggle", "Starting Auto Enchant.");
                await _enchant.StartAsync();
                return;
            }

            if (_appraise.AutoAppraiseEnabled)
            {
                AppLog.Info("MacroToggle", "Starting Appraise.");
                await _appraise.StartAsync();
                return;
            }

            if (_treasureAppraise.AutoTreasureEnabled)
            {
                AppLog.Info("MacroToggle", "Starting Treasure Appraise.");
                await _treasureAppraise.StartAsync();
                return;
            }

            if (_autoAngler.AutoAnglerEnabled)
            {
                AppLog.Info("MacroToggle", "Starting Auto Angler.");
                await _autoAngler.StartAsync();
                return;
            }

            AppLog.Info("MacroToggle", "Starting Fishing.");
            await _fishing.StartAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _toggleInProgress, 0);
            AppLog.Info("MacroToggle", $"ToggleMacroAsync EXIT running={IsRunning} active={ActiveMacroText}");
        }
    }

    public async Task ToggleMacroFromHotkeyAsync()
    {
        AppLog.Info("HotkeyToggle", $"ToggleMacroFromHotkeyAsync ENTER running={IsRunning} active={ActiveMacroText}");
        if (Interlocked.Exchange(ref _toggleInProgress, 1) == 1)
        {
            AppLog.Info("HotkeyToggle", "Suppressed: toggle already in progress.");
            return;
        }

        try
        {
            AppLog.Info(
                "HotkeyToggle",
                $"Enter: shouldStop={ShouldForceStopOnToggle()}, phase={_fishing.Phase}, " +
                $"runningFlags=f:{_fishing.IsRunning},e:{_enchant.IsRunning},a:{_appraise.IsRunning},t:{_treasureAppraise.IsRunning},g:{_autoAngler.IsRunning}");

            var nowTick = Environment.TickCount64;
            if (nowTick < Volatile.Read(ref _stopHotkeyLatchUntilTick))
            {
                AppLog.Info("HotkeyToggle", $"Action: stop latch active ({Volatile.Read(ref _stopHotkeyLatchUntilTick) - nowTick}ms left), forcing drain stop.");
                await DrainStopUntilStoppedAsync();
                return;
            }

            // Hotkey semantics are strict toggle:
            // one press while running => stop, one press while stopped => start.
            if (ShouldForceStopOnToggle())
            {
                Volatile.Write(ref _stopHotkeyLatchUntilTick, Environment.TickCount64 + StopHotkeyLatchMs);
                AppLog.Info("HotkeyToggle", "Action: drain stop (running detected).");
                await DrainStopUntilStoppedAsync();
                return;
            }

            AppLog.Info("HotkeyToggle", "Action: start selected automation.");

            if (_enchant.AutoEnchantEnabled)
            {
                AppLog.Info("HotkeyToggle", "Starting Auto Enchant.");
                await _enchant.StartAsync();
                return;
            }

            if (_appraise.AutoAppraiseEnabled)
            {
                AppLog.Info("HotkeyToggle", "Starting Appraise.");
                await _appraise.StartAsync();
                return;
            }

            if (_treasureAppraise.AutoTreasureEnabled)
            {
                AppLog.Info("HotkeyToggle", "Starting Treasure Appraise.");
                await _treasureAppraise.StartAsync();
                return;
            }

            if (_autoAngler.AutoAnglerEnabled)
            {
                AppLog.Info("HotkeyToggle", "Starting Auto Angler.");
                await _autoAngler.StartAsync();
                return;
            }

            AppLog.Info("HotkeyToggle", "Starting Fishing.");
            await _fishing.StartAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _toggleInProgress, 0);
            AppLog.Info("HotkeyToggle", $"ToggleMacroFromHotkeyAsync EXIT running={IsRunning} active={ActiveMacroText}");
        }
    }

    private async Task DrainStopUntilStoppedAsync()
    {
        if (Interlocked.Exchange(ref _stopDrainInProgress, 1) == 1)
        {
            AppLog.Info("HotkeyToggle", "Suppressed: stop drain already in progress.");
            return;
        }

        try
        {
            var deadline = DateTimeOffset.UtcNow.AddMilliseconds(StopDrainMaxMs);
            var pulses = 0;
            do
            {
                await StopAllAsync();
                pulses++;
                if (!ShouldForceStopOnToggle())
                {
                    AppLog.Info("HotkeyToggle", $"Drain complete after {pulses} pulses.");
                    break;
                }

                await Task.Delay(StopDrainIntervalMs);
            } while (DateTimeOffset.UtcNow < deadline);

            // Final stop pulse at the end of the drain window.
            await StopAllAsync();
            AppLog.Info("HotkeyToggle", $"Drain final pulse sent. pulses={pulses}");
        }
        finally
        {
            Volatile.Write(ref _stopDrainInProgress, 0);
        }
    }

    /// <summary>
    /// Stops all running automation immediately. Safe to call when none are running.
    /// </summary>
    public async Task StopAllAsync()
    {
        AppLog.Info(
            "HotkeyToggle",
            $"StopAll begin. phase={_fishing.Phase}, runningFlags=f:{_fishing.IsRunning},e:{_enchant.IsRunning},a:{_appraise.IsRunning},t:{_treasureAppraise.IsRunning},g:{_autoAngler.IsRunning}");

        // Always issue stop commands. Some failure modes can leave state flags
        // stale while background loops are still active.
        AppLog.Info("HotkeyToggle", "Stopping Fishing.");
        await _fishing.StopAsync();
        AppLog.Info("HotkeyToggle", "Stopping Enchant.");
        await _enchant.StopAsync();
        AppLog.Info("HotkeyToggle", "Stopping Appraise.");
        await _appraise.StopAsync();
        AppLog.Info("HotkeyToggle", "Stopping Treasure Appraise.");
        await _treasureAppraise.StopAsync();
        AppLog.Info("HotkeyToggle", "Stopping Auto Angler.");
        await _autoAngler.StopAsync();

        AppLog.Info(
            "HotkeyToggle",
            $"StopAll end. phase={_fishing.Phase}, runningFlags=f:{_fishing.IsRunning},e:{_enchant.IsRunning},a:{_appraise.IsRunning},t:{_treasureAppraise.IsRunning},g:{_autoAngler.IsRunning}");
    }

    private bool AnyMacroRunning()
    {
        return _fishing.IsRunning ||
            _enchant.IsRunning ||
            _appraise.IsRunning ||
            _treasureAppraise.IsRunning ||
            _autoAngler.IsRunning;
    }

    private bool ShouldForceStopOnToggle()
    {
        if (AnyMacroRunning())
        {
            return true;
        }

        // Treat non-idle fishing phases as running signals even if a status
        // flag briefly reports false.
        return !string.Equals(_fishing.Phase, "---", StringComparison.Ordinal);
    }

    private Task BeginHotkeyRebindAsync()
    {
        IsRebindingHotkey = true;
        return Task.CompletedTask;
    }

    private void RaiseFishingStateChanged()
    {
        var nowRunning = IsRunning;
        if (nowRunning != _lastObservedRunning)
        {
            AppLog.Info("MacroState", $"Running changed { _lastObservedRunning } -> {nowRunning}; active={ActiveMacroText}; status={StatusMessage}");
            _lastObservedRunning = nowRunning;
            MacroStartedAt = nowRunning ? DateTimeOffset.UtcNow : null;
        }

        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(RunStateText));
        OnPropertyChanged(nameof(StartStopButtonText));
        OnPropertyChanged(nameof(ActiveMacroText));
    }
}
