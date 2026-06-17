using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Client.Services;
using Client.Services.Fishing;

namespace Client.ViewModels;

public sealed class FishingAddonsViewModel : ViewModelBase
{
    private static readonly string[] ReconnectButtonPath =
    [
        "RobloxPromptGui",
        "promptOverlay",
        "ErrorPrompt",
        "MessageArea",
        "ErrorFrame",
        "ButtonArea",
        "ReconnectButton",
    ];

    private static readonly string[] FriendBoostIconPath =
    [
        "hud",
        "safezone",
        "statuses",
        "FriendBoost",
        "Icon",
    ];

    private readonly FishingViewModel _fishingViewModel;
    private readonly AutoTotemViewModel _autoTotemViewModel;
    private readonly AutoSovereignRechargeViewModel _autoSovereignRechargeViewModel;
    private readonly HuntDetectViewModel _huntDetectViewModel;
    private readonly Timer _autoReconnectTimer;
    private readonly object _autoReconnectSync = new();
    private RobloxMemory? _autoReconnectMemory;
    private bool _isAutoAquariumExpanded;
    private bool _isAutoReconnectExpanded;
    private bool _isAutoTotemExpanded;
    private bool _isAutoSovereignRechargeExpanded;
    private bool _isHuntDetectExpanded;
    private bool _autoReconnectEnabled;
    private bool _autoReconnectTicking;
    private bool _isMysticMirrorTestRunning;
    private bool _isAutoReconnectButtonVisible;
    private string _autoReconnectStatusText = "Auto Reconnect off.";
    private string _mysticMirrorTestStatusText = "Ready.";
    private string _fishingLocationStatusText = "Fishing location not set.";
    private string _fishingLocationDirection = string.Empty;
    private int _fishingLocationX;
    private int _fishingLocationY;

    public FishingAddonsViewModel(
        FishingViewModel fishingViewModel,
        AutoTotemViewModel autoTotemViewModel,
        AutoSovereignRechargeViewModel autoSovereignRechargeViewModel,
        HuntDetectViewModel? huntDetectViewModel = null)
    {
        _fishingViewModel = fishingViewModel;
        _autoTotemViewModel = autoTotemViewModel;
        _autoSovereignRechargeViewModel = autoSovereignRechargeViewModel;
        _huntDetectViewModel = huntDetectViewModel ?? new HuntDetectViewModel();
        _isAutoAquariumExpanded = false;
        _isAutoReconnectExpanded = false;
        _isAutoTotemExpanded = false;
        _isAutoSovereignRechargeExpanded = false;
        _isHuntDetectExpanded = false;

        ToggleAutoAquariumExpandedCommand = new RelayCommand(_ =>
        {
            if (IsAutoAquariumExpanded)
            {
                IsAutoAquariumExpanded = false;
            }
            else
            {
                IsAutoAquariumExpanded = true;
                IsAutoTotemExpanded = false;
                IsAutoSovereignRechargeExpanded = false;
                IsHuntDetectExpanded = false;
            }

            return Task.CompletedTask;
        });

        ToggleAutoReconnectExpandedCommand = new RelayCommand(_ =>
        {
            if (IsAutoReconnectExpanded)
            {
                IsAutoReconnectExpanded = false;
            }
            else
            {
                IsAutoReconnectExpanded = true;
                IsAutoAquariumExpanded = false;
                IsAutoTotemExpanded = false;
                IsAutoSovereignRechargeExpanded = false;
                IsHuntDetectExpanded = false;
            }

            return Task.CompletedTask;
        });
        TestMysticMirrorSequenceCommand = new RelayCommand(_ => TestMysticMirrorSequenceAsync(), _ => !IsMysticMirrorTestRunning);
        SetFishingLocationCommand = new RelayCommand(_ => SetFishingLocationAsync());

        ToggleAutoTotemExpandedCommand = new RelayCommand(_ =>
        {
            if (IsAutoTotemExpanded)
            {
                IsAutoTotemExpanded = false;
            }
            else
            {
                IsAutoTotemExpanded = true;
                IsAutoAquariumExpanded = false;
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
                IsAutoAquariumExpanded = false;
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
                IsAutoAquariumExpanded = false;
                IsAutoTotemExpanded = false;
                IsAutoSovereignRechargeExpanded = false;
            }

            return Task.CompletedTask;
        });

        _fishingViewModel.PropertyChanged += HandleFishingPropertyChanged;
        _autoTotemViewModel.PropertyChanged += HandleTotemPropertyChanged;
        _autoSovereignRechargeViewModel.PropertyChanged += HandleSovereignRechargePropertyChanged;
        _huntDetectViewModel.PropertyChanged += HandleHuntDetectPropertyChanged;
        _autoReconnectTimer = new Timer(_ => RefreshAutoReconnectStatus(), null, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1));
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

    public bool IsAutoReconnectButtonVisible
    {
        get => _isAutoReconnectButtonVisible;
        private set => SetProperty(ref _isAutoReconnectButtonVisible, value);
    }

    public string AutoReconnectStatusText
    {
        get => _autoReconnectStatusText;
        private set => SetProperty(ref _autoReconnectStatusText, value);
    }

    public bool AutoReconnectEnabled
    {
        get => _autoReconnectEnabled;
        set
        {
            if (!SetProperty(ref _autoReconnectEnabled, value))
            {
                return;
            }

            if (!value)
            {
                IsAutoReconnectButtonVisible = false;
                AutoReconnectStatusText = "Auto Reconnect off.";
                _autoReconnectMemory?.Dispose();
                _autoReconnectMemory = null;
            }
            else
            {
                AutoReconnectStatusText = "Not visible";
            }
        }
    }

    public bool IsMysticMirrorTestRunning
    {
        get => _isMysticMirrorTestRunning;
        private set
        {
            if (!SetProperty(ref _isMysticMirrorTestRunning, value))
            {
                return;
            }

            TestMysticMirrorSequenceCommand.RaiseCanExecuteChanged();
        }
    }

    public string MysticMirrorTestStatusText
    {
        get => _mysticMirrorTestStatusText;
        private set => SetProperty(ref _mysticMirrorTestStatusText, value);
    }

    public string FishingLocationStatusText
    {
        get => _fishingLocationStatusText;
        private set => SetProperty(ref _fishingLocationStatusText, value);
    }

    public string FishingLocationDirection => _fishingLocationDirection;

    public int FishingLocationX => _fishingLocationX;

    public int FishingLocationY => _fishingLocationY;

    public AutoReconnectSettingsSnapshot ExportAutoReconnectSettings()
    {
        return new AutoReconnectSettingsSnapshot
        {
            Enabled = AutoReconnectEnabled,
            FishingLocationDirection = _fishingLocationDirection,
            FishingLocationX = _fishingLocationX,
            FishingLocationY = _fishingLocationY,
        };
    }

    public void RestoreAutoReconnectSettings(AutoReconnectSettingsSnapshot? settings)
    {
        if (settings is null)
        {
            return;
        }

        AutoReconnectEnabled = settings.Enabled;
        SetFishingLocation(settings.FishingLocationDirection ?? string.Empty, settings.FishingLocationX, settings.FishingLocationY);
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

    public double AutoAquariumPendingThresholdMinutes
    {
        get => _fishingViewModel.AutoAquariumPendingThresholdMinutes;
        set
        {
            if (_fishingViewModel.AutoAquariumPendingThresholdMinutes == value)
            {
                return;
            }

            _fishingViewModel.AutoAquariumPendingThresholdMinutes = value;
            OnPropertyChanged(nameof(AutoAquariumPendingThresholdMinutes));
        }
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

    public bool IsAutoAquariumExpanded
    {
        get => _isAutoAquariumExpanded;
        set
        {
            if (!SetProperty(ref _isAutoAquariumExpanded, value))
            {
                return;
            }

            RaiseAddonVisibilityStateChanged();
            OnPropertyChanged(nameof(AutoAquariumExpandGlyph));
        }
    }

    public bool IsAutoReconnectExpanded
    {
        get => _isAutoReconnectExpanded;
        set
        {
            if (!SetProperty(ref _isAutoReconnectExpanded, value))
            {
                return;
            }

            RaiseAddonVisibilityStateChanged();
            OnPropertyChanged(nameof(AutoReconnectExpandGlyph));
        }
    }

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

    public bool HasOpenAddon => IsAutoAquariumExpanded || IsAutoReconnectExpanded || IsAutoTotemExpanded || IsAutoSovereignRechargeExpanded || IsHuntDetectExpanded;

    public bool ShowAutoAquariumSection => IsAutoAquariumExpanded || !HasOpenAddon;
    public bool ShowAutoReconnectSection => IsAutoReconnectExpanded || !HasOpenAddon;

    public bool ShowAutoTotemSection => IsAutoTotemExpanded || !HasOpenAddon;
    public bool ShowAutoSovereignRechargeSection => IsAutoSovereignRechargeExpanded || !HasOpenAddon;
    public bool ShowHuntDetectSection => IsHuntDetectExpanded || !HasOpenAddon;

    public string AutoAquariumExpandGlyph => IsAutoAquariumExpanded ? "Hide" : "Open";
    public string AutoReconnectExpandGlyph => IsAutoReconnectExpanded ? "Hide" : "Open";

    public string AutoTotemExpandGlyph => IsAutoTotemExpanded ? "Hide" : "Open";
    public string AutoSovereignRechargeExpandGlyph => IsAutoSovereignRechargeExpanded ? "Hide" : "Open";
    public string HuntDetectExpandGlyph => IsHuntDetectExpanded ? "Hide" : "Open";

    public string ActiveAddonTitle => IsAutoAquariumExpanded
        ? "Fishing Add-ons - Auto Aquarium"
        : IsAutoReconnectExpanded
            ? "Fishing Add-ons - Auto Reconnect"
            : IsAutoTotemExpanded
            ? "Fishing Add-ons - Auto Totem"
            : IsAutoSovereignRechargeExpanded
                ? "Fishing Add-ons - Auto Sovereign Recharge"
                : IsHuntDetectExpanded
                    ? "Fishing Add-ons - Hunt Detect"
            : "Fishing Add-ons";

    public RelayCommand ToggleAutoAquariumExpandedCommand { get; }
    public RelayCommand ToggleAutoReconnectExpandedCommand { get; }
    public RelayCommand TestMysticMirrorSequenceCommand { get; }
    public RelayCommand SetFishingLocationCommand { get; }

    public RelayCommand ToggleAutoTotemExpandedCommand { get; }
    public RelayCommand ToggleAutoSovereignRechargeExpandedCommand { get; }
    public RelayCommand ToggleHuntDetectExpandedCommand { get; }

    private void HandleFishingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FishingViewModel.AutoAquariumEnabled))
        {
            OnPropertyChanged(nameof(AutoAquariumEnabled));
        }

        if (e.PropertyName is nameof(FishingViewModel.AutoAquariumCycleDelayMinutes))
        {
            OnPropertyChanged(nameof(AutoAquariumCycleDelayMinutes));
            return;
        }

        if (e.PropertyName is nameof(FishingViewModel.AutoAquariumPendingThresholdMinutes))
        {
            OnPropertyChanged(nameof(AutoAquariumPendingThresholdMinutes));
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
        OnPropertyChanged(nameof(ShowAutoAquariumSection));
        OnPropertyChanged(nameof(ShowAutoReconnectSection));
        OnPropertyChanged(nameof(ShowAutoTotemSection));
        OnPropertyChanged(nameof(ShowAutoSovereignRechargeSection));
        OnPropertyChanged(nameof(ShowHuntDetectSection));
        OnPropertyChanged(nameof(ActiveAddonTitle));
    }

    private void RefreshAutoReconnectStatus()
    {
        if (!AutoReconnectEnabled)
        {
            return;
        }

        lock (_autoReconnectSync)
        {
            if (_autoReconnectTicking)
            {
                return;
            }

            _autoReconnectTicking = true;
        }

        try
        {
            var (visible, status) = ProbeAutoReconnectButton();
            Dispatcher.UIThread.Post(() =>
            {
                IsAutoReconnectButtonVisible = visible;
                AutoReconnectStatusText = status;
            });
        }
        catch (Exception ex)
        {
            AppLog.Error("AutoReconnect", "Failed to refresh reconnect button status.", ex);
            Dispatcher.UIThread.Post(() =>
            {
                IsAutoReconnectButtonVisible = false;
                AutoReconnectStatusText = "Not visible";
            });

            _autoReconnectMemory?.Dispose();
            _autoReconnectMemory = null;
        }
        finally
        {
            lock (_autoReconnectSync)
            {
                _autoReconnectTicking = false;
            }
        }
    }

    private async Task TestMysticMirrorSequenceAsync()
    {
        if (IsMysticMirrorTestRunning)
        {
            return;
        }

        IsMysticMirrorTestRunning = true;
        MysticMirrorTestStatusText = "Testing Mystic Mirror sequence...";

        try
        {
            var result = await Task.Run(() =>
            {
                using var runner = new AutoReconnectMysticMirrorRunner(message =>
                    Dispatcher.UIThread.Post(() => MysticMirrorTestStatusText = message));
                return runner.RunOnce(_fishingLocationDirection, _fishingLocationX, _fishingLocationY);
            });

            MysticMirrorTestStatusText = result.Message;
        }
        catch (Exception ex)
        {
            AppLog.Error("AutoReconnect", "Mystic Mirror test sequence failed.", ex);
            MysticMirrorTestStatusText = "Mystic Mirror test failed.";
        }
        finally
        {
            IsMysticMirrorTestRunning = false;
        }
    }

    private async Task SetFishingLocationAsync()
    {
        try
        {
            var result = await Task.Run(FindClosestCompassDirection);
            if (result is null)
            {
                FishingLocationStatusText = "Compass direction not found.";
                return;
            }

            SetFishingLocation(result.Value.Direction, result.Value.X, result.Value.Y);
        }
        catch (Exception ex)
        {
            AppLog.Error("AutoReconnect", "Failed to set fishing location.", ex);
            FishingLocationStatusText = "Failed to set fishing location.";
        }
    }

    private CompassLocation? FindClosestCompassDirection()
    {
        _autoReconnectMemory ??= new RobloxMemory(OffsetsSourceProvider.Current);
        _autoReconnectMemory.EnsureAttached();

        var playerGui = _autoReconnectMemory.FindPlayerGui();
        if (playerGui == 0)
        {
            return null;
        }

        var compass = FindCompassFrame(playerGui);
        if (compass == 0)
        {
            return null;
        }

        var screenBounds = GetScreenBounds();
        if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
        {
            return null;
        }

        var screenMiddleX = screenBounds.X + screenBounds.Width / 2.0;
        var screenMiddleY = screenBounds.Y + screenBounds.Height / 2.0;
        CompassLocation? best = null;
        var bestDistance = double.MaxValue;

        foreach (var child in _autoReconnectMemory.ReadChildren(compass))
        {
            if (!string.Equals(_autoReconnectMemory.ReadClass(child), "TextLabel", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var direction = (_autoReconnectMemory.ReadGuiText(child) ?? string.Empty).Trim();
            if (!IsCompassDirection(direction))
            {
                continue;
            }

            var bounds = _autoReconnectMemory.ReadGuiBounds(child, true);
            if (bounds is null)
            {
                continue;
            }

            var x = (int)Math.Round(bounds.Value.X + bounds.Value.Width * 0.5f);
            var y = (int)Math.Round(bounds.Value.Y + bounds.Value.Height * 0.5f);
            var distance = Math.Sqrt(Math.Pow(x - screenMiddleX, 2) + Math.Pow(y - screenMiddleY, 2));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = new CompassLocation(direction, x, y);
            }
        }

        return best;
    }

    private void SetFishingLocation(string direction, int x, int y)
    {
        _fishingLocationDirection = direction;
        _fishingLocationX = x;
        _fishingLocationY = y;
        OnPropertyChanged(nameof(FishingLocationDirection));
        OnPropertyChanged(nameof(FishingLocationX));
        OnPropertyChanged(nameof(FishingLocationY));
        FishingLocationStatusText = string.IsNullOrWhiteSpace(direction) || x == 0 || y == 0
            ? "Fishing location not set."
            : $"{direction} saved at {x}, {y}.";
    }

    private ulong FindCompassFrame(ulong playerGui)
    {
        if (_autoReconnectMemory is null)
        {
            return 0;
        }

        var hud = _autoReconnectMemory.FindChildByName(playerGui, "hud");
        var safezone = hud == 0 ? 0 : _autoReconnectMemory.FindDescendantByName(hud, "safezone");
        var compassRoot = safezone == 0 ? 0 : _autoReconnectMemory.FindChildByName(safezone, "compass");
        return compassRoot == 0 ? 0 : _autoReconnectMemory.FindChildByName(compassRoot, "Compass");
    }

    private static bool IsCompassDirection(string text)
    {
        return text is "N" or "S" or "E" or "W" or "NE" or "NW" or "SE" or "SW";
    }

    private static ClientRectangle GetScreenBounds()
    {
        return new ClientRectangle(
            GetSystemMetrics(SystemMetricVirtualScreenX),
            GetSystemMetrics(SystemMetricVirtualScreenY),
            GetSystemMetrics(SystemMetricVirtualScreenWidth),
            GetSystemMetrics(SystemMetricVirtualScreenHeight));
    }

    private (bool Visible, string Status) ProbeAutoReconnectButton()
    {
        _autoReconnectMemory ??= new RobloxMemory(OffsetsSourceProvider.Current);
        _autoReconnectMemory.EnsureAttached();

        var dataModel = _autoReconnectMemory.GetDataModel();
        if (dataModel == 0)
        {
            return (false, "Not visible");
        }

        if (TryFindVisibleCoreGuiPath(dataModel, ReconnectButtonPath, out var reconnectStatus))
        {
            return (true, reconnectStatus);
        }

        var localPlayer = _autoReconnectMemory.GetLocalPlayer();
        if (localPlayer == 0)
        {
            return (false, "Not visible");
        }

        var playerGui = _autoReconnectMemory.FindChildByClass(localPlayer, "PlayerGui");
        if (playerGui == 0)
        {
            return (false, "Not visible");
        }

        if (TryFindVisiblePlayerGuiPath(playerGui, FriendBoostIconPath, out var friendBoostStatus))
        {
            return (true, friendBoostStatus);
        }

        return (false, "Not visible");
    }

    private bool TryFindVisibleCoreGuiPath(ulong dataModel, IReadOnlyList<string> path, out string status)
    {
        status = string.Empty;
        if (_autoReconnectMemory is null)
        {
            return false;
        }

        var current = _autoReconnectMemory.FindDescendantByClass(dataModel, "CoreGui");
        if (current == 0)
        {
            return false;
        }

        return TryFindVisiblePath(current, path, "ReconnectButton", out status);
    }

    private bool TryFindVisiblePlayerGuiPath(ulong playerGui, IReadOnlyList<string> path, out string status)
    {
        return TryFindVisiblePath(playerGui, path, "FriendBoost Icon", out status);
    }

    private bool TryFindVisiblePath(ulong root, IReadOnlyList<string> path, string label, out string status)
    {
        status = string.Empty;
        if (_autoReconnectMemory is null)
        {
            return false;
        }

        var current = root;
        foreach (var segment in path)
        {
            current = _autoReconnectMemory.FindChildByName(current, segment);
            if (current == 0)
            {
                return false;
            }
        }

        if (_autoReconnectMemory.ReadGuiBounds(current, true) is null)
        {
            return false;
        }

        status = $"{label} - visible";
        return true;
    }

    private readonly record struct CompassLocation(string Direction, int X, int Y);

    private readonly record struct ClientRectangle(int X, int Y, int Width, int Height);

    private const int SystemMetricVirtualScreenX = 76;
    private const int SystemMetricVirtualScreenY = 77;
    private const int SystemMetricVirtualScreenWidth = 78;
    private const int SystemMetricVirtualScreenHeight = 79;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
