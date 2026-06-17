using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Client.Services.Fishing;

internal sealed class FileOffsetsSource : IOffsetsSource
{
    private static readonly Regex NamespaceRegex = new(@"^\s*namespace\s+([A-Za-z0-9_]+)\s*\{", RegexOptions.Compiled);
    private static readonly Regex OffsetRegex = new(@"^\s*inline\s+constexpr\s+uintptr_t\s+([A-Za-z0-9_]+)\s*=\s*(0x[0-9A-Fa-f]+|\d+)\s*;", RegexOptions.Compiled);
    private static readonly Regex VersionRegex = new(@"ClientVersion\s*=\s*""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex HeaderVersionRegex = new(@"Roblox Version\s*:\s*([^\s]+)", RegexOptions.Compiled);

    private readonly Dictionary<string, ulong> offsets = new(StringComparer.OrdinalIgnoreCase);

    public FileOffsetsSource(string path)
    {
        if (File.Exists(path))
        {
            Load(path);
        }
    }

    public string Version { get; private set; } = string.Empty;

    public bool IsPopulated => offsets.Count > 0;

    public bool TryGetOffset(string key, out ulong value) => offsets.TryGetValue(key, out value);

    private void Load(string path)
    {
        var namespaces = new Stack<string>();
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (Version is null)
            {
                var versionMatch = VersionRegex.Match(line);
                if (versionMatch.Success)
                {
                    Version = versionMatch.Groups[1].Value.Trim();
                }
                else
                {
                    var headerMatch = HeaderVersionRegex.Match(line);
                    if (headerMatch.Success)
                    {
                        Version = headerMatch.Groups[1].Value.Trim();
                    }
                }
            }

            var namespaceMatch = NamespaceRegex.Match(line);
            if (namespaceMatch.Success)
            {
                namespaces.Push(namespaceMatch.Groups[1].Value);
                continue;
            }

            if (line.StartsWith("}", StringComparison.Ordinal))
            {
                if (namespaces.Count > 0)
                {
                    namespaces.Pop();
                }
                continue;
            }

            var offsetMatch = OffsetRegex.Match(line);
            if (!offsetMatch.Success || namespaces.Count == 0)
            {
                continue;
            }

            var key = string.Join(".", namespaces.Reverse().Where(ns => !string.Equals(ns, "Offsets", StringComparison.OrdinalIgnoreCase)));
            var fullKey = key.Length == 0 ? offsetMatch.Groups[1].Value : key + "." + offsetMatch.Groups[1].Value;
            offsets[fullKey] = Convert.ToUInt64(offsetMatch.Groups[2].Value, 16);
        }
    }
}
