using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client.Services.Fishing;

internal sealed class ReelControlForm : Form
{
	private const int HotkeyId = 21041;

	private const int WmHotkey = 786;

	private const uint VkF3 = 114u;

	private const double AutoAquariumCycleDelaySeconds = 65 * 60;
	private const double AutoSovThresholdPercent = 91.0;
	private const int DefaultRodStartupHoldBiasMs = 3000;
	private readonly Button toggleButton;

	private readonly Timer timer;

	private readonly Timer topMostTimer;

	private readonly ReelLocator locator = new ReelLocator();

	private readonly ReelController controller = new ReelController();
	private readonly ReelController bellonaRightController = new ReelController();

	private readonly Tracking2Controller startupAssistController = new Tracking2Controller();

	private readonly ReelControlSettings controlSettings = new ReelControlSettings();

	private readonly Tracking2Settings startupAssistSettings = new Tracking2Settings();

	private readonly FishingHoldGate fishingGate = new FishingHoldGate();
	private readonly FishSkipSoundDetector fishSkipDetector = new FishSkipSoundDetector();

	private RodKind currentRodKind = RodKind.Default;

	private RodProfile currentRodProfile = new DefaultRodProfile();

	private readonly AquariumSequenceRunner aquariumRunner = new AquariumSequenceRunner();
	private readonly AutoSovereignRechargeRunner sovRunner;

	private readonly TranquilityController tranquilityController;

	private const double TrackingProbeDelaySeconds = 0.25;

	private readonly CheckBox autoAquariumToggle;
	private readonly CheckBox autoSovToggle;

	private readonly CheckBox autoAuroraToggle;
	private readonly CheckBox autoWindyDayToggle;
	private readonly CheckBox fishSkipToggle;
	private readonly CheckBox fishSkipNormalToggle;
	private readonly CheckBox fishSkipLegendaryToggle;
	private readonly GroupBox fishSkipGroup;

	private readonly Label offsetsStatusLabel;

	private readonly Label rodStatusLabel;

	private readonly Label macroStatusLabel;
	private readonly Label fishSkipSoundLabel;
	private readonly Button notifyHeaderButton;
	private readonly Panel notifyPanel;
	private readonly CheckBox notifyEnabledToggle;
	private readonly TextBox notifyWebhookInput;
	private readonly Button notifyTestButton;
	private readonly Label notifyWebhookLabel;
	private readonly Label notifyCountdownLabel;

	private readonly NumericUpDown rodSlotInput;
	private readonly ComboBox castModeInput;

	private bool running;

	private bool aquariumPending;

	private bool aquariumDue;
	private bool sovPending;

	private bool sovDue;

	private double nextSovCycleTime;
	private bool weatherToggleSyncing;

	private bool autoAuroraBlockedUntilCatchEnd;

	private bool autoAuroraNightCovered;

	private bool autoAuroraNeedsRodReequip;

	private string autoAuroraState = "IDLE";

	private int autoAuroraRetryCount;

	private long autoAuroraWaitStartedAt;

	private bool autoWindyDayBlockedUntilCatchEnd;

	private bool autoWindyDayNightCovered;

	private bool autoWindyDayNeedsRodReequip;

	private string autoWindyDayState = "IDLE";

	private int autoWindyDayRetryCount;

	private long autoWindyDayWaitStartedAt;

	private bool wasInMinigame;
	private bool notifySectionExpanded;
	private bool notifySettingsLoading;
	private bool notifySendInFlight;
	private long notifyTrackingDeadlineAt;
	private long notifyNextSendAt;

	private const int NotifyIfStoppedDelayMs = 180000;
	private const int NotifyIfStoppedRepeatMs = 60000;

	private bool startupAssistActive;

	private long startupAssistStartedAt;

	private double startupAssistStartFishCenter;

	private long fishingInputReadyAt;
	private long perfectCastReadyAt;
	private long nextFishSkipPollAt;
	private bool fishSkipEvadeActive;
	private bool fishSkipEvadeLegendary;
	private long fishSkipRequiemNextClickAt;
	private bool fishSkipRequiemClickDown;
	private bool bellonaRightHolding;
	private bool bellonaRightLastSecondaryPresent;
	private bool bellonaRightLastDesiredHolding;
	private long bellonaRightLastLogAt;
	private bool bellonaRightRawDesiredHolding;
	private long bellonaRightRawDesiredChangedAt;
	private long bellonaRightLastMetricsSeenAt;
	private double? bellonaRightLastFishCenter;
	private double? bellonaRightLastBarCenter;
	private long bellonaRightLastMotionAt;
	private long bellonaRightStaleSinceAt;
	private long bellonaRightLastStateLogAt;
	private long bellonaRightReacquireUntilAt;
	private long bellonaRightHybridHoldUntilAt;
	private bool bellonaSingleReelAssignRight;
	private long bellonaRmbWatchdogLogAt;
	private bool bellonaActiveThisTick;
	private const int BellonaRightPressDebounceMs = 30;
	private const int BellonaRightLossGraceMs = 220;
	private const int BellonaRightStaleWatchdogMs = 700;
	private const int BellonaRightReacquireReleaseMs = 0;
	private const int BellonaRightHybridHoldBiasMs = 35;

	private long lastShakedAt;

	private bool shakeInputActive;

	private bool hadMetricsLastTick;

	private double nextProbeTime;

	private double nextAquariumCycleTime;
	private FishSkipSoundSnapshot fishSkipSoundState = FishSkipSoundSnapshot.Empty;

	private const int AutoAuroraWaitMs = 30000;
	private const int AutoTotemActionDelayMs = 1000;

	private bool versionMismatchShown;

	private bool offsetsWarningShown;

	private readonly Stopwatch stopwatch = Stopwatch.StartNew();

	public ReelControlForm()
	{
	sovRunner = new AutoSovereignRechargeRunner(locator);
	Text = "Reel Control v2";
	base.Width = 292;
	base.Height = 415;
	MinimumSize = new Size(292, 415);
		base.StartPosition = FormStartPosition.CenterScreen;
		base.TopMost = true;
		BackColor = Color.FromArgb(24, 27, 31);
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 9f);
		tranquilityController = new TranquilityController(locator);
		Panel panel = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(14),
			BackColor = Color.FromArgb(24, 27, 31)
		};
		toggleButton = new Button
		{
			Text = "Start (F3)",
			Width = 112,
			Height = 32,
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(47, 129, 247),
			ForeColor = Color.White
		};
		toggleButton.FlatAppearance.BorderSize = 0;
		Button button = toggleButton;
		EventHandler value = delegate
		{
			ToggleRunning();
		};
		button.Click += value;
		panel.Controls.Add(toggleButton);
		autoAquariumToggle = new CheckBox
		{
			Text = "Auto Aquarium",
			Left = 0,
			Top = 42,
			Width = 130,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(autoAquariumToggle);
		autoSovToggle = new CheckBox
		{
			Text = "Auto Sov",
			Left = 0,
			Top = 67,
			Width = 130,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(autoSovToggle);
		autoSovToggle.Checked = false;
		autoAuroraToggle = new CheckBox
		{
			Text = "Auto Aurora - place in hotbar",
			Left = 0,
			Top = 92,
			Width = 250,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(autoAuroraToggle);
		autoWindyDayToggle = new CheckBox
		{
			Text = "Auto Windy Day - place in hotbar",
			Left = 0,
			Top = 117,
			Width = 250,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		autoWindyDayToggle.CheckedChanged += delegate
		{
			if (weatherToggleSyncing || !autoWindyDayToggle.Checked)
			{
				return;
			}

			weatherToggleSyncing = true;
			autoAuroraToggle.Checked = false;
			weatherToggleSyncing = false;
		};
		panel.Controls.Add(autoWindyDayToggle);
		Label rodSlotLabel = new Label
		{
			Text = "Rod Slot",
			Left = 0,
			Top = 145,
			Width = 60,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(rodSlotLabel);
		rodSlotInput = new NumericUpDown
		{
			Left = 66,
			Top = 142,
			Width = 60,
			Minimum = 1m,
			Maximum = 9m,
			DecimalPlaces = 0,
			Increment = 1m,
			Value = HotbarSlotSettings.RodSlot
		};
		rodSlotInput.ValueChanged += delegate
		{
			HotbarSlotSettings.RodSlot = (int)rodSlotInput.Value;
		};
		panel.Controls.Add(rodSlotInput);
		Label castModeLabel = new Label
		{
			Text = "Cast Mode",
			Left = 0,
			Top = 170,
			Width = 70,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(castModeLabel);
		castModeInput = new ComboBox
		{
			Left = 72,
			Top = 167,
			Width = 100,
			DropDownStyle = ComboBoxStyle.DropDownList
		};
		castModeInput.Items.AddRange(new object[] { "normal", "perfect" });
		castModeInput.SelectedItem = controlSettings.CastMode;
		castModeInput.SelectedIndexChanged += delegate
		{
			controlSettings.CastMode = castModeInput.SelectedItem?.ToString() ?? "normal";
		};
		panel.Controls.Add(castModeInput);
		fishSkipToggle = new CheckBox
		{
			Text = "Fish Skip",
			Left = 0,
			Top = 195,
			Width = 100,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31),
			Checked = controlSettings.FishSkipEnabled
		};
		fishSkipToggle.CheckedChanged += delegate
		{
			controlSettings.FishSkipEnabled = fishSkipToggle.Checked;
			fishSkipGroup.Enabled = fishSkipToggle.Checked;
			if (!fishSkipToggle.Checked)
			{
				fishSkipSoundState = FishSkipSoundSnapshot.Empty;
			}
		};
		panel.Controls.Add(fishSkipToggle);
		fishSkipGroup = new GroupBox
		{
			Text = "Skip Selected",
			Left = 0,
			Top = 192,
			Width = 240,
			Height = 62,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31),
			Enabled = controlSettings.FishSkipEnabled
		};
		fishSkipNormalToggle = new CheckBox
		{
			Text = "Normal",
			Left = 10,
			Top = 24,
			Width = 70,
			Checked = controlSettings.FishSkipNormal,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		fishSkipNormalToggle.CheckedChanged += delegate
		{
			controlSettings.FishSkipNormal = fishSkipNormalToggle.Checked;
		};
		fishSkipLegendaryToggle = new CheckBox
		{
			Text = "Leg/Mythic",
			Left = 104,
			Top = 24,
			Width = 124,
			Checked = controlSettings.FishSkipLegendaryMythic,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		fishSkipLegendaryToggle.CheckedChanged += delegate
		{
			controlSettings.FishSkipLegendaryMythic = fishSkipLegendaryToggle.Checked;
		};
		fishSkipGroup.Controls.Add(fishSkipNormalToggle);
		fishSkipGroup.Controls.Add(fishSkipLegendaryToggle);
		panel.Controls.Add(fishSkipGroup);
		notifyHeaderButton = new Button
		{
			Text = "Notify if stopped >",
			Left = 0,
			Top = 258,
			Width = 160,
			Height = 24,
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(37, 42, 48),
			ForeColor = Color.White
		};
		notifyHeaderButton.FlatAppearance.BorderSize = 0;
		notifyHeaderButton.Click += delegate
		{
			SetNotifySectionExpanded(!notifySectionExpanded);
		};
		panel.Controls.Add(notifyHeaderButton);
		notifyCountdownLabel = new Label
		{
			Text = "03:00",
			Left = 168,
			Top = 261,
			Width = 50,
			Height = 20,
			ForeColor = Color.FromArgb(251, 191, 36),
			BackColor = Color.FromArgb(24, 27, 31),
			TextAlign = ContentAlignment.MiddleLeft
		};
		panel.Controls.Add(notifyCountdownLabel);
		notifyPanel = new Panel
		{
			Left = 0,
			Top = 286,
			Width = 240,
			Height = 88,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = Color.FromArgb(24, 27, 31),
			Visible = false
		};
		notifyEnabledToggle = new CheckBox
		{
			Text = "On",
			Left = 8,
			Top = 7,
			Width = 46,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		notifyEnabledToggle.CheckedChanged += delegate
		{
			if (notifySettingsLoading)
			{
				return;
			}

			SaveNotifySettings();
		};
		notifyPanel.Controls.Add(notifyEnabledToggle);
		notifyWebhookLabel = new Label
		{
			Text = "Webhook",
			Left = 64,
			Top = 9,
			Width = 70,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		notifyPanel.Controls.Add(notifyWebhookLabel);
		notifyWebhookInput = new TextBox
		{
			Left = 8,
			Top = 31,
			Width = 220,
			Height = 23,
			BackColor = Color.FromArgb(17, 24, 39),
			ForeColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle
		};
		notifyWebhookInput.TextChanged += delegate
		{
			if (notifySettingsLoading)
			{
				return;
			}

			SaveNotifySettings();
		};
		notifyPanel.Controls.Add(notifyWebhookInput);
		notifyTestButton = new Button
		{
			Text = "Test",
			Left = 8,
			Top = 58,
			Width = 72,
			Height = 24,
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(47, 129, 247),
			ForeColor = Color.White
		};
		notifyTestButton.FlatAppearance.BorderSize = 0;
		notifyTestButton.Click += async delegate
		{
			await SendWebhookTestAsync().ConfigureAwait(true);
		};
		notifyPanel.Controls.Add(notifyTestButton);
		panel.Controls.Add(notifyPanel);
		offsetsStatusLabel = new Label
		{
			Text = "Disconnected",
			Left = 0,
			Top = 261,
			Width = 120,
			Height = 20,
			ForeColor = Color.FromArgb(248, 113, 113),
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(offsetsStatusLabel);
		rodStatusLabel = new Label
		{
			Text = "Rod: Unknown",
			Left = 0,
			Top = 289,
			Width = 150,
			Height = 20,
			ForeColor = Color.FromArgb(148, 163, 184),
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(rodStatusLabel);
		macroStatusLabel = new Label
		{
			Text = "Status: Idle",
			Left = 0,
			Top = 313,
			Width = 170,
			Height = 20,
			ForeColor = Color.FromArgb(148, 163, 184),
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(macroStatusLabel);
		fishSkipSoundLabel = new Label
		{
			Text = "Sounds: none",
			Left = 0,
			Top = 337,
			Width = 190,
			Height = 34,
			ForeColor = Color.FromArgb(148, 163, 184),
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(fishSkipSoundLabel);
		base.Controls.Add(panel);
		var savedNotifySettings = DiscordRoleGate.LoadWebhookSettings();
		notifySettingsLoading = true;
		notifyEnabledToggle.Checked = savedNotifySettings.Enabled;
		notifyWebhookInput.Text = savedNotifySettings.WebhookUrl;
		notifySettingsLoading = false;
		SetNotifySectionExpanded(false);
		timer = new Timer
		{
			Interval = Math.Max(1, controlSettings.UpdateRateMs)
		};
		timer.Tick += delegate
		{
			RunTick();
		};
		topMostTimer = new Timer
		{
			Interval = 1000
		};
		topMostTimer.Tick += delegate
		{
			ForceTopMost();
		};
		topMostTimer.Start();
		timer.Start();
		base.Load += delegate
		{
			ForceTopMost();
		};
		base.Shown += delegate
		{
			if (!running && (autoAquariumToggle.Checked || autoSovToggle.Checked || autoAuroraToggle.Checked || autoWindyDayToggle.Checked))
			{
				DebugLog.Write("ReelControlForm.Shown", "auto-start overlay");
				StartOverlay();
			}
		};
		base.Activated += delegate
		{
			ForceTopMost();
		};
		base.FormClosed += delegate
		{
			SaveNotifySettings();
			UnregisterHotKey(base.Handle, 21041);
			topMostTimer.Stop();
			timer.Stop();
			controller.Reset();
			aquariumRunner.Reset();
			tranquilityController.Reset();
			fishSkipDetector.Dispose();
			locator.Dispose();
		};
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		RegisterHotKey(base.Handle, 21041, 0u, 114u);
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 786 && m.WParam.ToInt32() == 21041)
		{
			ToggleRunning();
		}
		else
		{
			base.WndProc(ref m);
		}
	}

	private void ToggleRunning()
	{
		if (running)
		{
			StopOverlay();
		}
		else
		{
			StartOverlay();
		}
	}

	private void SetNotifySectionExpanded(bool expanded)
	{
		notifySectionExpanded = expanded;
		notifyPanel.Visible = expanded;
		notifyHeaderButton.Text = expanded ? "Notify if stopped v" : "Notify if stopped >";

		var offset = expanded ? 121 : 28;
		offsetsStatusLabel.Top = 261 + offset;
		rodStatusLabel.Top = 289 + offset;
		macroStatusLabel.Top = 313 + offset;
		fishSkipSoundLabel.Top = 337 + offset;

		base.Height = expanded ? 520 : 415;
		MinimumSize = new Size(292, expanded ? 520 : 415);
	}

	private void SaveNotifySettings()
	{
		if (notifySettingsLoading)
		{
			return;
		}

		DiscordRoleGate.SaveWebhookSettings(notifyEnabledToggle.Checked, notifyWebhookInput.Text.Trim());
	}

	private void UpdateStoppedNotification(bool trackingNow)
	{
		if (!running)
		{
			UpdateNotifyCountdownLabel(0);
			return;
		}

		if (trackingNow)
		{
			notifyTrackingDeadlineAt = 0;
			notifyNextSendAt = 0;
			notifySendInFlight = false;
			UpdateNotifyCountdownLabel(NotifyIfStoppedDelayMs);
			return;
		}

		if (!notifyEnabledToggle.Checked)
		{
			UpdateNotifyCountdownLabel(NotifyIfStoppedDelayMs);
			return;
		}

		if (string.IsNullOrWhiteSpace(notifyWebhookInput.Text))
		{
			UpdateNotifyCountdownLabel(NotifyIfStoppedDelayMs);
			return;
		}

		var now = Environment.TickCount64;
		if (notifyTrackingDeadlineAt == 0)
		{
			notifyTrackingDeadlineAt = now + NotifyIfStoppedDelayMs;
			notifyNextSendAt = notifyTrackingDeadlineAt;
		}

		var remainingMs = notifyTrackingDeadlineAt - now;
		UpdateNotifyCountdownLabel(remainingMs);
		if (remainingMs > 0 || notifySendInFlight)
		{
			return;
		}

		notifyNextSendAt = now;
		var webhookUrl = notifyWebhookInput.Text.Trim();
		notifySendInFlight = true;
		_ = SendStoppedNotificationAsync(webhookUrl);
	}

	private void UpdateNotifyCountdownLabel(long remainingMs)
	{
		var clamped = Math.Max(0L, remainingMs);
		var totalSeconds = (int)Math.Ceiling(clamped / 1000.0);
		var minutes = totalSeconds / 60;
		var seconds = totalSeconds % 60;
		notifyCountdownLabel.Text = $"{minutes:00}:{seconds:00}";
		notifyCountdownLabel.ForeColor = clamped == 0 ? Color.FromArgb(248, 113, 113) : Color.FromArgb(251, 191, 36);
	}

	private async Task SendStoppedNotificationAsync(string webhookUrl)
	{
		try
		{
			await DiscordWebhookNotifier.SendStoppedAsync(webhookUrl, "Prolly Disconnected meowmeowbark", default).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			DebugLog.Write("ReelControlForm.Notify", $"send failed: {ex.Message}");
		}
		finally
		{
			notifySendInFlight = false;
		}
	}

	private async Task SendWebhookTestAsync()
	{
		var webhookUrl = notifyWebhookInput.Text.Trim();
		notifyTestButton.Enabled = false;
		try
		{
			await DiscordWebhookNotifier.SendTestAsync(webhookUrl, default).ConfigureAwait(true);
			MessageBox.Show(this, "Test webhook sent.", "Notify if stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, "Webhook Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		finally
		{
			notifyTestButton.Enabled = true;
		}
	}

	private void StartOverlay()
	{
		running = true;
		DebugLog.Clear();
		versionMismatchShown = false;
		offsetsWarningShown = false;
		nextProbeTime = 0.0;
		nextAquariumCycleTime = 0.0;
		aquariumDue = false;
		nextSovCycleTime = 0.0;
		sovDue = autoSovToggle.Checked;
		wasInMinigame = false;
		notifyTrackingDeadlineAt = Environment.TickCount64 + NotifyIfStoppedDelayMs;
		notifyNextSendAt = notifyTrackingDeadlineAt;
		notifySendInFlight = false;
		aquariumPending = autoAquariumToggle.Checked;
		sovPending = autoSovToggle.Checked;
		aquariumRunner.Reset();
		sovRunner.Reset();
		tranquilityController.Reset();
		autoAuroraBlockedUntilCatchEnd = false;
		autoAuroraNightCovered = false;
		autoAuroraNeedsRodReequip = false;
		autoAuroraState = "IDLE";
		autoAuroraRetryCount = 0;
		autoAuroraWaitStartedAt = 0;
		autoWindyDayBlockedUntilCatchEnd = false;
		autoWindyDayNightCovered = false;
		autoWindyDayNeedsRodReequip = false;
		autoWindyDayState = "IDLE";
		autoWindyDayRetryCount = 0;
		autoWindyDayWaitStartedAt = 0;
		startupAssistController.Reset();
		startupAssistActive = false;
		startupAssistStartedAt = 0;
		startupAssistStartFishCenter = 0.0;
		fishingInputReadyAt = 0;
		perfectCastReadyAt = 0;
		nextFishSkipPollAt = 0;
		fishSkipEvadeActive = false;
		fishSkipEvadeLegendary = false;
		fishSkipRequiemNextClickAt = 0;
		fishSkipRequiemClickDown = false;
		fishSkipSoundState = FishSkipSoundSnapshot.Empty;
		UpdateFishSkipStatus();
		lastShakedAt = 0;
		shakeInputActive = false;
		hadMetricsLastTick = false;
		locator.ResetRodState();
		fishingGate.Reset();
		fishSkipDetector.Reset();
		currentRodProfile.Reset();
		ResetBellonaRightControlState(releaseInput: true);
		DebugLog.Write("ReelControlForm.StartOverlay", $"autoAquarium={autoAquariumToggle.Checked} rodSlot={HotbarSlotSettings.RodSlot} profile={currentRodProfile.Kind}");
		toggleButton.Text = "Stop (F3)";
		toggleButton.BackColor = Color.FromArgb(218, 54, 51);
		UpdateOffsetsStatus();
		RunTick();
	}

	private void StopOverlay()
	{
		running = false;
		controller.Reset();
		MouseInput.LeftUp();
		ResetBellonaRightControlState(releaseInput: true);
		bellonaActiveThisTick = false;
		ForceBellonaRightPhysicalUp();
		fishingGate.Reset();
		aquariumRunner.Reset();
		sovRunner.Reset();
		locator.ResetRodState();
		currentRodProfile.Reset();
		startupAssistController.Reset();
		startupAssistActive = false;
		startupAssistStartedAt = 0;
		startupAssistStartFishCenter = 0.0;
		fishingInputReadyAt = 0;
		perfectCastReadyAt = 0;
		nextFishSkipPollAt = 0;
		fishSkipEvadeActive = false;
		fishSkipEvadeLegendary = false;
		fishSkipRequiemNextClickAt = 0;
		fishSkipRequiemClickDown = false;
		fishSkipSoundState = FishSkipSoundSnapshot.Empty;
		UpdateFishSkipStatus();
		lastShakedAt = 0;
		shakeInputActive = false;
		hadMetricsLastTick = false;
		notifyTrackingDeadlineAt = 0;
		notifyNextSendAt = 0;
		notifySendInFlight = false;
		locator.ResetTargets();
		aquariumDue = false;
		nextAquariumCycleTime = 0.0;
		sovDue = autoSovToggle.Checked;
		sovPending = autoSovToggle.Checked;
		nextSovCycleTime = 0.0;
		autoAuroraBlockedUntilCatchEnd = false;
		autoAuroraNightCovered = false;
		autoAuroraNeedsRodReequip = false;
		autoAuroraState = "IDLE";
		autoAuroraRetryCount = 0;
		autoAuroraWaitStartedAt = 0;
		autoWindyDayBlockedUntilCatchEnd = false;
		autoWindyDayNightCovered = false;
		autoWindyDayNeedsRodReequip = false;
		autoWindyDayState = "IDLE";
		autoWindyDayRetryCount = 0;
		autoWindyDayWaitStartedAt = 0;
		fishSkipDetector.Reset();
		toggleButton.Text = "Start (F3)";
		toggleButton.BackColor = Color.FromArgb(47, 129, 247);
		DebugLog.Write("ReelControlForm.StopOverlay", "stopped");
	}

	private void RunTick()
	{
		UpdateOffsetsStatus();
		UpdateRodStatus();
		UpdateMacroStatus();
		UpdateFishSkipStatus();
		if (!running)
		{
			return;
		}

		double totalSeconds = stopwatch.Elapsed.TotalSeconds;
		DebugLog.Write("ReelControlForm.RunTick", $"tick time={totalSeconds:0.000} aquariumPending={aquariumPending} aquariumDue={aquariumDue} sovPending={sovPending} sovDue={sovDue} wasInMinigame={wasInMinigame} nextProbe={nextProbeTime:0.000}");
		try
		{
			var rodKind = currentRodKind;
			var rodDetected = false;
			if (locator.TryGetRodKind(out var detectedRodKind))
			{
				rodDetected = true;
				rodKind = ResolveMasterlineOverlayKind(detectedRodKind);
				UpdateRodProfile(rodKind);
			}
			BellonaRmbWatchdog();
			DebugLog.Write("ReelControlForm.RunTick", $"rodDetected={rodDetected} activeRod={rodKind} profile={currentRodProfile.Kind}");

			if (rodKind == RodKind.Tranquility && locator.IsTranquilityActive())
			{
				controller.Reset();
				tranquilityController.Update();
				wasInMinigame = true;
				DebugLog.Write("ReelControlForm.RunTick", "branch=tranquility");
				return;
			}

			if (rodKind == RodKind.SplitbranchTwig)
			{
				locator.TryHandleSplitbranchTwigCrateSelection();
				DebugLog.Write("ReelControlForm.RunTick", "branch=splitbranch");
			}

				if (rodKind == RodKind.MiguRod)
				{
					locator.TryHandleMiguCounterAttackShift();
					DebugLog.Write("ReelControlForm.RunTick", "branch=migu");
				}

				if (autoAuroraToggle.Checked && UpdateAutoAurora(totalSeconds))
				{
					return;
				}

				if (autoWindyDayToggle.Checked && UpdateAutoWindyDay(totalSeconds))
				{
					return;
				}

				if (autoAquariumToggle.Checked && aquariumPending)
				{
				try
				{
					AquariumSequenceResult aquariumSequenceResult = aquariumRunner.Step(0.25);
					if (aquariumSequenceResult.Completed)
					{
						aquariumPending = false;
						aquariumDue = false;
						nextAquariumCycleTime = totalSeconds + AutoAquariumCycleDelaySeconds;
						wasInMinigame = false;
						locator.ResetTargets();
						nextProbeTime = totalSeconds + TrackingProbeDelaySeconds;
						DebugLog.Write("ReelControlForm.RunTick", "branch=aquarium completed");
					}
					return;
				}
				catch
				{
					aquariumRunner.Reset();
					aquariumPending = false;
					aquariumDue = false;
					nextAquariumCycleTime = totalSeconds + AutoAquariumCycleDelaySeconds;
					locator.ResetTargets();
					nextProbeTime = totalSeconds + TrackingProbeDelaySeconds;
					DebugLog.Write("ReelControlForm.RunTick", "branch=aquarium failed");
				}
			}
			if (autoSovToggle.Checked && sovPending && !locator.HasTargets)
			{
				try
				{
					AutoSovereignRechargeResult sovSequenceResult = sovRunner.Step(91.0, 99.7);
					DebugLog.Write("ReelControlForm.RunTick", $"branch=sov status={sovSequenceResult.Status} power={(sovSequenceResult.CurrentPowerPercent?.ToString("0.0") ?? "null")} completed={sovSequenceResult.Completed} failed={sovSequenceResult.Failed}");
					if (sovSequenceResult.Completed)
					{
						sovPending = false;
						sovDue = false;
						nextSovCycleTime = 0.0;
						wasInMinigame = false;
						locator.ResetTargets();
						nextProbeTime = totalSeconds + TrackingProbeDelaySeconds;
						DebugLog.Write("ReelControlForm.RunTick", "branch=sov completed");
						return;
					}

					if (!sovSequenceResult.Failed && sovSequenceResult.Status.StartsWith("Power healthy", StringComparison.OrdinalIgnoreCase))
					{
						sovPending = false;
						sovDue = false;
						sovRunner.Reset();
						return;
					}
				}
				catch
				{
					sovRunner.Reset();
					sovPending = false;
					sovDue = false;
					nextSovCycleTime = 0.0;
					locator.ResetTargets();
					nextProbeTime = totalSeconds + TrackingProbeDelaySeconds;
					DebugLog.Write("ReelControlForm.RunTick", "branch=sov failed");
				}
			}
			if (autoAquariumToggle.Checked && !aquariumDue && nextAquariumCycleTime > 0.0 && totalSeconds >= nextAquariumCycleTime)
			{
				aquariumDue = true;
			}

			if (locator.IsShakeButtonVisible())
			{
				shakeInputActive = true;
				DebugLog.Write("ReelControlForm.RunTick", "branch=shake send-enter");
				NativeKeyboard.PressEnter();
				lastShakedAt = Environment.TickCount64;
			}
			else
			{
				shakeInputActive = false;
			}

			var bellonaOrderedContexts = rodKind == RodKind.BellonaWaraxe ? locator.GetOrderedReelContexts() : Array.Empty<OrderedReelContext>();
			var bellonaHasAnyContext = bellonaOrderedContexts.Count > 0;
			bellonaActiveThisTick = bellonaHasAnyContext;
			if (!locator.HasTargets && !bellonaHasAnyContext && totalSeconds < nextProbeTime)
			{
				ResetFishSkipEvade("casting-wait");
				DebugLog.Write("ReelControlForm.RunTick", "branch=casting wait");
				if (controlSettings.CastMode.Equals("perfect", StringComparison.OrdinalIgnoreCase) && perfectCastReadyAt != 0 && Environment.TickCount64 < perfectCastReadyAt)
				{
					DebugLog.Write("ReelControlForm.RunTick", $"branch=casting wait post-tracking remaining={perfectCastReadyAt - Environment.TickCount64}ms");
					ApplyFishingHold(false, null);
					return;
				}
				var castingDecision = controller.UpdateCasting(controlSettings.CastMode, locator.IsShakeButtonVisible(), locator.GetPowerBarPercent);
				ApplyFishingHold(castingDecision.Holding, null);
				return;
			}
			ReelMetrics snapshot;
			ReelContext bellonaLeftContext = null;
			ReelContext bellonaRightContext = null;
			if (rodKind == RodKind.BellonaWaraxe)
			{
				(bellonaLeftContext, bellonaRightContext) = ResolveBellonaSideContexts(bellonaOrderedContexts);
				var leftMetrics = bellonaLeftContext is null ? null : locator.ReadMetrics(bellonaLeftContext);
				var rightMetrics = bellonaRightContext is null ? null : locator.ReadMetrics(bellonaRightContext);
				if (leftMetrics is null && rightMetrics is null)
				{
					throw new NoMinigameException("Bellona reel contexts exist, but metrics are not readable yet.");
				}

				if (leftMetrics is null)
				{
					UpdateBellonaRightHold();
					wasInMinigame = true;
					hadMetricsLastTick = true;
					fishingInputReadyAt = 0;
					DebugLog.Write("ReelControlForm.RunTick", "bellona right-only tracking");
					return;
				}

				snapshot = leftMetrics;
			}
			else
			{
				ResetBellonaRightControlState(releaseInput: true);
				snapshot = locator.ReadSnapshot();
			}
			locator.TryGetFishingCompletionPercent(out var progress);
			if (autoSovToggle.Checked && locator.TryGetSovereignPowerPercent(out var powerPercent) && powerPercent.HasValue && powerPercent.Value <= AutoSovThresholdPercent && !sovPending && !sovDue)
			{
				sovDue = true;
				DebugLog.Write("ReelControlForm.AutoSov", $"due power={powerPercent:0.0} threshold={AutoSovThresholdPercent:0.0}");
			}
			var noteTarget = rodKind == RodKind.Pinion ? locator.GetActiveNoteTarget() : null;
			var adjustedSnapshot = rodKind == RodKind.Pinion ? currentRodProfile.AdjustTarget(snapshot, noteTarget) : snapshot;
			DebugLog.Write("ReelControlForm.RunTick", $"snapshot fish={snapshot.FishCenter:0.000000} bar={snapshot.PlayerbarCenter:0.000000} width={snapshot.PlayerbarWidth:0.000000} note={(noteTarget is null ? "null" : $"{noteTarget.Value.Sx:0.000000},{noteTarget.Value.Sy:0.000000}")} progress={(progress?.ToString("0.0") ?? "null")}");
			if (fishingInputReadyAt != 0 && Environment.TickCount64 < fishingInputReadyAt)
			{
				if (rodKind == RodKind.Dreambreaker)
				{
					fishingInputReadyAt = 0;
				}
				else
				{
				controller.Reset();
				UpdateBellonaRightHold();
				startupAssistController.Reset();
				DebugLog.Write("ReelControlForm.RunTick", $"input settle remaining={fishingInputReadyAt - Environment.TickCount64}ms");
				return;
				}
			}

			if (controlSettings.CastMode.Equals("perfect", StringComparison.OrdinalIgnoreCase) && perfectCastReadyAt != 0 && Environment.TickCount64 < perfectCastReadyAt)
			{
				DebugLog.Write("ReelControlForm.RunTick", $"branch=tracking post-tracking remaining={perfectCastReadyAt - Environment.TickCount64}ms");
				controller.Reset();
				UpdateBellonaRightHold();
				ApplyFishingHold(false, progress);
				return;
			}

			if (rodKind == RodKind.Dreambreaker && startupAssistStartedAt != 0 && Environment.TickCount64 - startupAssistStartedAt < 8000)
			{
				var dreambreakerDecision = controller.UpdateTracking(adjustedSnapshot, controlSettings, currentRodProfile.FishingActionDelayMs);
				dreambreakerDecision = ApplyFishSkipDecision(dreambreakerDecision, progress, "dreambreaker");
				ApplyFishingHold(dreambreakerDecision.Holding, progress);
				DebugLog.Write("ReelControlForm.RunTick", $"dreambreaker default-profile tracking remaining={8000 - (Environment.TickCount64 - startupAssistStartedAt)}ms holding={dreambreakerDecision.Holding}");
				return;
			}

			if (startupAssistActive)
			{
				var fishMoved = Math.Abs(snapshot.FishCenter - startupAssistStartFishCenter) >= 0.0125;
				var assistTimedOut = Environment.TickCount64 - startupAssistStartedAt >= 800;
				if (!fishMoved && !assistTimedOut)
				{
					var assistDecision = startupAssistController.Update(adjustedSnapshot, startupAssistSettings);
					assistDecision = ApplyFishSkipDecision(assistDecision, progress, "startupAssist");
					if (ShouldApplyDefaultRodStartupHoldBias(rodKind))
					{
						assistDecision = new ReelControlState(assistDecision.Error, assistDecision.Control, true);
					}
					ApplyFishingHold(assistDecision.Holding, progress);
					UpdateBellonaRightHold();
					DebugLog.Write("ReelControlForm.RunTick", $"startupAssist active fishMoved={fishMoved} timedOut={assistTimedOut} holding={assistDecision.Holding}");
					return;
				}

				startupAssistActive = false;
				startupAssistController.Reset();
			}

			EnterTrackingFromSnapshot(adjustedSnapshot, rodKind, progress, "normal", allowStartupAssist: true);
			UpdateBellonaRightHold();
		}
		catch (RobloxVersionMismatchException ex)
		{
			HandleVersionMismatch(ex);
		}
		catch (NoMinigameException)
		{
			ResetFishSkipEvade("no-minigame");
			var wasTracking = wasInMinigame;
			locator.ResetTargets();
			hadMetricsLastTick = false;
			fishingInputReadyAt = 0;
			if (wasTracking || controlSettings.CastMode.Equals("perfect", StringComparison.OrdinalIgnoreCase))
			{
				controller.Reset();
			}
			fishSkipSoundState = FishSkipSoundSnapshot.Empty;
			nextFishSkipPollAt = 0;
		fishSkipEvadeActive = false;
		fishSkipEvadeLegendary = false;
		fishSkipRequiemNextClickAt = 0;
		fishSkipRequiemClickDown = false;
		perfectCastReadyAt = controlSettings.CastMode.Equals("perfect", StringComparison.OrdinalIgnoreCase) ? Environment.TickCount64 + 3000 : 0;
			DebugLog.Write("ReelControlForm.RunTick", $"branch=no-minigame wasTracking={wasTracking} perfectCastReadyAt={(perfectCastReadyAt == 0 ? "0" : perfectCastReadyAt.ToString())}");
			nextProbeTime = totalSeconds + TrackingProbeDelaySeconds;
				if (autoAuroraBlockedUntilCatchEnd && !locator.HasTargets)
				{
				autoAuroraBlockedUntilCatchEnd = false;
			}
				if (autoAquariumToggle.Checked && aquariumDue && wasInMinigame)
				{
					wasInMinigame = false;
					aquariumPending = true;
					aquariumDue = false;
					nextAquariumCycleTime = 0.0;
					aquariumRunner.Reset();
					controller.Reset();
					locator.ResetTargets();
					nextProbeTime = 0.0;
					return;
				}
				if (autoSovToggle.Checked && sovDue && wasInMinigame)
				{
					wasInMinigame = false;
					sovPending = true;
					sovDue = false;
					nextSovCycleTime = 0.0;
					sovRunner.Reset();
					controller.Reset();
					locator.ResetTargets();
					nextProbeTime = 0.0;
					return;
				}
				if (autoSovToggle.Checked && (sovPending || sovDue))
				{
					controller.Reset();
					ResetBellonaRightControlState(releaseInput: true);
					ApplyFishingHold(false, null);
					return;
				}
				wasInMinigame = false;
				hadMetricsLastTick = false;
				fishingInputReadyAt = 0;
					var castDecision = controller.UpdateCasting(controlSettings.CastMode, locator.IsShakeButtonVisible(), locator.GetPowerBarPercent);
			ApplyFishingHold(castDecision.Holding, null);
		}
		catch (Exception)
		{
			controller.Reset();
			ResetBellonaRightControlState(releaseInput: true);
			locator.ResetTargets();
			nextProbeTime = totalSeconds + 1.0;
			hadMetricsLastTick = false;
			fishingInputReadyAt = 0;
		}
	}

	private bool UpdateAutoAurora(double totalSeconds)
	{
		if (!autoAuroraToggle.Checked)
		{
			ResetAutoAuroraControl();
			return false;
		}

		if (autoAuroraState != "IDLE")
		{
			UpdateAutoAuroraState(totalSeconds);
			return true;
		}

		if (autoAuroraBlockedUntilCatchEnd)
		{
			return false;
		}

		if (locator.IsAuroraActive())
		{
			CompleteAutoAuroraWorkflow(true);
			return false;
		}

		if (autoAuroraNightCovered)
		{
			if (locator.IsNightCycle())
			{
				return false;
			}

			autoAuroraNightCovered = false;
		}

		BeginAutoAuroraWorkflow(totalSeconds);
		return true;
	}

	private bool UpdateAutoWindyDay(double totalSeconds)
	{
		DebugLog.Write("ReelControlForm.AutoWindyDay", $"tick time={totalSeconds:0.000} checked={autoWindyDayToggle.Checked} state={autoWindyDayState} night={locator.IsNightCycle()} windy={locator.IsWindyActive()} blocked={autoWindyDayBlockedUntilCatchEnd} covered={autoWindyDayNightCovered} hasTargets={locator.HasTargets} inputReady={(fishingInputReadyAt != 0)} wasInMinigame={wasInMinigame}");

		if (!autoWindyDayToggle.Checked)
		{
			ResetAutoWindyDayControl();
			DebugLog.Write("ReelControlForm.AutoWindyDay", "disabled -> reset");
			return false;
		}

		if (autoWindyDayState != "IDLE")
		{
			DebugLog.Write("ReelControlForm.AutoWindyDay", $"state-advance from={autoWindyDayState}");
			UpdateAutoWindyDayState(totalSeconds);
			return true;
		}

		if (autoWindyDayBlockedUntilCatchEnd)
		{
			DebugLog.Write("ReelControlForm.AutoWindyDay", "blocked-until-catch-end");
			return false;
		}

		if (locator.IsWindyActive() && !locator.IsNightCycle())
		{
			DebugLog.Write("ReelControlForm.AutoWindyDay", "windy-already-active -> complete");
			CompleteAutoWindyDayWorkflow(true);
			return false;
		}

		DebugLog.Write("ReelControlForm.AutoWindyDay", "begin workflow");
		BeginAutoWindyDayWorkflow(totalSeconds);
		return true;
	}

	private void BeginAutoAuroraWorkflow(double totalSeconds)
	{
		autoAuroraRetryCount = 0;
		autoAuroraWaitStartedAt = 0;
		autoAuroraNeedsRodReequip = false;
		autoAuroraState = "WORKFLOW";
		DebugLog.Write("ReelControlForm.AutoAurora", $"begin time={totalSeconds:0.000} night={locator.IsNightCycle()} active={locator.IsAuroraActive()}");
		RunAutoAuroraWorkflowStep(totalSeconds);
	}

	private void RunAutoAuroraWorkflowStep(double totalSeconds)
	{
		if (locator.IsAuroraActive())
		{
			CompleteAutoAuroraWorkflow(true);
			return;
		}

		if (locator.IsNightCycle())
		{
			if (!locator.TryUseHotbarItem("Aurora Totem"))
			{
				CompleteAutoAuroraWorkflow(false);
				return;
			}

			autoAuroraNeedsRodReequip = true;
			autoAuroraState = "WAIT_AURORA";
			autoAuroraWaitStartedAt = Environment.TickCount64;
			DebugLog.Write("ReelControlForm.AutoAurora", $"step=aurora wait={AutoAuroraWaitMs}ms time={totalSeconds:0.000}");
			return;
		}

		if (!locator.TryUseHotbarItem("Sundial Totem"))
		{
			CompleteAutoAuroraWorkflow(false);
			return;
		}

		autoAuroraNeedsRodReequip = true;
		autoAuroraState = "WAIT_NIGHT";
		autoAuroraWaitStartedAt = Environment.TickCount64;
		DebugLog.Write("ReelControlForm.AutoAurora", $"step=sundial wait={AutoAuroraWaitMs}ms time={totalSeconds:0.000}");
	}

	private void BeginAutoWindyDayWorkflow(double totalSeconds)
	{
		autoWindyDayRetryCount = 0;
		autoWindyDayWaitStartedAt = 0;
		autoWindyDayNeedsRodReequip = false;
		autoWindyDayState = "WORKFLOW";
		DebugLog.Write("ReelControlForm.AutoWindyDay", $"begin time={totalSeconds:0.000} night={locator.IsNightCycle()} windy={locator.IsWindyActive()} hasTargets={locator.HasTargets} inputReady={(fishingInputReadyAt != 0)} wasInMinigame={wasInMinigame}");
		RunAutoWindyDayWorkflowStep(totalSeconds);
	}

	private void RunAutoWindyDayWorkflowStep(double totalSeconds)
	{
		DebugLog.Write("ReelControlForm.AutoWindyDay", $"workflow-step time={totalSeconds:0.000} state={autoWindyDayState} night={locator.IsNightCycle()} windy={locator.IsWindyActive()} retry={autoWindyDayRetryCount} reEquip={autoWindyDayNeedsRodReequip}");

		if (locator.IsNightCycle())
		{
			DebugLog.Write("ReelControlForm.AutoWindyDay", "workflow-step night -> sundial");
			if (!locator.TryUseHotbarItem("Sundial Totem"))
			{
				DebugLog.Write("ReelControlForm.AutoWindyDay", "workflow-step sundial failed");
				CompleteAutoWindyDayWorkflow(false);
				return;
			}

			autoWindyDayNeedsRodReequip = true;
			autoWindyDayState = "WAIT_DAY";
			autoWindyDayWaitStartedAt = Environment.TickCount64;
			DebugLog.Write("ReelControlForm.AutoWindyDay", $"step=sundial wait={AutoAuroraWaitMs}ms time={totalSeconds:0.000}");
			return;
		}

		if (locator.IsWindyActive())
		{
			DebugLog.Write("ReelControlForm.AutoWindyDay", "workflow-step windy-active -> complete");
			CompleteAutoWindyDayWorkflow(true);
			return;
		}

		DebugLog.Write("ReelControlForm.AutoWindyDay", "workflow-step day -> windset");
		if (!TryUseWindyTotem())
		{
			DebugLog.Write("ReelControlForm.AutoWindyDay", "workflow-step windset failed");
			CompleteAutoWindyDayWorkflow(false);
			return;
		}

		autoWindyDayNeedsRodReequip = true;
		autoWindyDayState = "WAIT_WINDY";
		autoWindyDayWaitStartedAt = Environment.TickCount64;
		DebugLog.Write("ReelControlForm.AutoWindyDay", $"step=windset wait={AutoAuroraWaitMs}ms time={totalSeconds:0.000}");
	}

	private void UpdateAutoWindyDayState(double totalSeconds)
	{
		DebugLog.Write("ReelControlForm.AutoWindyDay", $"state-tick time={totalSeconds:0.000} state={autoWindyDayState} night={locator.IsNightCycle()} windy={locator.IsWindyActive()} retry={autoWindyDayRetryCount} waitFor={(Environment.TickCount64 - autoWindyDayWaitStartedAt)}ms");

		if (autoWindyDayState == "WAIT_REEQUIP")
		{
			if (Environment.TickCount64 - autoWindyDayWaitStartedAt < AutoTotemActionDelayMs)
			{
				DebugLog.Write("ReelControlForm.AutoWindyDay", "state=WAIT_REEQUIP waiting");
				return;
			}

			DebugLog.Write("ReelControlForm.AutoWindyDay", "state=WAIT_REEQUIP -> complete");
			CompleteAutoWindyDayWorkflow(locator.IsWindyActive());
			return;
		}

		if (locator.IsWindyActive() && !locator.IsNightCycle())
		{
			if (autoWindyDayNeedsRodReequip)
			{
				autoWindyDayState = "WAIT_REEQUIP";
				autoWindyDayWaitStartedAt = Environment.TickCount64;
				DebugLog.Write("ReelControlForm.AutoWindyDay", $"step=finish wait={AutoTotemActionDelayMs}ms time={totalSeconds:0.000}");
				return;
			}

			DebugLog.Write("ReelControlForm.AutoWindyDay", "state tick windy-active -> complete");
			CompleteAutoWindyDayWorkflow(true);
			return;
		}

		switch (autoWindyDayState)
		{
			case "WAIT_DAY":
				DebugLog.Write("ReelControlForm.AutoWindyDay", "state=WAIT_DAY");
				if (!locator.IsNightCycle())
				{
					autoWindyDayRetryCount = 0;
					if (locator.IsWindyActive())
					{
						DebugLog.Write("ReelControlForm.AutoWindyDay", "day detected and already windy -> complete");
						CompleteAutoWindyDayWorkflow(true);
						return;
					}

					if (!TryUseWindyTotem())
					{
						DebugLog.Write("ReelControlForm.AutoWindyDay", "day detected but windset failed");
						CompleteAutoWindyDayWorkflow(false);
						return;
					}

					autoWindyDayNeedsRodReequip = true;
					autoWindyDayState = "WAIT_WINDY";
					autoWindyDayWaitStartedAt = Environment.TickCount64;
					DebugLog.Write("ReelControlForm.AutoWindyDay", $"retry=windset time={totalSeconds:0.000}");
					return;
				}

				if (Environment.TickCount64 - autoWindyDayWaitStartedAt < AutoAuroraWaitMs)
				{
					DebugLog.Write("ReelControlForm.AutoWindyDay", "waiting for day after sundial");
					return;
				}

				if (autoWindyDayRetryCount >= 1)
				{
					DebugLog.Write("ReelControlForm.AutoWindyDay", "sundial retry limit reached");
					CompleteAutoWindyDayWorkflow(false);
					return;
				}

				if (!locator.TryUseHotbarItem("Sundial Totem"))
				{
					DebugLog.Write("ReelControlForm.AutoWindyDay", "sundial retry failed");
					CompleteAutoWindyDayWorkflow(false);
					return;
				}

				autoWindyDayRetryCount += 1;
				autoWindyDayWaitStartedAt = Environment.TickCount64;
				DebugLog.Write("ReelControlForm.AutoWindyDay", $"retry=sundial count={autoWindyDayRetryCount}");
				return;

			case "WAIT_WINDY":
				DebugLog.Write("ReelControlForm.AutoWindyDay", "state=WAIT_WINDY");
				if (Environment.TickCount64 - autoWindyDayWaitStartedAt < AutoAuroraWaitMs)
				{
					DebugLog.Write("ReelControlForm.AutoWindyDay", "waiting for windy after windset");
					return;
				}

				if (autoWindyDayRetryCount >= 1)
				{
					DebugLog.Write("ReelControlForm.AutoWindyDay", "windset retry limit reached");
					CompleteAutoWindyDayWorkflow(false);
					return;
				}

				if (!TryUseWindyTotem())
				{
					DebugLog.Write("ReelControlForm.AutoWindyDay", "windset retry failed");
					CompleteAutoWindyDayWorkflow(false);
					return;
				}

				autoWindyDayRetryCount += 1;
				autoWindyDayWaitStartedAt = Environment.TickCount64;
				DebugLog.Write("ReelControlForm.AutoWindyDay", $"retry=windset count={autoWindyDayRetryCount}");
				return;

			case "WAIT_REEQUIP":
				if (Environment.TickCount64 - autoWindyDayWaitStartedAt < AutoTotemActionDelayMs)
				{
					DebugLog.Write("ReelControlForm.AutoWindyDay", "waiting for rod re-equip");
					return;
				}

				CompleteAutoWindyDayWorkflow(locator.IsWindyActive());
				return;
		}
	}

	private void CompleteAutoWindyDayWorkflow(bool success)
	{
		var needsRodReequip = autoWindyDayNeedsRodReequip;
		autoWindyDayState = "IDLE";
		autoWindyDayWaitStartedAt = 0;
		autoWindyDayRetryCount = 0;
		autoWindyDayNeedsRodReequip = false;

		autoWindyDayBlockedUntilCatchEnd = false;
		autoWindyDayNightCovered = false;

		if (needsRodReequip)
		{
			locator.TryEnsureRodEquipped();
		}

		DebugLog.Write("ReelControlForm.AutoWindyDay", $"complete success={success} blocked={autoWindyDayBlockedUntilCatchEnd} nightCovered={autoWindyDayNightCovered}");
	}

	private void ResetAutoWindyDayControl()
	{
		autoWindyDayState = "IDLE";
		autoWindyDayWaitStartedAt = 0;
		autoWindyDayRetryCount = 0;
		autoWindyDayBlockedUntilCatchEnd = false;
		autoWindyDayNightCovered = false;
		autoWindyDayNeedsRodReequip = false;
		DebugLog.Write("ReelControlForm.AutoWindyDay", "reset");
	}

	private bool TryUseWindyTotem()
	{
		return locator.TryUseHotbarItem("Windset Totem") || locator.TryUseHotbarItem("Windy Totem");
	}

	private void UpdateAutoAuroraState(double totalSeconds)
	{
		if (autoAuroraState == "WAIT_REEQUIP")
		{
			if (Environment.TickCount64 - autoAuroraWaitStartedAt < AutoTotemActionDelayMs)
			{
				return;
			}

			CompleteAutoAuroraWorkflow(locator.IsAuroraActive());
			return;
		}

		if (locator.IsAuroraActive())
		{
			if (autoAuroraNeedsRodReequip)
			{
				autoAuroraState = "WAIT_REEQUIP";
				autoAuroraWaitStartedAt = Environment.TickCount64;
				DebugLog.Write("ReelControlForm.AutoAurora", $"step=finish wait={AutoTotemActionDelayMs}ms time={totalSeconds:0.000}");
				return;
			}

			CompleteAutoAuroraWorkflow(true);
			return;
		}

		switch (autoAuroraState)
		{
			case "WAIT_NIGHT":
				if (locator.IsNightCycle())
				{
					autoAuroraRetryCount = 0;
					if (!locator.TryUseHotbarItem("Aurora Totem"))
					{
						CompleteAutoAuroraWorkflow(false);
						return;
					}

					autoAuroraNeedsRodReequip = true;
					autoAuroraState = "WAIT_AURORA";
					autoAuroraWaitStartedAt = Environment.TickCount64;
					DebugLog.Write("ReelControlForm.AutoAurora", $"retry=aurora time={totalSeconds:0.000}");
					return;
				}

				if (Environment.TickCount64 - autoAuroraWaitStartedAt < AutoAuroraWaitMs)
				{
					return;
				}

				if (autoAuroraRetryCount >= 1)
				{
					CompleteAutoAuroraWorkflow(false);
					return;
				}

				if (!locator.TryUseHotbarItem("Sundial Totem"))
				{
					CompleteAutoAuroraWorkflow(false);
					return;
				}

				autoAuroraRetryCount += 1;
				autoAuroraWaitStartedAt = Environment.TickCount64;
				DebugLog.Write("ReelControlForm.AutoAurora", $"retry=sundial count={autoAuroraRetryCount}");
				return;

			case "WAIT_AURORA":
				if (Environment.TickCount64 - autoAuroraWaitStartedAt < AutoAuroraWaitMs)
				{
					return;
				}

				if (autoAuroraRetryCount >= 1)
				{
					CompleteAutoAuroraWorkflow(false);
					return;
				}

				if (!locator.TryUseHotbarItem("Aurora Totem"))
				{
					CompleteAutoAuroraWorkflow(false);
					return;
				}

				autoAuroraRetryCount += 1;
				autoAuroraWaitStartedAt = Environment.TickCount64;
				DebugLog.Write("ReelControlForm.AutoAurora", $"retry=aurora count={autoAuroraRetryCount}");
				return;

			case "WAIT_REEQUIP":
				if (Environment.TickCount64 - autoAuroraWaitStartedAt < AutoTotemActionDelayMs)
				{
					return;
				}

				CompleteAutoAuroraWorkflow(locator.IsAuroraActive());
				return;
		}
	}

	private void CompleteAutoAuroraWorkflow(bool success)
	{
		var needsRodReequip = autoAuroraNeedsRodReequip;
		autoAuroraState = "IDLE";
		autoAuroraWaitStartedAt = 0;
		autoAuroraRetryCount = 0;
		autoAuroraNeedsRodReequip = false;

		if (success)
		{
			autoAuroraNightCovered = true;
			autoAuroraBlockedUntilCatchEnd = false;
		}
		else
		{
			autoAuroraBlockedUntilCatchEnd = true;
		}

		if (needsRodReequip)
		{
			locator.TryEnsureRodEquipped();
		}

		DebugLog.Write("ReelControlForm.AutoAurora", $"complete success={success} nightCovered={autoAuroraNightCovered} blocked={autoAuroraBlockedUntilCatchEnd}");
	}

	private void ResetAutoAuroraControl()
	{
		autoAuroraState = "IDLE";
		autoAuroraWaitStartedAt = 0;
		autoAuroraRetryCount = 0;
		autoAuroraBlockedUntilCatchEnd = false;
		autoAuroraNightCovered = false;
		autoAuroraNeedsRodReequip = false;
	}

	private void HandleVersionMismatch(RobloxVersionMismatchException ex)
	{
		controller.Reset();
		MouseInput.LeftUp();
		fishingGate.Reset();
		aquariumRunner.Reset();
		locator.ResetTargets();
		StopOverlay();
		offsetsStatusLabel.Text = "Mismatch";
		offsetsStatusLabel.ForeColor = Color.FromArgb(248, 113, 113);
		if (versionMismatchShown)
		{
			return;
		}

		versionMismatchShown = true;
		MessageBox.Show(
			this,
			ex.Message,
			"Roblox Version Mismatch",
			MessageBoxButtons.OK,
			MessageBoxIcon.Error);
	}

	private void UpdateOffsetsStatus()
	{
		try
		{
			if (!OffsetsSourceProvider.Current.IsPopulated)
			{
				SetOffsetsDisconnected();
				return;
			}

			if (locator.TryProbeOffsetsWorking(out var versionMismatch))
			{
				offsetsWarningShown = false;
				offsetsStatusLabel.Text = "Connected";
				offsetsStatusLabel.ForeColor = Color.FromArgb(74, 222, 128);
				return;
			}

			if (versionMismatch)
			{
				offsetsStatusLabel.Text = "Mismatch";
				offsetsStatusLabel.ForeColor = Color.FromArgb(248, 113, 113);
				ShowOffsetsWarningIfNeeded();
				return;
			}

			SetOffsetsDisconnected();
		}
		catch
		{
			SetOffsetsDisconnected();
		}
	}

	private void SetOffsetsDisconnected()
	{
		offsetsStatusLabel.Text = "Disconnected";
		offsetsStatusLabel.ForeColor = Color.FromArgb(248, 113, 113);
		ShowOffsetsWarningIfNeeded();
	}

	private void ShowOffsetsWarningIfNeeded()
	{
		if (offsetsWarningShown)
		{
			return;
		}

		offsetsWarningShown = true;
		MessageBox.Show(
			this,
			"Roblox version mismatch, update your offsets and make sure your on the latest Roblox version.",
			"Roblox Version Mismatch",
			MessageBoxButtons.OK,
			MessageBoxIcon.Error);
	}

	private void UpdateRodStatus()
	{
		if (!locator.TryGetRodEquippedState(out var rodEquipped))
		{
			rodStatusLabel.Text = "Rod: Unknown";
			rodStatusLabel.ForeColor = Color.FromArgb(148, 163, 184);
			return;
		}

		if (rodEquipped)
		{
			if (locator.TryGetRodKind(out var rodKind))
			{
				rodStatusLabel.Text = $"Rod: {rodKind}";
			}
			else
			{
				rodStatusLabel.Text = "Rod: Equipped";
			}
			rodStatusLabel.ForeColor = Color.FromArgb(74, 222, 128);
			return;
		}

		rodStatusLabel.Text = "Rod: Unequipped";
		rodStatusLabel.ForeColor = Color.FromArgb(248, 113, 113);
		if (running && !(autoSovToggle.Checked && (sovPending || sovDue || sovRunner.IsActive)))
		{
			locator.TryEnsureRodEquipped();
		}
	}

	private void UpdateMacroStatus()
	{
		var statusText = "Idle";
		var statusColor = Color.FromArgb(148, 163, 184);

		if (running)
		{
			if ((autoAuroraToggle.Checked && autoAuroraState != "IDLE") ||
				(autoWindyDayToggle.Checked && autoWindyDayState != "IDLE"))
			{
				statusText = "Totem";
				statusColor = Color.FromArgb(251, 191, 36);
			}
			else if (autoSovToggle.Checked && (sovPending || sovDue))
			{
				statusText = "Sov";
				statusColor = Color.FromArgb(96, 165, 250);
			}
			else if (autoAquariumToggle.Checked && (aquariumPending || aquariumDue))
			{
				statusText = "Aquarium";
				statusColor = Color.FromArgb(96, 165, 250);
			}
			else if (shakeInputActive || locator.IsShakeButtonVisible())
			{
				statusText = "Shaking";
				statusColor = Color.FromArgb(244, 114, 182);
			}
			else if (locator.HasTargets || bellonaActiveThisTick)
			{
				statusText = "Tracking";
				statusColor = Color.FromArgb(74, 222, 128);
			}
			else if (fishingInputReadyAt != 0 || hadMetricsLastTick || startupAssistActive || wasInMinigame)
			{
				statusText = "Casting";
				statusColor = Color.FromArgb(250, 204, 21);
			}
		}

		macroStatusLabel.Text = $"Status: {statusText}";
		macroStatusLabel.ForeColor = statusColor;
		UpdateStoppedNotification(statusText == "Tracking");
	}

	private void UpdateFishSkipStatus()
	{
		var summary = fishSkipSoundState.HasAny ? fishSkipSoundState.Summary : "none";
		var evadeMode = GetFishSkipEvadeLabel();
		fishSkipSoundLabel.Text = string.IsNullOrWhiteSpace(evadeMode) ? $"Sounds: {summary}" : $"Sounds: {summary} | Evade: {evadeMode}";
		fishSkipSoundLabel.ForeColor = fishSkipSoundState.HasAny ? Color.FromArgb(96, 165, 250) : Color.FromArgb(148, 163, 184);
	}

	private void ResetFishSkipEvade(string reason)
	{
		if (!fishSkipEvadeActive && !fishSkipEvadeLegendary)
		{
			return;
		}

		fishSkipEvadeActive = false;
		fishSkipEvadeLegendary = false;
		fishSkipRequiemNextClickAt = 0;
		fishSkipRequiemClickDown = false;
	}

	private void EnterTrackingFromSnapshot(ReelMetrics snapshot, RodKind rodKind, double? progress, string traceLabel, bool allowStartupAssist)
	{
		shakeInputActive = false;
		nextProbeTime = 0.0;
		fishingInputReadyAt = 0;
		wasInMinigame = true;
		DebugLog.Write("ReelControlForm.EnterTrackingFromSnapshot", $"trace={traceLabel} rod={rodKind} progress={(progress?.ToString("0.0") ?? "null")} fish={snapshot.FishCenter:0.000000} bar={snapshot.PlayerbarCenter:0.000000} width={snapshot.PlayerbarWidth:0.000000} hadMetricsLast={hadMetricsLastTick} startupAssistActive={startupAssistActive} allowStartupAssist={allowStartupAssist}");
		if (!hadMetricsLastTick && allowStartupAssist)
		{
			startupAssistStartedAt = Environment.TickCount64;
			startupAssistStartFishCenter = snapshot.FishCenter;
			if (rodKind != RodKind.Dreambreaker)
			{
				startupAssistActive = true;
				fishingInputReadyAt = Environment.TickCount64 + 125;
				startupAssistController.Reset();
			}
			else
			{
				startupAssistActive = false;
				fishingInputReadyAt = 0;
				startupAssistController.Reset();
			}
		}
		else if (!allowStartupAssist)
		{
			startupAssistActive = false;
			fishingInputReadyAt = 0;
			startupAssistController.Reset();
		}

		hadMetricsLastTick = true;
		var noteTarget = rodKind == RodKind.Pinion ? locator.GetActiveNoteTarget() : null;
		var adjustedSnapshot = rodKind == RodKind.Pinion ? currentRodProfile.AdjustTarget(snapshot, noteTarget) : snapshot;
		var decision = ApplyFishSkipDecision(controller.UpdateTracking(adjustedSnapshot, controlSettings, currentRodProfile.FishingActionDelayMs), progress, traceLabel);
		if (ShouldApplyDefaultRodStartupHoldBias(rodKind))
		{
			decision = new ReelControlState(decision.Error, decision.Control, true);
		}
		ApplyFishingHold(decision.Holding, progress);
		UpdateBellonaRightHold();
		DebugLog.Write("ReelControlForm.EnterTrackingFromSnapshot", $"trace={traceLabel} note={(noteTarget is null ? "null" : $"{noteTarget.Value.Sx:0.000000},{noteTarget.Value.Sy:0.000000}")} adjustedFish={adjustedSnapshot.FishCenter:0.000000} adjustedBar={adjustedSnapshot.PlayerbarCenter:0.000000} holding={decision.Holding}");
	}

	private void UpdateBellonaRightHold()
	{
		if (currentRodProfile.Kind != RodKind.BellonaWaraxe)
		{
			ResetBellonaRightControlState(releaseInput: true);
			bellonaActiveThisTick = false;
			return;
		}

		var now = Environment.TickCount64;
		var (_, rightContext) = ResolveBellonaSideContexts(locator.GetOrderedReelContexts());
		if (rightContext is null)
		{
			if (bellonaRightLastMetricsSeenAt != 0 && now - bellonaRightLastMetricsSeenAt <= BellonaRightLossGraceMs)
			{
				DebugLog.Write("BellonaDual", $"Right context missing, grace active ({now - bellonaRightLastMetricsSeenAt}ms).");
				return;
			}

			ResetBellonaRightControlState(releaseInput: true);
			if (MouseInput.IsRightButtonDown())
			{
				MouseInput.RightUp();
				DebugLog.Write("BellonaDual", "Forced RMB up (right context missing but button still down).");
			}
			if (bellonaRightLastSecondaryPresent)
			{
				DebugLog.Write("BellonaDual", "Secondary reel missing; releasing RMB.");
			}
			return;
		}

		bellonaRightLastSecondaryPresent = true;
		var secondaryMetrics = locator.ReadMetrics(rightContext);
		if (secondaryMetrics is null)
		{
			if (bellonaRightLastMetricsSeenAt != 0 && now - bellonaRightLastMetricsSeenAt <= BellonaRightLossGraceMs)
			{
				DebugLog.Write("BellonaDual", $"Right metrics missing, grace active ({now - bellonaRightLastMetricsSeenAt}ms).");
				return;
			}

			ResetBellonaRightControlState(releaseInput: true);
			DebugLog.Write("BellonaDual", "Secondary reel metrics unavailable; releasing RMB.");
			return;
		}

		bellonaRightLastMetricsSeenAt = now;
		UpdateBellonaRightMotionWatchdog(secondaryMetrics, now);

		var desiredHolding = bellonaRightController.UpdateTracking(secondaryMetrics, controlSettings, currentRodProfile.FishingActionDelayMs).Holding;
		desiredHolding = ApplyBellonaRightHybridHoldBias(desiredHolding, secondaryMetrics, now);

		if (now < bellonaRightReacquireUntilAt)
		{
			DebugLog.Write("BellonaDual", $"Right reacquire window active ({bellonaRightReacquireUntilAt - now}ms left).");
		}

		if (now - bellonaRightLastLogAt >= 350 || desiredHolding != bellonaRightLastDesiredHolding)
		{
			DebugLog.Write(
				"BellonaDual",
				$"secondary fish={secondaryMetrics.FishCenter:0.000} bar={secondaryMetrics.PlayerbarCenter:0.000} width={secondaryMetrics.PlayerbarWidth:0.000} desiredRmbHold={desiredHolding}");
			bellonaRightLastLogAt = now;
			bellonaRightLastDesiredHolding = desiredHolding;
		}

		ApplyBellonaRightHold(desiredHolding);
	}

	private void ResetBellonaRightControlState(bool releaseInput)
	{
		if (releaseInput)
		{
			ReleaseBellonaRightHold();
		}

		bellonaRightController.Reset();
		bellonaRightLastSecondaryPresent = false;
		bellonaRightRawDesiredHolding = false;
		bellonaRightRawDesiredChangedAt = Environment.TickCount64;
		bellonaRightLastFishCenter = null;
		bellonaRightLastBarCenter = null;
		bellonaRightLastMotionAt = 0;
		bellonaRightStaleSinceAt = 0;
		bellonaRightReacquireUntilAt = 0;
		bellonaRightHybridHoldUntilAt = 0;
	}

	private bool ApplyBellonaRightHybridHoldBias(bool desiredHolding, ReelMetrics metrics, long now)
	{
		if (desiredHolding)
		{
			bellonaRightHybridHoldUntilAt = now + BellonaRightHybridHoldBiasMs;
			return true;
		}

		if (bellonaRightHolding &&
			now < bellonaRightHybridHoldUntilAt &&
			Math.Abs(metrics.FishCenter - metrics.PlayerbarCenter) <= 0.022)
		{
			DebugLog.Write(
				"BellonaDual",
				$"Right hybrid hold-bias active ({bellonaRightHybridHoldUntilAt - now}ms left, err={Math.Abs(metrics.FishCenter - metrics.PlayerbarCenter):0.000}).");
			return true;
		}

		return false;
	}

	private void UpdateBellonaRightMotionWatchdog(ReelMetrics metrics, long now)
	{
		var moved =
			!bellonaRightLastFishCenter.HasValue ||
			!bellonaRightLastBarCenter.HasValue ||
			Math.Abs(metrics.FishCenter - bellonaRightLastFishCenter.Value) >= 0.0015 ||
			Math.Abs(metrics.PlayerbarCenter - bellonaRightLastBarCenter.Value) >= 0.0015;

		if (moved)
		{
			bellonaRightLastMotionAt = now;
			bellonaRightStaleSinceAt = 0;
		}
		else if (bellonaRightStaleSinceAt == 0)
		{
			bellonaRightStaleSinceAt = now;
		}

		if (bellonaRightStaleSinceAt != 0 && now - bellonaRightStaleSinceAt >= BellonaRightStaleWatchdogMs)
		{
			DebugLog.Write(
				"BellonaDual",
				$"Right metrics stale for {now - bellonaRightStaleSinceAt}ms (fish={metrics.FishCenter:0.000}, bar={metrics.PlayerbarCenter:0.000}); resetting right controller.");
			bellonaRightController.Reset();
			bellonaRightReacquireUntilAt = now + BellonaRightReacquireReleaseMs;
			bellonaRightStaleSinceAt = now;
		}

		if (now - bellonaRightLastStateLogAt >= 500)
		{
			var idleMs = bellonaRightLastMotionAt == 0 ? -1 : now - bellonaRightLastMotionAt;
			DebugLog.Write(
				"BellonaDual",
				$"RightState: holding={bellonaRightHolding}, fish={metrics.FishCenter:0.000}, bar={metrics.PlayerbarCenter:0.000}, idleMs={idleMs}");
			bellonaRightLastStateLogAt = now;
		}

		bellonaRightLastFishCenter = metrics.FishCenter;
		bellonaRightLastBarCenter = metrics.PlayerbarCenter;
	}

	private (ReelContext Left, ReelContext Right) ResolveBellonaSideContexts(IReadOnlyList<OrderedReelContext> ordered)
	{
		if (ordered.Count == 0)
		{
			return (null, null);
		}

		if (ordered.Count >= 2)
		{
			bellonaSingleReelAssignRight = true;
			return (ordered[0].Context, ordered[ordered.Count - 1].Context);
		}

		var only = ordered[0];
		if (bellonaSingleReelAssignRight)
		{
			if (only.BarX <= 0.42)
			{
				bellonaSingleReelAssignRight = false;
			}
		}
		else if (only.BarX >= 0.58)
		{
			bellonaSingleReelAssignRight = true;
		}

		return bellonaSingleReelAssignRight
			? (null, only.Context)
			: (only.Context, null);
	}

	private void BellonaRmbWatchdog()
	{
		if (currentRodProfile.Kind == RodKind.BellonaWaraxe)
		{
			return;
		}

		if (!bellonaRightHolding && MouseInput.IsRightButtonDown())
		{
			MouseInput.RightUp();
			var now = Environment.TickCount64;
			if (now - bellonaRmbWatchdogLogAt >= 500)
			{
				DebugLog.Write("BellonaDual", "Watchdog forced RMB up outside Bellona mode.");
				bellonaRmbWatchdogLogAt = now;
			}
		}
	}

	private void ApplyBellonaRightHold(bool desiredHolding)
	{
		if (desiredHolding == bellonaRightHolding)
		{
			if (desiredHolding && !MouseInput.IsRightButtonDown())
			{
				MouseInput.RightDown();
				DebugLog.Write("BellonaDual", "RMB resync down (internal hold true, physical up)");
			}
			return;
		}

		if (desiredHolding)
		{
			MouseInput.RightDown();
			DebugLog.Write("BellonaDual", "RMB down");
		}
		else
		{
			MouseInput.RightUp();
			DebugLog.Write("BellonaDual", "RMB up");
		}

		bellonaRightHolding = desiredHolding;
	}

	private void ReleaseBellonaRightHold()
	{
		if (!bellonaRightHolding && !MouseInput.IsRightButtonDown())
		{
			return;
		}

		MouseInput.RightUp();
		bellonaRightHolding = false;
	}

	private void ForceBellonaRightPhysicalUp()
	{
		if (MouseInput.IsRightButtonDown())
		{
			MouseInput.RightUp();
			DebugLog.Write("BellonaDual", "Forced RMB up on stop.");
		}

		bellonaRightHolding = false;
	}

	private RodKind ResolveMasterlineOverlayKind(RodKind rodKind)
	{
		if (rodKind != RodKind.MasterlineRod)
		{
			return rodKind;
		}

		try
		{
			foreach (var name in locator.GetMasterlineOverlayRodNames())
			{
				var overlayKind = RodClassifier.Classify(name);
				if (overlayKind is not RodKind.Default and not RodKind.MasterlineRod)
				{
					return overlayKind;
				}
			}
		}
		catch
		{
		}

		return RodKind.MasterlineRod;
	}

	private void UpdateRodProfile(RodKind rodKind)
	{
		if (rodKind == currentRodKind)
		{
			return;
		}

		currentRodProfile.Reset();
		currentRodProfile = RodProfile.For(rodKind);
		currentRodKind = rodKind;
		DebugLog.Write("ReelControlForm.UpdateRodProfile", $"profile={currentRodProfile.Kind}");
	}

	private ReelControlState ApplyFishSkipDecision(ReelControlState decision, double? progress, string traceLabel)
	{
		RefreshFishSkipSoundState();
		if (!ShouldInvertFishSkip())
		{
			return decision;
		}

		if (currentRodKind == RodKind.Requiem)
		{
			var inverted = new ReelControlState(decision.Error, decision.Control, !decision.Holding);
			return inverted;
		}

		var aggressiveHold = decision.Error < 0;
		return new ReelControlState(decision.Error, decision.Control, aggressiveHold);
	}

	private bool ShouldApplyDefaultRodStartupHoldBias(RodKind rodKind)
	{
		if (rodKind != RodKind.Default && rodKind != RodKind.MasterlineRod && rodKind != RodKind.SplitbranchTwig && rodKind != RodKind.MiguRod)
		{
			return false;
		}

		if (startupAssistStartedAt == 0)
		{
			return false;
		}

		var elapsed = Environment.TickCount64 - startupAssistStartedAt;
		if (elapsed >= DefaultRodStartupHoldBiasMs)
		{
			return false;
		}

		return true;
	}

	private void RefreshFishSkipSoundState()
	{
		if (!controlSettings.FishSkipEnabled)
		{
			fishSkipSoundState = FishSkipSoundSnapshot.Empty;
			nextFishSkipPollAt = 0;
			fishSkipEvadeActive = false;
			fishSkipEvadeLegendary = false;
			UpdateFishSkipStatus();
			return;
		}

		var now = Environment.TickCount64;
		if (now < nextFishSkipPollAt)
		{
			return;
		}

		nextFishSkipPollAt = now + 200;
		var previous = fishSkipSoundState;
		fishSkipSoundState = fishSkipDetector.Poll();
		if (fishSkipSoundState.HasOther)
		{
			fishSkipEvadeActive = false;
			fishSkipEvadeLegendary = false;
		}
		else if (locator.HasTargets && fishSkipSoundState.ShouldInvertForLegendary && controlSettings.FishSkipLegendaryMythic)
		{
			fishSkipEvadeActive = true;
			fishSkipEvadeLegendary = true;
		}
		else if (locator.HasTargets && fishSkipSoundState.ShouldInvertForNormal && controlSettings.FishSkipNormal)
		{
			fishSkipEvadeActive = true;
			fishSkipEvadeLegendary = false;
		}
		else if (!locator.HasTargets)
		{
			ResetFishSkipEvade("not-tracking");
		}

		UpdateFishSkipStatus();
	}

	private bool ShouldInvertFishSkip()
	{
		if (!controlSettings.FishSkipEnabled)
		{
			return false;
		}

		if (!fishSkipEvadeActive)
		{
			return false;
		}

		if (!locator.HasTargets)
		{
			ResetFishSkipEvade("out-of-tracking");
			return false;
		}

		if (fishSkipEvadeLegendary)
		{
			return controlSettings.FishSkipLegendaryMythic;
		}

		return controlSettings.FishSkipNormal;
	}

	private string GetFishSkipEvadeLabel()
	{
		if (!controlSettings.FishSkipEnabled)
		{
			return string.Empty;
		}

		if (!fishSkipEvadeActive)
		{
			return string.Empty;
		}

		var mode = fishSkipEvadeLegendary ? "Leg/Mythic" : "Normal";
		return $"{mode} active";
	}

	private void ApplyFishingHold(bool desired, double? progress)
	{
		if (ShouldUseRequiemFishSkipSpam())
		{
			ApplyRequiemFishSkipSpam();
			DebugLog.Write("ReelControlForm.ApplyFishingHold", $"mode={controlSettings.CastMode} desired={desired} transformed=requiem-spam action=Spam profile={currentRodProfile.Kind} progress={(progress?.ToString("0.0") ?? "null")} held={fishingGate.Held}");
			return;
		}

		var transformed = currentRodProfile.TransformHold(desired, progress);
		var now = Environment.TickCount64;
		var refreshMs = progress.HasValue ? currentRodProfile.FishingHoldRefreshMs : 0;
		var action = fishingGate.Decide(transformed, now, currentRodProfile.FishingActionDelayMs, refreshMs);
		var forcedRelease = false;
		if (controlSettings.CastMode.Equals("perfect", StringComparison.OrdinalIgnoreCase) && !transformed && fishingGate.Held && action == FishingHoldAction.None)
		{
			action = fishingGate.ForceRelease(now);
			forcedRelease = action == FishingHoldAction.Release;
		}
		if (action == FishingHoldAction.Press)
		{
			DebugLog.Write("ReelControlForm.ApplyFishingHold", $"mode={controlSettings.CastMode} desired={desired} transformed={transformed} action=Press heldBefore={fishingGate.Held} progress={(progress?.ToString("0.0") ?? "null")}");
			MouseInput.LeftDown();
		}
		else if (action == FishingHoldAction.Release)
		{
			DebugLog.Write("ReelControlForm.ApplyFishingHold", $"mode={controlSettings.CastMode} desired={desired} transformed={transformed} action=Release forced={forcedRelease} heldBefore={fishingGate.Held} progress={(progress?.ToString("0.0") ?? "null")}");
			MouseInput.LeftUp();
		}
		else
		{
			DebugLog.Write("ReelControlForm.ApplyFishingHold", $"mode={controlSettings.CastMode} desired={desired} transformed={transformed} action=None forced={forcedRelease} held={fishingGate.Held} progress={(progress?.ToString("0.0") ?? "null")}");
		}

		DebugLog.Write("ReelControlForm.ApplyFishingHold", $"desired={desired} transformed={transformed} action={action} profile={currentRodProfile.Kind} delayMs={currentRodProfile.FishingActionDelayMs} held={fishingGate.Held}");
	}

	private bool ShouldUseRequiemFishSkipSpam()
	{
		return controlSettings.FishSkipEnabled &&
			currentRodKind == RodKind.Requiem &&
			fishSkipEvadeActive &&
			locator.HasTargets;
	}

	private void ApplyRequiemFishSkipSpam()
	{
		var now = Environment.TickCount64;
		if (fishSkipRequiemNextClickAt != 0 && now < fishSkipRequiemNextClickAt)
		{
			return;
		}

		fishingGate.Reset();
		if (fishSkipRequiemClickDown)
		{
			MouseInput.LeftUp();
		}
		else
		{
			MouseInput.LeftDown();
		}

		fishSkipRequiemClickDown = !fishSkipRequiemClickDown;
		fishSkipRequiemNextClickAt = now + 35;
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	private void ForceTopMost()
	{
		if (base.IsDisposed || !base.IsHandleCreated)
		{
			return;
		}
		base.TopMost = true;
		SetWindowPos(base.Handle, new IntPtr(-1), 0, 0, 0, 0, 19u);
	}
}
