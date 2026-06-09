using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Client.Services.Fishing;

internal sealed class ReelLocator : IDisposable
{
	private const int MaxDepth = 128;

	private const int MaxNodes = 60000;

	private Process process;

	private ProcessMemory memory;

	private OffsetTable offsets;

	private ulong baseAddress;

	private ulong reelAddress;

	private ulong fishAddress;

	private ulong playerbarAddress;

	private ulong containerAddress;

	private IntPtr robloxWindow;

	private int framesUntilRescan;

	public bool HasTargets
	{
		get
		{
			return reelAddress != 0 && fishAddress != 0 && playerbarAddress != 0 && containerAddress != 0;
		}
	}

	public ReelSnapshot ReadSnapshot()
	{
		EnsureConnected();
		if (fishAddress == 0 || playerbarAddress == 0 || containerAddress == 0 || framesUntilRescan <= 0)
		{
			FindTargets();
			framesUntilRescan = 50;
		}
		framesUntilRescan--;
		if (!TryReadGuiBounds(fishAddress, out var position, out var size) || !TryReadGuiBounds(playerbarAddress, out var position2, out var size2) || !TryReadGuiBounds(containerAddress, out var position3, out var size3))
		{
			ResetTargets();
			throw new NoMinigameException("Found reel objects, but one or more GUI bounds are not readable.");
		}
		if (!IsReelActive())
		{
			ResetTargets();
			throw new NoMinigameException("Reel exists, but the minigame is not active.");
		}
		Point clientScreenOrigin = GetClientScreenOrigin(robloxWindow);
		RectangleF rectangleF = new RectangleF((float)clientScreenOrigin.X + position3.X, (float)clientScreenOrigin.Y + position3.Y, size3.X, size3.Y);
		Rectangle clientScreenRectangle = GetClientScreenRectangle(robloxWindow);
		if (!IntersectsWithPadding(rectangleF, clientScreenRectangle, 2f))
		{
			ResetTargets();
			throw new NoMinigameException("Reel exists, but its container is outside the Roblox window.");
		}
		if (rectangleF.Width < 20f || rectangleF.Height < 8f)
		{
			ResetTargets();
			throw new NoMinigameException("Reel exists, but its active container is too small.");
		}
		float num = (float)clientScreenOrigin.X + position.X + size.X * 0.5f;
		float y = (float)clientScreenOrigin.Y + position.Y + size.Y * 0.5f;
		float num2 = (float)clientScreenOrigin.X + position2.X + size2.X * 0.5f;
		float y2 = (float)clientScreenOrigin.Y + position2.Y + size2.Y * 0.5f;
		if (!ContainsWithPadding(rectangleF, num, y, 4f) || !ContainsWithPadding(rectangleF, num2, y2, 4f))
		{
			ResetTargets();
			throw new NoMinigameException("Reel exists, but fish/playerbar are not inside the active container.");
		}
		return new ReelSnapshot(rectangleF, num, num2, Math.Max(1f, size2.X));
	}

	public void ResetTargets()
	{
		reelAddress = 0uL;
		fishAddress = 0uL;
		playerbarAddress = 0uL;
		containerAddress = 0uL;
		framesUntilRescan = 0;
	}

	private void EnsureConnected()
	{
		if (process == null || process.HasExited || memory == null)
		{
			DisposeMemory();
			offsets = new OffsetTable(OffsetsSourceProvider.Current);
			process = FindRobloxProcess();
			robloxWindow = process.MainWindowHandle;
			if (robloxWindow == IntPtr.Zero)
			{
				throw new InvalidOperationException("Found Roblox, but its main window handle is not ready.");
			}
			baseAddress = GetMainModuleBase(process);
			memory = ProcessMemory.Open(process.Id);
			ResetTargets();
		}
	}

	private void FindTargets()
	{
		ulong num = memory.ReadPtr(baseAddress + offsets.Get("FakeDataModel", "Pointer"));
		ulong num2 = memory.ReadPtr(num + offsets.Get("FakeDataModel", "RealDataModel"));
		if (!ProcessMemory.IsLikelyUserModeAddress(num2))
		{
			throw new InvalidOperationException("DataModel pointer resolved to an invalid address.");
		}
		ulong num3 = FindLocalPlayerGui(num2);
		if (num3 == 0)
		{
			throw new NoMinigameException("Could not find LocalPlayer.PlayerGui.");
		}
		ReelTargets reelTargets = FindReelTargets(num3);
		if (reelTargets == null)
		{
			throw new NoMinigameException("Could not find an active PlayerGui.reel with readable fish/playerbar bounds.");
		}
		reelAddress = reelTargets.Reel;
		fishAddress = reelTargets.Fish;
		playerbarAddress = reelTargets.Playerbar;
		containerAddress = reelTargets.Container;
	}

	private ReelTargets FindReelTargets(ulong root)
	{
		Queue<Tuple<ulong, int>> queue = new Queue<Tuple<ulong, int>>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		queue.Enqueue(Tuple.Create(root, 0));
		int num = 0;
		while (queue.Count > 0 && num++ < 60000)
		{
			Tuple<ulong, int> tuple = queue.Dequeue();
			ulong item = tuple.Item1;
			int item2 = tuple.Item2;
			if (!hashSet.Add(item))
			{
				continue;
			}
			string a = ReadInstanceName(item);
			if (string.Equals(a, "reel", StringComparison.OrdinalIgnoreCase) && IsClass(item, "ScreenGui"))
			{
				ulong num2 = FindDescendantByName(item, "fish");
				ulong num3 = FindDescendantByName(item, "playerbar");
				if (num2 != 0 && num3 != 0)
				{
					ulong num4 = memory.ReadPtr(num3 + offsets.Get("Instance", "Parent"));
					if (!ProcessMemory.IsLikelyUserModeAddress(num4))
					{
						num4 = item;
					}
					if (HasReadableGuiBounds(num2) && HasReadableGuiBounds(num3) && HasReadableGuiBounds(num4))
					{
						return new ReelTargets(item, num2, num3, num4);
					}
				}
			}
			if (item2 >= 128)
			{
				continue;
			}
			foreach (ulong item3 in EnumerateChildren(item))
			{
				queue.Enqueue(Tuple.Create(item3, item2 + 1));
			}
		}
		return null;
	}

	private ulong FindLocalPlayerGui(ulong dataModel)
	{
		ulong num = FindDescendantByClass(dataModel, "Players");
		if (num == 0)
		{
			return 0uL;
		}
		ulong num2 = memory.ReadPtr(num + offsets.Get("Player", "LocalPlayer"));
		if (!ProcessMemory.IsLikelyUserModeAddress(num2))
		{
			return 0uL;
		}
		ulong num3 = FindDescendantByClass(num2, "PlayerGui");
		if (num3 != 0)
		{
			return num3;
		}
		return FindDescendantByName(num2, "PlayerGui");
	}

	private ulong FindDescendantByClass(ulong root, string className)
	{
		Queue<Tuple<ulong, int>> queue = new Queue<Tuple<ulong, int>>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		queue.Enqueue(Tuple.Create(root, 0));
		int num = 0;
		while (queue.Count > 0 && num++ < 60000)
		{
			Tuple<ulong, int> tuple = queue.Dequeue();
			ulong item = tuple.Item1;
			int item2 = tuple.Item2;
			if (!hashSet.Add(item))
			{
				continue;
			}
			if (IsClass(item, className))
			{
				return item;
			}
			if (item2 >= 128)
			{
				continue;
			}
			foreach (ulong item3 in EnumerateChildren(item))
			{
				queue.Enqueue(Tuple.Create(item3, item2 + 1));
			}
		}
		return 0uL;
	}

	private ulong FindDescendantByName(ulong root, string name)
	{
		Queue<Tuple<ulong, int>> queue = new Queue<Tuple<ulong, int>>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		queue.Enqueue(Tuple.Create(root, 0));
		int num = 0;
		while (queue.Count > 0 && num++ < 60000)
		{
			Tuple<ulong, int> tuple = queue.Dequeue();
			ulong item = tuple.Item1;
			int item2 = tuple.Item2;
			if (!hashSet.Add(item))
			{
				continue;
			}
			if (string.Equals(ReadInstanceName(item), name, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
			if (item2 >= 128)
			{
				continue;
			}
			foreach (ulong item3 in EnumerateChildren(item))
			{
				queue.Enqueue(Tuple.Create(item3, item2 + 1));
			}
		}
		return 0uL;
	}

	private IEnumerable<ulong> EnumerateChildren(ulong instance)
	{
		ulong childrenVector = memory.ReadPtr(instance + offsets.Get("Instance", "ChildrenStart"));
		if (!ProcessMemory.IsLikelyUserModeAddress(childrenVector))
		{
			yield break;
		}
		ulong start = memory.ReadPtr(childrenVector);
		ulong end = memory.ReadPtr(childrenVector + offsets.Get("Instance", "ChildrenEnd"));
		if (!LooksLikeVectorRange(start, end))
		{
			start = childrenVector;
			end = memory.ReadPtr(instance + offsets.Get("Instance", "ChildrenStart") + offsets.Get("Instance", "ChildrenEnd"));
		}
		if (!LooksLikeVectorRange(start, end))
		{
			yield break;
		}
		ulong thisOffset = offsets.Get("Instance", "This");
		for (ulong entry = start; entry < end; entry += 16)
		{
			ulong child = memory.ReadPtr(entry);
			if (!ProcessMemory.IsLikelyUserModeAddress(child))
			{
				child = memory.ReadPtr(entry + thisOffset);
			}
			if (ProcessMemory.IsLikelyUserModeAddress(child))
			{
				yield return child;
			}
		}
	}

	private static bool LooksLikeVectorRange(ulong start, ulong end)
	{
		return ProcessMemory.IsLikelyUserModeAddress(start) && ProcessMemory.IsLikelyUserModeAddress(end) && end >= start && end - start <= 1048576 && (end - start) % 16 == 0;
	}

	private static bool ContainsWithPadding(RectangleF rectangle, float x, float y, float padding)
	{
		return x >= rectangle.Left - padding && x <= rectangle.Right + padding && y >= rectangle.Top - padding && y <= rectangle.Bottom + padding;
	}

	private static bool IntersectsWithPadding(RectangleF rectangle, Rectangle other, float padding)
	{
		RectangleF rect = new RectangleF((float)other.Left - padding, (float)other.Top - padding, (float)other.Width + padding * 2f, (float)other.Height + padding * 2f);
		return rectangle.IntersectsWith(rect);
	}

	private string ReadInstanceName(ulong instance)
	{
		ulong stringAddress = memory.ReadPtr(instance + offsets.Get("Instance", "Name"));
		return memory.ReadRobloxString(stringAddress);
	}

	private bool IsClass(ulong instance, string className)
	{
		ulong num = memory.ReadPtr(instance + offsets.Get("Instance", "ClassDescriptor"));
		ulong stringAddress = memory.ReadPtr(num + offsets.Get("Instance", "ClassName"));
		return string.Equals(memory.ReadRobloxString(stringAddress), className, StringComparison.OrdinalIgnoreCase);
	}

	private bool IsReelActive()
	{
		if (ProcessMemory.IsLikelyUserModeAddress(reelAddress) && TryReadBool(reelAddress + offsets.Get("GuiObject", "ScreenGui_Enabled"), out var value) && !value)
		{
			return false;
		}
		if (ProcessMemory.IsLikelyUserModeAddress(fishAddress) && TryReadBool(fishAddress + offsets.Get("GuiObject", "Visible"), out var value2) && !value2)
		{
			return false;
		}
		if (ProcessMemory.IsLikelyUserModeAddress(playerbarAddress) && TryReadBool(playerbarAddress + offsets.Get("GuiObject", "Visible"), out var value3) && !value3)
		{
			return false;
		}
		if (ProcessMemory.IsLikelyUserModeAddress(containerAddress) && TryReadBool(containerAddress + offsets.Get("GuiObject", "Visible"), out var value4) && !value4)
		{
			return false;
		}
		return true;
	}

	private bool TryReadBool(ulong address, out bool value)
	{
		value = false;
		byte[] array = memory.ReadBytes(address, 1);
		if (array == null || array.Length == 0)
		{
			return false;
		}
		value = array[0] != 0;
		return true;
	}

	private bool HasReadableGuiBounds(ulong instance)
	{
		Vector2 position;
		Vector2 size;
		return TryReadGuiBounds(instance, out position, out size);
	}

	private bool TryReadGuiBounds(ulong instance, out Vector2 position, out Vector2 size)
	{
		position = default(Vector2);
		size = default(Vector2);
		if (!ProcessMemory.IsLikelyUserModeAddress(instance))
		{
			return false;
		}
		if (!TryReadVector2(instance + offsets.Get("GuiBase2D", "AbsolutePosition"), out position))
		{
			return false;
		}
		if (!TryReadVector2(instance + offsets.Get("GuiBase2D", "AbsoluteSize"), out size))
		{
			return false;
		}
		return IsReasonableVector(position, 20000f) && IsReasonableVector(size, 10000f) && size.X > 1f && size.Y > 1f;
	}

	private static bool IsReasonableVector(Vector2 value, float maxAbs)
	{
		return !float.IsNaN(value.X) && !float.IsNaN(value.Y) && !float.IsInfinity(value.X) && !float.IsInfinity(value.Y) && Math.Abs(value.X) <= maxAbs && Math.Abs(value.Y) <= maxAbs;
	}

	private bool TryReadVector2(ulong address, out Vector2 value)
	{
		value = default(Vector2);
		byte[] array = memory.ReadBytes(address, 8);
		if (array == null)
		{
			return false;
		}
		value = new Vector2(BitConverter.ToSingle(array, 0), BitConverter.ToSingle(array, 4));
		return true;
	}

	public void Dispose()
	{
		DisposeMemory();
	}

	private void DisposeMemory()
	{
		if (memory != null)
		{
			memory.Dispose();
			memory = null;
		}
		process = null;
		ResetTargets();
	}

	private static Process FindRobloxProcess()
	{
		Process process = (from p in Process.GetProcessesByName("RobloxPlayerBeta")
			orderby SafeStartTimeTicks(p) descending
			select p).FirstOrDefault();
		if (process != null)
		{
			return process;
		}
		process = (from p in Process.GetProcesses()
			where p.ProcessName.IndexOf("Roblox", StringComparison.OrdinalIgnoreCase) >= 0
			orderby SafeStartTimeTicks(p) descending
			select p).FirstOrDefault();
		if (process == null)
		{
			throw new InvalidOperationException("No running Roblox process was found.");
		}
		return process;
	}

	private static long SafeStartTimeTicks(Process process)
	{
		try
		{
			return process.StartTime.Ticks;
		}
		catch
		{
			return 0L;
		}
	}

	private static ulong GetMainModuleBase(Process process)
	{
		try
		{
			return (ulong)process.MainModule.BaseAddress.ToInt64();
		}
		catch (Exception innerException)
		{
			throw new InvalidOperationException("Could not read Roblox's main module base address. Run this as x64 and, if needed, as administrator.", innerException);
		}
	}

	private static Point GetClientScreenOrigin(IntPtr window)
	{
		Point lpPoint = new Point(0, 0);
		ClientToScreen(window, ref lpPoint);
		return lpPoint;
	}

	private static Rectangle GetClientScreenRectangle(IntPtr window)
	{
		if (!GetClientRect(window, out var lpRect))
		{
			return Rectangle.Empty;
		}
		Point clientScreenOrigin = GetClientScreenOrigin(window);
		return new Rectangle(clientScreenOrigin.X, clientScreenOrigin.Y, Math.Max(0, lpRect.Right - lpRect.Left), Math.Max(0, lpRect.Bottom - lpRect.Top));
	}

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);
}
