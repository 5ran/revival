using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataModelTreeViewer;

internal sealed record OffsetEntry(string Category, string Name, string Value, string Kind);

internal static partial class OffsetsParser
{
    public static IReadOnlyList<OffsetEntry> Parse(string path)
    {
        if (!File.Exists(path))
        {
            return Array.Empty<OffsetEntry>();
        }

        var stack = new Stack<string>();
        var entries = new List<OffsetEntry>();
        foreach (var line in File.ReadLines(path))
        {
            var namespaceMatch = NamespaceRegex().Match(line);
            if (namespaceMatch.Success)
            {
                stack.Push(namespaceMatch.Groups[1].Value);
                continue;
            }

            if (line.Trim() == "}" && stack.Count > 0)
            {
                stack.Pop();
                continue;
            }

            var offsetMatch = OffsetRegex().Match(line);
            if (offsetMatch.Success)
            {
                var category = stack.Count == 0 ? "Offsets" : string.Join(".", stack.Reverse());
                entries.Add(new OffsetEntry(category, offsetMatch.Groups[1].Value, offsetMatch.Groups[2].Value, "uintptr_t"));
                continue;
            }

            var stringMatch = StringRegex().Match(line);
            if (stringMatch.Success)
            {
                var category = stack.Count == 0 ? "Offsets" : string.Join(".", stack.Reverse());
                entries.Add(new OffsetEntry(category, stringMatch.Groups[1].Value, stringMatch.Groups[2].Value, "string"));
            }
        }

        return entries;
    }

    [GeneratedRegex(@"namespace\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{")]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex(@"inline\s+constexpr\s+uintptr_t\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(0x[0-9A-Fa-f]+|\d+)")]
    private static partial Regex OffsetRegex();

    [GeneratedRegex(@"inline\s+std::string\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*""([^""]*)""")]
    private static partial Regex StringRegex();
}
