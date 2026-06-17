using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MarkerWatch;

internal sealed class MarkerWatchForm : Form
{
    private const int HotkeyId = 0x317;
    private const int WmHotkey = 0x0312;
    private const uint VkF3 = 0x72;

    private readonly MarkerWatchProbe _probe = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly System.Windows.Forms.Timer _topMostTimer = new();
    private readonly Button _toggleButton;
    private readonly Label _statusValue;
    private readonly Label _sideValue;
    private readonly Label _deltaValue;
    private readonly TextBox _logBox;
    private bool _running;
    private bool _lastMarkerVisible;
    private MarkerSide _lastSide = MarkerSide.Center;
    private string? _lastStatus;

    public MarkerWatchForm()
    {
        Text = "Marker Watch";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 520;
        Height = 380;
        MinimumSize = new Size(460, 320);
        TopMost = true;
        BackColor = Color.FromArgb(18, 18, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9f);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = BackColor,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new Label
        {
            Text = "Marker Watch",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 16f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(header, 0, 0);

        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 10),
        };

        _toggleButton = new Button
        {
            Text = "Start (F3)",
            Width = 120,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(47, 129, 247),
            ForeColor = Color.White,
        };
        _toggleButton.FlatAppearance.BorderSize = 0;
        _toggleButton.Click += (_, _) => ToggleRunning();
        controls.Controls.Add(_toggleButton);

        var hint = new Label
        {
            Text = "Always on top. Logs only, no input.",
            AutoSize = true,
            Padding = new Padding(10, 8, 0, 0),
            ForeColor = Color.FromArgb(185, 185, 185),
        };
        controls.Controls.Add(hint);

        root.Controls.Add(controls, 0, 1);

        var stateGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
        };
        stateGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        stateGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddStateRow(stateGrid, 0, "Status", out _statusValue);
        AddStateRow(stateGrid, 1, "Side", out _sideValue);
        AddStateRow(stateGrid, 2, "Delta", out _deltaValue);
        root.Controls.Add(stateGrid, 0, 2);

        _logBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            BackColor = Color.FromArgb(12, 12, 12),
            ForeColor = Color.Gainsboro,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9f),
        };
        root.Controls.Add(_logBox, 0, 3);

        var footer = new Label
        {
            Text = "F3 toggles watch mode.",
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
            ForeColor = Color.FromArgb(150, 150, 150),
        };
        root.Controls.Add(footer, 0, 4);

        Controls.Add(root);

        _timer.Interval = 100;
        _timer.Tick += (_, _) => PollMarker();

        _topMostTimer.Interval = 1000;
        _topMostTimer.Tick += (_, _) => ForceTopMost();

        Load += (_, _) => ForceTopMost();
        Activated += (_, _) => ForceTopMost();
        Shown += (_, _) => ForceTopMost();
        FormClosed += (_, _) =>
        {
            UnregisterHotKey(Handle, HotkeyId);
            _timer.Stop();
            _topMostTimer.Stop();
            _probe.Dispose();
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterHotKey(Handle, HotkeyId, 0, VkF3);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            ToggleRunning();
            return;
        }

        base.WndProc(ref m);
    }

    private void ToggleRunning()
    {
        _running = !_running;
        _toggleButton.Text = _running ? "Stop (F3)" : "Start (F3)";
        _toggleButton.BackColor = _running ? Color.FromArgb(218, 54, 51) : Color.FromArgb(47, 129, 247);
        Log(_running ? "Watch started." : "Watch stopped.");

        if (_running)
        {
            _timer.Start();
            PollMarker();
            return;
        }

        _timer.Stop();
        SetState("Stopped", "—", "—");
    }

    private void PollMarker()
    {
        if (!_running)
        {
            return;
        }

        MarkerWatchSnapshot snapshot;
        try
        {
            snapshot = _probe.Poll();
        }
        catch (Exception ex)
        {
            snapshot = MarkerWatchSnapshot.Fault(ex.Message);
        }

        if (!string.Equals(_lastStatus, snapshot.StatusText, StringComparison.Ordinal))
        {
            Log(snapshot.StatusText);
            _lastStatus = snapshot.StatusText;
        }

        Log(snapshot.Error is not null
            ? $"poll error: {snapshot.Error}"
            : snapshot.IsAttached
                ? snapshot.IsMarkerVisible
                    ? $"poll attached: side={snapshot.Side} deltaX={snapshot.DeltaX:0.0} pointer=({snapshot.MarkerX:0.0},{snapshot.MarkerY:0.0}) center=({snapshot.CenterX:0.0},{snapshot.CenterY:0.0})"
                    : "poll attached: pointer not visible"
                : $"poll waiting: {snapshot.DetailText}");

        if (snapshot.Error is not null)
        {
            SetState("Error", snapshot.Error, "—");
            return;
        }

        if (!snapshot.IsAttached)
        {
            SetState(snapshot.StatusText, snapshot.DetailText, "—");
            return;
        }

        if (!_lastMarkerVisible && snapshot.IsMarkerVisible)
        {
            Log($"Pointer appeared at x={snapshot.MarkerX:0.0}, y={snapshot.MarkerY:0.0}.");
        }

        if (_lastMarkerVisible && !snapshot.IsMarkerVisible)
        {
            Log("Pointer disappeared.");
        }

        if (_lastMarkerVisible != snapshot.IsMarkerVisible)
        {
            _lastMarkerVisible = snapshot.IsMarkerVisible;
        }

        if (_lastSide != snapshot.Side)
        {
            _lastSide = snapshot.Side;
            Log($"Side changed to {_lastSide}.");
        }

        var sideText = snapshot.Side switch
        {
            MarkerSide.Left => "Left",
            MarkerSide.Right => "Right",
            _ => "Center",
        };

        SetState(snapshot.StatusText, $"{sideText} | markerX={snapshot.MarkerX:0.0} centerX={snapshot.CenterX:0.0}", $"Δx {snapshot.DeltaX:0.0}");
        if (!string.IsNullOrWhiteSpace(snapshot.Path))
        {
            Log(snapshot.Path);
        }
    }

    private void SetState(string status, string side, string delta)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetState(status, side, delta)));
            return;
        }

        _statusValue.Text = status;
        _sideValue.Text = side;
        _deltaValue.Text = delta;
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Log(message)));
            return;
        }

        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _logBox.AppendText(line + Environment.NewLine);
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.ScrollToCaret();
    }

    private void AddStateRow(TableLayoutPanel grid, int row, string label, out Label valueLabel)
    {
        var key = new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = Color.FromArgb(170, 170, 170),
            Margin = new Padding(0, 4, 0, 4),
        };

        valueLabel = new Label
        {
            Text = "—",
            AutoSize = true,
            ForeColor = Color.White,
            Margin = new Padding(0, 4, 0, 4),
        };

        grid.Controls.Add(key, 0, row);
        grid.Controls.Add(valueLabel, 1, row);
    }

    private void ForceTopMost()
    {
        TopMost = true;
        BringToFront();
        Activate();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
