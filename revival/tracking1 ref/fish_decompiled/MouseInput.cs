using System;
using System.Runtime.InteropServices;

internal static class MouseInput
{
	private const uint InputMouse = 0u;

	private const uint MouseEventFLeftDown = 2u;

	private const uint MouseEventFLeftUp = 4u;
	private const uint MouseEventFRightDown = 8u;
	private const uint MouseEventFRightUp = 16u;
	private const int VkRButton = 0x02;

	public static void LeftDown()
	{
		DebugLog.Write("MouseInput.LeftDown", "sending");
		SendMouse(MouseEventFLeftDown);
	}

	public static void LeftUp()
	{
		DebugLog.Write("MouseInput.LeftUp", "sending");
		SendMouse(MouseEventFLeftUp);
	}

	public static void RightDown()
	{
		DebugLog.Write("MouseInput.RightDown", "sending");
		SendMouse(MouseEventFRightDown);
	}

	public static void RightUp()
	{
		DebugLog.Write("MouseInput.RightUp", "sending");
		SendMouse(MouseEventFRightUp);
	}

	public static bool IsRightButtonDown()
	{
		return (GetAsyncKeyState(VkRButton) & 0x8000) != 0;
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int vKey);

	private static void SendMouse(uint flags)
	{
		INPUT[] inputs = new INPUT[1];
		inputs[0].type = 0u;
		inputs[0].U.mi = new MOUSEINPUT
		{
			dwFlags = flags
		};
		var sent = SendInput(1u, inputs, Marshal.SizeOf<INPUT>());
		DebugLog.Write("MouseInput.SendMouse", $"flags={flags} sent={sent} lastError={Marshal.GetLastWin32Error()}");
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct INPUTUNION
	{
		[FieldOffset(0)]
		public MOUSEINPUT mi;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct INPUT
	{
		public uint type;
		public INPUTUNION U;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MOUSEINPUT
	{
		public int dx;
		public int dy;
		public uint mouseData;
		public uint dwFlags;
		public uint time;
		public UIntPtr dwExtraInfo;
	}
}
