using System;
using System.Collections.Generic;
using Avalonia.Media;
using Client.Services;
using System.Threading;

namespace Client.Services.Fishing;

// Noiseform behaves like the default rod, but it owns a dedicated overlay hook
// so we can highlight the three beam zones without touching the generic loop.
internal sealed class NoiseformRodProfile : RodProfile
{
    private readonly object _overlayLock = new();
    private List<BellonaDebugBox> _lastBoxes = [];
    private long _lastOverlayLogAt;
    private string _lastOverlayMessage = string.Empty;
    private long _lastOverlaySeenAt;
    private bool _overlayVisible;
    private ulong _lastBar;
    private long _lastScanAt;
    private int _lastBeamZoneCount = -1;
    private int _scanInFlight;
    private const int OverlayGraceMs = 600;
    private const int ScanThrottleMs = 50;

    public override RodKind Kind => RodKind.Noiseform;

    public override void UpdateOverlay(RobloxMemory memory, FishingRuntimeContext context, ReelContext? reelContext)
    {
        var now = Environment.TickCount64;
        if (reelContext is null)
        {
            ClearOverlayIfExpired(now, "reelContext missing");
            return;
        }

        if (reelContext.Bar == _lastBar && now - _lastScanAt < ScanThrottleMs)
        {
            ClearOverlayIfExpired(now, "scan throttled");
            return;
        }

        _lastBar = reelContext.Bar;
        _lastScanAt = now;
        if (Interlocked.Exchange(ref _scanInFlight, 1) == 1)
        {
            ClearOverlayIfExpired(now, "scan busy");
            return;
        }

        var reelGui = context.GetPrimaryReelGuiAddress();
        var bar = reelContext.Bar;
        try
        {
            var barPath = DescribePath(memory, bar);
            var reelPath = DescribePath(memory, reelGui);
            var boxes = FindBeamZoneBoxes(memory, bar, reelGui);

            if (boxes.Count != _lastBeamZoneCount)
            {
                LogOverlay($"reelPath={reelPath} barPath={barPath} bar=0x{bar:X} beamZones={boxes.Count} childNames={DescribeImmediateChildren(memory, bar)}");
                _lastBeamZoneCount = boxes.Count;
            }

            if (boxes.Count == 0)
            {
                ClearOverlayIfExpired(Environment.TickCount64, "beamZone scan empty");
                return;
            }

            lock (_overlayLock)
            {
                _lastBoxes = new List<BellonaDebugBox>(boxes);
                _overlayVisible = true;
                _lastOverlaySeenAt = Environment.TickCount64;
            }

            BellonaDebugOverlayService.Update(boxes);
        }
        catch (Exception ex)
        {
            AppLog.FishingError("NoiseformOverlay", "overlay scan failed", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _scanInFlight, 0);
        }
    }

    private void ClearOverlayIfExpired(long now, string reason)
    {
        if (!_overlayVisible)
        {
            return;
        }

        if (now - _lastOverlaySeenAt < OverlayGraceMs)
        {
            if (now - _lastOverlayLogAt >= 500)
            {
                LogOverlay($"overlay retained ({reason})");
            }

            if (_lastBoxes.Count > 0)
            {
                BellonaDebugOverlayService.Update(_lastBoxes);
            }

            return;
        }

        lock (_overlayLock)
        {
            if (!_overlayVisible)
            {
                return;
            }

            _overlayVisible = false;
            _lastBoxes = [];
            _lastBeamZoneCount = -1;
        }

        BellonaDebugOverlayService.Hide();
        LogOverlay($"overlay cleared ({reason})");
    }

    private static List<BellonaDebugBox> FindBeamZoneBoxes(RobloxMemory memory, ulong root, ulong fallbackRoot)
    {
        var boxes = new List<BellonaDebugBox>(3);
        CollectBeamZoneBoxes(memory, root, boxes);
        if (boxes.Count < 3)
        {
            CollectBeamZoneBoxes(memory, fallbackRoot, boxes);
        }

        return boxes;
    }

    private static void CollectBeamZoneBoxes(RobloxMemory memory, ulong root, List<BellonaDebugBox> boxes)
    {
        if (root == 0 || boxes.Count >= 3)
        {
            return;
        }

        var queue = new Queue<ulong>();
        var seen = new HashSet<ulong>();
        queue.Enqueue(root);

        while (queue.Count > 0 && boxes.Count < 3)
        {
            var current = queue.Dequeue();
            if (!seen.Add(current))
            {
                continue;
            }

            foreach (var child in memory.ReadChildren(current))
            {
                if (!RobloxMemory.IsValidAddress(child))
                {
                    continue;
                }

                var name = memory.ReadName(child);
                if (name.Equals("beamZone", StringComparison.OrdinalIgnoreCase))
                {
                    var bounds = memory.ReadGuiBounds(child, false);
                    if (bounds is not null)
                    {
                        boxes.Add(new BellonaDebugBox(
                            bounds.Value.X,
                            bounds.Value.Y,
                            bounds.Value.Width,
                            bounds.Value.Height,
                            Color.FromRgb(0, 191, 255)));
                    }
                    else
                    {
                        AppLog.Fishing("NoiseformOverlay", $"beamZone without bounds child=0x{child:X} path={DescribePath(memory, child)}");
                    }

                    continue;
                }

                queue.Enqueue(child);
            }
        }
    }

    private static string DescribePath(RobloxMemory memory, ulong address)
    {
        if (!RobloxMemory.IsValidAddress(address))
        {
            return "<invalid>";
        }

        var parts = new List<string>();
        var current = address;
        var guard = 0;
        while (RobloxMemory.IsValidAddress(current) && guard++ < 64)
        {
            var name = memory.ReadName(current);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = memory.ReadClass(current);
            }

            parts.Add(string.IsNullOrWhiteSpace(name) ? $"0x{current:X}" : name);
            current = memory.ReadParent(current);
            if (current == 0)
            {
                break;
            }
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static string DescribeImmediateChildren(RobloxMemory memory, ulong root)
    {
        if (!RobloxMemory.IsValidAddress(root))
        {
            return "<invalid>";
        }

        var names = new List<string>();
        foreach (var child in memory.ReadChildren(root))
        {
            var name = memory.ReadName(child);
            var cls = memory.ReadClass(child);
            names.Add(string.IsNullOrWhiteSpace(name) ? cls : $"{name}:{cls}");
            if (names.Count >= 12)
            {
                break;
            }
        }

        return names.Count == 0 ? "<none>" : string.Join(", ", names);
    }

    private void LogOverlay(string message)
    {
        var now = Environment.TickCount64;
        if (now - _lastOverlayLogAt < 200 && string.Equals(message, _lastOverlayMessage, StringComparison.Ordinal))
        {
            return;
        }

        _lastOverlayLogAt = now;
        _lastOverlayMessage = message;
        AppLog.Fishing("NoiseformOverlay", message);
    }
}
