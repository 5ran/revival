using System;

namespace Client.Services.Fishing;

public interface IOffsetsSource
{
    string Version { get; }
    bool IsPopulated { get; }
    bool TryGetOffset(string key, out ulong value);
}
