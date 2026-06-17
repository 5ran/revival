using System;
using Avalonia;
using Avalonia.Media;
using Client.Services;
using Client.Services.Fishing;

namespace Client.ViewModels;

public sealed class LullabyModeOptionViewModel : ViewModelBase
{
    private const string DefaultProgressSpamAt = "100";
    private const string DefaultProgressStopSpamBelow = "0";
    private const string DefaultClickDelaySeconds = "0.1";

    public static LullabyModeOptionViewModel None { get; } = new("None", Fade("#8A8A8A"));

    public static LullabyModeOptionViewModel Resistant { get; } = new("Resistant", Fade("#EAD65C"));

    public static LullabyModeOptionViewModel Quickening { get; } = new("Quickening", FadeBetween("#FFFFFF", "#BEECF2"));

    public static LullabyModeOptionViewModel Strengthening { get; } = new("Strengthening", Fade("#C26D35"));

    public static LullabyModeOptionViewModel Fortuitous { get; } = new("Fortuitous", Fade("#C4F59A"));

    public static LullabyModeOptionViewModel Prismatic { get; } = new("Prismatic", new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#BEECF2"), 0.0),
            new GradientStop(Color.Parse("#C4F59A"), 1.0),
        },
    });

    public LullabyModeOptionViewModel(string name, IBrush brush)
    {
        Name = name;
        Brush = brush;
    }

    public string Name { get; }

    public IBrush Brush { get; }

    public override string ToString() => Name;

    public void ApplySnapshot(LullabyModeSettingsSnapshot snapshot)
    {
    }

    public LullabyModeSettingsSnapshot ToSnapshot()
    {
        return new LullabyModeSettingsSnapshot
        {
            Name = Name,
            ProgressSpamAt = DefaultProgressSpamAt,
            ProgressStopSpamBelow = DefaultProgressStopSpamBelow,
            ClickDelaySeconds = DefaultClickDelaySeconds,
        };
    }

    private static IBrush Fade(string hex)
    {
        return FadeBetween(hex, hex, 0x80);
    }

    private static IBrush FadeBetween(string startHex, string endHex, byte endAlpha = 0x80)
    {
        var start = Color.Parse(startHex);
        var end = Color.Parse(endHex);
        var faded = Color.FromArgb(endAlpha, end.R, end.G, end.B);
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(start, 0.0),
                new GradientStop(faded, 1.0),
            },
        };
    }
}
