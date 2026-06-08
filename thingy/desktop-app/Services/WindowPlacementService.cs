using System.IO;
using System.Text.Json;
using System.Windows;

namespace OpenMacroSwift.Desktop.Services;

public sealed class WindowPlacementService
{
    private readonly string settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenMacroSwift",
        "window.json");

    public void Restore(Window window)
    {
        if (!File.Exists(settingsPath)) return;

        try
        {
            var state = JsonSerializer.Deserialize<WindowStateDto>(File.ReadAllText(settingsPath));
            if (state is null) return;
            if (state.Left < -10000 || state.Top < -10000) return;
            if (!IsOnVirtualScreen(state)) return;

            window.Left = state.Left;
            window.Top = state.Top;
            window.Width = Math.Max(980, state.Width);
            window.Height = Math.Max(680, state.Height);
        }
        catch
        {
            // Bad local settings should never block the app from opening.
        }
    }

    public void Save(Window window)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        Rect bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;

        if (bounds.Left < -10000 || bounds.Top < -10000) return;

        var state = new WindowStateDto(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(state));
    }

    private static bool IsOnVirtualScreen(WindowStateDto state)
    {
        Rect screen = new(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        Rect window = new(state.Left, state.Top, state.Width, state.Height);
        return screen.IntersectsWith(window);
    }

    private sealed record WindowStateDto(double Left, double Top, double Width, double Height);
}

