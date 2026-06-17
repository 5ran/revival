using System;

internal sealed class NoMinigameException : Exception
{
	public NoMinigameException(string message)
		: base(message)
	{
	}
}
