using System;

internal sealed class RobloxVersionMismatchException : InvalidOperationException
{
    public RobloxVersionMismatchException(string message)
        : base(message)
    {
    }
}
