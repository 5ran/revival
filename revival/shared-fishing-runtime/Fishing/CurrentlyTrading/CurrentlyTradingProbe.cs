using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Services.Fishing;

internal sealed class CurrentlyTradingProbe : IDisposable
{
    private readonly RobloxMemory _memory = new(OffsetsSourceProvider.Current);
    private readonly HashSet<ulong> _seen = new();

    public CurrentlyTradingSnapshot Read()
    {
        _memory.EnsureAttached();

        var playerGui = _memory.FindPlayerGui();
        if (playerGui == 0)
        {
            return CurrentlyTradingSnapshot.Waiting;
        }

        var booths = FindSalesBooths(playerGui).ToArray();
        if (booths.Length == 0)
        {
            return CurrentlyTradingSnapshot.WithEntries(Array.Empty<CurrentlyTradingEntrySnapshot>());
        }

        var entries = new List<CurrentlyTradingEntrySnapshot>();
        foreach (var booth in booths)
        {
            var boothName = _memory.ReadName(booth);
            var boothEntries = ReadBoothEntries(booth, boothName);
            entries.AddRange(boothEntries);
        }

        return CurrentlyTradingSnapshot.WithEntries(entries);
    }

    public void Dispose()
    {
        _memory.Dispose();
    }

    private IEnumerable<ulong> FindSalesBooths(ulong root)
    {
        foreach (var item in Traverse(root, 256))
        {
            var name = _memory.ReadName(item);
            if (!string.IsNullOrWhiteSpace(name) && name.StartsWith("SalesBooth", StringComparison.OrdinalIgnoreCase))
            {
                yield return item;
            }
        }
    }

    private IReadOnlyList<CurrentlyTradingEntrySnapshot> ReadBoothEntries(ulong booth, string boothName)
    {
        var scrollingFrame = FindScrollingFrame(booth);
        if (scrollingFrame == 0)
        {
            return Array.Empty<CurrentlyTradingEntrySnapshot>();
        }

        var entries = new List<CurrentlyTradingEntrySnapshot>();
        foreach (var child in _memory.ReadChildren(scrollingFrame))
        {
            if (string.Equals(_memory.ReadClass(child), "UIGridLayout", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var slotName = _memory.ReadName(child).Trim();
            if (!IsNumericSlot(slotName))
            {
                continue;
            }

            var name = ReadEntryName(child);
            var offer = ReadEntryOffer(child);
            entries.Add(new CurrentlyTradingEntrySnapshot(boothName, slotName, name, offer));
        }

        return entries;
    }

    private ulong FindScrollingFrame(ulong booth)
    {
        foreach (var item in Traverse(booth, 32))
        {
            if (!string.Equals(_memory.ReadClass(item), "ScrollingFrame", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = _memory.ReadName(item);
            if (string.Equals(name, "ScrollingFrame", StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return 0;
    }

    private string ReadEntryName(ulong entryRoot)
    {
        var header = FindTextByName(entryRoot, "Header");
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header;
        }

        foreach (var item in Traverse(entryRoot, 16))
        {
            if (!string.Equals(_memory.ReadClass(item), "TextLabel", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = _memory.ReadGuiText(item).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (IsOfferText(text))
            {
                continue;
            }

            return text;
        }

        return "(unknown)";
    }

    private string ReadEntryOffer(ulong entryRoot)
    {
        var label = FindTextByName(entryRoot, "Label");
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        foreach (var item in Traverse(entryRoot, 16))
        {
            var text = _memory.ReadGuiText(item).Trim();
            if (IsOfferText(text))
            {
                return text;
            }
        }

        return "(unknown)";
    }

    private string FindTextByName(ulong root, string name)
    {
        foreach (var item in Traverse(root, 16))
        {
            if (!string.Equals(_memory.ReadName(item), name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = _memory.ReadGuiText(item).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static bool IsNumericSlot(string value)
    {
        return value.Length > 0 && value.All(char.IsDigit);
    }

    private static bool IsOfferText(string text)
    {
        return text.Contains("Offer", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<ulong> Traverse(ulong root, int maxDepth)
    {
        _seen.Clear();
        var queue = new Queue<(ulong Address, int Depth)>();
        queue.Enqueue((root, 0));

        while (queue.Count > 0)
        {
            var (address, depth) = queue.Dequeue();
            if (!_seen.Add(address))
            {
                continue;
            }

            yield return address;
            if (depth >= maxDepth)
            {
                continue;
            }

            foreach (var child in _memory.ReadChildren(address))
            {
                queue.Enqueue((child, depth + 1));
            }
        }
    }
}

internal readonly record struct CurrentlyTradingSnapshot(bool IsWaiting, IReadOnlyList<CurrentlyTradingEntrySnapshot> Entries)
{
    public static CurrentlyTradingSnapshot Waiting => new(true, Array.Empty<CurrentlyTradingEntrySnapshot>());
    public static CurrentlyTradingSnapshot WithEntries(IReadOnlyList<CurrentlyTradingEntrySnapshot> entries) => new(false, entries);
    public int SalesBoothCount => Entries.Count == 0 ? 0 : Entries.Select(entry => entry.BoothName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
}

internal readonly record struct CurrentlyTradingEntrySnapshot(string BoothName, string SlotName, string Name, string Offer)
{
    public string SearchText => string.Join(' ', BoothName, SlotName, Name, Offer);
}
