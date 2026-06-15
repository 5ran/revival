using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Client.Services.Fishing;

internal sealed class TraderAddressProbe : IDisposable
{
    private readonly RobloxMemory _memory = new(OffsetsSourceProvider.Current);
    private readonly HashSet<ulong> _seen = new();

    public TraderItemSnapshotResult ReadCurrentItems()
    {
        _memory.EnsureAttached();

        var playerGui = _memory.FindPlayerGui();
        if (playerGui == 0)
        {
            return TraderItemSnapshotResult.Waiting;
        }

        var container = FindContainer(playerGui);
        if (container == 0)
        {
            return TraderItemSnapshotResult.Waiting;
        }

        var items = ReadItems(container);
        return TraderItemSnapshotResult.WithItems(items);
    }

    public TraderScanResult Scan(IReadOnlyList<TraderSearchSpec> selectedItems)
    {
        _memory.EnsureAttached();

        var playerGui = _memory.FindPlayerGui();
        if (playerGui == 0)
        {
            return TraderScanResult.Waiting;
        }

        var container = FindContainer(playerGui);
        if (container == 0)
        {
            return TraderScanResult.Waiting;
        }

        var items = ReadItems(container);

        foreach (var item in items)
        {
            foreach (var selected in selectedItems)
            {
                if (item.Matches(selected))
                {
                    return TraderScanResult.Matched(item.Address, item.Name, item.Price, items);
                }
            }
        }

        return TraderScanResult.WithItems(items);
    }

    public bool TryReadContainerCenter(out int x, out int y)
    {
        x = 0;
        y = 0;

        _memory.EnsureAttached();

        var playerGui = _memory.FindPlayerGui();
        if (playerGui == 0)
        {
            return false;
        }

        var container = FindContainer(playerGui);
        if (container == 0)
        {
            return false;
        }

        var bounds = ReadScreenBounds(container, visibleRequired: false);
        if (bounds is null)
        {
            return false;
        }

        x = (int)Math.Round(bounds.Value.X + bounds.Value.Width / 2f);
        y = (int)Math.Round(bounds.Value.Y + bounds.Value.Height / 2f);
        return true;
    }

    public bool TryReadConfirmButtonCenter(out int x, out int y)
    {
        x = 0;
        y = 0;

        _memory.EnsureAttached();

        var playerGui = _memory.FindPlayerGui();
        if (playerGui == 0)
        {
            return false;
        }

        var button =
            FindByPath(playerGui, "FrameTemplate", "TextButton", "promptconfirmation", "frame", "frame", "buttons")
            ?? FindByPath(playerGui, "FrameTemplate", "TextButton", "screengui", "promptconfirmation", "frame", "frame", "buttons")
            ?? FindByPath(playerGui, "FrameTemplate", "TextButton", "promptconfirmation", "buttons")
            ?? FindByNameClass(playerGui, "FrameTemplate", "TextButton");

        if (button == 0)
        {
            return false;
        }

        return TryReadItemCenter(button, out x, out y);
    }

    public void Dispose()
    {
        _memory.Dispose();
    }

    private ulong FindContainer(ulong root)
    {
        return FindByPath(root, "Container", string.Empty, "playergui", "hud", "safezone", "aquarium", "container", "itemlist", "list", "container")
            ?? FindByPath(root, "Container", string.Empty, "hud", "safezone", "aquarium", "container", "itemlist", "list", "container")
            ?? FindByPath(root, "Container", string.Empty, "safezone", "aquarium", "container", "itemlist", "list", "container")
            ?? 0;
    }

    private ulong? FindByPath(ulong root, string name, string classNeedle, params string[] segments)
    {
        foreach (var item in Traverse(root, 256))
        {
            var nameMatches = string.IsNullOrWhiteSpace(name) || string.Equals(_memory.ReadName(item), name, StringComparison.OrdinalIgnoreCase);
            if (!nameMatches || !ClassMatches(_memory.ReadClass(item), classNeedle))
            {
                continue;
            }

            if (PathContains(item, segments))
            {
                return item;
            }
        }

        return null;
    }

    private ulong FindByNameClass(ulong root, string name, string classNeedle)
    {
        foreach (var item in Traverse(root, 256))
        {
            var nameMatches = string.Equals(_memory.ReadName(item), name, StringComparison.OrdinalIgnoreCase);
            if (!nameMatches || !ClassMatches(_memory.ReadClass(item), classNeedle))
            {
                continue;
            }

            return item;
        }

        return 0;
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

    private bool PathContains(ulong item, IReadOnlyList<string> segments)
    {
        var path = BuildPath(item).ToLowerInvariant();
        foreach (var segment in segments)
        {
            if (!path.Contains((segment ?? string.Empty).ToLowerInvariant(), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private string BuildPath(ulong instance)
    {
        var parts = new List<string>();
        var current = instance;
        for (var i = 0; i < 14 && RobloxMemory.IsValidAddress(current); i++)
        {
            var name = _memory.ReadName(current);
            if (!string.IsNullOrWhiteSpace(name))
            {
                parts.Add(name);
            }

            var parent = _memory.ReadPtr(current + _memory.GetOffset("Parent"));
            if (!RobloxMemory.IsValidAddress(parent) || parent == current)
            {
                break;
            }

            current = parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    public bool TryReadItemCenter(ulong item, out int x, out int y)
    {
        x = 0;
        y = 0;

        var bounds = ReadScreenBounds(item, visibleRequired: false);
        if (bounds is null)
        {
            return false;
        }

        x = (int)Math.Round(bounds.Value.X + bounds.Value.Width / 2f);
        y = (int)Math.Round(bounds.Value.Y + bounds.Value.Height / 2f);
        return true;
    }

    private GuiBounds? ReadScreenBounds(ulong address, bool visibleRequired)
    {
        var bounds = _memory.ReadGuiBounds(address, visibleRequired);
        if (bounds is null)
        {
            return null;
        }

        var origin = GetClientScreenOrigin();
        return new GuiBounds(origin.X + bounds.Value.X, origin.Y + bounds.Value.Y, bounds.Value.Width, bounds.Value.Height);
    }

    private WinPoint GetClientScreenOrigin()
    {
        var point = new WinPoint(0, 0);
        ClientToScreen(_memory.WindowHandle, ref point);
        return point;
    }

    private List<TraderItemInfo> ReadItems(ulong container)
    {
        var items = new List<TraderItemInfo>();
        foreach (var child in _memory.ReadChildren(container))
        {
            if (string.Equals(_memory.ReadClass(child), "UIGridLayout", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = FindItemName(child);
            var price = FindItemPrice(child);
            items.Add(new TraderItemInfo(child, name, price));
        }

        return items;
    }

    private string FindItemName(ulong item)
    {
        foreach (var descendant in Traverse(item, 12))
        {
            if (!string.Equals(_memory.ReadClass(descendant), "TextLabel", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = _memory.ReadGuiText(descendant).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return "(unknown)";
    }

    private string FindItemPrice(ulong item)
    {
        foreach (var descendant in Traverse(item, 12))
        {
            var text = _memory.ReadGuiText(descendant).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (string.Equals(_memory.ReadClass(descendant), "TextBox", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_memory.ReadName(descendant), "TextBox", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }
        }

        return "(unknown)";
    }

    private static bool ClassMatches(string className, string classNeedle)
    {
        if (string.IsNullOrWhiteSpace(classNeedle))
        {
            return true;
        }

        return classNeedle.Contains("Button", StringComparison.OrdinalIgnoreCase)
            ? className.Contains("Button", StringComparison.OrdinalIgnoreCase)
            : string.Equals(className, classNeedle, StringComparison.OrdinalIgnoreCase);
    }

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref WinPoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct WinPoint
    {
        public int X;
        public int Y;

        public WinPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}

internal readonly record struct TraderItemInfo(ulong Address, string Name, string Price)
{
    public bool Matches(TraderSearchSpec selected)
    {
        var nameNeedle = NormalizeSearchText(selected.Name);
        if (nameNeedle.Length > 0 && !NormalizeSearchText(Name).Contains(nameNeedle, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryParsePrice(Price, out var itemPrice))
        {
            return false;
        }

        if (!TryParsePrice(selected.Price, out var targetPrice))
        {
            return false;
        }

        return itemPrice <= targetPrice;
    }

    private static string NormalizeSearchText(string? value)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return string.Empty;
        }

        return text;
    }

    private static bool TryParsePrice(string? value, out decimal price)
    {
        var text = NormalizePriceText(value);
        return decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out price);
    }

    private static string NormalizePriceText(string? value)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return string.Empty;
        }

        return new string(text.Where(ch => char.IsDigit(ch) || ch == '.' || ch == '-').ToArray());
    }
}

internal readonly record struct TraderItemSnapshotResult(bool IsWaiting, IReadOnlyList<TraderItemInfo> Items)
{
    public static TraderItemSnapshotResult Waiting => new(true, Array.Empty<TraderItemInfo>());
    public static TraderItemSnapshotResult WithItems(IReadOnlyList<TraderItemInfo> items) => new(false, items);
}

internal readonly record struct TraderSearchSpec(string Name, string Price);

internal readonly record struct TraderScanResult(bool IsWaiting, bool IsMatched, ulong MatchedAddress = 0, string? Name = null, string? Price = null, IReadOnlyList<TraderItemInfo>? Items = null)
{
    public static TraderScanResult Waiting => new(true, false);
    public static TraderScanResult Idle => new(false, false);
    public static TraderScanResult WithItems(IReadOnlyList<TraderItemInfo> items) => new(false, false, Items: items);
    public static TraderScanResult Matched(ulong address, string name, string price, IReadOnlyList<TraderItemInfo> items) => new(false, true, address, name, price, items);
}
