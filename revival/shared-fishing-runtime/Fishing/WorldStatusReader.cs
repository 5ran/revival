using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Client.Services.Fishing;

internal sealed class WorldStatusReader : IDisposable
{
    private static readonly TimeSpan SurgeMaxDuration = TimeSpan.FromMinutes(20);
    private readonly RobloxMemory _memory = new(OffsetsSourceProvider.Current);
    private readonly HashSet<string> _seenChatEntries = new(StringComparer.Ordinal);
    private bool _chatPrimed;
    private DateTimeOffset _lastDebugLogAt = DateTimeOffset.MinValue;
    private bool _shinySurgeActive;
    private bool _sparklingSurgeActive;
    private bool _mutationSurgeActive;
    private DateTimeOffset? _shinySurgeStartedAt;
    private DateTimeOffset? _sparklingSurgeStartedAt;
    private DateTimeOffset? _mutationSurgeStartedAt;

    public void Dispose()
    {
        _memory.Dispose();
    }

    public bool TryRead(
        out string weather,
        out string secondaryWeather,
        out string eventWeather,
        out string cycle,
        out bool shinySurge,
        out bool sparklingSurge,
        out bool mutationSurge)
    {
        weather = string.Empty;
        secondaryWeather = string.Empty;
        eventWeather = string.Empty;
        cycle = string.Empty;
        shinySurge = false;
        sparklingSurge = false;
        mutationSurge = false;

        try
        {
            _memory.EnsureAttached();
            var world = GetWorldConfig();
            var weatherText = GetCurrentWeather(world);
            var meteorologicalText = GetCurrentMeteorological(world);
            var eventText = GetCurrentEvent(world);
            var cycleText = GetCurrentCycle();

            UpdateSurgesFromChat();
            shinySurge = _shinySurgeActive;
            sparklingSurge = _sparklingSurgeActive;
            mutationSurge = _mutationSurgeActive;

            weather = NormalizeWorldNone(weatherText);
            secondaryWeather = ResolveMeteorological(meteorologicalText, eventText);
            eventWeather = ResolveEventDisplay(eventText);
            cycle = ResolveCycle(cycleText);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void UpdateSurgesFromChat()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = ReadChatEntries();
        if (!_chatPrimed)
        {
            foreach (var entry in entries)
            {
                _seenChatEntries.Add($"{entry.Address}:{Normalize(entry.Text)}");
            }
            _chatPrimed = true;
            return;
        }

        foreach (var entry in entries)
        {
            var entryKey = $"{entry.Address}:{Normalize(entry.Text)}";
            if (!_seenChatEntries.Add(entryKey))
            {
                continue;
            }

            var text = Normalize(entry.Text);
            if (text.Length == 0)
            {
                continue;
            }

            if (ContainsPhrase(text, "There is currently a Shiny surge"))
            {
                _shinySurgeActive = true;
                _sparklingSurgeActive = false;
                _mutationSurgeActive = false;
                _shinySurgeStartedAt = now;
                _sparklingSurgeStartedAt = null;
                _mutationSurgeStartedAt = null;
                AppLog.Info("WorldStatusReader", $"surge chat active: shiny | '{text}'");
            }
            else if (ContainsPhrase(text, "Shiny Surge is now over"))
            {
                _shinySurgeActive = false;
                _shinySurgeStartedAt = null;
                AppLog.Info("WorldStatusReader", $"surge chat gone: shiny | '{text}'");
            }

            if (ContainsPhrase(text, "Today is the Day of the Luminous") ||
                ContainsPhrase(text, "Tonight is the Night of the Luminous"))
            {
                _shinySurgeActive = false;
                _sparklingSurgeActive = true;
                _mutationSurgeActive = false;
                _shinySurgeStartedAt = null;
                _sparklingSurgeStartedAt = now;
                _mutationSurgeStartedAt = null;
                AppLog.Info("WorldStatusReader", $"surge chat active: sparkling | '{text}'");
            }
            else if (ContainsPhrase(text, "Night of the Luminous is now over"))
            {
                _sparklingSurgeActive = false;
                _sparklingSurgeStartedAt = null;
                AppLog.Info("WorldStatusReader", $"surge chat gone: sparkling | '{text}'");
            }

            if (ContainsPhrase(text, "There is currently a Mutation surge"))
            {
                _shinySurgeActive = false;
                _sparklingSurgeActive = false;
                _mutationSurgeActive = true;
                _shinySurgeStartedAt = null;
                _sparklingSurgeStartedAt = null;
                _mutationSurgeStartedAt = now;
                AppLog.Info("WorldStatusReader", $"surge chat active: mutation | '{text}'");
            }
            else if (ContainsPhrase(text, "Mutation Surge is now over"))
            {
                _mutationSurgeActive = false;
                _mutationSurgeStartedAt = null;
                AppLog.Info("WorldStatusReader", $"surge chat gone: mutation | '{text}'");
            }
        }

        if (_shinySurgeActive && _shinySurgeStartedAt is { } shinySince && now - shinySince >= SurgeMaxDuration)
        {
            _shinySurgeActive = false;
            _shinySurgeStartedAt = null;
        }

        if (_sparklingSurgeActive && _sparklingSurgeStartedAt is { } sparklingSince && now - sparklingSince >= SurgeMaxDuration)
        {
            _sparklingSurgeActive = false;
            _sparklingSurgeStartedAt = null;
        }

        if (_mutationSurgeActive && _mutationSurgeStartedAt is { } mutationSince && now - mutationSince >= SurgeMaxDuration)
        {
            _mutationSurgeActive = false;
            _mutationSurgeStartedAt = null;
        }

        if (now - _lastDebugLogAt >= TimeSpan.FromSeconds(2))
        {
            _lastDebugLogAt = now;
            AppLog.Info(
                "WorldStatusReader",
                $"surge state: shiny={_shinySurgeActive}, sparkling={_sparklingSurgeActive}, mutation={_mutationSurgeActive}, seen={_seenChatEntries.Count}");
        }
    }

    private List<ChatEntry> ReadChatEntries()
    {
        var results = new List<ChatEntry>();
        var dataModel = _memory.GetDataModel();
        if (dataModel == 0)
        {
            return results;
        }

        CollectTextChatServiceMessages(results, dataModel);
        CollectExperienceChatUiBodyText(results, dataModel);
        return results;
    }

    private void CollectTextChatServiceMessages(List<ChatEntry> results, ulong dataModel)
    {
        var textChatService = _memory.FindDescendantByClass(dataModel, "TextChatService");
        if (textChatService == 0)
        {
            return;
        }

        var stack = new Stack<ulong>();
        var seen = new HashSet<ulong>();
        stack.Push(textChatService);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!seen.Add(current))
            {
                continue;
            }

            var className = _memory.ReadClass(current);
            if (className.Contains("TextChatMessage", StringComparison.OrdinalIgnoreCase))
            {
                var text = ReadTextChatMessageText(current);
                if (text.Length > 0)
                {
                    results.Add(new ChatEntry(current, text));
                }
            }

            foreach (var child in _memory.ReadChildren(current))
            {
                stack.Push(child);
            }
        }
    }

    private void CollectExperienceChatUiBodyText(List<ChatEntry> results, ulong dataModel)
    {
        var coreGui = _memory.FindDescendantByClass(dataModel, "CoreGui");
        if (coreGui == 0)
        {
            return;
        }

        var experienceChat = _memory.FindDescendantByName(coreGui, "ExperienceChat");
        if (experienceChat == 0)
        {
            return;
        }

        var stack = new Stack<ulong>();
        var seen = new HashSet<ulong>();
        stack.Push(experienceChat);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!seen.Add(current))
            {
                continue;
            }

            var name = _memory.ReadName(current);
            var className = _memory.ReadClass(current);
            if (string.Equals(name, "BodyText", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(className, "TextLabel", StringComparison.OrdinalIgnoreCase))
            {
                var text = Normalize(_memory.ReadGuiText(current));
                if (text.Length > 0)
                {
                    results.Add(new ChatEntry(current, text));
                }
            }

            foreach (var child in _memory.ReadChildren(current))
            {
                stack.Push(child);
            }
        }
    }

    private string ReadTextChatMessageText(ulong address)
    {
        if (address == 0)
        {
            return string.Empty;
        }

        var text = Normalize(_memory.ReadGuiText(address));
        if (text.Length > 0)
        {
            return text;
        }

        try
        {
            var textOffset = _memory.GetOffset("Text");
            var indirect = Normalize(_memory.ReadString(_memory.ReadPtr(address + textOffset)));
            if (indirect.Length > 0)
            {
                return indirect;
            }
        }
        catch
        {
            // Best-effort chat parsing for status UI.
        }

        return string.Empty;
    }

    private ulong GetWorldConfig()
    {
        var dataModel = _memory.GetDataModel();
        if (dataModel == 0)
        {
            return 0;
        }

        var replicatedStorage = _memory.FindChildByClass(dataModel, "ReplicatedStorage");
        if (replicatedStorage == 0)
        {
            return 0;
        }

        var world = _memory.FindChildByName(replicatedStorage, "world");
        return world;
    }

    private static string NormalizeWorldNone(string value)
    {
        var trimmed = value.Trim();
        return string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase) ? string.Empty : trimmed;
    }

    private static string ReadWorldStringValue(RobloxMemory memory, ulong instanceAddr)
    {
        if (instanceAddr == 0)
        {
            return string.Empty;
        }

        try
        {
            var valueOffset = memory.GetOffset("Value");
            var embedded = memory.ReadString(instanceAddr + valueOffset);
            if (!string.IsNullOrWhiteSpace(embedded))
            {
                return embedded.Trim();
            }

            var ptr = memory.ReadPtr(instanceAddr + valueOffset);
            if (ptr != 0)
            {
                var indirect = memory.ReadString(ptr);
                if (!string.IsNullOrWhiteSpace(indirect))
                {
                    return indirect.Trim();
                }
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private string GetCurrentWeather(ulong world)
    {
        if (world == 0)
        {
            return string.Empty;
        }

        var weather = _memory.FindChildByName(world, "weather");
        return weather == 0 ? string.Empty : NormalizeWorldNone(ReadWorldStringValue(_memory, weather));
    }

    private string GetCurrentMeteorological(ulong world)
    {
        if (world == 0)
        {
            return string.Empty;
        }

        var weather = _memory.FindChildByName(world, "weather");
        if (weather == 0)
        {
            return string.Empty;
        }

        var meteorological = _memory.FindChildByName(weather, "meteorological");
        return meteorological == 0 ? string.Empty : NormalizeWorldNone(ReadWorldStringValue(_memory, meteorological));
    }

    private string GetCurrentEvent(ulong world)
    {
        if (world == 0)
        {
            return string.Empty;
        }

        var eventInst = _memory.FindChildByName(world, "event");
        return eventInst == 0 ? string.Empty : NormalizeWorldNone(ReadWorldStringValue(_memory, eventInst));
    }

    private string GetCurrentCycle()
    {
        var world = GetWorldConfig();
        if (world == 0)
        {
            return string.Empty;
        }

        var cycle = _memory.FindChildByName(world, "cycle");
        return cycle == 0 ? string.Empty : NormalizeWorldNone(ReadWorldStringValue(_memory, cycle));
    }

    private static string ResolveMeteorological(string meteorologicalText, string eventText)
    {
        if (Contains(meteorologicalText, "aurora"))
        {
            return "Aurora Borealis";
        }

        if (Contains(eventText, "night of the luminous"))
        {
            return "Sparkling";
        }

        if (Contains(eventText, "starfall"))
        {
            return "Starfall";
        }

        if (Contains(eventText, "eclipse"))
        {
            return "Eclipse";
        }

        if (Contains(eventText, "rainbow"))
        {
            return "Rainbow";
        }

        return string.Empty;
    }

    private static string ResolveEventDisplay(string eventText)
    {
        if (Contains(eventText, "night of the luminous"))
        {
            return "Sparkling";
        }

        return NormalizeWorldNone(eventText);
    }

    private static string ResolveCycle(string cycleText)
    {
        if (ContainsWholeWord(cycleText, "night"))
        {
            return "Night";
        }

        if (ContainsWholeWord(cycleText, "day"))
        {
            return "Day";
        }

        return string.Empty;
    }

    private static bool Contains(string text, string needle)
    {
        return text.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsWholeWord(string text, string word)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
    }

    private static bool ContainsPhrase(string text, string phrase)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }

        var parts = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var pattern = $@"\b{string.Join(@"\s+", Array.ConvertAll(parts, Regex.Escape))}\b";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    private static bool AnyStatusMatches(
        IReadOnlyList<string> statuses,
        string phrase,
        bool requireActiveSignal = false,
        bool requirePlusMarker = false)
    {
        foreach (var status in statuses)
        {
            if (!IsNamedStatus(status, phrase))
            {
                continue;
            }

            if (requirePlusMarker && !HasPlusMarkerForPhrase(status, phrase))
            {
                continue;
            }

            if (requireActiveSignal && !HasActiveStatusSignal(status))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool HasActiveStatusSignal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        return Regex.IsMatch(normalized, @"\d", RegexOptions.CultureInvariant) ||
            normalized.Contains('%', StringComparison.Ordinal) ||
            normalized.Contains('+', StringComparison.Ordinal) ||
            ContainsPhrase(normalized, "increased") ||
            ContainsPhrase(normalized, "boost");
    }

    private static bool HasPlusMarkerForPhrase(string text, string phrase)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }

        var parts = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var phrasePattern = string.Join(@"\s+", Array.ConvertAll(parts, Regex.Escape));
        return Regex.IsMatch(
            text,
            $@"\+\s*{phrasePattern}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool IsNamedStatus(string text, string phrase)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }

        var haystack = text.Trim().ToLowerInvariant();
        var needle = phrase.Trim().ToLowerInvariant();
        if (!haystack.StartsWith(needle, StringComparison.Ordinal))
        {
            return false;
        }

        if (haystack.Length == needle.Length)
        {
            return true;
        }

        var next = haystack[needle.Length];
        return next == ' ' || next == ':' || next == '-' || next == '(';
    }

    private static string Normalize(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
    }

    private readonly record struct ChatEntry(ulong Address, string Text);
}
