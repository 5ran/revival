using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Services.Fishing;

namespace Client.Services;

/// <summary>
/// Loads the local offsets snapshot from disk and keeps it in memory for the
/// lifetime of the process. Refresh and clear are intentionally no-ops so the
/// runtime keeps using the same offsets once the file has been read.
/// </summary>
public sealed class OffsetsService : IOffsetsRuntime
{
    private readonly Dictionary<string, ulong> _offsets = new(StringComparer.OrdinalIgnoreCase);

    public OffsetsService()
    {
        AppLog.Info("OffsetsService", $"Ctor baseDir={AppContext.BaseDirectory} cwd={Environment.CurrentDirectory}");
        LoadFromDisk();
        OffsetsSourceProvider.Register(this);
    }

    public string? Version { get; private set; }

    public bool IsPopulated => _offsets.Count > 0;

    public bool TryGetOffset(string key, out ulong value) => _offsets.TryGetValue(key, out value);

    public Task RefreshAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        _ = accessToken;
        _ = cancellationToken;
        AppLog.Info("OffsetsService", "Refresh skipped; using local offsets snapshot.");
        return Task.CompletedTask;
    }

    public void Clear()
    {
        AppLog.Info("OffsetsService", "Clear skipped; keeping local offsets snapshot.");
    }

    private void LoadFromDisk()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "offsets.hpp");

        if (!File.Exists(path))
        {
            AppLog.Error("OffsetsService", $"Missing offsets file {path}.");
            throw new FileNotFoundException("offsets.hpp not found next to the executable.", path);
        }

        AppLog.Info("OffsetsService", $"Using offsets file {path}");
        LoadHeader(path);

        if (_offsets.Count == 0)
        {
            throw new InvalidDataException("Offsets file contained no usable offsets.");
        }

        AppLog.Info("OffsetsService", $"Loaded {_offsets.Count} offsets from {path}. version={Version ?? "none"}.");
    }

    private void LoadHeader(string path)
    {
        var namespaceStack = new Stack<string>();

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (Version is null && TryReadHeaderVersion(line, out var version))
            {
                Version = version;
            }

            if (line.StartsWith("namespace ", StringComparison.Ordinal))
            {
                var name = line["namespace ".Length..].Trim();
                var brace = name.IndexOf('{');
                if (brace >= 0)
                {
                    name = name[..brace].Trim();
                }

                if (!string.Equals(name, "Offsets", StringComparison.Ordinal))
                {
                    namespaceStack.Push(name);
                }

                continue;
            }

            if (line.StartsWith("inline constexpr uintptr_t ", StringComparison.Ordinal))
            {
                var remainder = line["inline constexpr uintptr_t ".Length..];
                var equals = remainder.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                var name = remainder[..equals].Trim();
                var valueText = remainder[(equals + 1)..].Trim().TrimEnd(';');
                if (valueText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    valueText = valueText[2..];
                }

                if (!ulong.TryParse(valueText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                {
                    continue;
                }

                var key = namespaceStack.Count == 0
                    ? name
                    : string.Join(".", namespaceStack.Reverse()) + "." + name;
                _offsets[key] = value;
                _offsets.TryAdd(name, value);
            }
            else if (line == "}" && namespaceStack.Count > 0)
            {
                namespaceStack.Pop();
            }
        }

        if (Version is null)
        {
            Version = Path.GetFileNameWithoutExtension(path);
        }

        AppLog.Info("OffsetsService", $"Parsed header offsets. count={_offsets.Count} version={Version ?? "none"}");
    }

    private static bool TryReadHeaderVersion(string line, out string? version)
    {
        version = null;

        if (line.Contains("ClientVersion", StringComparison.Ordinal))
        {
            var equals = line.IndexOf('=');
            if (equals < 0)
            {
                return false;
            }

            var firstQuote = line.IndexOf('"', equals + 1);
            if (firstQuote < 0)
            {
                return false;
            }

            var secondQuote = line.IndexOf('"', firstQuote + 1);
            if (secondQuote <= firstQuote)
            {
                return false;
            }

            version = line[(firstQuote + 1)..secondQuote].Trim();
            return !string.IsNullOrWhiteSpace(version);
        }

        if (line.Contains("Roblox Version", StringComparison.Ordinal))
        {
            var colon = line.LastIndexOf(':');
            if (colon < 0)
            {
                return false;
            }

            version = line[(colon + 1)..].Trim().TrimEnd('*', '/', ' ');
            return !string.IsNullOrWhiteSpace(version);
        }

        return false;
    }
}
