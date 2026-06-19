using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
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

	private long rodEquipRetryAt;

	private long lastRodEquipAttemptAt;

	private long rodEquipHoldoffUntil;

	private int rodUnequippedStreak;

	private ulong cachedWorkspace;

	private ulong cachedLocalPlayer;

	private ulong cachedCharacter;

	private string cachedPlayerName = string.Empty;

	private long nextCharacterRefreshAt;
	private ulong cachedPowerBar;
	private double? lastPowerPercent;
	private long lastPowerSeenAt;

	private ulong cachedWorldConfig;

	private ulong cachedHotbarGui;

	private ulong cachedShakeGui;

	private ulong cachedShakeSafezone;

	private ulong cachedShakeButton;

	private const int SplitbranchCrateClickCooldownMs = 700;

	private const int MiguCounterShiftDelayMs = 480;

	private long splitbranchLastCrateClickAt;

	private bool miguCounterWasVisible;

	private bool miguShiftFiredThisAppearance;

	private long miguShiftFireAt;

	public IntPtr WindowHandle => robloxWindow;

	public bool HasTargets
	{
		get
		{
			return reelAddress != 0 && fishAddress != 0 && playerbarAddress != 0 && containerAddress != 0;
		}
	}

	public ReelMetrics ReadSnapshot()
	{
		EnsureConnected();
		if (fishAddress == 0 || playerbarAddress == 0 || containerAddress == 0 || framesUntilRescan <= 0)
		{
			FindTargets();
			framesUntilRescan = 50;
		}
		framesUntilRescan--;
		if (!TryReadGuiBounds(containerAddress, out var position3, out var size3))
		{
			ResetTargets();
			throw new NoMinigameException("Found reel objects, but the reel container bounds are not readable.");
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
		var fish = FindChildByName(containerAddress, "fish");
		var playerbar = FindChildByName(containerAddress, "playerbar");
		if (fish == 0 || playerbar == 0)
		{
			ResetTargets();
			throw new NoMinigameException("Reel exists, but fish/playerbar are missing.");
		}

		if (!TryReadUDim2Scale(fish + offsets.Get("GuiObject", "Position"), out var fishPosition) ||
			!TryReadUDim2Scale(fish + offsets.Get("GuiObject", "Size"), out var fishSize) ||
			!TryReadUDim2Scale(playerbar + offsets.Get("GuiObject", "Position"), out var playerbarPosition) ||
			!TryReadUDim2Scale(playerbar + offsets.Get("GuiObject", "Size"), out var playerbarSize))
		{
			ResetTargets();
			throw new NoMinigameException("Reel exists, but fish/playerbar metrics are not readable.");
		}

		var fishCenterX = fishPosition.X + fishSize.X * 0.5f;
		var playerbarCenterX = playerbarPosition.X;
		var playerbarWidth = Math.Max(0.001f, playerbarSize.X);
		if (!IsReasonableScale(fishCenterX) || !IsReasonableScale(playerbarCenterX) || playerbarWidth > 1.5f)
		{
			ResetTargets();
			throw new NoMinigameException("Reel exists, but fish/playerbar metrics are invalid.");
		}

		DebugLog.Write(
			"ReelLocator.ReadSnapshot",
			$"container=({rectangleF.Left:0.0},{rectangleF.Top:0.0},{rectangleF.Width:0.0},{rectangleF.Height:0.0}) fish=({fishCenterX:0.000000}) bar=({playerbarCenterX:0.000000}) width={playerbarWidth:0.000000}");
		return new ReelMetrics(fishCenterX, playerbarCenterX, playerbarWidth);
	}

	public IReadOnlyList<OrderedReelContext> GetOrderedReelContexts()
	{
		EnsureConnected();
		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			return Array.Empty<OrderedReelContext>();
		}

		var contexts = new List<OrderedReelContext>();
		foreach (var child in ReadChildren(playerGui))
		{
			if (!string.Equals(ReadName(child), "reel", StringComparison.OrdinalIgnoreCase) ||
				!string.Equals(ReadClass(child), "ScreenGui", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var context = BuildReelContext(child);
			if (context is null)
			{
				continue;
			}

			var barPos = ReadFramePositionScale(context.Bar);
			contexts.Add(new OrderedReelContext(context, barPos.X));
		}

		contexts.Sort((left, right) => left.BarX.CompareTo(right.BarX));
		return contexts;
	}

	public ReelMetrics? ReadMetrics(ReelContext context)
	{
		if (context.Fish == 0 || context.Playerbar == 0 || context.Bar == 0)
		{
			return null;
		}

		if (!TryReadUDim2Scale(context.Fish + offsets.Get("GuiObject", "Position"), out var fishPosition) ||
			!TryReadUDim2Scale(context.Fish + offsets.Get("GuiObject", "Size"), out var fishSize) ||
			!TryReadUDim2Scale(context.Playerbar + offsets.Get("GuiObject", "Position"), out var playerbarPosition) ||
			!TryReadUDim2Scale(context.Playerbar + offsets.Get("GuiObject", "Size"), out var playerbarSize))
		{
			return null;
		}

		var fishCenterX = fishPosition.X + fishSize.X * 0.5f;
		var playerbarCenterX = playerbarPosition.X;
		var playerbarWidth = Math.Max(0.001f, playerbarSize.X);
		if (!IsReasonableScale(fishCenterX) || !IsReasonableScale(playerbarCenterX) || playerbarWidth > 1.5f)
		{
			return null;
		}

		return new ReelMetrics(fishCenterX, playerbarCenterX, playerbarWidth);
	}

	public void ResetTargets()
	{
		reelAddress = 0uL;
		fishAddress = 0uL;
		playerbarAddress = 0uL;
		containerAddress = 0uL;
		framesUntilRescan = 0;
		cachedWorldConfig = 0uL;
		cachedHotbarGui = 0uL;
		ResetShakeCache();
	}

	public bool IsNightCycle()
	{
		return GetCurrentCycle().IndexOf("night", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public bool IsAuroraActive()
	{
		return GetCurrentMeteorological().IndexOf("aurora", StringComparison.OrdinalIgnoreCase) >= 0 ||
			GetCurrentWeather().IndexOf("aurora", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public bool IsWindyActive()
	{
		return GetCurrentWeather().IndexOf("windy", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public bool IsTotemBlocked()
	{
		var meteorological = GetCurrentMeteorological();
		var weather = GetCurrentWeather();
		return meteorological.IndexOf("starfall", StringComparison.OrdinalIgnoreCase) >= 0 ||
			meteorological.IndexOf("rainbow", StringComparison.OrdinalIgnoreCase) >= 0 ||
			weather.IndexOf("starfall", StringComparison.OrdinalIgnoreCase) >= 0 ||
			weather.IndexOf("rainbow", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public bool TryUseHotbarItem(string itemName)
	{
		if (string.IsNullOrWhiteSpace(itemName))
		{
			return false;
		}

		EnsureConnected();
		var slotKey = GetHotbarItemSlotKey(itemName);
		if (string.IsNullOrWhiteSpace(slotKey))
		{
			DebugLog.Write("ReelLocator.TryUseHotbarItem", $"item=\"{itemName}\" slot=missing");
			return false;
		}

		for (var attempt = 0; attempt < 2; attempt++)
		{
			var equippedBefore = string.Empty;
			try
			{
				equippedBefore = GetEquippedToolName(Environment.TickCount64);
			}
			catch
			{
			}

			if (!string.Equals(equippedBefore, itemName, StringComparison.OrdinalIgnoreCase))
			{
				if (!SelectHotbarSlot(slotKey))
				{
					DebugLog.Write("ReelLocator.TryUseHotbarItem", $"item=\"{itemName}\" slot=\"{slotKey}\" select=failed");
					return false;
				}

				Thread.Sleep(175);
			}

			MouseInput.LeftDown();
			MouseInput.LeftUp();
			Thread.Sleep(100);

			var equippedAfter = string.Empty;
			try
			{
				equippedAfter = GetEquippedToolName(Environment.TickCount64);
			}
			catch
			{
			}

			if (string.Equals(equippedAfter, itemName, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(equippedBefore, itemName, StringComparison.OrdinalIgnoreCase))
			{
				DebugLog.Write("ReelLocator.TryUseHotbarItem", $"item=\"{itemName}\" slot=\"{slotKey}\" attempt={attempt + 1} result=success");
				return true;
			}

			Thread.Sleep(125);
		}

		DebugLog.Write("ReelLocator.TryUseHotbarItem", $"item=\"{itemName}\" slot=\"{slotKey}\" result=failed");
		return false;
	}

	public string GetHotbarItemSlotKey(string itemName)
	{
		var itemAddr = FindHotbarItemByName(itemName);
		return itemAddr == 0 ? string.Empty : ReadHotbarItemSlotKey(itemAddr);
	}

	public bool TryEnsureRodEquipped()
	{
		EnsureConnected();
		var now = Environment.TickCount64;
		var equippedTool = GetEquippedToolName(now);
		var rodEquipped = IsRodEquipped(equippedTool) || IsSelectedRodEquipped(equippedTool);
		if (rodEquipped)
		{
			rodUnequippedStreak = 0;
			return false;
		}

		rodUnequippedStreak = Math.Min(rodUnequippedStreak + 1, 10);
		if (rodUnequippedStreak < 2)
		{
			return false;
		}

		if (now < rodEquipRetryAt || now < rodEquipHoldoffUntil || now - lastRodEquipAttemptAt < 1200)
		{
			return false;
		}

		lastRodEquipAttemptAt = now;
		rodEquipRetryAt = now + 2200;
		rodEquipHoldoffUntil = now + 2200;
		return EnsureRodEquipped();
	}

	public bool TryGetRodEquippedState(out bool rodEquipped)
	{
		rodEquipped = false;
		try
		{
			EnsureConnected();
			var now = Environment.TickCount64;
			string equippedTool = string.Empty;
			string selectedDisplay = string.Empty;
			try
			{
				equippedTool = GetEquippedToolName(now);
			}
			catch
			{
			}

			try
			{
				selectedDisplay = GetHotbarRodDisplayText();
			}
			catch
			{
			}

			if (string.IsNullOrWhiteSpace(equippedTool) && string.IsNullOrWhiteSpace(selectedDisplay))
			{
				return false;
			}

			if (IsRodEquipped(equippedTool))
			{
				rodEquipped = true;
				return true;
			}

			if (!string.IsNullOrWhiteSpace(equippedTool) && !string.IsNullOrWhiteSpace(selectedDisplay))
			{
				rodEquipped = IsSelectedRodEquipped(equippedTool, selectedDisplay);
				return true;
			}

			rodEquipped = false;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool TryGetRodKind(out RodKind rodKind)
	{
		rodKind = RodKind.Default;
		try
		{
			var displayText = GetHotbarRodDisplayText();
			rodKind = RodClassifier.Classify(displayText);
			DebugLog.Write("ReelLocator.TryGetRodKind", $"display=\"{displayText}\" kind={rodKind}");
			return true;
		}
		catch
		{
			DebugLog.Write("ReelLocator.TryGetRodKind", "failed");
			return false;
		}
	}

	public bool IsVisible(ulong instance)
	{
		return TryReadGuiBounds(instance, out _, out _);
	}

	public bool IsShakeButtonVisible()
	{
		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			DebugLog.Write("ReelLocator.IsShakeButtonVisible", "playerGui=0");
			ResetShakeCache();
			return false;
		}

		if (!IsCached(cachedShakeGui, "shakeui"))
		{
			cachedShakeGui = FindChildByName(playerGui, "shakeui");
			if (cachedShakeGui == 0)
			{
				cachedShakeGui = FindDescendantByName(playerGui, "shakeui");
			}
		}

		if (cachedShakeGui == 0 || !IsVisible(cachedShakeGui))
		{
			DebugLog.Write("ReelLocator.IsShakeButtonVisible", $"shakeGui={(cachedShakeGui == 0 ? "0" : $"0x{cachedShakeGui:X}")} visible=false");
			ResetShakeCache();
			return false;
		}

		if (!IsCached(cachedShakeSafezone, "safezone"))
		{
			cachedShakeSafezone = FindChildByName(cachedShakeGui, "safezone");
			if (cachedShakeSafezone == 0)
			{
				cachedShakeSafezone = FindDescendantByName(cachedShakeGui, "safezone");
			}
		}

		if (cachedShakeSafezone == 0)
		{
			DebugLog.Write("ReelLocator.IsShakeButtonVisible", "safezone=0");
			cachedShakeButton = 0;
			return false;
		}

		if (!IsCached(cachedShakeButton, "button"))
		{
			cachedShakeButton = FindChildByName(cachedShakeSafezone, "button");
			if (cachedShakeButton == 0)
			{
				cachedShakeButton = FindDescendantByName(cachedShakeSafezone, "button");
			}
		}

		return cachedShakeButton != 0 &&
			string.Equals(ReadClass(cachedShakeButton), "ImageButton", StringComparison.OrdinalIgnoreCase) &&
			IsVisible(cachedShakeButton);
	}

	public bool HasActiveFishingContext()
	{
		var reelGui = FindReelGui();
		if (reelGui == 0)
		{
			DebugLog.Write("ReelLocator.HasActiveFishingContext", "reelGui=0");
			return false;
		}

		var bar = FindChildByName(reelGui, "bar");
		if (bar == 0)
		{
			DebugLog.Write("ReelLocator.HasActiveFishingContext", $"reelGui=0x{reelGui:X} bar=0");
			return false;
		}

		var fish = FindChildByName(bar, "fish");
		var playerbar = FindChildByName(bar, "playerbar");
		var active = fish != 0 && playerbar != 0;
		DebugLog.Write(
			"ReelLocator.HasActiveFishingContext",
			$"reelGui=0x{reelGui:X} bar=0x{bar:X} fish=0x{fish:X} playerbar=0x{playerbar:X} active={active}");
		return active;
	}

	public IReadOnlyList<ulong> ReadChildrenPublic(ulong instance)
	{
		return ReadChildren(instance);
	}

	public string ReadNamePublic(ulong instance)
	{
		return ReadName(instance);
	}

	public string ReadClassPublic(ulong instance)
	{
		return ReadClass(instance);
	}

	private void ResetShakeCache()
	{
		cachedShakeGui = 0uL;
		cachedShakeSafezone = 0uL;
		cachedShakeButton = 0uL;
	}

	private bool IsCached(ulong instance, string name)
	{
		return instance != 0uL && string.Equals(ReadName(instance), name, StringComparison.OrdinalIgnoreCase);
	}

	public Vector2 ReadFramePosition(ulong instance)
	{
		if (!ProcessMemory.IsLikelyUserModeAddress(instance))
		{
			return default;
		}

		return TryReadVector2(instance + offsets.Get("GuiBase2D", "AbsolutePosition"), out var position) ? position : default;
	}

	public Vector2 ReadFramePositionScale(ulong instance)
	{
		if (!ProcessMemory.IsLikelyUserModeAddress(instance))
		{
			return default;
		}

		var offset = offsets.Get("GuiObject", "Position");
		return TryReadUDim2Scale(instance + offset, out var position) ? position : default;
	}

	public NoteTarget? GetActiveNoteTarget()
	{
		var bar = HasTargets ? FindDescendantByName(reelAddress, "bar") : 0;
		if (bar == 0)
		{
			var reelGui = FindReelGui();
			if (reelGui == 0)
			{
				return null;
			}

			bar = FindChildByName(reelGui, "bar");
			if (bar == 0 || !TryReadGuiBounds(bar, out _, out _))
			{
				return null;
			}
		}

		var noteContainer = FindChildByName(bar, "noteContainer");
		if (noteContainer == 0)
		{
			return null;
		}

		NoteTarget? best = null;
		var bestY = -999999.0;
		foreach (var noteName in new[] { "note1", "note2" })
		{
			var noteAddr = FindChildByName(noteContainer, noteName);
			if (noteAddr == 0)
			{
				continue;
			}

			var pos = ReadFramePositionScale(noteAddr);
			var sx = pos.X;
			var sy = pos.Y;
			if (sy > 0.55f || sy < -30f)
			{
				continue;
			}

			if (sy > bestY)
			{
				bestY = sy;
				best = new NoteTarget(sx, sy);
			}
		}

		DebugLog.Write("ReelLocator.GetActiveNoteTarget", best is null ? "none" : $"note=({best.Value.Sx:0.000000},{best.Value.Sy:0.000000})");
		return best;
	}

	public bool TryGetFishingCompletionPercent(out double? percent)
	{
		percent = null;
		try
		{
			var reelGui = FindReelGui();
			if (reelGui == 0)
			{
				return false;
			}

			var bar = FindChildByName(reelGui, "bar");
			if (bar == 0)
			{
				return false;
			}

			var progress = FindChildByName(bar, "progress");
			var progressBar = progress == 0 ? 0 : FindChildByName(progress, "bar");
			if (progressBar == 0)
			{
				return false;
			}

			if (!TryReadGuiSize(progressBar, out var size))
			{
				return false;
			}

			percent = Math.Clamp(size.X * 100.0, 0, 100);
			DebugLog.Write("ReelLocator.TryGetFishingCompletionPercent", $"progress={percent:0.0}");
			return true;
		}
		catch
		{
			DebugLog.Write("ReelLocator.TryGetFishingCompletionPercent", "failed");
			return false;
		}
	}

	public bool TryGetSovereignPowerPercent(out double? percent)
	{
		percent = null;
		try
		{
			var label = ResolveSovereignPowerLabel();
			if (label == 0)
			{
				return false;
			}

			var text = ReadGuiText(label);
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}

			var match = Regex.Match(text, @"([0-9]+(?:\.[0-9]+)?)\s*%", RegexOptions.CultureInvariant);
			if (!match.Success)
			{
				return false;
			}

			if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
			{
				return false;
			}

			percent = Math.Clamp(parsed, 0, 100);
			DebugLog.Write("ReelLocator.TryGetSovereignPowerPercent", $"power={percent:0.0}");
			return true;
		}
		catch
		{
			DebugLog.Write("ReelLocator.TryGetSovereignPowerPercent", "failed");
			return false;
		}
	}

	private ulong ResolveSovereignPowerLabel()
	{
		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			return 0;
		}

		var powerLabel = FindByPath(playerGui, "backpack", "hotbar", "Folder", "Frame", "Frame", "powerbar", "bar", "powerLabel");
		if (powerLabel != 0)
		{
			return powerLabel;
		}

		return FindByPath(playerGui, "backpack", "hotbar", "powerLabel", "TextLabel", "powerbar", "bar");
	}

	public bool TryClickGuiCenter(ulong instance)
	{
		if (!TryReadGuiBounds(instance, out var position, out var size))
		{
			return false;
		}

		var point = new Point((int)Math.Round(position.X + size.X / 2f), (int)Math.Round(position.Y + size.Y / 2f));
		NativeMouse.ClickAt(point.X, point.Y);
		return true;
	}

	public bool TryOpenInventory()
	{
		NativeKeyboard.PressG(robloxWindow);
		return true;
	}

	public bool TryPrepareSovereignInventoryTargets(out ulong searchFrame, out ulong itemContainer)
	{
		searchFrame = 0;
		itemContainer = 0;

		var inventoryFrame = ResolveInventoryFrame();
		if (inventoryFrame == 0)
		{
			DebugLog.Write("ReelLocator.TryPrepareSovereignInventoryTargets", "inventoryFrame=0");
			return false;
		}

		searchFrame = ResolveInventorySearchFrame(inventoryFrame);
		itemContainer = ResolveInventoryItemContainer(inventoryFrame);
		DebugLog.Write(
			"ReelLocator.TryPrepareSovereignInventoryTargets",
			$"inventory=0x{inventoryFrame:X} search=0x{searchFrame:X} itemContainer=0x{itemContainer:X}");
		return searchFrame != 0 && itemContainer != 0;
	}

	public bool TrySelectInventoryItem(ulong itemContainer, string itemName)
	{
		var target = FindInventoryItemTarget(itemContainer, itemName);
		if (target == 0)
		{
			return false;
		}

		return TryClickGuiCenter(target);
	}

	public bool TryClickEnchantButton()
	{
		var enchantButton = EnsureEnchantTarget();
		return enchantButton != 0 && TryClickGuiCenter(enchantButton);
	}

	public ulong GetTranquilityRoot()
	{
		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			return 0;
		}

		var gui = FindChildByName(playerGui, "TranquilityRodRhythmGame");
		return gui == 0 ? 0 : FindChildByName(gui, "RhythmGame");
	}

	public ulong GetTranquilityLaneContainer(ulong root)
	{
		return root == 0 ? 0 : FindChildByName(root, "LaneContainer");
	}

	public ulong GetTranquilityLane(ulong container, int index)
	{
		return container == 0 ? 0 : FindChildByName(container, $"Lane{index}");
	}

	public bool IsTranquilityActive()
	{
		var root = GetTranquilityRoot();
		return root != 0 && GetTranquilityLaneContainer(root) != 0;
	}

	public double? ReadTranquilityProgressPercent(ulong root)
	{
		if (root == 0)
		{
			return null;
		}

		var healthBar = FindChildByName(root, "HealthBar");
		var fill = healthBar == 0 ? 0 : FindChildByName(healthBar, "Fill");
		if (fill == 0)
		{
			return null;
		}

		return Math.Clamp(TryReadGuiSize(fill, out var size) ? size.X * 100.0 : -1.0, 0, 100);
	}

	public string GetTranquilityLaneKey(ulong root, ulong lane, int index)
	{
		string[] fallback = ["", "A", "S", "D", "F"];

		var label = root == 0 ? 0 : FindChildByName(root, $"KeyLabel{index}");
		if (label == 0 && lane != 0)
		{
			label = FindChildByName(lane, "KeyLabel");
		}

		if (label != 0)
		{
			var keyText = ReadGuiText(label).Trim();
			if (keyText.Length == 1)
			{
				return keyText.ToUpperInvariant();
			}
		}

		return index >= 1 && index <= 4 ? fallback[index] : string.Empty;
	}

	public IReadOnlyList<string> GetMasterlineOverlayRodNames()
	{
		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			return Array.Empty<string>();
		}

		var hud = FindChildByName(playerGui, "hud");
		if (hud == 0)
		{
			hud = FindDescendantByName(playerGui, "hud");
		}

		if (hud == 0)
		{
			return Array.Empty<string>();
		}

		var hudFrame = FindChildByName(hud, "Frame");
		if (hudFrame == 0)
		{
			hudFrame = FindDescendantByName(hud, "Frame");
		}

		var safezone = hudFrame == 0 ? 0 : FindChildByName(hudFrame, "safezone");
		if (safezone == 0)
		{
			safezone = FindDescendantByName(hud, "safezone");
		}

		if (safezone == 0)
		{
			return Array.Empty<string>();
		}

		var statuses = FindChildByName(safezone, "statuses");
		if (statuses == 0)
		{
			statuses = FindDescendantByName(safezone, "statuses");
		}

		if (statuses == 0)
		{
			return Array.Empty<string>();
		}

		ulong masterlineStatus = 0;
		foreach (var child in ReadChildren(statuses))
		{
			var name = ReadName(child);
			if (name.StartsWith("Masterline", StringComparison.OrdinalIgnoreCase))
			{
				masterlineStatus = child;
				break;
			}
		}

		if (masterlineStatus == 0)
		{
			masterlineStatus = FindDescendantByName(statuses, "Masterline");
		}

		if (masterlineStatus == 0)
		{
			return Array.Empty<string>();
		}

		var tooltip = FindChildByName(masterlineStatus, "tooltip");
		if (tooltip == 0)
		{
			tooltip = FindDescendantByName(masterlineStatus, "tooltip");
		}

		if (tooltip == 0)
		{
			return Array.Empty<string>();
		}

		var text = ReadGuiText(tooltip).Trim();
		if (text.Length == 0)
		{
			return Array.Empty<string>();
		}

		var names = new List<string>(4);
		foreach (var raw in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
		{
			var line = raw.Trim();
			line = line.TrimStart('•').Trim();
			if (line.Length > 0)
			{
				names.Add(line);
			}
		}

		return names;
	}

	public bool TryHandleSplitbranchTwigCrateSelection()
	{
		if (!TryGetRodKind(out var kind) || kind != RodKind.SplitbranchTwig)
		{
			splitbranchLastCrateClickAt = 0;
			DebugLog.Write("ReelLocator.Splitbranch", $"skip kind={kind}");
			return false;
		}

		var now = Environment.TickCount64;
		if (now - splitbranchLastCrateClickAt < SplitbranchCrateClickCooldownMs)
		{
			return false;
		}

		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			return false;
		}

		var fishSelection = FindChildByName(playerGui, "FishSelection");
		if (fishSelection == 0)
		{
			return false;
		}

		var listRoot = FindDescendantByName(fishSelection, "List");
		if (listRoot == 0)
		{
			listRoot = fishSelection;
		}

		foreach (var node in Traverse(listRoot, 64))
		{
			if (!string.Equals(ReadClass(node), "TextLabel", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var text = NormalizeLoose(ReadGuiText(node));
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}

			var path = BuildPath(node).ToLowerInvariant();
			if (!path.Contains("fishselection", StringComparison.Ordinal) ||
				!path.Contains("list", StringComparison.Ordinal) ||
				!path.Contains("button", StringComparison.Ordinal))
			{
				continue;
			}

			var clickable = FindClickableAncestor(node, fishSelection);
			if (clickable != 0)
			{
				var point = GetClickPoint(clickable);
				global::NativeMouse.ClickAt(point.X, point.Y);
				splitbranchLastCrateClickAt = now;
				DebugLog.Write("ReelLocator.Splitbranch", $"clicked={point.X},{point.Y}");
				return true;
			}
		}

		return false;
	}

	public bool TryHandleMiguCounterAttackShift()
	{
		if (!TryGetRodKind(out var kind) || kind != RodKind.MiguRod)
		{
			miguCounterWasVisible = false;
			miguShiftFiredThisAppearance = false;
			miguShiftFireAt = 0;
			DebugLog.Write("ReelLocator.Migu", $"skip kind={kind}");
			return false;
		}

		var now = Environment.TickCount64;
		var counterAttack = ResolveCounterAttackFrame();
		if (counterAttack == 0)
		{
			miguCounterWasVisible = false;
			miguShiftFiredThisAppearance = false;
			miguShiftFireAt = 0;
			DebugLog.Write("ReelLocator.Migu", "counterAttack missing");
			return false;
		}

		var visible = TryReadGuiBounds(counterAttack, out _, out _);
		if (!visible)
		{
			if (miguCounterWasVisible)
			{
				miguCounterWasVisible = false;
				miguShiftFiredThisAppearance = false;
				miguShiftFireAt = 0;
			}
			DebugLog.Write("ReelLocator.Migu", "counterAttack invisible");
			return false;
		}

		if (!miguCounterWasVisible)
		{
			miguCounterWasVisible = true;
			miguShiftFiredThisAppearance = false;
			miguShiftFireAt = now + MiguCounterShiftDelayMs;
			return false;
		}

		if (!miguShiftFiredThisAppearance && miguShiftFireAt != 0 && now >= miguShiftFireAt)
		{
			NativeKeyboard.PressShift(robloxWindow);
			miguShiftFiredThisAppearance = true;
			DebugLog.Write("ReelLocator.Migu", "shift pressed");
			return true;
		}

		return false;
	}

	public void ResetRodState()
	{
		splitbranchLastCrateClickAt = 0;
		miguCounterWasVisible = false;
		miguShiftFiredThisAppearance = false;
		miguShiftFireAt = 0;
	}

	public double? GetPowerBarPercent()
	{
		const long PowerBarStickyMs = 160;
		var now = Environment.TickCount64;
		if (!IsCached(cachedPowerBar, "bar"))
		{
			cachedPowerBar = ResolvePowerBar();
		}

		if (cachedPowerBar == 0)
		{
			return lastPowerPercent.HasValue && now - lastPowerSeenAt <= PowerBarStickyMs ? lastPowerPercent : null;
		}

		if (!TryReadGuiSize(cachedPowerBar, out var size))
		{
			cachedPowerBar = 0;
			return lastPowerPercent.HasValue && now - lastPowerSeenAt <= PowerBarStickyMs ? lastPowerPercent : null;
		}

		var scaleY = size.Y;
		if (double.IsNaN(scaleY) || double.IsInfinity(scaleY) || scaleY < -0.05 || scaleY > 1.5)
		{
			cachedPowerBar = 0;
			return lastPowerPercent.HasValue && now - lastPowerSeenAt <= PowerBarStickyMs ? lastPowerPercent : null;
		}

		var percent = Math.Clamp(scaleY * 100.0, 0, 100);
		lastPowerPercent = percent;
		lastPowerSeenAt = now;
		return percent;
	}

	public bool TryProbeOffsetsWorking(out bool versionMismatch)
	{
		versionMismatch = false;
		try
		{
			EnsureConnected();
			ulong dataModel = GetDataModel();
			if (dataModel == 0)
			{
				return false;
			}

			if (GetLocalPlayer() == 0)
			{
				return false;
			}

			return true;
		}
		catch (RobloxVersionMismatchException)
		{
			versionMismatch = true;
			return false;
		}
		catch
		{
			return false;
		}
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
			RobloxVersionGuard.EnsureCompatible(process, offsets.Version);
			baseAddress = GetMainModuleBase(process);
			memory = ProcessMemory.Open(process.Id);
			ResetTargets();
		}
	}

	private bool EnsureRodEquipped()
	{
		var slot = Math.Clamp(HotbarSlotSettings.RodSlot, 1, 9);
		NativeKeyboard.PressDigit(slot, robloxWindow);
		return true;
	}

	private bool SelectHotbarSlot(string slotKey)
	{
		if (string.IsNullOrWhiteSpace(slotKey))
		{
			return false;
		}

		if (int.TryParse(slotKey.Trim(), out var slot))
		{
			NativeKeyboard.PressDigit(Math.Clamp(slot, 1, 9), robloxWindow);
		}
		else if (slotKey.Length == 1)
		{
			NativeKeyboard.PressKey(slotKey[0]);
		}
		else
		{
			return false;
		}

		Thread.Sleep(75);
		return true;
	}

	private string GetEquippedToolName(long now)
	{
		EnsureCharacterPointers(now);
		if (cachedCharacter == 0)
		{
			return string.Empty;
		}

		foreach (var child in ReadChildren(cachedCharacter))
		{
			if (string.Equals(ReadClass(child), "Tool", StringComparison.OrdinalIgnoreCase))
			{
				return NormalizeRodDisplayText(ReadName(child));
			}
		}

		if (now >= nextCharacterRefreshAt)
		{
			cachedCharacter = 0;
			EnsureCharacterPointers(now);
			if (cachedCharacter != 0)
			{
				foreach (var child in ReadChildren(cachedCharacter))
				{
					if (string.Equals(ReadClass(child), "Tool", StringComparison.OrdinalIgnoreCase))
					{
						return NormalizeRodDisplayText(ReadName(child));
					}
				}
			}
		}

		return string.Empty;
	}

	private void EnsureCharacterPointers(long now)
	{
		if (cachedCharacter != 0 && now < nextCharacterRefreshAt)
		{
			return;
		}

		cachedWorkspace = FindWorkspace();
		cachedLocalPlayer = GetLocalPlayer();
		if (cachedWorkspace == 0 || cachedLocalPlayer == 0)
		{
			cachedCharacter = 0;
			cachedPlayerName = string.Empty;
			nextCharacterRefreshAt = now + 500;
			return;
		}

		cachedPlayerName = ReadName(cachedLocalPlayer);
		if (string.IsNullOrWhiteSpace(cachedPlayerName))
		{
			cachedCharacter = 0;
			nextCharacterRefreshAt = now + 500;
			return;
		}

		cachedCharacter = FindChildByName(cachedWorkspace, cachedPlayerName);
		nextCharacterRefreshAt = now + (cachedCharacter == 0 ? 500 : 2000);
	}

	private bool IsSelectedRodEquipped(string equippedToolName)
	{
		return IsSelectedRodEquipped(equippedToolName, GetHotbarRodDisplayText());
	}

	private bool IsSelectedRodEquipped(string equippedToolName, string selectedDisplay)
	{
		if (string.IsNullOrWhiteSpace(equippedToolName))
		{
			return false;
		}

		var selected = NormalizeRodDisplayText(selectedDisplay);
		var equipped = NormalizeRodDisplayText(equippedToolName);
		if (selected.Length == 0 || equipped.Length == 0)
		{
			return false;
		}

		if (TextRoughMatch(selected, equipped))
		{
			return true;
		}

		foreach (var line in selected.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			if (TextRoughMatch(line, equipped))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsRodEquipped(string equippedToolName)
	{
		if (string.IsNullOrWhiteSpace(equippedToolName))
		{
			return false;
		}

		return equippedToolName.Contains("rod", StringComparison.OrdinalIgnoreCase) ||
			equippedToolName.Contains("aria", StringComparison.OrdinalIgnoreCase) ||
			equippedToolName.Contains("castbound", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TextRoughMatch(string left, string right)
	{
		if (left.Length == 0 || right.Length == 0)
		{
			return false;
		}

		return string.Equals(left, right, StringComparison.OrdinalIgnoreCase) ||
			left.Contains(right, StringComparison.OrdinalIgnoreCase) ||
			right.Contains(left, StringComparison.OrdinalIgnoreCase);
	}

	private string GetHotbarRodDisplayText()
	{
		var hotbar = GetHotbarGui();
		if (hotbar == 0)
		{
			return string.Empty;
		}

		var slots = new List<ulong>();
		foreach (var slot in ReadChildren(hotbar))
		{
			if (!string.Equals(ReadClass(slot), "ImageButton", StringComparison.OrdinalIgnoreCase) ||
				!string.Equals(ReadName(slot), "ItemTemplate", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			slots.Add(slot);
		}

		if (slots.Count == 0)
		{
			return string.Empty;
		}

		var selectedIndex = Math.Clamp(HotbarSlotSettings.RodSlot, 1, 9) - 1;
		if (selectedIndex >= slots.Count)
		{
			selectedIndex = slots.Count - 1;
		}

		var selectedText = ReadSlotText(slots[selectedIndex]);
		if (!string.IsNullOrWhiteSpace(selectedText))
		{
			return selectedText;
		}

		foreach (var slot in slots)
		{
			var text = ReadSlotText(slot);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}

		return string.Empty;
	}

	private ulong GetHotbarGui()
	{
		if (cachedHotbarGui != 0 && ProcessMemory.IsLikelyUserModeAddress(cachedHotbarGui))
		{
			return cachedHotbarGui;
		}

		var localPlayer = GetLocalPlayer();
		if (localPlayer == 0)
		{
			return 0;
		}

		var playerGui = FindDescendantByClass(localPlayer, "PlayerGui");
		var backpack = playerGui == 0 ? 0 : FindDescendantByName(playerGui, "backpack");
		var hotbar = backpack == 0 ? 0 : FindDescendantByName(backpack, "hotbar");
		if (hotbar != 0)
		{
			cachedHotbarGui = hotbar;
		}

		return hotbar;
	}

	private string ReadSlotText(ulong slot)
	{
		var nameInstance = FindChildByName(slot, "ItemName");
		if (nameInstance == 0)
		{
			return string.Empty;
		}

		return NormalizeRodDisplayText(ReadGuiText(nameInstance));
	}

	private ulong GetLocalPlayer()
	{
		var dataModel = GetDataModel();
		if (dataModel == 0)
		{
			return 0;
		}

		var players = FindDescendantByClass(dataModel, "Players");
		if (players == 0)
		{
			return 0;
		}

		var localPlayer = memory.ReadPtr(players + offsets.Get("Player", "LocalPlayer"));
		return ProcessMemory.IsLikelyUserModeAddress(localPlayer) ? localPlayer : 0;
	}

	private ulong GetDataModel()
	{
		var fakeDataModel = memory.ReadPtr(baseAddress + offsets.Get("FakeDataModel", "Pointer"));
		var dataModel = memory.ReadPtr(fakeDataModel + offsets.Get("FakeDataModel", "RealDataModel"));
		return ProcessMemory.IsLikelyUserModeAddress(dataModel) ? dataModel : 0;
	}

	private ulong GetWorldConfig()
	{
		if (cachedWorldConfig != 0 && ProcessMemory.IsLikelyUserModeAddress(cachedWorldConfig))
		{
			return cachedWorldConfig;
		}

		var dataModel = GetDataModel();
		if (dataModel == 0)
		{
			return 0;
		}

		var replicatedStorage = FindDescendantByClass(dataModel, "ReplicatedStorage");
		if (replicatedStorage == 0)
		{
			return 0;
		}

		var world = FindChildByName(replicatedStorage, "world");
		if (world != 0)
		{
			cachedWorldConfig = world;
		}

		return world;
	}

	private string GetCurrentWeather()
	{
		var weather = GetWorldWeatherInstance();
		return weather == 0 ? string.Empty : ReadWorldStringValue(weather).Trim();
	}

	private string GetCurrentMeteorological()
	{
		var weather = GetWorldWeatherInstance();
		if (weather == 0)
		{
			return string.Empty;
		}

		var meteorological = FindChildByName(weather, "meteorological");
		return NormalizeWorldNone(ReadWorldStringValue(meteorological));
	}

	private string GetCurrentCycle()
	{
		var world = GetWorldConfig();
		return world == 0 ? string.Empty : ReadWorldStringValue(FindChildByName(world, "cycle")).Trim();
	}

	private ulong GetWorldWeatherInstance()
	{
		var world = GetWorldConfig();
		return world == 0 ? 0 : FindChildByName(world, "weather");
	}

	private string ReadWorldStringValue(ulong instanceAddr)
	{
		if (!ProcessMemory.IsLikelyUserModeAddress(instanceAddr))
		{
			return string.Empty;
		}

		var valueOffset = offsets.Get("Misc", "Value");
		var embedded = memory.ReadRobloxString(instanceAddr + valueOffset);
		if (!string.IsNullOrEmpty(embedded))
		{
			return embedded;
		}

		var ptr = memory.ReadPtr(instanceAddr + valueOffset);
		return ProcessMemory.IsLikelyUserModeAddress(ptr) ? memory.ReadRobloxString(ptr) : string.Empty;
	}

	private static string NormalizeWorldNone(string value)
	{
		var trimmed = value?.Trim() ?? string.Empty;
		return trimmed.Equals("none", StringComparison.OrdinalIgnoreCase) ? string.Empty : trimmed;
	}

	private ulong FindHotbarItemByName(string itemName)
	{
		var hotbar = GetHotbarGui();
		if (hotbar == 0)
		{
			return 0;
		}

		foreach (var itemAddr in ReadChildren(hotbar))
		{
			if (!string.Equals(ReadClass(itemAddr), "ImageButton", StringComparison.OrdinalIgnoreCase) ||
				!string.Equals(ReadName(itemAddr), "ItemTemplate", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (string.Equals(ReadHotbarItemName(itemAddr), itemName, StringComparison.OrdinalIgnoreCase))
			{
				return itemAddr;
			}
		}

		return 0;
	}

	private string ReadHotbarItemName(ulong itemAddr)
	{
		var nameInst = FindChildByName(itemAddr, "ItemName");
		return nameInst == 0 ? string.Empty : NormalizeHotbarItemText(ReadGuiText(nameInst));
	}

	private string ReadHotbarItemSlotKey(ulong itemAddr)
	{
		foreach (var childAddr in ReadChildren(itemAddr))
		{
			if (string.Equals(ReadClass(childAddr), "TextLabel", StringComparison.OrdinalIgnoreCase) &&
				string.Equals(ReadName(childAddr), "TextLabel", StringComparison.OrdinalIgnoreCase))
			{
				return NormalizeHotbarItemText(ReadGuiText(childAddr));
			}
		}

		return string.Empty;
	}

	private string NormalizeHotbarItemText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		return Regex.Replace(text, "<[^>]+>", string.Empty, RegexOptions.CultureInvariant).Trim();
	}

	private ulong FindWorkspace()
	{
		var dataModel = GetDataModel();
		if (dataModel == 0)
		{
			return 0;
		}

		return FindDescendant(dataModel, item =>
			string.Equals(ReadName(item), "Workspace", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ReadClass(item), "Workspace", StringComparison.OrdinalIgnoreCase));
	}

	private ulong FindChildByClass(ulong parent, string className)
	{
		foreach (var child in ReadChildren(parent))
		{
			if (string.Equals(ReadClass(child), className, StringComparison.OrdinalIgnoreCase))
			{
				return child;
			}
		}

		return 0;
	}

	private ulong FindChildByName(ulong parent, string name)
	{
		foreach (var child in ReadChildren(parent))
		{
			if (string.Equals(ReadName(child), name, StringComparison.OrdinalIgnoreCase))
			{
				return child;
			}
		}

		return 0;
	}

	private ulong FindDescendantByClass(ulong root, string className)
	{
		return FindDescendant(root, item => IsClass(item, className));
	}

	private ulong FindDescendantByName(ulong root, string name)
	{
		return FindDescendant(root, item => string.Equals(ReadName(item), name, StringComparison.OrdinalIgnoreCase));
	}

	private ulong FindDescendantFrameByName(ulong root, string name)
	{
		return FindDescendant(root, item =>
			string.Equals(ReadName(item), name, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(ReadClass(item), "Frame", StringComparison.OrdinalIgnoreCase));
	}

	private ulong FindDescendant(ulong root, Func<ulong, bool> predicate)
	{
		Queue<ulong> queue = new Queue<ulong>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		queue.Enqueue(root);
		int depth = 0;
		while (queue.Count > 0 && depth++ < 60000)
		{
			ulong item = queue.Dequeue();
			if (!hashSet.Add(item))
			{
				continue;
			}
			if (predicate(item))
			{
				return item;
			}
			if (depth >= MaxDepth)
			{
				continue;
			}
			foreach (ulong child in ReadChildren(item))
			{
				queue.Enqueue(child);
			}
		}
		return 0;
	}

	public IReadOnlyList<ulong> ReadChildren(ulong instance)
	{
		if (!ProcessMemory.IsLikelyUserModeAddress(instance))
		{
			return Array.Empty<ulong>();
		}

		ulong listPtr = memory.ReadPtr(instance + offsets.Get("Instance", "ChildrenStart"));
		ulong arrayStart = memory.ReadPtr(listPtr);
		ulong arrayEnd = memory.ReadPtr(listPtr + offsets.Get("Instance", "ChildrenEnd"));
		if (!LooksLikeVectorRange(arrayStart, arrayEnd))
		{
			arrayStart = listPtr;
			arrayEnd = memory.ReadPtr(instance + offsets.Get("Instance", "ChildrenStart") + offsets.Get("Instance", "ChildrenEnd"));
		}
		if (!LooksLikeVectorRange(arrayStart, arrayEnd))
		{
			return Array.Empty<ulong>();
		}

		List<ulong> children = new List<ulong>();
		for (ulong entry = arrayStart; entry < arrayEnd && children.Count < 2000; entry += 16)
		{
			ulong child = memory.ReadPtr(entry);
			if (ProcessMemory.IsLikelyUserModeAddress(child))
			{
				children.Add(child);
			}
		}

		return children;
	}

	public string ReadName(ulong instance)
	{
		ulong stringAddress = memory.ReadPtr(instance + offsets.Get("Instance", "Name"));
		return memory.ReadRobloxString(stringAddress);
	}

	public string ReadClass(ulong instance)
	{
		ulong descriptor = memory.ReadPtr(instance + offsets.Get("Instance", "ClassDescriptor"));
		ulong stringAddress = memory.ReadPtr(descriptor + offsets.Get("Instance", "ClassName"));
		return memory.ReadRobloxString(stringAddress);
	}

	private string ReadGuiText(ulong instance)
	{
		foreach (var offsetName in new[] { "Text", "TextLabelText", "ContentText" })
		{
			try
			{
				ulong num = offsets.Get("GuiObject", offsetName);
				ulong stringAddress = memory.ReadPtr(instance + num);
				string text = memory.ReadRobloxString(stringAddress);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
				text = memory.ReadRobloxString(instance + num);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
			catch
			{
			}
		}

		return string.Empty;
	}

	private bool TryReadGuiSize(ulong instance, out Vector2 size)
	{
		size = default;
		return TryReadGuiBounds(instance, out _, out size);
	}

	private ulong FindPlayerGui()
	{
		var localPlayer = GetLocalPlayer();
		if (localPlayer == 0)
		{
			return 0;
		}

		var playerGui = FindDescendantByClass(localPlayer, "PlayerGui");
		if (playerGui != 0)
		{
			return playerGui;
		}

		return FindDescendantByName(localPlayer, "PlayerGui");
	}

	private ulong FindReelGui()
	{
		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			return 0;
		}

		return FindChildByName(playerGui, "reel");
	}

	private ReelContext BuildReelContext(ulong reelGui)
	{
		var bar = FindChildByName(reelGui, "bar");
		if (bar == 0 || !TryReadGuiBounds(bar, out _, out _))
		{
			return null;
		}

		var fish = FindChildByName(bar, "fish");
		var playerbar = FindChildByName(bar, "playerbar");
		if (fish == 0 || playerbar == 0 || !HasReadableGuiBounds(fish) || !HasReadableGuiBounds(playerbar))
		{
			return null;
		}

		return new ReelContext(reelGui, bar, fish, playerbar);
	}

	private ulong ResolvePowerBar()
	{
		var workspace = FindWorkspace();
		var localPlayer = GetLocalPlayer();
		if (workspace == 0 || localPlayer == 0)
		{
			return 0;
		}

		var playerName = ReadName(localPlayer);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			return 0;
		}

		var character = FindChildByName(workspace, playerName);
		var rootPart = character == 0 ? 0 : FindChildByName(character, "HumanoidRootPart");
		var powerGui = rootPart == 0 ? 0 : FindChildByName(rootPart, "power");
		var bar = powerGui == 0 ? 0 : FindDescendantFrameByName(powerGui, "bar");
		if (bar != 0)
		{
			return bar;
		}

		powerGui = rootPart == 0 ? 0 : FindDescendantByName(rootPart, "power");
		bar = powerGui == 0 ? 0 : FindDescendantFrameByName(powerGui, "bar");
		if (bar != 0)
		{
			return bar;
		}

		powerGui = character == 0 ? 0 : FindDescendantByName(character, "power");
		return powerGui == 0 ? 0 : FindDescendantFrameByName(powerGui, "bar");
	}

	private ulong ResolveInventoryFrame()
	{
		var localPlayer = GetLocalPlayer();
		if (localPlayer == 0)
		{
			return 0;
		}

		var playerGui = FindDescendantByClass(localPlayer, "PlayerGui");
		if (playerGui == 0)
		{
			playerGui = FindDescendantByName(localPlayer, "PlayerGui");
		}

		var backpack = playerGui == 0 ? 0 : FindDescendantByName(playerGui, "backpack");
		return backpack == 0 ? 0 : FindDescendantByName(backpack, "inventory");
	}

	private ulong ResolveInventorySearchFrame(ulong inventory)
	{
		var byPath = FindByPath(inventory, "Search", "Frame", "topbar", "search");
		if (byPath != 0)
		{
			return byPath;
		}

		var byDescendant = FindDescendantByName(inventory, "search");
		if (byDescendant != 0)
		{
			return byDescendant;
		}

		return FindByPath(inventory, "Search", string.Empty, "topbar");
	}

	private ulong ResolveInventoryItemContainer(ulong inventory)
	{
		var byName = FindChildByName(inventory, "itemContainer");
		if (byName != 0)
		{
			return byName;
		}

		var byDescendant = FindDescendantByName(inventory, "itemContainer");
		if (byDescendant != 0)
		{
			return byDescendant;
		}

		return FindByPath(inventory, "itemContainer", "Frame");
	}

	private ulong FindInventoryItemTarget(ulong itemContainer, string itemName)
	{
		var needle = NormalizeLoose(itemName);
		ulong partialMatch = 0;
		foreach (var node in Traverse(itemContainer, 32))
		{
			var cls = ReadClass(node);
			if (!cls.Contains("Text", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var text = NormalizeLoose(ReadGuiText(node));
			if (text.Length == 0)
			{
				continue;
			}

			var exact = IsInventoryItemExactMatch(text, needle);
			var contains = text.Contains(needle, StringComparison.OrdinalIgnoreCase);
			if (!exact && !contains)
			{
				continue;
			}

			var clickable = FindInventoryClickableAncestor(node, itemContainer);
			if (clickable == 0)
			{
				continue;
			}

			if (exact)
			{
				return clickable;
			}

			if (partialMatch == 0)
			{
				partialMatch = clickable;
			}
		}

		return partialMatch;
	}

	private bool IsInventoryItemExactMatch(string text, string needle)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(needle))
		{
			return false;
		}

		var normalized = text.Replace("\r", "\n", StringComparison.Ordinal).Trim();
		return string.Equals(normalized, needle, StringComparison.OrdinalIgnoreCase);
	}

	private ulong EnsureEnchantTarget()
	{
		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			return 0;
		}

		var enchantButton = FindByPath(playerGui, "Enchant", "Button", "backpack", "inventory", "topbuttons", "textbutton", "enchant");
		if (enchantButton == 0)
		{
			enchantButton = FindByPath(playerGui, string.Empty, "Button", "backpack", "inventory", "topbuttons", "enchant");
		}

		if (enchantButton == 0)
		{
			enchantButton = FindByNameClass(playerGui, "Enchant", "Button");
		}

		return enchantButton;
	}

	private ulong FindByNameClass(ulong root, string name, string classNeedle)
	{
		foreach (var item in Traverse(root, 256))
		{
			if (string.Equals(ReadName(item), name, StringComparison.OrdinalIgnoreCase) &&
				ClassMatches(ReadClass(item), classNeedle))
			{
				return item;
			}
		}

		return 0;
	}

	private bool ClassMatches(string value, string classNeedle)
	{
		return value.IndexOf(classNeedle, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string NormalizeLoose(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		return text.Replace("\r", "\n").Trim();
	}

	private ulong ResolveCounterAttackFrame()
	{
		var playerGui = FindPlayerGui();
		if (playerGui == 0)
		{
			return 0;
		}

		var screenGui = FindChildByName(playerGui, "reel");
		if (screenGui == 0)
		{
			screenGui = FindDescendantByName(playerGui, "reel");
			if (screenGui == 0)
			{
				return 0;
			}
		}

		var counterAttack = FindDescendantByName(screenGui, "counterAttack");
		if (counterAttack == 0)
		{
			return 0;
		}

		var cls = ReadClass(counterAttack);
		return cls.Equals("Frame", StringComparison.OrdinalIgnoreCase) ? counterAttack : 0;
	}

	private IEnumerable<ulong> Traverse(ulong root, int maxDepth)
	{
		if (!ProcessMemory.IsLikelyUserModeAddress(root))
		{
			yield break;
		}

		var queue = new Queue<(ulong Item, int Depth)>();
		var seen = new HashSet<ulong>();
		queue.Enqueue((root, 0));
		while (queue.Count > 0)
		{
			var (item, depth) = queue.Dequeue();
			if (!seen.Add(item))
			{
				continue;
			}

			yield return item;
			if (depth >= maxDepth)
			{
				continue;
			}

			foreach (var child in ReadChildren(item))
			{
				queue.Enqueue((child, depth + 1));
			}
		}
	}

	private ulong FindClickableAncestor(ulong node, ulong root)
	{
		var current = memory.ReadPtr(node + offsets.Get("Instance", "Parent"));
		while (ProcessMemory.IsLikelyUserModeAddress(current) && current != 0 && current != root)
		{
			var cls = ReadClass(current);
			if (cls.Equals("TextButton", StringComparison.OrdinalIgnoreCase) ||
				cls.Equals("ImageButton", StringComparison.OrdinalIgnoreCase))
			{
				return current;
			}

			current = memory.ReadPtr(current + offsets.Get("Instance", "Parent"));
		}

		return 0;
	}

	private ulong FindInventoryClickableAncestor(ulong node, ulong stopRoot)
	{
		ulong frameFallback = 0;
		var current = node;
		for (var i = 0; i < 10 && ProcessMemory.IsLikelyUserModeAddress(current); i++)
		{
			var cls = ReadClass(current);
			if (cls.Contains("Button", StringComparison.OrdinalIgnoreCase))
			{
				if (TryReadGuiBounds(current, out _, out var size) && size.X > 10 && size.Y > 10)
				{
					return current;
				}
			}
			else if (cls.Contains("Frame", StringComparison.OrdinalIgnoreCase) &&
				TryReadGuiBounds(current, out _, out var bounds) &&
				bounds.X > 10 && bounds.Y > 10 && bounds.X <= 500 && bounds.Y <= 110)
			{
				frameFallback = current;
			}

			if (current == stopRoot)
			{
				break;
			}

			var parent = memory.ReadPtr(current + offsets.Get("Instance", "Parent"));
			if (!ProcessMemory.IsLikelyUserModeAddress(parent) || parent == current)
			{
				break;
			}

			current = parent;
		}

		return frameFallback;
	}

	private ulong FindByPath(ulong root, params string[] path)
	{
		var current = root;
		foreach (var segment in path)
		{
			if (string.IsNullOrWhiteSpace(segment))
			{
				continue;
			}

			current = FindDescendantByName(current, segment);
			if (current == 0)
			{
				return 0;
			}
		}

		return current;
	}

	private string BuildPath(ulong instance)
	{
		List<string> segments = new List<string>();
		ulong current = instance;
		for (var i = 0; i < 14 && ProcessMemory.IsLikelyUserModeAddress(current); i++)
		{
			var name = ReadName(current);
			if (!string.IsNullOrEmpty(name))
			{
				segments.Add(name);
			}

			var parent = memory.ReadPtr(current + offsets.Get("Instance", "Parent"));
			if (!ProcessMemory.IsLikelyUserModeAddress(parent) || parent == current)
			{
				break;
			}

			current = parent;
		}

		segments.Reverse();
		return string.Join("/", segments.ToArray());
	}

	private Point GetClickPoint(ulong instance)
	{
		if (TryReadGuiBounds(instance, out var position, out var size))
		{
			return new Point((int)Math.Round(position.X + size.X / 2f), (int)Math.Round(position.Y + size.Y / 2f));
		}

		return Point.Empty;
	}
	private static string NormalizeRodDisplayText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		string normalized = text.Replace("\r", "\n", StringComparison.Ordinal);
		normalized = Regex.Replace(normalized, "<[^>]+>", string.Empty, RegexOptions.CultureInvariant);
		normalized = Regex.Replace(normalized, "[ \t]+", " ", RegexOptions.CultureInvariant);
		normalized = Regex.Replace(normalized, "\n+", "\n", RegexOptions.CultureInvariant);
		return normalized.Trim();
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

	private bool TryReadUDim2Scale(ulong address, out Vector2 value)
	{
		value = default(Vector2);
		byte[] array = memory.ReadBytes(address, 16);
		if (array == null || array.Length < 16)
		{
			return false;
		}

		value = new Vector2(BitConverter.ToSingle(array, 0), BitConverter.ToSingle(array, 8));
		return true;
	}

	private static bool IsReasonableScale(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 5f;
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
