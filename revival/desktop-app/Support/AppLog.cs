using System;

namespace Client.Services;

internal static class AppLog
{
    public static void Info(string category, string message)
    {
        _ = category;
        _ = message;
    }

    public static void Error(string category, string message, Exception? ex = null)
    {
        _ = category;
        _ = message;
        _ = ex;
    }

    public static void Fishing(string category, string message)
    {
        _ = category;
        _ = message;
    }
}
