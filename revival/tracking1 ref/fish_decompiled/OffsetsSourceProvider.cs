using System;

namespace Client.Services.Fishing;

internal static class OffsetsSourceProvider
{
    private static IOffsetsSource _current = new EmptyOffsetsSource();

    public static void Register(IOffsetsSource source)
    {
        _current = source ?? throw new ArgumentNullException(nameof(source));
    }

    public static IOffsetsSource Current =>
        _current;

    private sealed class EmptyOffsetsSource : IOffsetsSource
    {
        public string Version => string.Empty;
        public bool IsPopulated => false;
        public bool TryGetOffset(string key, out ulong value)
        {
            value = 0;
            return false;
        }
    }
}
