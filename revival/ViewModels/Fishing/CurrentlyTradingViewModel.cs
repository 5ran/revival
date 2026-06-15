using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Client.Services.Fishing;

namespace Client.ViewModels;

public sealed class CurrentlyTradingViewModel : ViewModelBase, IDisposable
{
    private readonly CurrentlyTradingProbe _probe = new();
    private readonly Timer _pollTimer;
    private bool _enabled;
    private string _statusText = "Currently Trading idle.";
    private string _searchQuery = string.Empty;
    private int _lastSalesBoothCount;
    private readonly Dictionary<string, CurrentlyTradingEntryViewModel> _allEntriesByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<CurrentlyTradingEntryViewModel> _filteredEntries = new();
    private IReadOnlyList<CurrentlyTradingEntryViewModel> _latestEntries = Array.Empty<CurrentlyTradingEntryViewModel>();

    public CurrentlyTradingViewModel()
    {
        _pollTimer = new Timer(_ => RefreshStatus(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        RefreshCommand = new RelayCommand(_ =>
        {
            RefreshStatus();
            return Task.CompletedTask;
        });
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (!SetProperty(ref _enabled, value))
            {
                return;
            }

            if (value)
            {
                RefreshStatus();
                _pollTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(350));
            }
            else
            {
                _pollTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                StatusText = "Currently Trading off.";
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!SetProperty(ref _searchQuery, value))
            {
                return;
            }

            ApplyFilter(_latestEntries);
        }
    }

    public int SalesBoothCount => _lastSalesBoothCount;

    public ObservableCollection<CurrentlyTradingEntryViewModel> FilteredEntries => _filteredEntries;

    public RelayCommand RefreshCommand { get; }

    private void RefreshStatus()
    {
        if (!Enabled)
        {
            Dispatcher.UIThread.Post(() => StatusText = "Currently Trading off.");
            return;
        }

        try
        {
            var snapshot = _probe.Read();
            if (snapshot.IsWaiting)
            {
                Dispatcher.UIThread.Post(() => StatusText = "Waiting for PlayerGui...");
                return;
            }

            var entries = new List<CurrentlyTradingEntryViewModel>();
            foreach (var entry in snapshot.Entries)
            {
                var key = BuildEntryKey(entry.BoothName, entry.SlotName);
                if (!_allEntriesByKey.TryGetValue(key, out var viewModel))
                {
                    viewModel = new CurrentlyTradingEntryViewModel(key, entry.BoothName, entry.SlotName, entry.Name, entry.Offer);
                    _allEntriesByKey[key] = viewModel;
                }
                else
                {
                    viewModel.Update(entry.BoothName, entry.SlotName, entry.Name, entry.Offer);
                }

                entries.Add(viewModel);
            }

            _lastSalesBoothCount = snapshot.SalesBoothCount;
            Dispatcher.UIThread.Post(() =>
            {
                _latestEntries = entries;
                ApplyFilter(entries);
                StatusText = $"SalesBooths: {snapshot.SalesBoothCount}  Entries: {entries.Count}";
                OnPropertyChanged(nameof(SalesBoothCount));
            });
        }
        catch
        {
            Dispatcher.UIThread.Post(() => StatusText = "---");
        }
    }

    private void ApplyFilter(IReadOnlyList<CurrentlyTradingEntryViewModel> sourceEntries)
    {
        var query = (SearchQuery ?? string.Empty).Trim();
        if (query.Length == 0)
        {
            SyncCollection(sourceEntries, _filteredEntries);
            return;
        }

        var filtered = new List<CurrentlyTradingEntryViewModel>();
        foreach (var entry in sourceEntries)
        {
            if (entry.Matches(query))
            {
                filtered.Add(entry);
            }
        }

        SyncCollection(filtered, _filteredEntries);
    }

    private static void SyncCollection(IReadOnlyList<CurrentlyTradingEntryViewModel> desired, ObservableCollection<CurrentlyTradingEntryViewModel> target)
    {
        var desiredIndex = 0;
        while (desiredIndex < desired.Count)
        {
            if (desiredIndex >= target.Count)
            {
                target.Insert(desiredIndex, desired[desiredIndex]);
                desiredIndex++;
                continue;
            }

            if (!ReferenceEquals(target[desiredIndex], desired[desiredIndex]))
            {
                var currentIndex = -1;
                for (var i = desiredIndex + 1; i < target.Count; i++)
                {
                    if (ReferenceEquals(target[i], desired[desiredIndex]))
                    {
                        currentIndex = i;
                        break;
                    }
                }

                if (currentIndex >= 0)
                {
                    target.Move(currentIndex, desiredIndex);
                }
                else
                {
                    target.Insert(desiredIndex, desired[desiredIndex]);
                }
            }

            desiredIndex++;
        }

        while (target.Count > desired.Count)
        {
            target.RemoveAt(target.Count - 1);
        }
    }

    private static string BuildEntryKey(string boothName, string slotName)
    {
        return string.Join("|", boothName ?? string.Empty, slotName ?? string.Empty);
    }

    public void Dispose()
    {
        _pollTimer.Dispose();
        _probe.Dispose();
    }
}

public sealed class CurrentlyTradingEntryViewModel : ViewModelBase
{
    private string _boothName = string.Empty;
    private string _slotName = string.Empty;
    private string _name = string.Empty;
    private string _offer = string.Empty;

    public CurrentlyTradingEntryViewModel(string key, string boothName, string slotName, string name, string offer)
    {
        Key = key;
        BoothName = boothName;
        SlotName = slotName;
        Name = name;
        Offer = offer;
    }

    public string Key { get; }

    public string BoothName
    {
        get => _boothName;
        private set => SetProperty(ref _boothName, value);
    }

    public string SlotName
    {
        get => _slotName;
        private set => SetProperty(ref _slotName, value);
    }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public string Offer
    {
        get => _offer;
        private set => SetProperty(ref _offer, value);
    }

    public string Summary => $"{SlotName}: {Name}  {Offer}";

    public void Update(string boothName, string slotName, string name, string offer)
    {
        BoothName = boothName;
        SlotName = slotName;
        Name = name;
        Offer = offer;
    }

    public bool Matches(string query)
    {
        return Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
            || BoothName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Offer.Contains(query, StringComparison.OrdinalIgnoreCase)
            || SlotName.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
