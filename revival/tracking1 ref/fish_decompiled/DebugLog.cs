using System;
using System.IO;

internal static class DebugLog
{
    private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tracking1_debug.log");

    public static void Clear()
    {
        try
        {
            if (File.Exists(LogPath))
            {
                File.Delete(LogPath);
            }
        }
        catch
        {
        }
    }

    public static void Write(string source, string message)
    {
        // Intentionally disabled in the shareable build.
    }
}
