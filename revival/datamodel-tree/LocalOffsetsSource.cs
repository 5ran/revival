using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Client.Services.Fishing;

namespace DataModelTreeViewer;

internal sealed class LocalOffsetsSource : IOffsetsSource
{
    private readonly Dictionary<string, ulong> offsets = new(StringComparer.OrdinalIgnoreCase);

    public string? Version { get; private set; }
    public bool IsPopulated => offsets.Count > 0;

    public static LocalOffsetsSource Load(string path)
    {
        var source = new LocalOffsetsSource();
        source.Reload(path);
        return source;
    }

    public void Reload(string path)
    {
        offsets.Clear();
        Version = null;

        if (!File.Exists(path))
        {
            return;
        }

        var entries = OffsetsParser.Parse(path);
        foreach (var entry in entries.Where(entry => entry.Kind.Equals("uintptr_t", StringComparison.OrdinalIgnoreCase)))
        {
            if (!TryParseInteger(entry.Value, out var value))
            {
                continue;
            }

            var key = entry.Category.StartsWith("Offsets.", StringComparison.OrdinalIgnoreCase)
                ? entry.Category["Offsets.".Length..] + "." + entry.Name
                : entry.Category + "." + entry.Name;
            offsets[key] = value;
            offsets[entry.Name] = value;
        }

        Version = entries.FirstOrDefault(entry =>
            entry.Category.Equals("Offsets", StringComparison.OrdinalIgnoreCase) &&
            entry.Name.Equals("Version", StringComparison.OrdinalIgnoreCase))?.Value;
    }

    public bool TryGetOffset(string key, out ulong value)
    {
        return offsets.TryGetValue(key, out value);
    }

    private static bool TryParseInteger(string text, out ulong value)
    {
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ulong.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
