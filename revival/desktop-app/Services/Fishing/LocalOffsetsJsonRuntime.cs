using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Services.Fishing;

internal sealed class LocalOffsetsJsonRuntime : IOffsetsRuntime
{
    private readonly Dictionary<string, ulong> offsets = new(StringComparer.OrdinalIgnoreCase);

    public LocalOffsetsJsonRuntime()
    {
        LoadFromDisk();
        OffsetsSourceProvider.Register(this);
    }

    public string? Version { get; private set; }

    public bool IsPopulated => offsets.Count > 0;

    public bool TryGetOffset(string key, out ulong value) => offsets.TryGetValue(key, out value);

    public Task RefreshAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        _ = accessToken;
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    public void Clear()
    {
        // Keep local offsets loaded for the WPF overlay path.
    }

    private void LoadFromDisk()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "offsets.json"),
            Path.Combine(Environment.CurrentDirectory, "offsets.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "offsets.json")),
        ];

        string? path = null;
        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                path = candidate;
                break;
            }
        }

        if (path is null)
        {
            throw new FileNotFoundException("offsets.json not found next to the WPF executable.");
        }

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        if (root.TryGetProperty("Roblox Version", out var versionElement) && versionElement.ValueKind == JsonValueKind.String)
        {
            Version = versionElement.GetString();
        }
        else if (root.TryGetProperty("version", out var lowerVersion) && lowerVersion.ValueKind == JsonValueKind.String)
        {
            Version = lowerVersion.GetString();
        }

        if (!root.TryGetProperty("Offsets", out var offsetsElement) || offsetsElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("offsets.json missing Offsets object.");
        }

        foreach (var ns in offsetsElement.EnumerateObject())
        {
            if (ns.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var field in ns.Value.EnumerateObject())
            {
                if (!TryReadUInt64(field.Value, out var value))
                {
                    continue;
                }

                offsets[$"{ns.Name}.{field.Name}"] = value;
                offsets.TryAdd(field.Name, value);
            }
        }

        if (offsets.Count == 0)
        {
            throw new InvalidDataException("offsets.json contained no usable offsets.");
        }
    }

    private static bool TryReadUInt64(JsonElement element, out ulong value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetUInt64(out value),
            JsonValueKind.String => TryParseNumeric(element.GetString(), out value),
            _ => false,
        };
    }

    private static bool TryParseNumeric(string? text, out ulong value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();
        var style = NumberStyles.Integer;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
            style = NumberStyles.HexNumber;
        }

        return ulong.TryParse(text, style, CultureInfo.InvariantCulture, out value);
    }
}
