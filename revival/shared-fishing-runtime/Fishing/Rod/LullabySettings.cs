using System;
using System.Collections.Generic;
using Client.Services;

namespace Client.Services.Fishing;

internal sealed class LullabyModeConfig
{
    public LullabyModeConfig(string name, double progressSpamAt, double progressStopSpamBelow, double clickDelaySeconds)
    {
        Name = name;
        ProgressSpamAt = progressSpamAt;
        ProgressStopSpamBelow = progressStopSpamBelow;
        ClickDelaySeconds = clickDelaySeconds;
    }

    public string Name { get; }

    public double ProgressSpamAt { get; set; }

    public double ProgressStopSpamBelow { get; set; }

    public double ClickDelaySeconds { get; set; }
}

internal static class LullabySettings
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, LullabyModeConfig> Modes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["None"] = new("None", 100, 0, 0.1),
        ["Resistant"] = new("Resistant", 100, 0, 0.1),
        ["Quickening"] = new("Quickening", 100, 0, 0.1),
        ["Strengthening"] = new("Strengthening", 100, 0, 0.1),
        ["Fortuitous"] = new("Fortuitous", 100, 0, 0.1),
        ["Prismatic"] = new("Prismatic", 100, 0, 0.1),
    };

    private static string _selectedModeName = "None";

    public static string SelectedModeName
    {
        get
        {
            lock (SyncRoot)
            {
                return _selectedModeName;
            }
        }
        set
        {
            lock (SyncRoot)
            {
                _selectedModeName = Modes.ContainsKey(value) ? value : "None";
            }
        }
    }

    public static LullabyModeConfig Current
    {
        get
        {
            lock (SyncRoot)
            {
                return Modes[_selectedModeName];
            }
        }
    }

    public static IReadOnlyList<LullabyModeConfig> All
    {
        get
        {
            lock (SyncRoot)
            {
                return new List<LullabyModeConfig>(Modes.Values);
            }
        }
    }

    public static void UpdateMode(string name, double progressSpamAt, double progressStopSpamBelow, double clickDelaySeconds)
    {
        lock (SyncRoot)
        {
            if (!Modes.TryGetValue(name, out var config))
            {
                config = new LullabyModeConfig(name, progressSpamAt, progressStopSpamBelow, clickDelaySeconds);
                Modes[name] = config;
            }
            else
            {
                config.ProgressSpamAt = progressSpamAt;
                config.ProgressStopSpamBelow = progressStopSpamBelow;
                config.ClickDelaySeconds = clickDelaySeconds;
            }
        }
    }

    public static void ApplySnapshots(IEnumerable<LullabyModeSettingsSnapshot>? snapshots)
    {
        lock (SyncRoot)
        {
            if (snapshots is null)
            {
                return;
            }

            foreach (var snapshot in snapshots)
            {
                if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.Name))
                {
                    continue;
                }

                var start = Parse(snapshot.ProgressSpamAt, 100);
                var stop = Parse(snapshot.ProgressStopSpamBelow, 0);
                var click = Parse(snapshot.ClickDelaySeconds, 0.1);
                UpdateMode(snapshot.Name, start, stop, click);
            }
        }
    }

    public static LullabyModeSettingsSnapshot[] ExportSnapshots()
    {
        lock (SyncRoot)
        {
            var list = new List<LullabyModeSettingsSnapshot>(Modes.Count);
            foreach (var config in Modes.Values)
            {
                list.Add(new LullabyModeSettingsSnapshot
                {
                    Name = config.Name,
                    ProgressSpamAt = config.ProgressSpamAt.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ProgressStopSpamBelow = config.ProgressStopSpamBelow.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ClickDelaySeconds = config.ClickDelaySeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                });
            }

            return list.ToArray();
        }
    }

    private static double Parse(string? text, double fallback)
    {
        if (double.TryParse((text ?? string.Empty).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ||
            double.TryParse((text ?? string.Empty).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out value))
        {
            return value;
        }

        return fallback;
    }
}
