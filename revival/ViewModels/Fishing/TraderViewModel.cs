using System;
using System.Collections.ObjectModel;
using Client.Services.Fishing;
using Client.Services;
using System.Linq;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using TimersTimer = System.Timers.Timer;

namespace Client.ViewModels;

public sealed class TraderViewModel : ViewModelBase
{
    private bool _traderEnabled;
    private bool _isRunning;
    private string _statusText = "Trader idle.";
    private readonly TraderAddressProbe _addressProbe = new();
    private readonly TimersTimer _pollTimer;
    private readonly TimersTimer _sequenceTimer;
    private string _searchName = string.Empty;
    private string _searchPrice = string.Empty;
    private TraderSearchEntryViewModel? _selectedEntry;
    private int _sequenceInProgress;
    private bool _awaitingConfirm;
    private ulong _pendingMatchAddress;
    private int _pendingMatchX;
    private int _pendingMatchY;
    private long _nextPendingMatchClickAt;
    private string _pendingMatchName = string.Empty;
    private string _pendingMatchPrice = string.Empty;

    public TraderViewModel()
    {
        _pollTimer = new TimersTimer(350);
        _pollTimer.AutoReset = true;
        _pollTimer.Elapsed += (_, _) => RefreshTraderStatus();
        _sequenceTimer = new TimersTimer(150);
        _sequenceTimer.AutoReset = true;
        _sequenceTimer.Elapsed += (_, _) => RunTraderSequence();
        SearchEntries = new ObservableCollection<TraderSearchEntryViewModel>();
        AddSearchEntryCommand = new RelayCommand(_ => AddSearchEntryAsync());
        RemoveSearchEntryCommand = new RelayCommand(RemoveSearchEntryAsync);
        SelectEntryCommand = new RelayCommand(SelectEntryAsync);
    }

    public bool TraderEnabled
    {
        get => _traderEnabled;
        set
        {
            if (!SetProperty(ref _traderEnabled, value))
            {
                return;
            }

            if (value)
            {
                RefreshTraderStatus();
                _pollTimer.Start();
                _sequenceTimer.Start();
            }
            else
            {
                _ = StopAsync();
                _pollTimer.Stop();
                _sequenceTimer.Stop();
            }
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SearchName
    {
        get => _searchName;
        set => SetProperty(ref _searchName, value);
    }

    public string SearchPrice
    {
        get => _searchPrice;
        set => SetProperty(ref _searchPrice, value);
    }

    public ObservableCollection<TraderSearchEntryViewModel> SearchEntries { get; }

    public TraderSearchEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    public bool HasSearchEntries => SearchEntries.Count > 0;

    public RelayCommand AddSearchEntryCommand { get; }

    public RelayCommand RemoveSearchEntryCommand { get; }

    public RelayCommand SelectEntryCommand { get; }

    public Task StartAsync()
    {
        if (!TraderEnabled)
        {
            StatusText = "---";
            return Task.CompletedTask;
        }

        if (!IsRunning)
        {
            IsRunning = true;
            _awaitingConfirm = false;
            _pendingMatchAddress = 0;
            _pendingMatchX = 0;
            _pendingMatchY = 0;
            _nextPendingMatchClickAt = 0;
            _pendingMatchName = string.Empty;
            _pendingMatchPrice = string.Empty;
            RefreshTraderStatus();
            _sequenceTimer.Start();
            OnPropertyChanged(nameof(IsRunning));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (IsRunning)
        {
            IsRunning = false;
            OnPropertyChanged(nameof(IsRunning));
        }

        _sequenceTimer.Stop();
        _pollTimer.Stop();
        _awaitingConfirm = false;
        _pendingMatchAddress = 0;
        _pendingMatchX = 0;
        _pendingMatchY = 0;
        _nextPendingMatchClickAt = 0;
        _pendingMatchName = string.Empty;
        _pendingMatchPrice = string.Empty;

        if (!TraderEnabled)
        {
            StatusText = "---";
        }

        return Task.CompletedTask;
    }

    public TraderSettingsSnapshot ExportSettings()
    {
        return new TraderSettingsSnapshot
        {
            Enabled = TraderEnabled,
            SearchEntries = SearchEntries
                .Select(entry => new TraderSearchEntrySnapshot
                {
                    Name = entry.Name,
                    Price = entry.Price,
                    IsSelected = entry.IsSelected,
                })
                .ToArray(),
        };
    }

    public void ImportSettings(TraderSettingsSnapshot? snapshot)
    {
        SearchEntries.Clear();
        if (snapshot?.SearchEntries is not { Length: > 0 } entries)
        {
            OnPropertyChanged(nameof(SearchEntries));
            OnPropertyChanged(nameof(HasSearchEntries));
            return;
        }

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) && string.IsNullOrWhiteSpace(entry.Price))
            {
                continue;
            }

            SearchEntries.Add(new TraderSearchEntryViewModel(entry.Name, entry.Price)
            {
                IsSelected = entry.IsSelected,
            });
        }

        OnPropertyChanged(nameof(SearchEntries));
        OnPropertyChanged(nameof(HasSearchEntries));
    }

    private Task AddSearchEntryAsync()
    {
        var name = (SearchName ?? string.Empty).Trim();
        var price = (SearchPrice ?? string.Empty).Trim();
        if (name.Length == 0 && price.Length == 0)
        {
            return Task.CompletedTask;
        }

        SearchEntries.Add(new TraderSearchEntryViewModel(name, price));
        SearchName = string.Empty;
        SearchPrice = string.Empty;
        OnPropertyChanged(nameof(SearchEntries));
        OnPropertyChanged(nameof(HasSearchEntries));
        return Task.CompletedTask;
    }

    private Task RemoveSearchEntryAsync(object? parameter)
    {
        if (parameter is TraderSearchEntryViewModel entry)
        {
            SearchEntries.Remove(entry);
            if (ReferenceEquals(SelectedEntry, entry))
            {
                SelectedEntry = null;
            }

            OnPropertyChanged(nameof(SearchEntries));
            OnPropertyChanged(nameof(HasSearchEntries));
        }

        return Task.CompletedTask;
    }

    private Task SelectEntryAsync(object? parameter)
    {
        if (parameter is TraderSearchEntryViewModel entry)
        {
            entry.IsSelected = !entry.IsSelected;
            SelectedEntry = entry.IsSelected ? entry : null;
            OnPropertyChanged(nameof(SearchEntries));
        }

        return Task.CompletedTask;
    }

    private void RefreshTraderStatus()
    {
        if (!TraderEnabled)
        {
            Dispatcher.UIThread.Post(() => StatusText = "---");
            return;
        }

        try
        {
            var snapshot = _addressProbe.Scan(BuildSelectedSpecs());
            var nextText = snapshot.IsWaiting
                ? "Waiting for UI to appear..."
                : snapshot.IsMatched && snapshot.Name is not null && snapshot.Price is not null
                    ? BuildMatchedStatus(snapshot.Name, snapshot.Price)
                    : BuildSnapshotStatus(snapshot.Items ?? _addressProbe.ReadCurrentItems().Items);

            Dispatcher.UIThread.Post(() => StatusText = nextText);
        }
        catch
        {
            Dispatcher.UIThread.Post(() => StatusText = "---");
        }
    }

    private void RunTraderSequence()
    {
        if (!TraderEnabled || !IsRunning)
        {
            return;
        }

        if (Interlocked.Exchange(ref _sequenceInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            AppLog.Info("TraderSeq", "Sequence tick entered.");
            if (!_addressProbe.TryReadContainerCenter(out var centerX, out var centerY))
            {
                AppLog.Info("TraderSeq", "Container center not ready; waiting for UI.");
                Dispatcher.UIThread.Post(() => StatusText = "Waiting for UI to appear...");
                return;
            }

            AppLog.Info("TraderSeq", $"Container center resolved at {centerX},{centerY}.");
            NativeMouse.MoveTo(centerX, centerY);
            NativeMouse.ScrollDownShort();

            var selected = BuildSelectedSpecs();
            AppLog.Info("TraderSeq", $"Selected specs count={selected.Length}.");
            if (selected.Length == 0)
            {
                AppLog.Info("TraderSeq", "No selected specs; refreshing snapshot only.");
                RefreshTraderStatus();
                return;
            }

            if (_awaitingConfirm)
            {
                if (_addressProbe.TryReadConfirmButtonCenter(out var confirmX, out var confirmY))
                {
                    AppLog.Info("TraderSeq", $"Confirm button resolved at {confirmX},{confirmY}; clicking.");
                    NativeMouse.ClickAt(confirmX, confirmY);
                    _awaitingConfirm = false;
                    _pendingMatchAddress = 0;
                    _pendingMatchX = 0;
                    _pendingMatchY = 0;
                    _nextPendingMatchClickAt = 0;
                    _pendingMatchName = string.Empty;
                    _pendingMatchPrice = string.Empty;
                    NativeMouse.MoveTo(centerX, centerY);
                    NativeMouse.ScrollDownShort();
                    RefreshTraderStatus();
                    Dispatcher.UIThread.Post(() => StatusText = "Confirmed.");
                    AppLog.Info("TraderSeq", $"Confirmation clicked; recentered at {centerX},{centerY} and resumed searching.");
                    return;
                }

                var now = Environment.TickCount64;
                if (_pendingMatchAddress != 0 && now >= _nextPendingMatchClickAt)
                {
                    AppLog.Info("TraderSeq", $"Popup not visible yet; re-clicking pending match addr=0x{_pendingMatchAddress:X} at {_pendingMatchX},{_pendingMatchY}.");
                    NativeMouse.ClickAt(_pendingMatchX, _pendingMatchY);
                    _nextPendingMatchClickAt = now + 120;
                    Dispatcher.UIThread.Post(() => StatusText = BuildMatchedStatus(_pendingMatchName, _pendingMatchPrice));
                    return;
                }

                AppLog.Info("TraderSeq", "Awaiting confirmation button.");
                Dispatcher.UIThread.Post(() => StatusText = "Waiting for popup...");
                return;
            }

            var scan = _addressProbe.Scan(selected);
            if (scan.IsWaiting)
            {
                AppLog.Info("TraderSeq", "Scan waiting on UI.");
                Dispatcher.UIThread.Post(() => StatusText = "Waiting for UI to appear...");
                return;
            }

            if (scan.IsMatched && scan.Name is not null && scan.Price is not null)
            {
                AppLog.Info("TraderSeq", $"Matched item '{scan.Name}' price={scan.Price} addr=0x{scan.MatchedAddress:X}.");
                AppLog.Info("TraderSeq", $"Matched item click target is the parent container at addr=0x{scan.MatchedAddress:X}.");
                if (_addressProbe.TryReadItemCenter(scan.MatchedAddress, out var itemX, out var itemY))
                {
                    AppLog.Info("TraderSeq", $"Matched item center resolved from parent bounds at {itemX},{itemY}.");
                    _pendingMatchAddress = scan.MatchedAddress;
                    _pendingMatchX = itemX;
                    _pendingMatchY = itemY;
                    _nextPendingMatchClickAt = Environment.TickCount64;
                    _pendingMatchName = scan.Name;
                    _pendingMatchPrice = scan.Price;
                    NativeMouse.ClickAt(itemX, itemY);
                    _awaitingConfirm = true;
                    Dispatcher.UIThread.Post(() => StatusText = BuildMatchedStatus(scan.Name, scan.Price));
                    AppLog.Info("TraderSeq", "Matched item clicked; waiting for popup.");
                    return;
                }
                else
                {
                    AppLog.Info("TraderSeq", $"Could not resolve center for parent item addr=0x{scan.MatchedAddress:X}.");
                }

                Dispatcher.UIThread.Post(() => StatusText = BuildMatchedStatus(scan.Name, scan.Price));
                StopTraderSequence();
                AppLog.Info("TraderSeq", "Matched item handled without click center; trader sequence stopped.");
                return;
            }

            var items = scan.Items ?? Array.Empty<TraderItemInfo>();
            AppLog.Info("TraderSeq", $"No match. Visible items={items.Count}.");
            Dispatcher.UIThread.Post(() => StatusText = BuildSnapshotStatus(items));
        }
        catch
        {
            AppLog.Info("TraderSeq", "Sequence tick failed; showing fallback status.");
            Dispatcher.UIThread.Post(() => StatusText = "---");
        }
        finally
        {
            Interlocked.Exchange(ref _sequenceInProgress, 0);
        }
    }

    private void StopTraderSequence()
    {
        if (IsRunning)
        {
            IsRunning = false;
            OnPropertyChanged(nameof(IsRunning));
        }

        _sequenceTimer.Stop();
        _pollTimer.Stop();
        _awaitingConfirm = false;
        _pendingMatchAddress = 0;
        _pendingMatchX = 0;
        _pendingMatchY = 0;
        _nextPendingMatchClickAt = 0;
        _pendingMatchName = string.Empty;
        _pendingMatchPrice = string.Empty;
        AppLog.Info("TraderSeq", "Sequence timers stopped.");
    }

    private static string BuildSnapshotStatus(TraderItemSnapshotResult snapshot)
    {
        if (snapshot.IsWaiting)
        {
            return "Waiting for UI to appear...";
        }

        var items = snapshot.Items;
        if (items.Count == 0)
        {
            return "---";
        }

        return BuildStatusText(items);
    }

    private static string BuildSnapshotStatus(IReadOnlyList<TraderItemInfo> items)
    {
        if (items.Count == 0)
        {
            return "---";
        }

        return BuildStatusText(items);
    }

    private static string BuildMatchedStatus(string name, string price)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "Matched:",
            $"Name: {name}",
            $"Price: {price}",
        });
    }

    private TraderSearchSpec[] BuildSelectedSpecs()
    {
        return SearchEntries
            .Select(entry => new TraderSearchSpec(entry.Name, entry.Price))
            .ToArray();
    }

    private static string BuildStatusText(IReadOnlyList<TraderItemInfo> items)
    {
        var lines = new List<string>();
        foreach (var item in items)
        {
            lines.Add($"Name: {item.Name}");
            lines.Add($"Price: {item.Price}");
            lines.Add(string.Empty);
        }

        if (lines.Count == 0)
        {
            return "---";
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join(Environment.NewLine, lines);
    }

}

public sealed class TraderSearchEntryViewModel : ViewModelBase
{
    private bool _isSelected;

    public TraderSearchEntryViewModel(string name, string price)
    {
        Name = name;
        Price = price;
    }

    public string Name { get; }
    public string Price { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
