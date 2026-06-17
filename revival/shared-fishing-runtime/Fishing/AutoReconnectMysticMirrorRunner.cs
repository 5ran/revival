using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Client.Services.Fishing;

internal sealed class AutoReconnectMysticMirrorRunner : IDisposable
{
    private const string MysticMirrorName = "Mystic Mirror";
    private const int FishingLocationTolerancePixels = 5;
    private static readonly TimeSpan TeleportButtonWaitTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FishingLocationAlignTimeout = TimeSpan.FromSeconds(20);
    private readonly RobloxMemory _memory = new(OffsetsSourceProvider.Current);
    private readonly Action<string>? _reportStatus;
    private ulong _cachedCompassFrame;
    private string _cachedCompassDirection = string.Empty;
    private ulong _cachedCompassDirectionLabel;

    public AutoReconnectMysticMirrorRunner(Action<string>? reportStatus = null)
    {
        _reportStatus = reportStatus;
    }

    public AutoReconnectMysticMirrorResult RunOnce(string fishingDirection, int fishingX, int fishingY)
    {
        _memory.EnsureAttached();

        var hotbarSlot = GetHotbarItemSlotKey(MysticMirrorName);
        if (hotbarSlot > 0)
        {
            NativeKeyboard.PressDigit(hotbarSlot, _memory.WindowHandle);
            Thread.Sleep(175);
            return ClickMiddleAndWaitForTeleportButton(out var teleportButton) &&
                ClickTeleportButtonOnceAndWait(teleportButton) &&
                AlignFishingLocation(fishingDirection, fishingX, fishingY)
                ? new AutoReconnectMysticMirrorResult(true, $"Mystic Mirror selected from hotbar slot {hotbarSlot}; teleport clicked.")
                : new AutoReconnectMysticMirrorResult(false, $"Mystic Mirror selected from hotbar slot {hotbarSlot}; teleport button not visible.");
        }

        if (!OpenInventoryIfNeeded())
        {
            return new AutoReconnectMysticMirrorResult(false, "Could not open inventory.");
        }

        try
        {
            if (!SearchAndSelectInventoryItem(MysticMirrorName))
            {
                return new AutoReconnectMysticMirrorResult(false, "Mystic Mirror not found in inventory.");
            }

            CloseInventoryIfOpen();
            return ClickMiddleAndWaitForTeleportButton(out var teleportButton) &&
                ClickTeleportButtonOnceAndWait(teleportButton) &&
                AlignFishingLocation(fishingDirection, fishingX, fishingY)
                ? new AutoReconnectMysticMirrorResult(true, "Mystic Mirror selected from inventory; teleport clicked.")
                : new AutoReconnectMysticMirrorResult(false, "Mystic Mirror selected from inventory; teleport button not visible.");
        }
        finally
        {
            CloseInventoryIfOpen();
        }
    }

    public void Dispose()
    {
        _memory.Dispose();
    }

    private bool OpenInventoryIfNeeded()
    {
        if (IsInventoryOpen())
        {
            return true;
        }

        NativeKeyboard.PressG(_memory.WindowHandle);
        Thread.Sleep(300);
        for (var i = 0; i < 10; i++)
        {
            Thread.Sleep(80);
            if (IsInventoryOpen())
            {
                return true;
            }
        }

        NativeKeyboard.PressG(_memory.WindowHandle);
        Thread.Sleep(300);
        for (var i = 0; i < 10; i++)
        {
            Thread.Sleep(80);
            if (IsInventoryOpen())
            {
                return true;
            }
        }

        return false;
    }

    private bool CloseInventoryIfOpen()
    {
        if (!IsInventoryOpen())
        {
            return true;
        }

        NativeKeyboard.PressG(_memory.WindowHandle);
        Thread.Sleep(160);
        for (var i = 0; i < 10; i++)
        {
            Thread.Sleep(80);
            if (!IsInventoryOpen())
            {
                return true;
            }
        }

        return false;
    }

    private bool SearchAndSelectInventoryItem(string itemName)
    {
        var searchFrame = ResolveInventorySearchFrame();
        var itemContainer = ResolveInventoryItemContainer();
        if (searchFrame == 0 || itemContainer == 0)
        {
            return false;
        }

        ClickGuiCenter(searchFrame);
        Thread.Sleep(120);
        ClearInventorySearchInput();
        NativeKeyboard.TypeText(itemName, _memory.WindowHandle);
        Thread.Sleep(600);

        var target = FindInventoryItemTarget(itemContainer, itemName);
        if (target == 0)
        {
            return false;
        }

        ClickGuiCenter(target);
        Thread.Sleep(300);
        return true;
    }

    private void ClearInventorySearchInput()
    {
        for (var i = 0; i < 2; i++)
        {
            NativeKeyboard.PressCtrlA(_memory.WindowHandle);
            Thread.Sleep(80);
            NativeKeyboard.PressBackspace(_memory.WindowHandle);
            Thread.Sleep(80);
        }
    }

    private int GetHotbarItemSlotKey(string itemName)
    {
        var hotbar = GetHotbarGui();
        if (hotbar == 0)
        {
            return 0;
        }

        foreach (var slot in _memory.ReadChildren(hotbar))
        {
            if (!string.Equals(_memory.ReadClass(slot), "ImageButton", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(_memory.ReadName(slot), "ItemTemplate", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var nameLabel = _memory.FindChildByName(slot, "ItemName");
            var text = nameLabel == 0 ? string.Empty : NormalizeLoose(_memory.ReadGuiText(nameLabel));
            if (!string.Equals(text, NormalizeLoose(itemName), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var child in _memory.ReadChildren(slot))
            {
                if (!string.Equals(_memory.ReadClass(child), "TextLabel", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var raw = NormalizeLoose(_memory.ReadGuiText(child));
                if (int.TryParse(raw, out var parsed) && parsed is >= 1 and <= 9)
                {
                    return parsed;
                }
            }
        }

        return 0;
    }

    private ulong FindInventoryItemTarget(ulong itemContainer, string itemName)
    {
        var needle = NormalizeLoose(itemName);
        ulong partialMatch = 0;
        foreach (var node in Traverse(itemContainer, 32))
        {
            var cls = _memory.ReadClass(node);
            if (!cls.Contains("Text", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = NormalizeLoose(_memory.ReadGuiText(node));
            if (text.Length == 0)
            {
                continue;
            }

            var exact = IsInventoryItemExactMatch(text, needle);
            var contains = text.Contains(needle, StringComparison.OrdinalIgnoreCase);
            if (!exact && !contains)
            {
                continue;
            }

            var clickable = FindClickableAncestor(node, itemContainer);
            if (clickable == 0)
            {
                continue;
            }

            if (exact)
            {
                return clickable;
            }

            partialMatch = partialMatch == 0 ? clickable : partialMatch;
        }

        return partialMatch;
    }

    private ulong FindClickableAncestor(ulong node, ulong stopRoot)
    {
        ulong frameFallback = 0;
        var current = node;
        for (var i = 0; i < 10 && RobloxMemory.IsValidAddress(current); i++)
        {
            var cls = _memory.ReadClass(current);
            if (cls.Contains("Button", StringComparison.OrdinalIgnoreCase))
            {
                if (_memory.ReadGuiBounds(current, false) is { Width: > 10, Height: > 10 })
                {
                    return current;
                }
            }
            else if (cls.Contains("Frame", StringComparison.OrdinalIgnoreCase) &&
                _memory.ReadGuiBounds(current, false) is { Width: > 10, Height: > 10 } bounds &&
                bounds.Width <= 500 &&
                bounds.Height <= 110)
            {
                frameFallback = current;
            }

            if (current == stopRoot)
            {
                break;
            }

            var parent = _memory.ReadParent(current);
            if (!RobloxMemory.IsValidAddress(parent) || parent == current)
            {
                break;
            }

            current = parent;
        }

        return frameFallback;
    }

    private bool IsInventoryOpen()
    {
        var inventory = ResolveInventoryFrame();
        return inventory != 0 && _memory.IsVisible(inventory, "FrameVisible");
    }

    private ulong ResolveInventoryFrame()
    {
        var localPlayer = _memory.GetLocalPlayer();
        if (localPlayer == 0)
        {
            return 0;
        }

        var playerGui = _memory.FindChildByClass(localPlayer, "PlayerGui");
        var backpack = playerGui == 0 ? 0 : _memory.FindChildByName(playerGui, "backpack");
        return backpack == 0 ? 0 : _memory.FindChildByName(backpack, "inventory");
    }

    private ulong ResolveInventorySearchFrame()
    {
        var inventory = ResolveInventoryFrame();
        if (inventory == 0)
        {
            return 0;
        }

        var byPath = FindByPath(inventory, "Search", "Frame", "topbar", "search");
        return byPath != 0 ? byPath : FindByPath(inventory, "Search", string.Empty, "topbar");
    }

    private ulong ResolveInventoryItemContainer()
    {
        var inventory = ResolveInventoryFrame();
        if (inventory == 0)
        {
            return 0;
        }

        var byName = _memory.FindChildByName(inventory, "itemContainer");
        return byName != 0 ? byName : FindByPath(inventory, "itemContainer", "Frame");
    }

    private ulong GetHotbarGui()
    {
        var localPlayer = _memory.GetLocalPlayer();
        if (localPlayer == 0)
        {
            return 0;
        }

        var playerGui = _memory.FindChildByClass(localPlayer, "PlayerGui");
        var backpack = playerGui == 0 ? 0 : _memory.FindChildByName(playerGui, "backpack");
        return backpack == 0 ? 0 : _memory.FindChildByName(backpack, "hotbar");
    }

    private bool ClickMiddleAndWaitForTeleportButton(out ulong teleportButton)
    {
        ClickRobloxCenter();

        var deadline = DateTimeOffset.UtcNow + TeleportButtonWaitTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            teleportButton = ResolveVisibleTeleportButton();
            if (teleportButton != 0)
            {
                return true;
            }

            Thread.Sleep(100);
        }

        teleportButton = ResolveVisibleTeleportButton();
        return teleportButton != 0;
    }

    private ulong ResolveVisibleTeleportButton()
    {
        var button = ResolveTeleportButton();
        return button != 0 && _memory.ReadGuiBounds(button, true) is not null ? button : 0;
    }

    private bool ClickTeleportButtonOnceAndWait(ulong teleportButton)
    {
        var target = ResolveVisibleTeleportButton();
        if (target == 0)
        {
            return false;
        }

        ClickGuiCenter(target);
        return true;
    }

    private bool AlignFishingLocation(string direction, int targetX, int targetY)
    {
        if (string.IsNullOrWhiteSpace(direction) || targetX == 0 || targetY == 0)
        {
            _reportStatus?.Invoke("No saved fishing location; skipping alignment.");
            return true;
        }

        _reportStatus?.Invoke($"Aligning {direction} to {targetX}, {targetY}.");
        var deadline = DateTimeOffset.UtcNow + FishingLocationAlignTimeout;
        var holding = false;
        try
        {
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (TryReadCompassDirectionCenterCached(direction, out var x, out var y))
                {
                    var dx = x - targetX;
                    var dy = y - targetY;
                    if (Math.Abs(dx) <= FishingLocationTolerancePixels && Math.Abs(dy) <= FishingLocationTolerancePixels)
                    {
                        _reportStatus?.Invoke($"Aligned {direction} at {x}, {y}.");
                        PressShiftTwice();
                        return true;
                    }
                }

                if (!holding)
                {
                    _reportStatus?.Invoke($"Holding Right Arrow for {direction} alignment.");
                    NativeKeyboard.RightArrowDown(_memory.WindowHandle);
                    holding = true;
                }

                Thread.Sleep(5);
            }

            var aligned = TryReadCompassDirectionCenterCached(direction, out var finalX, out var finalY) &&
                Math.Abs(finalX - targetX) <= FishingLocationTolerancePixels &&
                Math.Abs(finalY - targetY) <= FishingLocationTolerancePixels;
            if (aligned)
            {
                _reportStatus?.Invoke($"Aligned {direction} at {finalX}, {finalY}.");
                PressShiftTwice();
            }

            return aligned;
        }
        finally
        {
            if (holding)
            {
                NativeKeyboard.RightArrowUp();
                _reportStatus?.Invoke("Released Right Arrow.");
            }
        }
    }

    private void PressShiftTwice()
    {
        _reportStatus?.Invoke("Pressing Left Shift twice.");
        NativeKeyboard.PressShift(_memory.WindowHandle);
        Thread.Sleep(120);
        NativeKeyboard.PressShift(_memory.WindowHandle);
    }

    private bool TryReadCompassDirectionCenter(string direction, out int x, out int y)
    {
        x = 0;
        y = 0;
        var compass = ResolveCompassFrame();
        if (compass == 0)
        {
            return false;
        }

        foreach (var child in _memory.ReadChildren(compass))
        {
            if (!string.Equals(_memory.ReadClass(child), "TextLabel", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = (_memory.ReadGuiText(child) ?? string.Empty).Trim();
            if (!string.Equals(text, direction, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var bounds = _memory.ReadGuiBounds(child, true);
            if (bounds is null)
            {
                return false;
            }

            x = (int)Math.Round(bounds.Value.X + bounds.Value.Width * 0.5f);
            y = (int)Math.Round(bounds.Value.Y + bounds.Value.Height * 0.5f);
            return true;
        }

        return false;
    }

    private bool TryReadCompassDirectionCenterCached(string direction, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (_cachedCompassDirectionLabel == 0 ||
            !string.Equals(_cachedCompassDirection, direction, StringComparison.OrdinalIgnoreCase) ||
            _memory.ReadGuiBounds(_cachedCompassDirectionLabel, true) is null)
        {
            _cachedCompassDirection = direction;
            _cachedCompassFrame = ResolveCompassFrame();
            _cachedCompassDirectionLabel = _cachedCompassFrame == 0 ? 0 : FindCompassDirectionLabel(_cachedCompassFrame, direction);
        }

        if (_cachedCompassDirectionLabel == 0)
        {
            return false;
        }

        var bounds = _memory.ReadGuiBounds(_cachedCompassDirectionLabel, true);
        if (bounds is null)
        {
            return false;
        }

        x = (int)Math.Round(bounds.Value.X + bounds.Value.Width * 0.5f);
        y = (int)Math.Round(bounds.Value.Y + bounds.Value.Height * 0.5f);
        return true;
    }

    private ulong FindCompassDirectionLabel(ulong compass, string direction)
    {
        foreach (var child in _memory.ReadChildren(compass))
        {
            if (!string.Equals(_memory.ReadClass(child), "TextLabel", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = (_memory.ReadGuiText(child) ?? string.Empty).Trim();
            if (string.Equals(text, direction, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return 0;
    }

    private ulong ResolveTeleportButton()
    {
        var playerGui = _memory.FindPlayerGui();
        if (playerGui == 0)
        {
            return 0;
        }

        var hud = _memory.FindChildByName(playerGui, "hud");
        var safezone = hud == 0 ? 0 : _memory.FindDescendantByName(hud, "safezone");
        var mysticMirror = safezone == 0 ? 0 : _memory.FindChildByName(safezone, "MysticMirror");
        return mysticMirror == 0 ? 0 : _memory.FindChildByName(mysticMirror, "teleportButton");
    }

    private ulong ResolveCompassFrame()
    {
        var playerGui = _memory.FindPlayerGui();
        if (playerGui == 0)
        {
            return 0;
        }

        var hud = _memory.FindChildByName(playerGui, "hud");
        var safezone = hud == 0 ? 0 : _memory.FindDescendantByName(hud, "safezone");
        var compassRoot = safezone == 0 ? 0 : _memory.FindChildByName(safezone, "compass");
        return compassRoot == 0 ? 0 : _memory.FindChildByName(compassRoot, "Compass");
    }

    private void ClickRobloxCenter()
    {
        var rect = GetClientScreenRectangle();
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        NativeMouse.ClickAt(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    }

    private ulong FindByPath(ulong root, string name, string classNeedle, params string[] segments)
    {
        foreach (var item in Traverse(root, 64))
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

        return 0;
    }

    private IEnumerable<ulong> Traverse(ulong root, int maxDepth)
    {
        var seen = new HashSet<ulong>();
        var queue = new Queue<(ulong Address, int Depth)>();
        queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            var (address, depth) = queue.Dequeue();
            if (!seen.Add(address))
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
        for (var i = 0; i < 64 && RobloxMemory.IsValidAddress(current); i++)
        {
            var name = _memory.ReadName(current);
            if (!string.IsNullOrWhiteSpace(name))
            {
                parts.Add(name);
            }

            var parent = _memory.ReadParent(current);
            if (!RobloxMemory.IsValidAddress(parent) || parent == current)
            {
                break;
            }

            current = parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private void ClickGuiCenter(ulong address)
    {
        var bounds = _memory.ReadGuiBounds(address, false)
            ?? throw new InvalidOperationException("Could not read UI bounds.");
        var origin = GetClientScreenOrigin();
        var x = (int)Math.Round(origin.X + bounds.X + bounds.Width * 0.5f);
        var y = (int)Math.Round(origin.Y + bounds.Y + bounds.Height * 0.5f);
        NativeMouse.ClickAt(x, y);
    }

    private WinPoint GetClientScreenOrigin()
    {
        var point = new WinPoint(0, 0);
        ClientToScreen(_memory.WindowHandle, ref point);
        return point;
    }

    private ClientRectangle GetClientScreenRectangle()
    {
        if (!GetClientRect(_memory.WindowHandle, out var rect))
        {
            return new ClientRectangle(0, 0, 0, 0);
        }

        var origin = GetClientScreenOrigin();
        return new ClientRectangle(origin.X, origin.Y, Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
    }

    private static string NormalizeLoose(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim().ToLowerInvariant();
    }

    private static bool IsInventoryItemExactMatch(string normalizedText, string normalizedNeedle)
    {
        return normalizedText.Equals(normalizedNeedle, StringComparison.OrdinalIgnoreCase) ||
            normalizedText.StartsWith(normalizedNeedle + " x", StringComparison.OrdinalIgnoreCase) ||
            normalizedText.StartsWith(normalizedNeedle + " (", StringComparison.OrdinalIgnoreCase);
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

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out ClientRect rect);

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

    private readonly record struct ClientRectangle(int X, int Y, int Width, int Height);

    [StructLayout(LayoutKind.Sequential)]
    private struct ClientRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

internal readonly record struct AutoReconnectMysticMirrorResult(bool Success, string Message);
