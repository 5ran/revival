using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Client.Services.Fishing;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
internal sealed class FishSkipSoundDetector : IDisposable
{
    private static readonly HashSet<string> TargetSoundNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bite",
        "bitelegendary",
        "biteexotic",
        "bitecataclysmic",
        "bitespecial",
        "bitedivine",
    };

    private readonly OffsetTable offsets = new(OffsetsSourceProvider.Current);
    private Process? process;
    private ProcessMemory? memory;
    private ulong baseAddress;
    private string selfPlayerName = string.Empty;

    public void Reset()
    {
        selfPlayerName = string.Empty;
        DisposeMemory();
    }

    public FishSkipSoundSnapshot Poll()
    {
        try
        {
            EnsureAttached();
            if (memory is null || !OffsetsSourceProvider.Current.IsPopulated)
            {
                return FishSkipSoundSnapshot.Empty;
            }

            var dataModel = GetDataModel();
            if (!ProcessMemory.IsLikelyUserModeAddress(dataModel))
            {
                return FishSkipSoundSnapshot.Empty;
            }

            if (string.IsNullOrWhiteSpace(selfPlayerName))
            {
                selfPlayerName = FindLocalPlayerName(dataModel);
                if (!string.IsNullOrWhiteSpace(selfPlayerName))
                {
                    DebugLog.Write("FishSkipSoundDetector", $"self-player discovered name='{selfPlayerName}'");
                }
            }

            if (string.IsNullOrWhiteSpace(selfPlayerName))
            {
                return FishSkipSoundSnapshot.Empty;
            }

            var matches = FindSelfMatches(dataModel);
            return FishSkipSoundSnapshot.FromNames(matches.Select(match => match.DisplayName).ToList());
        }
        catch (Exception ex)
        {
            DebugLog.Write("FishSkipSoundDetector", $"poll failed error='{ex.Message}'");
            return FishSkipSoundSnapshot.Empty;
        }
    }

    public void Dispose()
    {
        DisposeMemory();
    }

    private void EnsureAttached()
    {
        if (process is { HasExited: false } && memory is not null)
        {
            return;
        }

        DisposeMemory();
        if (!OffsetsSourceProvider.Current.IsPopulated)
        {
            return;
        }

        process = Process.GetProcessesByName("RobloxPlayerBeta").FirstOrDefault()
            ?? Process.GetProcesses().FirstOrDefault(p => p.ProcessName.Contains("Roblox", StringComparison.OrdinalIgnoreCase));
        if (process is null)
        {
            return;
        }

        baseAddress = unchecked((ulong)process.MainModule!.BaseAddress.ToInt64());
        memory = ProcessMemory.Open(process.Id);
        DebugLog.Write("FishSkipSoundDetector", $"attached pid={process.Id} base=0x{baseAddress:X}");
    }

    private ulong GetDataModel()
    {
        if (memory is null)
        {
            return 0;
        }

        var fakeDataModel = memory.ReadUInt64(baseAddress + offsets.Get("FakeDataModel", "Pointer"));
        if (!ProcessMemory.IsLikelyUserModeAddress(fakeDataModel))
        {
            return 0;
        }

        var dataModel = memory.ReadUInt64(fakeDataModel + offsets.Get("FakeDataModel", "RealDataModel"));
        return ProcessMemory.IsLikelyUserModeAddress(dataModel) ? dataModel : 0;
    }

    private IReadOnlyList<SoundMatch> FindSelfMatches(ulong dataModel)
    {
        var matches = new List<SoundMatch>();
        if (string.IsNullOrWhiteSpace(selfPlayerName) || memory is null)
        {
            return matches;
        }

        var workspace = FindChildByName(dataModel, "Workspace");
        if (!ProcessMemory.IsLikelyUserModeAddress(workspace))
        {
            return matches;
        }

        var head = FindChildByName(FindChildByName(workspace, selfPlayerName), "Head");
        if (!ProcessMemory.IsLikelyUserModeAddress(head))
        {
            return matches;
        }

        foreach (var child in ReadChildren(head))
        {
            if (!ProcessMemory.IsLikelyUserModeAddress(child))
            {
                continue;
            }

            if (!string.Equals(ReadClass(child), "Sound", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = ReadName(child);
            if (!IsTargetSound(name))
            {
                continue;
            }

            var playbackSpeed = ReadFloat(child + offsets.Get("Sound", "PlaybackSpeed"));
            var volume = ReadFloat(child + offsets.Get("Sound", "Volume"));
            if (playbackSpeed <= 0.01f || volume <= 0f)
            {
                continue;
            }

            matches.Add(new SoundMatch(
                child,
                name,
                ReadSoundId(child),
                BuildPath(child)));
        }

        return matches;
    }

    private static bool IsTargetSound(string name)
    {
        return TargetSoundNames.Contains(name);
    }

    private string BuildPath(ulong instance)
    {
        var parts = new List<string>();
        var current = instance;
        for (var i = 0; i < 16 && ProcessMemory.IsLikelyUserModeAddress(current); i++)
        {
            var name = ReadName(current);
            if (!string.IsNullOrWhiteSpace(name))
            {
                parts.Add(name);
            }

            var parent = ReadParent(current);
            if (!ProcessMemory.IsLikelyUserModeAddress(parent) || parent == current)
            {
                break;
            }

            current = parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private string ReadSoundId(ulong instance)
    {
        if (memory is null)
        {
            return string.Empty;
        }

        var raw = memory.ReadUInt64(instance + offsets.Get("Sound", "SoundId"));
        return ProcessMemory.IsLikelyUserModeAddress(raw) ? memory.ReadRobloxString(raw) : string.Empty;
    }

    private float ReadFloat(ulong address)
    {
        if (memory is null)
        {
            return 0f;
        }

        var bytes = memory.ReadBytes(address, 4);
        return bytes is null ? 0f : BitConverter.ToSingle(bytes, 0);
    }

    private IReadOnlyList<ulong> ReadChildren(ulong instance)
    {
        if (memory is null || !ProcessMemory.IsLikelyUserModeAddress(instance))
        {
            return Array.Empty<ulong>();
        }

        var listPtr = memory.ReadUInt64(instance + offsets.Get("Instance", "ChildrenStart"));
        var start = memory.ReadUInt64(listPtr);
        var end = memory.ReadUInt64(listPtr + offsets.Get("Instance", "ChildrenEnd"));
        if (!LooksLikeVectorRange(start, end))
        {
            return Array.Empty<ulong>();
        }

        var children = new List<ulong>();
        for (var entry = start; entry < end && children.Count < 2000; entry += 0x10)
        {
            var child = memory.ReadUInt64(entry);
            if (ProcessMemory.IsLikelyUserModeAddress(child))
            {
                children.Add(child);
            }
        }

        return children;
    }

    private string ReadName(ulong instance)
    {
        if (memory is null)
        {
            return string.Empty;
        }

        var stringAddress = memory.ReadUInt64(instance + offsets.Get("Instance", "Name"));
        return memory.ReadRobloxString(stringAddress);
    }

    private string ReadClass(ulong instance)
    {
        if (memory is null)
        {
            return string.Empty;
        }

        var descriptor = memory.ReadUInt64(instance + offsets.Get("Instance", "ClassDescriptor"));
        var stringAddress = memory.ReadUInt64(descriptor + offsets.Get("Instance", "ClassName"));
        return memory.ReadRobloxString(stringAddress);
    }

    private ulong ReadParent(ulong instance)
    {
        if (memory is null || !ProcessMemory.IsLikelyUserModeAddress(instance))
        {
            return 0;
        }

        return memory.ReadUInt64(instance + offsets.Get("Instance", "Parent"));
    }

    private ulong FindChildByName(ulong parent, string name)
    {
        foreach (var child in ReadChildren(parent))
        {
            if (string.Equals(ReadName(child), name, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return 0;
    }

    private string FindLocalPlayerName(ulong dataModel)
    {
        var players = FindChildByName(dataModel, "Players");
        if (!ProcessMemory.IsLikelyUserModeAddress(players))
        {
            return string.Empty;
        }

        var queue = new Queue<(ulong Instance, int Depth)>();
        queue.Enqueue((players, 0));

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (!ProcessMemory.IsLikelyUserModeAddress(current) || depth > 8)
            {
                continue;
            }

            var currentName = ReadName(current);
            var currentClass = ReadClass(current);
            if (string.Equals(currentName, "PlayerGui", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentClass, "PlayerGui", StringComparison.OrdinalIgnoreCase))
            {
                var owner = FindOwningPlayerName(current);
                if (!string.IsNullOrWhiteSpace(owner))
                {
                    return owner;
                }
            }

            foreach (var child in ReadChildren(current))
            {
                queue.Enqueue((child, depth + 1));
            }
        }

        foreach (var player in ReadChildren(players))
        {
            if (!string.Equals(ReadClass(player), "Player", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (HasPlayerGui(player))
            {
                return ReadName(player);
            }
        }

        return string.Empty;
    }

    private string FindOwningPlayerName(ulong playerGui)
    {
        var current = ReadParent(playerGui);
        for (var i = 0; i < 8 && ProcessMemory.IsLikelyUserModeAddress(current); i++)
        {
            var name = ReadName(current);
            var className = ReadClass(current);
            if (string.Equals(className, "Player", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Player", StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }

            current = ReadParent(current);
        }

        return string.Empty;
    }

    private bool HasPlayerGui(ulong player)
    {
        var queue = new Queue<(ulong Instance, int Depth)>();
        queue.Enqueue((player, 0));

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (!ProcessMemory.IsLikelyUserModeAddress(current) || depth > 5)
            {
                continue;
            }

            if (string.Equals(ReadName(current), "PlayerGui", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ReadClass(current), "PlayerGui", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var child in ReadChildren(current))
            {
                queue.Enqueue((child, depth + 1));
            }
        }

        return false;
    }

    private static bool LooksLikeVectorRange(ulong start, ulong end)
    {
        return ProcessMemory.IsLikelyUserModeAddress(start) &&
               ProcessMemory.IsLikelyUserModeAddress(end) &&
               end >= start &&
               end - start <= 0x100000 &&
               (end - start) % 0x10 == 0;
    }

    private void DisposeMemory()
    {
        memory?.Dispose();
        memory = null;
        process = null;
        baseAddress = 0;
    }

    private sealed record SoundMatch(ulong InstanceAddress, string DisplayName, string SoundId, string Path);
}

[Obfuscation(Exclude = true, ApplyToMembers = true)]
internal readonly record struct FishSkipSoundSnapshot(
    bool HasBite,
    bool HasLegendary,
    bool HasOther,
    int MatchCount,
    string Summary)
{
    public static FishSkipSoundSnapshot Empty => new(false, false, false, 0, string.Empty);

    public bool HasAny => MatchCount > 0;

    public bool ShouldInvertForNormal =>
        HasBite && !HasLegendary && !HasOther;

    public bool ShouldInvertForLegendary =>
        HasLegendary && !HasOther;

    public static FishSkipSoundSnapshot FromNames(IReadOnlyCollection<string> names)
    {
        if (names.Count == 0)
        {
            return Empty;
        }

        var hasBite = names.Any(name => string.Equals(name, "bite", StringComparison.OrdinalIgnoreCase));
        var hasLegendary = names.Any(name => string.Equals(name, "bitelegendary", StringComparison.OrdinalIgnoreCase));
        var hasOther = names.Any(name => !string.Equals(name, "bite", StringComparison.OrdinalIgnoreCase) &&
                                           !string.Equals(name, "bitelegendary", StringComparison.OrdinalIgnoreCase));
        return new FishSkipSoundSnapshot(
            hasBite,
            hasLegendary,
            hasOther,
            names.Count,
            string.Join(",", names.Distinct(StringComparer.OrdinalIgnoreCase)));
    }
}
