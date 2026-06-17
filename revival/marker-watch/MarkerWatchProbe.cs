using System;
using Client.Services;
using Client.Services.Fishing;

namespace MarkerWatch;

internal sealed class MarkerWatchProbe : IDisposable
{
    private readonly OffsetsService _offsets = new();
    private readonly RobloxMemory _memory;

    public MarkerWatchProbe()
    {
        _memory = new RobloxMemory(_offsets);
    }

    public MarkerWatchSnapshot Poll()
    {
        try
        {
            _memory.EnsureAttached();

            var playerGui = _memory.FindPlayerGui();
            if (playerGui == 0)
            {
                return MarkerWatchSnapshot.Waiting("Waiting for PlayerGui...");
            }

            var minigame = _memory.FindDescendantByName(playerGui, "NukeMinigame");
            if (minigame == 0 || !string.Equals(_memory.ReadClass(minigame), "ScreenGui", StringComparison.OrdinalIgnoreCase))
            {
                return MarkerWatchSnapshot.Waiting("Waiting for NukeMinigame...");
            }

            var center = _memory.FindDescendantByName(minigame, "Center");
            if (center == 0 || !string.Equals(_memory.ReadClass(center), "Frame", StringComparison.OrdinalIgnoreCase))
            {
                return MarkerWatchSnapshot.Waiting("Waiting for Center...");
            }

            var marker = _memory.FindDescendantByName(center, "Marker");
            if (marker == 0 || !string.Equals(_memory.ReadClass(marker), "ImageLabel", StringComparison.OrdinalIgnoreCase))
            {
                return MarkerWatchSnapshot.Waiting("Waiting for Marker...");
            }

            var pointer = _memory.FindDescendantByName(marker, "Pointer");
            if (pointer == 0 || !string.Equals(_memory.ReadClass(pointer), "ImageLabel", StringComparison.OrdinalIgnoreCase))
            {
                return MarkerWatchSnapshot.Waiting("Waiting for Pointer...");
            }

            var centerBounds = _memory.ReadGuiBounds(center, visibleRequired: false);
            var pointerBounds = _memory.ReadGuiBounds(pointer, visibleRequired: false);
            if (centerBounds is null || pointerBounds is null)
            {
                return MarkerWatchSnapshot.Waiting("Waiting for readable bounds...");
            }

            var centerX = centerBounds.Value.X + centerBounds.Value.Width / 2f;
            var markerX = pointerBounds.Value.X + pointerBounds.Value.Width / 2f;
            var delta = markerX - centerX;
            var tolerance = Math.Max(2.0f, pointerBounds.Value.Width * 0.1f);
            var side = delta < -tolerance
                ? MarkerSide.Left
                : delta > tolerance
                    ? MarkerSide.Right
                    : MarkerSide.Center;

            return MarkerWatchSnapshot.Active(
                pointerBounds.Value.X,
                pointerBounds.Value.Y,
                pointerBounds.Value.Width,
                pointerBounds.Value.Height,
                centerBounds.Value.X,
                centerBounds.Value.Y,
                centerBounds.Value.Width,
                centerBounds.Value.Height,
                delta,
                side,
                BuildPath(pointer));
        }
        catch (Exception ex)
        {
            return MarkerWatchSnapshot.Fault(ex.Message);
        }
    }

    private string BuildPath(ulong instance)
    {
        var current = instance;
        var parts = new System.Collections.Generic.List<string>();
        for (var i = 0; i < 16 && RobloxMemory.IsValidAddress(current); i++)
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

    public void Dispose()
    {
        _memory.Dispose();
    }
}

internal enum MarkerSide
{
    Center,
    Left,
    Right,
}

internal readonly record struct MarkerWatchSnapshot(
    bool IsAttached,
    bool IsMarkerVisible,
    string StatusText,
    string DetailText,
    MarkerSide Side,
    double DeltaX,
    float MarkerX,
    float MarkerY,
    float MarkerWidth,
    float MarkerHeight,
    float CenterX,
    float CenterY,
    float CenterWidth,
    float CenterHeight,
    string? Path,
    string? Error)
{
    public static MarkerWatchSnapshot Waiting(string status) =>
        new(false, false, status, status, MarkerSide.Center, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null);

    public static MarkerWatchSnapshot Fault(string message) =>
        new(false, false, "Error", message, MarkerSide.Center, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, message);

    public static MarkerWatchSnapshot Active(
        float markerX,
        float markerY,
        float markerWidth,
        float markerHeight,
        float centerX,
        float centerY,
        float centerWidth,
        float centerHeight,
        double deltaX,
        MarkerSide side,
        string path)
    {
        var sideText = side switch
        {
            MarkerSide.Left => "Left",
            MarkerSide.Right => "Right",
            _ => "Center",
        };

        return new MarkerWatchSnapshot(
            true,
            true,
            $"Pointer visible - {sideText}",
            $"Pointer: {sideText} | deltaX={deltaX:0.0}",
            side,
            deltaX,
            markerX,
            markerY,
            markerWidth,
            markerHeight,
            centerX,
            centerY,
            centerWidth,
            centerHeight,
            path,
            null);
    }
}
