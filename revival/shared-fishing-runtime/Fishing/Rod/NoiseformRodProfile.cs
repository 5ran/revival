using System;
using System.Collections.Generic;
using Avalonia.Media;
using Client.Services;
using System.Threading;
using System.Runtime.InteropServices;

namespace Client.Services.Fishing;

// Noiseform behaves like the default rod, but it owns a dedicated overlay hook
// so we can highlight the three beam zones without touching the generic loop.
internal sealed class NoiseformRodProfile : RodProfile
{
    private static readonly Color[] MatchColors =
    [
        Color.FromRgb(20, 101, 54),
        Color.FromRgb(8, 24, 14),
        Color.FromRgb(123, 124, 123),
    ];

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
            ApplyDistinctColors(boxes);

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

        boxes.Sort(static (left, right) =>
        {
            var x = left.X.CompareTo(right.X);
            if (x != 0)
            {
                return x;
            }

            var y = left.Y.CompareTo(right.Y);
            if (y != 0)
            {
                return y;
            }

            return left.Width.CompareTo(right.Width);
        });

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
                            MatchColors[0]));
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

    private static void ApplyDistinctColors(IList<BellonaDebugBox> boxes)
    {
        if (boxes.Count == 0)
        {
            return;
        }

        var count = Math.Min(boxes.Count, MatchColors.Length);
        var scores = new double[count, MatchColors.Length];
        var points = new List<(int X, int Y)>(12);
        var hdc = GetDC(IntPtr.Zero);

        try
        {
            for (var i = 0; i < count; i++)
            {
                points.Clear();
                CollectSamplePoints(boxes[i], points);

                for (var colorIndex = 0; colorIndex < MatchColors.Length; colorIndex++)
                {
                    scores[i, colorIndex] = ScoreCandidate(hdc, points, MatchColors[colorIndex]);
                }
            }

            var assignment = FindBestAssignment(scores, count, MatchColors.Length);
            for (var i = 0; i < count; i++)
            {
                var colorIndex = assignment[i];
                if (colorIndex < 0 || colorIndex >= MatchColors.Length)
                {
                    continue;
                }

                boxes[i] = boxes[i] with { Color = MatchColors[colorIndex] };
            }
        }
        finally
        {
            if (hdc != IntPtr.Zero)
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }
    }

    private static void CollectSamplePoints(BellonaDebugBox box, List<(int X, int Y)> points)
    {
        var left = (int)Math.Round(box.X);
        var top = (int)Math.Round(box.Y);
        var width = Math.Max(1, (int)Math.Round(box.Width));
        var height = Math.Max(1, (int)Math.Round(box.Height));
        var centerX = left + width / 2;
        var centerY = top + height / 2;
        var radiusX = Math.Max(1, width / 2);
        var radiusY = Math.Max(1, height / 2);
        var offsetsX = new[] { -0.28, 0.0, 0.28 };
        var offsetsY = new[] { -0.28, 0.0, 0.28 };

        foreach (var oy in offsetsY)
        {
            var sampleY = centerY + (int)Math.Round(radiusY * oy);
            foreach (var ox in offsetsX)
            {
                var sampleX = centerX + (int)Math.Round(radiusX * ox);
                if (IsInsideEllipse(sampleX, sampleY, centerX, centerY, radiusX, radiusY))
                {
                    points.Add((sampleX, sampleY));
                }
            }
        }

        if (points.Count == 0)
        {
            points.Add((centerX, centerY));
        }
    }

    private static bool IsInsideEllipse(int x, int y, int centerX, int centerY, int radiusX, int radiusY)
    {
        var dx = (x - centerX) / (double)radiusX;
        var dy = (y - centerY) / (double)radiusY;
        return dx * dx + dy * dy <= 1.0;
    }

    private static double ScoreCandidate(IntPtr hdc, IReadOnlyList<(int X, int Y)> points, Color candidate)
    {
        if (hdc == IntPtr.Zero || points.Count == 0)
        {
            return double.MaxValue / 4;
        }

        var total = 0.0;
        var seen = 0;
        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var pixel = GetPixel(hdc, point.X, point.Y);
            if (pixel == InvalidPixel)
            {
                continue;
            }

            var r = GetRValue(pixel);
            var g = GetGValue(pixel);
            var b = GetBValue(pixel);
            total += ColorDistanceSquared(r, g, b, candidate);
            seen++;
        }

        return seen == 0 ? double.MaxValue / 4 : total / seen;
    }

    private static double ColorDistanceSquared(byte r, byte g, byte b, Color candidate)
    {
        var dr = r - candidate.R;
        var dg = g - candidate.G;
        var db = b - candidate.B;
        return dr * dr + dg * dg + db * db;
    }

    private static int[] FindBestAssignment(double[,] scores, int boxCount, int colorCount)
    {
        var best = new int[boxCount];
        Array.Fill(best, -1);
        var current = new int[boxCount];
        Array.Fill(current, -1);
        var used = new bool[colorCount];
        var bestScore = double.MaxValue;

        void Search(int boxIndex, double totalScore)
        {
            if (boxIndex == boxCount)
            {
                if (totalScore < bestScore)
                {
                    bestScore = totalScore;
                    Array.Copy(current, best, boxCount);
                }

                return;
            }

            for (var colorIndex = 0; colorIndex < colorCount; colorIndex++)
            {
                if (used[colorIndex])
                {
                    continue;
                }

                used[colorIndex] = true;
                current[boxIndex] = colorIndex;
                Search(boxIndex + 1, totalScore + scores[boxIndex, colorIndex]);
                used[colorIndex] = false;
                current[boxIndex] = -1;
            }
        }

        Search(0, 0);
        return best;
    }

    private const int InvalidPixel = -1;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetPixel(IntPtr hdc, int nXPos, int nYPos);

    private static byte GetRValue(int pixel) => (byte)(pixel & 0xFF);
    private static byte GetGValue(int pixel) => (byte)((pixel >> 8) & 0xFF);
    private static byte GetBValue(int pixel) => (byte)((pixel >> 16) & 0xFF);

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
