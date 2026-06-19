using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Client.Services.Fishing;

internal sealed class AutoSovereignRechargeRunner
{
	private const int InventoryToggleDelayMs = 200;
	private const int SearchFocusDelayMs = 100;
	private const int SearchResultDelayMs = 700;
	private const int PostTypeSettleDelayMs = 250;
	private const int PostEnchantDelayMs = 350;
	private const int RetryDelayMs = 500;
	private const int MissingRelicRetryDelayMs = 600;
	private const string RelicName = "Enchant Relic";
	private const string InventorySearchText = "mutation:no Enchant Relic";

	private readonly ReelLocator locator;
	private string state = "IDLE";
	private bool recharging;
	private bool searchPrimed;
	private bool relicSelectedForUse;
	private DateTimeOffset nextStepAt = DateTimeOffset.MinValue;
	private ulong searchFrame;
	private ulong itemContainer;
	private ulong enchantButton;

	public AutoSovereignRechargeRunner(ReelLocator locator)
	{
		this.locator = locator;
	}

	public bool IsActive => recharging;

	public void Reset()
	{
		state = "IDLE";
		recharging = false;
		searchPrimed = false;
		relicSelectedForUse = false;
		nextStepAt = DateTimeOffset.MinValue;
		searchFrame = 0;
		itemContainer = 0;
		enchantButton = 0;
	}

	public AutoSovereignRechargeResult Step(double minPercent, double maxPercent)
	{
		if (locator.WindowHandle == IntPtr.Zero)
		{
			Reset();
			return new AutoSovereignRechargeResult(false, false, "Waiting for Roblox window focus.", ReadPowerPercent());
		}

		var normalizedMin = Math.Clamp(minPercent, 0, 100);
		var normalizedMax = Math.Clamp(maxPercent, normalizedMin, 100);
		var currentPower = ReadPowerPercent();
		var now = DateTimeOffset.UtcNow;

		if (!recharging)
		{
			if (currentPower is null)
			{
				recharging = true;
				state = "OPEN_INVENTORY";
				searchPrimed = false;
				relicSelectedForUse = false;
				nextStepAt = DateTimeOffset.MinValue;
				return new AutoSovereignRechargeResult(false, false, "Power read unavailable; starting sequence.", null);
			}

			if (currentPower.Value >= normalizedMin)
			{
				return new AutoSovereignRechargeResult(false, false, $"Power healthy: {currentPower.Value:0.0}% (min {normalizedMin:0.0}%).", currentPower);
			}

			recharging = true;
			state = "OPEN_INVENTORY";
			searchPrimed = false;
			relicSelectedForUse = false;
			nextStepAt = DateTimeOffset.MinValue;
		}

		if (currentPower is not null && currentPower.Value >= normalizedMax)
		{
			if (state != "IDLE")
			{
				NativeKeyboard.PressG(locator.WindowHandle);
			}

			Reset();
			return new AutoSovereignRechargeResult(true, false, $"Power recharged: {currentPower.Value:0.0}% (max {normalizedMax:0.0}%).", currentPower);
		}

		if (now < nextStepAt)
		{
			return new AutoSovereignRechargeResult(false, false, $"Recharging... {FormatPower(currentPower)}", currentPower);
		}

		try
		{
			switch (state)
			{
				case "OPEN_INVENTORY":
					locator.TryOpenInventory();
					state = "PREPARE_SEARCH";
					nextStepAt = now.AddMilliseconds(InventoryToggleDelayMs);
					return new AutoSovereignRechargeResult(false, false, "Opening inventory.", currentPower);

				case "PREPARE_SEARCH":
					if (!locator.TryPrepareSovereignInventoryTargets(out searchFrame, out itemContainer))
					{
						nextStepAt = now.AddMilliseconds(RetryDelayMs);
						return new AutoSovereignRechargeResult(false, true, "Inventory search targets not found.", currentPower);
					}

					if (!searchPrimed)
					{
						locator.TryClickGuiCenter(searchFrame);
						Thread.Sleep(SearchFocusDelayMs);
						DebugLog.Write("AutoSovereignRechargeRunner.Step", "clear search ctrl+a backspace");
						NativeKeyboard.PressCtrlA(locator.WindowHandle);
						Thread.Sleep(25);
						NativeKeyboard.PressBackspace(locator.WindowHandle);
						Thread.Sleep(25);
						NativeKeyboard.TypeText(InventorySearchText, locator.WindowHandle, 35);
						nextStepAt = DateTimeOffset.UtcNow.AddMilliseconds(PostTypeSettleDelayMs);
						searchPrimed = true;
						state = "WAIT_SEARCH_SETTLE";
						return new AutoSovereignRechargeResult(false, false, "Typing inventory search.", currentPower);
					}

					state = "CLICK_RELIC";
					nextStepAt = now.AddMilliseconds(SearchResultDelayMs);
					return new AutoSovereignRechargeResult(false, false, "Searching Enchant Relic.", currentPower);

				case "WAIT_SEARCH_SETTLE":
					state = "CLICK_RELIC";
					nextStepAt = now.AddMilliseconds(SearchResultDelayMs);
					return new AutoSovereignRechargeResult(false, false, "Searching Enchant Relic.", currentPower);

				case "CLICK_RELIC":
					if (!locator.TrySelectInventoryItem(itemContainer, RelicName))
					{
						searchPrimed = false;
						relicSelectedForUse = false;
						state = "PREPARE_SEARCH";
						nextStepAt = now.AddMilliseconds(MissingRelicRetryDelayMs);
						return new AutoSovereignRechargeResult(false, true, "Could not find Enchant Relic in inventory.", currentPower);
					}

					relicSelectedForUse = true;
					state = "CLICK_ENCHANT";
					nextStepAt = now.AddMilliseconds(InventoryToggleDelayMs);
					return new AutoSovereignRechargeResult(false, false, "Selecting Enchant Relic.", currentPower);

				case "CLICK_ENCHANT":
					if (!relicSelectedForUse)
					{
						state = "CLICK_RELIC";
						nextStepAt = now.AddMilliseconds(RetryDelayMs);
						return new AutoSovereignRechargeResult(false, true, "Relic not selected; retrying.", currentPower);
					}

					if (!locator.TryClickEnchantButton())
					{
						state = "CLICK_ENCHANT";
						nextStepAt = now.AddMilliseconds(RetryDelayMs);
						return new AutoSovereignRechargeResult(false, true, "Enchant button not ready; retrying.", currentPower);
					}

					relicSelectedForUse = false;
					state = "CONFIRM_ENCHANT";
					nextStepAt = now.AddMilliseconds(InventoryToggleDelayMs);
					return new AutoSovereignRechargeResult(false, false, "Using Enchant Relic.", currentPower);

				case "CONFIRM_ENCHANT":
					NativeKeyboard.PressEnter(locator.WindowHandle);
					state = "CLICK_RELIC";
					nextStepAt = now.AddMilliseconds(PostEnchantDelayMs);
					return new AutoSovereignRechargeResult(false, false, $"Recharge cycle complete. {FormatPower(currentPower)}", currentPower);

				default:
					Reset();
					return new AutoSovereignRechargeResult(false, false, "Idle.", currentPower);
			}
		}
		catch
		{
			Reset();
			throw;
		}
	}

	private double? ReadPowerPercent()
	{
		if (!locator.TryGetSovereignPowerPercent(out var power))
		{
			return null;
		}

		return power;
	}

	private static string FormatPower(double? power)
	{
		return power is null ? "Power unknown." : $"Power {power.Value:0.0}%";
	}
}

internal readonly record struct AutoSovereignRechargeResult(bool Completed, bool Failed, string Status, double? CurrentPowerPercent);
