using System;
using System.Diagnostics;

internal static class RobloxVersionGuard
{
    public static void EnsureCompatible(Process process, string expectedVersion)
    {
        expectedVersion = expectedVersion.Trim();
        if (string.IsNullOrWhiteSpace(expectedVersion))
        {
            return;
        }

        string actualVersion = GetProcessVersion(process);
        if (string.IsNullOrWhiteSpace(actualVersion))
        {
            return;
        }

        if (!string.Equals(actualVersion.Trim(), expectedVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new RobloxVersionMismatchException(
                "Roblox version mismatch. Update your offsets and make sure you're on the latest Roblox version.\n\n" +
                "Offsets: " + expectedVersion + "\n" +
                "Roblox: " + actualVersion.Trim());
        }
    }

    private static string GetProcessVersion(Process process)
    {
        try
        {
            return process.MainModule?.FileVersionInfo.ProductVersion
                ?? process.MainModule?.FileVersionInfo.FileVersion
                ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
