using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Client.Services.Fishing;

internal sealed class EmbeddedOffsetsSource : IOffsetsSource
{
    private static readonly Regex NamespaceRegex = new(@"^\s*namespace\s+([A-Za-z0-9_]+)\s*\{", RegexOptions.Compiled);
    private static readonly Regex OffsetRegex = new(@"^\s*inline\s+constexpr\s+uintptr_t\s+([A-Za-z0-9_]+)\s*=\s*(0x[0-9A-Fa-f]+|\d+)\s*;", RegexOptions.Compiled);
    private static readonly Regex VersionRegex = new(@"ClientVersion\s*=\s*""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex HeaderVersionRegex = new(@"Roblox Version\s*:\s*([^\s]+)", RegexOptions.Compiled);

    private readonly Dictionary<string, ulong> offsets = new(StringComparer.OrdinalIgnoreCase);

    public EmbeddedOffsetsSource()
    {
        Load();
    }

    public string Version { get; private set; } = string.Empty;

    public bool IsPopulated => offsets.Count > 0;

    public bool TryGetOffset(string key, out ulong value) => offsets.TryGetValue(key, out value);

    private void Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => string.Equals(name, "offsets.hpp", StringComparison.OrdinalIgnoreCase) ||
                                    name.EndsWith(".offsets.hpp", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException("Embedded offsets resource was not found.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException("Embedded offsets resource could not be opened.");
        }

        using var reader = new StreamReader(stream);
        LoadFromReader(reader);
    }

    private void LoadFromReader(TextReader reader)
    {
        var namespaces = new Stack<string>();
        string? rawLine;
        while ((rawLine = reader.ReadLine()) is not null)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(Version))
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
