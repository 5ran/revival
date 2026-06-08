using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Client.Services.Fishing;

internal sealed class AquariumSequenceRunner
{
	private Process process;

	private ProcessMemory memory;

	private OffsetTable offsets;

	private ulong baseAddress;

	private IntPtr robloxWindow;

	private ulong playerGuiAddress;

	private ulong aquariumButton;

	private ulong feedAnchor;

	private string aquariumPath;

	private string feedPath;

	private ulong infoLabel;

	private string infoPath;

	private AquariumSequencePhase phase;

	private double nextActionTime;

	private double feedUntilTime;

	private double nextScrollTime;

	private bool didFastScrollUp;

	private readonly Stopwatch stopwatch = Stopwatch.StartNew();

	public void Reset()
	{
		phase = AquariumSequencePhase.Resolve;
		nextActionTime = 0.0;
		feedUntilTime = 0.0;
		nextScrollTime = 0.0;
		didFastScrollUp = false;
		infoLabel = 0uL;
		infoPath = null;
	}

	public bool TryReadNextTimerRemainingSeconds(out int remainingSeconds)
	{
		remainingSeconds = -1;
		EnsureConnected();
		EnsureTargets();
		ulong num = EnsureInfoLabel();
		if (!ProcessMemory.IsLikelyUserModeAddress(num))
		{
			return false;
		}
		string text = ReadGuiText(num);
		if (!TryParseNextSeconds(text, out remainingSeconds))
		{
			infoLabel = 0uL;
			infoPath = null;
			num = EnsureInfoLabel();
			if (!ProcessMemory.IsLikelyUserModeAddress(num))
			{
				return false;
			}
			text = ReadGuiText(num);
			if (!TryParseNextSeconds(text, out remainingSeconds))
			{
				return false;
			}
		}
		return true;
	}

	public AquariumSequenceResult Step(double clickDelaySeconds)
	{
		EnsureConnected();
		double totalSeconds = stopwatch.Elapsed.TotalSeconds;
		if (totalSeconds < nextActionTime)
		{
			return AquariumSequenceResult.Running;
		}
		if (phase == AquariumSequencePhase.Resolve)
		{
			EnsureTargets();
			phase = AquariumSequencePhase.OpenAquarium;
			return AquariumSequenceResult.Running;
		}
		if (phase == AquariumSequencePhase.OpenAquarium)
		{
			ClickCenter(aquariumButton, visibleRequired: true);
			phase = AquariumSequencePhase.WaitAfterOpen;
			nextActionTime = totalSeconds + Math.Max(0.5, clickDelaySeconds);
			return AquariumSequenceResult.Running;
		}
		if (phase == AquariumSequencePhase.WaitAfterOpen)
		{
			if (feedAnchor == 0 && !RefreshFeedAnchor())
			{
				nextActionTime = totalSeconds + 0.1;
				return AquariumSequenceResult.Running;
			}
			RectangleF bounds = ReadBounds(feedAnchor, visibleRequired: false);
			Point point = new Point((int)Math.Round(bounds.Left + bounds.Width * 0.25f), (int)Math.Round(bounds.Top + bounds.Height * 0.75f));
			ClickAt(point.X, point.Y);
			MoveTo(point.X, point.Y);
			feedUntilTime = totalSeconds + 3.333;
			nextScrollTime = totalSeconds;
			didFastScrollUp = false;
			phase = AquariumSequencePhase.Feeding;
			nextActionTime = totalSeconds + clickDelaySeconds;
			return AquariumSequenceResult.Running;
		}
		if (phase == AquariumSequencePhase.Feeding)
		{
			if (totalSeconds >= feedUntilTime)
			{
				phase = AquariumSequencePhase.CloseAquarium;
				nextActionTime = totalSeconds;
				return AquariumSequenceResult.Running;
			}
			Point cursor = GetCursorPosPoint();
			ClickAt(cursor.X, cursor.Y);
			if (totalSeconds >= nextScrollTime)
			{
				if (!didFastScrollUp)
				{
					for (int i = 0; i < 50; i++)
					{
						mouse_event(2048u, 0u, 0u, 120u, UIntPtr.Zero);
					}
					didFastScrollUp = true;
					nextScrollTime = totalSeconds + 0.5;
					nextActionTime = totalSeconds + 0.02;
					return AquariumSequenceResult.Running;
				}
				mouse_event(2048u, 0u, 0u, 4294967176u, UIntPtr.Zero);
				nextScrollTime = totalSeconds + Math.Max(0.15, clickDelaySeconds * 0.675);
			}
			nextActionTime = totalSeconds + 0.02;
			return AquariumSequenceResult.Running;
		}
		if (phase == AquariumSequencePhase.CloseAquarium)
		{
			ClickCenter(aquariumButton, visibleRequired: true);
			phase = AquariumSequencePhase.CenterClick;
			nextActionTime = totalSeconds + clickDelaySeconds;
			return AquariumSequenceResult.Running;
		}
		Rectangle clientScreenRectangle = GetClientScreenRectangle(robloxWindow);
		ClickAt(clientScreenRectangle.Left + clientScreenRectangle.Width / 2, clientScreenRectangle.Top + clientScreenRectangle.Height / 2);
		phase = AquariumSequencePhase.Resolve;
		nextActionTime = totalSeconds + clickDelaySeconds;
		return AquariumSequenceResult.Done;
	}

	private void EnsureConnected()
	{
		if (process != null && !process.HasExited && memory != null)
		{
			return;
		}
		offsets = new OffsetTable(OffsetsSourceProvider.Current);
		process = Process.GetProcessesByName("RobloxPlayerBeta").OrderByDescending((Process p) => SafeStartTime(p)).FirstOrDefault();
		if (process == null)
		{
			process = Process.GetProcesses().Where((Process p) => p.ProcessName.IndexOf("Roblox", StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending((Process p) => SafeStartTime(p)).FirstOrDefault();
		}
		if (process == null)
		{
			throw new InvalidOperationException("No running Roblox process was found.");
		}
		robloxWindow = process.MainWindowHandle;
		if (robloxWindow == IntPtr.Zero)
		{
			throw new InvalidOperationException("Roblox window is not ready.");
		}
		baseAddress = unchecked((ulong)process.MainModule.BaseAddress.ToInt64());
		memory = ProcessMemory.Open(process.Id);
		aquariumButton = 0uL;
		feedAnchor = 0uL;
	}

	private static long SafeStartTime(Process process)
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

	private void EnsureTargets()
	{
		if (aquariumButton != 0)
		{
			return;
		}
		ulong num = memory.ReadPtr(baseAddress + offsets.Get("FakeDataModel", "Pointer"));
		ulong num2 = memory.ReadPtr(num + offsets.Get("FakeDataModel", "RealDataModel"));
		if (!ProcessMemory.IsLikelyUserModeAddress(num2))
		{
			throw new InvalidOperationException("DataModel pointer is invalid.");
		}
		ulong num3 = FindLocalPlayerGui(num2);
		if (num3 == 0)
		{
			throw new InvalidOperationException("Could not find LocalPlayer.PlayerGui.");
		}
		playerGuiAddress = num3;
		ulong num4 = FindPersonalAquariumButton(num3);
		if (num4 == 0)
		{
			throw new InvalidOperationException("PersonalAquarium button was not found.");
		}
		aquariumButton = num4;
		aquariumPath = BuildPath(num4);
		feedAnchor = FindFeedAnchor(num3);
		feedPath = ((feedAnchor != 0) ? BuildPath(feedAnchor) : null);
		infoLabel = 0uL;
		infoPath = null;
	}

	private ulong FindPersonalAquariumButton(ulong root)
	{
		ulong num = FindByPath(root, "PersonalAquarium", "TextButton", "hud", "safezone", "topbar", "personalaquarium");
		if (num != 0)
		{
			return num;
		}
		num = FindByPath(root, "PersonalAquarium", "TextButton", "screengui", "hud", "safezone", "topbar", "personalaquarium");
		if (num != 0)
		{
			return num;
		}
		return FindByNameClass(root, "PersonalAquarium", "Button");
	}

	private ulong EnsureInfoLabel()
	{
		if (ProcessMemory.IsLikelyUserModeAddress(infoLabel)
			&& string.Equals(ReadName(infoLabel), "Info", StringComparison.OrdinalIgnoreCase)
			&& ClassMatches(ReadClass(infoLabel), "TextLabel"))
		{
			return infoLabel;
		}
		if (!string.IsNullOrWhiteSpace(infoPath))
		{
			ulong num = ResolveByPath(playerGuiAddress, infoPath, "Info", "TextLabel");
			if (ProcessMemory.IsLikelyUserModeAddress(num))
			{
				infoLabel = num;
				return infoLabel;
			}
		}
		ulong num2 = FindByPath(playerGuiAddress, "Info", "TextLabel", "safezone", "personalaquarium", "more", "profit", "header", "info");
		if (num2 == 0)
		{
			num2 = FindByNameClass(playerGuiAddress, "Info", "TextLabel");
		}
		if (ProcessMemory.IsLikelyUserModeAddress(num2))
		{
			infoLabel = num2;
			infoPath = BuildPath(num2);
		}
		return infoLabel;
	}

	private bool RefreshFeedAnchor()
	{
		ulong num = ResolveByPath(playerGuiAddress, feedPath, "FoodList", "Frame");
		if (num == 0)
		{
			num = FindFeedAnchor(playerGuiAddress);
		}
		if (num == 0)
		{
			return false;
		}
		feedAnchor = num;
		feedPath = BuildPath(num);
		return true;
	}

	private ulong FindFeedAnchor(ulong root)
	{
		ulong num = FindByPath(root, "FoodList", "Frame", "safezone", "personalaquarium", "more", "fishfood", "foodlist");
		if (num != 0)
		{
			return num;
		}
		num = FindByPath(root, "FoodList", "", "safezone", "personalaquarium", "more", "fishfood", "foodlist");
		if (num != 0)
		{
			return num;
		}
		num = FindByPath(root, "", "Frame", "safezone", "personalaquarium", "more", "fishfood");
		if (num != 0)
		{
			return num;
		}
		return FindAnyByPath(root, "safezone", "personalaquarium", "more", "fishfood");
	}

	private ulong FindLocalPlayerGui(ulong dataModel)
	{
		ulong num = FindByNameClass(dataModel, "Players", "");
		if (num == 0)
		{
			return 0;
		}
		ulong num2 = memory.ReadPtr(num + offsets.Get("Player", "LocalPlayer"));
		if (!ProcessMemory.IsLikelyUserModeAddress(num2))
		{
			return 0;
		}
		return FindByNameClass(num2, "PlayerGui", "");
	}

	private ulong FindByNameClass(ulong root, string name, string classNeedle)
	{
		Queue<Tuple<ulong, int>> queue = new Queue<Tuple<ulong, int>>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		queue.Enqueue(Tuple.Create(root, 0));
		while (queue.Count > 0)
		{
			Tuple<ulong, int> tuple = queue.Dequeue();
			ulong item = tuple.Item1;
			int item2 = tuple.Item2;
			if (!hashSet.Add(item))
			{
				continue;
			}
			if (string.Equals(ReadName(item), name, StringComparison.OrdinalIgnoreCase) && ClassMatches(ReadClass(item), classNeedle))
			{
				return item;
			}
			if (item2 < 256)
			{
				foreach (ulong item3 in EnumerateChildren(item))
				{
					queue.Enqueue(Tuple.Create(item3, item2 + 1));
				}
			}
		}
		return 0uL;
	}

	private ulong FindByPath(ulong root, string name, string classNeedle, params string[] segments)
	{
		Queue<Tuple<ulong, int>> queue = new Queue<Tuple<ulong, int>>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		queue.Enqueue(Tuple.Create(root, 0));
		while (queue.Count > 0)
		{
			Tuple<ulong, int> tuple = queue.Dequeue();
			ulong item = tuple.Item1;
			int item2 = tuple.Item2;
			if (!hashSet.Add(item))
			{
				continue;
			}
			bool flag = string.IsNullOrWhiteSpace(name) || string.Equals(ReadName(item), name, StringComparison.OrdinalIgnoreCase);
			if (flag && ClassMatches(ReadClass(item), classNeedle))
			{
				string text = BuildPath(item).ToLowerInvariant();
				bool flag2 = true;
				string[] array = segments;
				foreach (string text2 in array)
				{
					if (!text.Contains((text2 ?? string.Empty).ToLowerInvariant()))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					return item;
				}
			}
			if (item2 < 256)
			{
				foreach (ulong item3 in EnumerateChildren(item))
				{
					queue.Enqueue(Tuple.Create(item3, item2 + 1));
				}
			}
		}
		return 0uL;
	}

	private ulong FindAnyByPath(ulong root, params string[] segments)
	{
		Queue<Tuple<ulong, int>> queue = new Queue<Tuple<ulong, int>>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		queue.Enqueue(Tuple.Create(root, 0));
		while (queue.Count > 0)
		{
			Tuple<ulong, int> tuple = queue.Dequeue();
			ulong item = tuple.Item1;
			int item2 = tuple.Item2;
			if (!hashSet.Add(item))
			{
				continue;
			}
			string text = BuildPath(item).ToLowerInvariant();
			bool flag = true;
			string[] array = segments;
			foreach (string text2 in array)
			{
				if (!text.Contains((text2 ?? string.Empty).ToLowerInvariant()))
				{
					flag = false;
					break;
				}
			}
			if (flag && TryReadBounds(item, visibleRequired: false, out var _))
			{
				return item;
			}
			if (item2 < 256)
			{
				foreach (ulong item3 in EnumerateChildren(item))
				{
					queue.Enqueue(Tuple.Create(item3, item2 + 1));
				}
			}
		}
		return 0uL;
	}

	private ulong ResolveByPath(ulong root, string path, string name, string classNeedle)
	{
		if (root == 0 || string.IsNullOrWhiteSpace(path))
		{
			return 0uL;
		}
		Queue<Tuple<ulong, int>> queue = new Queue<Tuple<ulong, int>>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		queue.Enqueue(Tuple.Create(root, 0));
		while (queue.Count > 0)
		{
			Tuple<ulong, int> tuple = queue.Dequeue();
			ulong item = tuple.Item1;
			int item2 = tuple.Item2;
			if (!hashSet.Add(item))
			{
				continue;
			}
			if (string.Equals(ReadName(item), name, StringComparison.OrdinalIgnoreCase) && ClassMatches(ReadClass(item), classNeedle) && string.Equals(BuildPath(item), path, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
			if (item2 < 256)
			{
				foreach (ulong item3 in EnumerateChildren(item))
				{
					queue.Enqueue(Tuple.Create(item3, item2 + 1));
				}
			}
		}
		return 0uL;
	}

	private string ReadName(ulong instance)
	{
		return memory.ReadRobloxString(memory.ReadPtr(instance + offsets.Get("Instance", "Name")));
	}

	private string ReadClass(ulong instance)
	{
		ulong num = memory.ReadPtr(instance + offsets.Get("Instance", "ClassDescriptor"));
		return memory.ReadRobloxString(memory.ReadPtr(num + offsets.Get("Instance", "ClassName")));
	}

	private bool ClassMatches(string className, string classNeedle)
	{
		if (string.IsNullOrWhiteSpace(classNeedle))
		{
			return true;
		}
		if (classNeedle.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return className != null && className.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return string.Equals(className, classNeedle, StringComparison.OrdinalIgnoreCase);
	}

	private string BuildPath(ulong instance)
	{
		List<string> list = new List<string>();
		ulong num = instance;
		for (int i = 0; i < 14 && ProcessMemory.IsLikelyUserModeAddress(num); i++)
		{
			string text = ReadName(num);
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(text);
			}
			ulong num2 = memory.ReadPtr(num + offsets.Get("Instance", "Parent"));
			if (!ProcessMemory.IsLikelyUserModeAddress(num2) || num2 == num)
			{
				break;
			}
			num = num2;
		}
		list.Reverse();
		return string.Join("/", list.ToArray());
	}

	private IEnumerable<ulong> EnumerateChildren(ulong instance)
	{
		ulong num = memory.ReadPtr(instance + offsets.Get("Instance", "ChildrenStart"));
		if (!ProcessMemory.IsLikelyUserModeAddress(num))
		{
			yield break;
		}
		ulong num2 = memory.ReadPtr(num);
		ulong num3 = memory.ReadPtr(num + offsets.Get("Instance", "ChildrenEnd"));
		if (!LooksLikeRange(num2, num3))
		{
			num2 = num;
			num3 = memory.ReadPtr(instance + offsets.Get("Instance", "ChildrenStart") + offsets.Get("Instance", "ChildrenEnd"));
		}
		if (!LooksLikeRange(num2, num3))
		{
			yield break;
		}
		ulong num4 = offsets.Get("Instance", "This");
		for (ulong num5 = num2; num5 < num3; num5 += 16)
		{
			ulong num6 = memory.ReadPtr(num5);
			if (!ProcessMemory.IsLikelyUserModeAddress(num6))
			{
				num6 = memory.ReadPtr(num5 + num4);
			}
			if (ProcessMemory.IsLikelyUserModeAddress(num6))
			{
				yield return num6;
			}
		}
	}

	private static bool LooksLikeRange(ulong start, ulong end)
	{
		return ProcessMemory.IsLikelyUserModeAddress(start) && ProcessMemory.IsLikelyUserModeAddress(end) && end >= start && end - start <= 1048576 && (end - start) % 16 == 0;
	}

	private void ClickCenter(ulong address, bool visibleRequired)
	{
		RectangleF bounds = ReadBounds(address, visibleRequired);
		Point point = new Point((int)Math.Round(bounds.Left + bounds.Width * 0.5f), (int)Math.Round(bounds.Top + bounds.Height * 0.5f));
		ClickAt(point.X, point.Y);
	}

	private RectangleF ReadBounds(ulong address, bool visibleRequired)
	{
		if (!TryReadBounds(address, visibleRequired, out var bounds))
		{
			throw new InvalidOperationException("Could not read GUI bounds.");
		}
		return bounds;
	}

	private bool TryReadBounds(ulong address, bool visibleRequired, out RectangleF bounds)
	{
		bounds = RectangleF.Empty;
		if (!TryReadVector2(address + offsets.Get("GuiBase2D", "AbsolutePosition"), out var value))
		{
			return false;
		}
		if (!TryReadVector2(address + offsets.Get("GuiBase2D", "AbsoluteSize"), out var value2))
		{
			return false;
		}
		if (visibleRequired && TryReadBool(address + offsets.Get("GuiObject", "Visible"), out var value3) && !value3)
		{
			return false;
		}
		Point clientScreenOrigin = GetClientScreenOrigin(robloxWindow);
		bounds = new RectangleF((float)clientScreenOrigin.X + value.X, (float)clientScreenOrigin.Y + value.Y, value2.X, value2.Y);
		return bounds.Width > 1f && bounds.Height > 1f;
	}

	private bool TryReadVector2(ulong address, out Vector2 value)
	{
		value = default(Vector2);
		byte[] bytes = memory.ReadBytes(address, 8);
		if (bytes == null)
		{
			return false;
		}
		value = new Vector2(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4));
		return true;
	}

	private bool TryReadBool(ulong address, out bool value)
	{
		value = false;
		byte[] bytes = memory.ReadBytes(address, 1);
		if (bytes == null || bytes.Length == 0)
		{
			return false;
		}
		value = bytes[0] != 0;
		return true;
	}

	private string ReadGuiText(ulong instance)
	{
		try
		{
			ulong num = offsets.Get("GuiObject", "Text");
			ulong stringAddress = memory.ReadPtr(instance + num);
			string text = memory.ReadRobloxString(stringAddress);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return memory.ReadRobloxString(instance + num);
		}
		catch
		{
			return string.Empty;
		}
	}

	private static bool TryParseNextSeconds(string text, out int seconds)
	{
		seconds = -1;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Match match = Regex.Match(text, "Next:\\s*(?:(\\d+)\\s*m)?\\s*(?:(\\d+)\\s*s)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (!match.Success)
		{
			return false;
		}
		int num = 0;
		int num2 = 0;
		if (match.Groups[1].Success)
		{
			int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out num);
		}
		if (match.Groups[2].Success)
		{
			int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out num2);
		}
		seconds = Math.Max(0, num * 60 + num2);
		return true;
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

	private static void ClickAt(int x, int y)
	{
		MoveTo(x, y);
		mouse_event(1u, 1u, 0u, 0u, UIntPtr.Zero);
		System.Threading.Thread.Sleep(10);
		mouse_event(1u, 4294967295u, 0u, 0u, UIntPtr.Zero);
		System.Threading.Thread.Sleep(10);
		MoveTo(x, y);
		MouseInput.LeftDown();
		System.Threading.Thread.Sleep(35);
		MoveTo(x, y);
		MouseInput.LeftUp();
	}

	private static void MoveTo(int x, int y)
	{
		SetCursorPos(x, y);
	}

	private static Point GetCursorPosPoint()
	{
		GetCursorPos(out var lpPoint);
		return lpPoint;
	}

	[DllImport("user32.dll")]
	private static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out Point lpPoint);

	[DllImport("user32.dll")]
	private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);
}

internal enum AquariumSequencePhase
{
	Resolve,
	OpenAquarium,
	WaitAfterOpen,
	Feeding,
	CloseAquarium,
	CenterClick
}

internal struct AquariumSequenceResult
{
	public static readonly AquariumSequenceResult Running = new AquariumSequenceResult(completed: false);

	public static readonly AquariumSequenceResult Done = new AquariumSequenceResult(completed: true);

	public bool CompletedFlag;

	private AquariumSequenceResult(bool completed)
	{
		CompletedFlag = completed;
	}

	public bool Completed
	{
		get
		{
			return CompletedFlag;
		}
	}
}
