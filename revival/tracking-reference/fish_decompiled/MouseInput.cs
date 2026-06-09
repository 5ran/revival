using System;
using System.Runtime.InteropServices;

internal static class MouseInput
{
	private const uint MouseEventLeftDown = 2u;

	private const uint MouseEventLeftUp = 4u;

	public static void LeftDown()
	{
		mouse_event(2u, 0u, 0u, 0u, UIntPtr.Zero);
	}

	public static void LeftUp()
	{
		mouse_event(4u, 0u, 0u, 0u, UIntPtr.Zero);
	}

	[DllImport("user32.dll")]
	private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}
