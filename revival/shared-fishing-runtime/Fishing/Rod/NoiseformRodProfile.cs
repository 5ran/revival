using System;
using System.Collections.Generic;
using Avalonia.Media;
using Client.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Services.Fishing;

// Noiseform behaves like the default rod, but it owns a dedicated overlay hook
// so we can highlight the three beam zones without touching the generic loop.
internal sealed class NoiseformRodProfile : RodProfile
{
    private static long _lastOverlayLogAt;
    private static string _lastOverlayMessage = string.Empty;
    private static long _lastOverlaySeenAt;
    private static bool _overlayVisible;
    private static ulong _lastBar;
    private static long _lastScanAt;
    private static int _lastBeamZoneCount = -1;
    private static int _scanInFlight;
    private const int OverlayGraceMs = 350;
    private const int ScanThrottleMs = 50;

    public override RodKind Kind => RodKind.Noiseform;

    public override void UpdateOverlay(RobloxMemory memory, FishingRuntimeContext context, ReelContext? reelContext)
    {
        if (reelContext is null)
        {
            LogOverlay("overlay skipped: reelContext missing");
            if (_overlayVisible)
            {
                BellonaDebugOverlayService.Hide();
                _overlayVisible = false;
            }
            _lastBar = 0;
            _lastBeamZoneCount = -1;
            return;
        }

        var now = Environment.TickCount64;
        if (reelContext.Bar == _lastBar && now - _lastScanAt < ScanThrottleMs)
        {
            return;
        }

        _lastBar = reelContext.Bar;
        _lastScanAt = now;
        if (Interlocked.Exchange(ref _scanInFlight, 1) == 1)
        {
            return;
        }

        var reelGui = context.GetPrimaryReelGuiAddress();
        var bar = reelContext.Bar;
        _ = Task.Run(() =>
        {
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
                    if (_overlayVisible && Environment.TickCount64 - _lastOverlaySeenAt < OverlayGraceMs)
                    {
                        return;
                    }

                    if (_overlayVisible)
                    {
                        BellonaDebugOverlayService.Hide();
                        _overlayVisible = false;
                    }

                    if (Environment.TickCount64 - _lastOverlayLogAt >= 500)
                    {
                        LogOverlay("beamZone scan empty");
                    }
                    return;
                }

                _overlayVisible = true;
                _lastOverlaySeenAt = Environment.TickCount64;
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
        });
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
                        LogOverlay($"beamZone without bounds child=0x{child:X} path={DescribePath(memory, child)}");
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

    private static void LogOverlay(string message)
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
