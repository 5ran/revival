using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal sealed class ReelControlForm : Form
{
	private const int HotkeyId = 21041;

	private const int WmHotkey = 786;

	private const uint VkF3 = 114u;

	private readonly Button toggleButton;

	private readonly Timer timer;

	private readonly Timer topMostTimer;

	private readonly ReelLocator locator = new ReelLocator();

	private readonly ReelController controller = new ReelController();

	private readonly ReelControlSettings controlSettings = new ReelControlSettings();

	private readonly AquariumSequenceRunner aquariumRunner = new AquariumSequenceRunner();

	private readonly CheckBox autoAquariumToggle;

	private readonly NumericUpDown autoAquariumCycleDelayInput;

	private bool running;

	private bool aquariumPending;

	private bool aquariumDue;

	private bool wasInMinigame;

	private double nextProbeTime;

	private double nextAquariumCycleTime;

	private readonly Stopwatch stopwatch = Stopwatch.StartNew();

	public ReelControlForm()
	{
		Text = "Reel Control v2";
		base.Width = 260;
		base.Height = 220;
		MinimumSize = new Size(240, 210);
		base.StartPosition = FormStartPosition.CenterScreen;
		base.TopMost = true;
		BackColor = Color.FromArgb(24, 27, 31);
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 9f);
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
		Label value2 = new Label
		{
			Text = "Cycle Delay (min)",
			Left = 0,
			Top = 70,
			Width = 100,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31)
		};
		panel.Controls.Add(value2);
		autoAquariumCycleDelayInput = new NumericUpDown
		{
			Left = 106,
			Top = 67,
			Width = 70,
			DecimalPlaces = 2,
			Minimum = 0m,
			Maximum = 1440m,
			Increment = 0.5m,
			Value = 65m
		};
		panel.Controls.Add(autoAquariumCycleDelayInput);
		base.Controls.Add(panel);
		timer = new Timer
		{
			Interval = 20
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
		base.Load += delegate
		{
			ForceTopMost();
		};
		base.Activated += delegate
		{
			ForceTopMost();
		};
		base.FormClosed += delegate
		{
			UnregisterHotKey(base.Handle, 21041);
			topMostTimer.Stop();
			timer.Stop();
			controller.Reset();
			aquariumRunner.Reset();
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

	private void StartOverlay()
	{
		running = true;
		nextProbeTime = 0.0;
		nextAquariumCycleTime = 0.0;
		aquariumDue = false;
		wasInMinigame = false;
		aquariumPending = autoAquariumToggle.Checked;
		aquariumRunner.Reset();
		toggleButton.Text = "Stop (F3)";
		toggleButton.BackColor = Color.FromArgb(218, 54, 51);
		timer.Start();
		RunTick();
	}

	private void StopOverlay()
	{
		running = false;
		timer.Stop();
		controller.Reset();
		aquariumRunner.Reset();
		locator.ResetTargets();
		aquariumDue = false;
		nextAquariumCycleTime = 0.0;
		toggleButton.Text = "Start (F3)";
		toggleButton.BackColor = Color.FromArgb(47, 129, 247);
	}

	private void RunTick()
	{
		double totalSeconds = stopwatch.Elapsed.TotalSeconds;
		try
		{
			if (autoAquariumToggle.Checked && aquariumPending)
			{
				try
				{
					AquariumSequenceResult aquariumSequenceResult = aquariumRunner.Step(0.25);
					if (aquariumSequenceResult.Completed)
					{
						aquariumPending = false;
						aquariumDue = false;
						nextAquariumCycleTime = totalSeconds + (double)autoAquariumCycleDelayInput.Value * 60.0;
						wasInMinigame = false;
						locator.ResetTargets();
						nextProbeTime = totalSeconds + 0.15;
					}
					return;
				}
				catch
				{
					aquariumRunner.Reset();
					aquariumPending = false;
					aquariumDue = false;
					nextAquariumCycleTime = totalSeconds + (double)autoAquariumCycleDelayInput.Value * 60.0;
					locator.ResetTargets();
					nextProbeTime = totalSeconds + 0.25;
				}
			}
			if (autoAquariumToggle.Checked && !aquariumDue && nextAquariumCycleTime > 0.0 && totalSeconds >= nextAquariumCycleTime)
			{
				aquariumDue = true;
			}

			if (!locator.HasTargets && totalSeconds < nextProbeTime)
			{
				controller.UpdateCasting();
				return;
			}
			ReelSnapshot snapshot = locator.ReadSnapshot();
			wasInMinigame = true;
			controller.UpdateTracking(snapshot, controlSettings);
		}
		catch (NoMinigameException)
		{
			locator.ResetTargets();
			nextProbeTime = totalSeconds + 0.25;
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
			wasInMinigame = false;
			controller.UpdateCasting();
		}
		catch (Exception)
		{
			controller.Reset();
			locator.ResetTargets();
			nextProbeTime = totalSeconds + 1.0;
		}
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
