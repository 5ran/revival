using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace OpenMacroSwift.Desktop.Services;

public sealed class TrackingOverlayService : IDisposable
{
    private const string MappingName = @"Local\OpenMacroSwiftOverlay";
    private const string TrackingMetricsMappingName = @"Local\OpenMacroSwiftTrackingMetrics";
    private const uint OverlayMagic = 0x4F4D4F56;
    private const uint TrackingMetricsMagic = 0x4F4D5452; // OMTR
    private const int OverlayStateSize = 88;
    private const int TrackingMetricsStateSize = 120;
    private const int MagicOffset = 0;
    private const int SequenceOffset = 4;
    private const int VisibleOffset = 8;
    private const int TickMsOffset = 16;
    private const int FishXOffset = 24;
    private const int ControlXOffset = 56;
    private const int MetricsPhaseOffset = 8;
    private const int MetricsHasMetricsOffset = 12;
    private const int MetricsTickMsOffset = 16;
    private const int MetricsFishCenterOffset = 24;
    private const int MetricsPlayerbarCenterOffset = 32;
    private const int MetricsPlayerbarWidthOffset = 40;
    private const int MetricsProgressOffset = 48;
    private const int MetricsHasProgressOffset = 56;
    private const int MetricsShakeVisibleOffset = 60;
    private const int MetricsReelVisibleOffset = 64;
    private const int MetricsHasDecisionOffset = 72;
    private const int MetricsDesiredHoldOffset = 76;
    private const int MetricsDecisionModeOffset = 80;
    private const int MetricsDecisionErrorOffset = 88;
    private const int MetricsDecisionControlOffset = 96;
    private const int MetricsDecisionAppliedOffset = 112;
    private const long StaleThresholdMs = 500;
    private const long SwiftTrackingTickMs = 20;
    private const long SwiftReelInputSettleMs = 125;

    private MemoryMappedFile? mapping;
    private MemoryMappedViewAccessor? accessor;
    private MemoryMappedFile? trackingMetricsMapping;
    private MemoryMappedViewAccessor? trackingMetricsAccessor;
    private OverlayWindow? window;
    private Dispatcher? overlayDispatcher;
    private DispatcherTimer? overlayTimer;
    private Thread? overlayThread;
    private Client.Services.Fishing.LocalOffsetsJsonRuntime? offsetsRuntime;
    private Client.Services.Fishing.RobloxMemory? overlayMemory;
    private Client.Services.Fishing.FishingRuntimeContext? overlayContext;
    private readonly Client.Services.Fishing.Tracking1Controller tracking1Controller = new();
    private readonly Client.Services.Fishing.Tracking2Controller tracking2Controller = new();
    private readonly Client.Services.Fishing.Tracking3Controller tracking3Controller = new();
    private readonly Client.Services.Fishing.Tracking1Settings tracking1Settings = new();
    private readonly Client.Services.Fishing.Tracking2Settings tracking2Settings = new();
    private readonly Client.Services.Fishing.Tracking3Settings tracking3Settings = new();
    private readonly Client.Services.Fishing.FishingHoldGate csharpFishingGate = new();
    private readonly object trackingModeLock = new();
    private string trackingMode = "Hybrid";
    private bool hadMetricsLastTick;
    private long csharpFishingInputReadyAt;
    private long csharpLastDecisionAt;
    private CSharpTrackingDecision? lastCSharpDecision;
    private IntPtr lastRobloxHandle = IntPtr.Zero;
    private double cachedOriginX;
    private double cachedOriginY;
    private long lastOriginRefreshMs;
    private long lastTraceMs;
    private string lastTraceMessage = string.Empty;
    private StreamWriter? logWriter;

    public void Start()
    {
        if (overlayThread is not null)
        {
            return;
        }

        overlayThread = new Thread(OverlayThreadMain)
        {
            IsBackground = true,
            Name = "OpenMacro Overlay"
        };
        overlayThread.SetApartmentState(ApartmentState.STA);
        overlayThread.Start();
        Trace("overlay.thread.start");
    }

    public bool TryRefreshFromSharedMemory()
    {
        return TryRefreshFromRuntime();
    }

    public void SetTrackingMode(string mode)
    {
        lock (trackingModeLock)
        {
            trackingMode = string.IsNullOrWhiteSpace(mode) ? "Hybrid" : mode;
            ResetCSharpTrackingControllers();
        }
    }

    private bool TryRefreshFromRuntime()
    {
        var tickStart = Stopwatch.GetTimestamp();
        if (!EnsureRuntime())
        {
            Trace("runtime.init_failed");
            Hide();
            return false;
        }

        var memory = overlayMemory!;
        var context = overlayContext!;
        try
        {
            memory.EnsureAttached();
        }
        catch
        {
            Hide();
            return false;
        }

        if (!context.TryGetTracking1State(out var phase, out var reel, out var metrics) ||
            reel is null ||
            metrics is null)
        {
            hadMetricsLastTick = false;
            ReleaseCSharpFishingHold(Environment.TickCount64);
            WriteTrackingMetrics(phase, metrics, context.GetFishingCompletionPercent(), null);
            TraceOnce($"phase.{phase.ToLowerInvariant()}");
            Hide();
            return false;
        }

        if (!hadMetricsLastTick)
        {
            ResetCSharpTrackingControllers();
        }

        var decision = ComputeCSharpTrackingDecision(metrics);
        hadMetricsLastTick = true;
        WriteTrackingMetrics(phase, metrics, context.GetFishingCompletionPercent(), decision);
        Hide();

        if (Environment.GetEnvironmentVariable("OPENMACRO_OVERLAY_DEBUG") == "1")
        {
            Trace($"refresh.ok phase={phase} totalMs={ElapsedMs(tickStart):0.00} decision={(decision?.HasDecision == true ? "1" : "0")}");
        }

        return true;
    }

    public void Hide()
    {
        if (overlayDispatcher is not null && !overlayDispatcher.CheckAccess())
        {
            try
            {
                overlayDispatcher.BeginInvoke(Hide);
            }
            catch
            {
            }
            return;
        }

        if (window is null)
        {
            return;
        }

        TraceOnce("window.hide");
        window.Hide();
    }

    public void Dispose()
    {
        Stop();
        try
        {
            window?.Close();
        }
        catch
        {
        }

        window = null;
        ResetMapping();
        ResetTrackingMetricsMapping();
        try
        {
            logWriter?.Dispose();
        }
        catch
        {
        }
        logWriter = null;
    }

    public void Stop()
    {
        var dispatcher = overlayDispatcher;
        if (dispatcher is not null)
        {
            try
            {
                dispatcher.InvokeShutdown();
            }
            catch
            {
            }
        }

        if (overlayThread is not null && overlayThread.IsAlive)
        {
            overlayThread.Join(1000);
        }

        Trace("overlay.thread.stop");
        overlayThread = null;
        overlayDispatcher = null;
        overlayTimer = null;
    }

    private void TraceOnce(string message)
    {
        long now = Environment.TickCount64;
        if (message == lastTraceMessage && now - lastTraceMs < 250)
        {
            return;
        }

        lastTraceMessage = message;
        lastTraceMs = now;
        Trace(message);
    }

    private void Trace(string message)
    {
        try
        {
            logWriter ??= new StreamWriter(new FileStream(
                Path.Combine(AppContext.BaseDirectory, "openmacro_overlay_debug.log"),
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite))
            {
                AutoFlush = true
            };

            logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
        }
        catch
        {
        }
    }

    private static double ElapsedMs(long startTimestamp)
    {
        return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }

    private void OverlayThreadMain()
    {
        overlayDispatcher = Dispatcher.CurrentDispatcher;
        overlayTimer = new DispatcherTimer(DispatcherPriority.Render, overlayDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        overlayTimer.Tick += (_, _) => RefreshOverlayTick();
        overlayTimer.Start();
        Dispatcher.Run();
    }

    private void RefreshOverlayTick()
    {
        try
        {
            if (!TryRefreshFromSharedMemory())
            {
                Hide();
            }
        }
        catch
        {
            Hide();
        }
    }

    private bool EnsureRuntime()
    {
        if (overlayMemory is not null && overlayContext is not null)
        {
            return true;
        }

        try
        {
            offsetsRuntime ??= new Client.Services.Fishing.LocalOffsetsJsonRuntime();
            overlayMemory ??= new Client.Services.Fishing.RobloxMemory(Client.Services.Fishing.OffsetsSourceProvider.Current);
            overlayContext ??= new Client.Services.Fishing.FishingRuntimeContext(overlayMemory);
            Trace("runtime.ready");
            return true;
        }
        catch
        {
            overlayContext = null;
            overlayMemory = null;
            return false;
        }
    }

    private void EnsureWindow()
    {
        if (window is not null)
        {
            return;
        }

        window = new OverlayWindow();
        Trace("window.create");
    }

    private bool TryReadSnapshot(out OverlaySnapshot snapshot)
    {
        snapshot = default;
        if (!EnsureMapping())
        {
            return false;
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            uint sequenceStart = accessor!.ReadUInt32(SequenceOffset);
            if ((sequenceStart & 1) != 0)
            {
                continue;
            }

            uint magic = accessor.ReadUInt32(MagicOffset);
            if (magic != OverlayMagic)
            {
                return false;
            }

            snapshot = new OverlaySnapshot(
                accessor.ReadUInt32(VisibleOffset) != 0,
                accessor.ReadUInt64(TickMsOffset),
                accessor.ReadDouble(FishXOffset),
                accessor.ReadDouble(FishXOffset + 8),
                accessor.ReadDouble(FishXOffset + 16),
                accessor.ReadDouble(FishXOffset + 24),
                accessor.ReadDouble(ControlXOffset),
                accessor.ReadDouble(ControlXOffset + 8),
                accessor.ReadDouble(ControlXOffset + 16),
                accessor.ReadDouble(ControlXOffset + 24));

            uint sequenceEnd = accessor.ReadUInt32(SequenceOffset);
            if (sequenceStart == sequenceEnd && (sequenceEnd & 1) == 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool EnsureMapping()
    {
        if (mapping is not null && accessor is not null)
        {
            return true;
        }

        ResetMapping();
        try
        {
            mapping = MemoryMappedFile.CreateOrOpen(MappingName, OverlayStateSize, MemoryMappedFileAccess.ReadWrite);
            accessor = mapping.CreateViewAccessor(0, OverlayStateSize, MemoryMappedFileAccess.Read);
            Trace("mapping.ready");
            return true;
        }
        catch
        {
            ResetMapping();
            return false;
        }
    }

    private void ResetMapping()
    {
        try { accessor?.Dispose(); } catch { }
        try { mapping?.Dispose(); } catch { }
        accessor = null;
        mapping = null;
    }

    private bool EnsureTrackingMetricsMapping()
    {
        if (trackingMetricsMapping is not null && trackingMetricsAccessor is not null)
        {
            return true;
        }

        ResetTrackingMetricsMapping();
        try
        {
            trackingMetricsMapping = MemoryMappedFile.CreateOrOpen(
                TrackingMetricsMappingName,
                TrackingMetricsStateSize,
                MemoryMappedFileAccess.ReadWrite);
            trackingMetricsAccessor = trackingMetricsMapping.CreateViewAccessor(
                0,
                TrackingMetricsStateSize,
                MemoryMappedFileAccess.ReadWrite);
            Trace("tracking_metrics.mapping.ready");
            return true;
        }
        catch
        {
            ResetTrackingMetricsMapping();
            return false;
        }
    }

    private void ResetTrackingMetricsMapping()
    {
        try { trackingMetricsAccessor?.Dispose(); } catch { }
        try { trackingMetricsMapping?.Dispose(); } catch { }
        trackingMetricsAccessor = null;
        trackingMetricsMapping = null;
    }

    private void ResetCSharpTrackingControllers()
    {
        tracking1Controller.Reset();
        tracking2Controller.Reset();
        tracking3Controller.Reset();
        csharpFishingGate.Reset();
        csharpFishingInputReadyAt = 0;
        csharpLastDecisionAt = 0;
        lastCSharpDecision = null;
    }

    private CSharpTrackingDecision? ComputeCSharpTrackingDecision(Client.Services.Fishing.ReelMetrics metrics)
    {
        var now = Environment.TickCount64;
        if (!hadMetricsLastTick)
        {
            ResetCSharpTrackingControllers();
            csharpFishingInputReadyAt = now + SwiftReelInputSettleMs;
            Trace("csharp_tracker.reel_enter settle_ms=125");
            ReleaseCSharpFishingHold(now);
            return null;
        }

        if (csharpFishingInputReadyAt != 0 && now < csharpFishingInputReadyAt)
        {
            ReleaseCSharpFishingHold(now);
            return null;
        }

        csharpFishingInputReadyAt = 0;
        if (lastCSharpDecision is not null && now - csharpLastDecisionAt < SwiftTrackingTickMs)
        {
            return lastCSharpDecision;
        }

        string mode;
        lock (trackingModeLock)
        {
            mode = trackingMode;
        }

        if (string.Equals(mode, "Spam", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "Tracking 2", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "Tracking2", StringComparison.OrdinalIgnoreCase))
        {
            var decision = tracking2Controller.Update(metrics, tracking2Settings);
            ApplyCSharpFishingHold(decision.Holding, now);
            lastCSharpDecision = new CSharpTrackingDecision(true, decision.Holding, 2, decision.Error, decision.Control, true);
            csharpLastDecisionAt = now;
            return lastCSharpDecision;
        }

        if (string.Equals(mode, "Predict", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "Tracking 1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "Tracking1", StringComparison.OrdinalIgnoreCase))
        {
            var decision = tracking1Controller.UpdateTracking(metrics, tracking1Settings);
            ApplyCSharpFishingHold(decision.Holding, now);
            lastCSharpDecision = new CSharpTrackingDecision(true, decision.Holding, 1, decision.Error, decision.Control, true);
            csharpLastDecisionAt = now;
            return lastCSharpDecision;
        }

        var hybrid = tracking3Controller.Update(metrics, tracking3Settings);
        ApplyCSharpFishingHold(hybrid.DesiredHolding, now);
        lastCSharpDecision = new CSharpTrackingDecision(true, hybrid.DesiredHolding, 30 + (int)hybrid.Mode, hybrid.Error, hybrid.Control, true);
        csharpLastDecisionAt = now;
        return lastCSharpDecision;
    }

    private void ApplyCSharpFishingHold(bool desired, long now)
    {
        var action = csharpFishingGate.Decide(desired, now, 0);
        if (action == Client.Services.Fishing.FishingHoldAction.Press)
        {
            Client.Services.Fishing.NativeMouse.LeftDown();
        }
        else if (action == Client.Services.Fishing.FishingHoldAction.Release)
        {
            Client.Services.Fishing.NativeMouse.LeftUp();
        }
    }

    private void ReleaseCSharpFishingHold(long now)
    {
        if (csharpFishingGate.ForceRelease(now) == Client.Services.Fishing.FishingHoldAction.Release)
        {
            Client.Services.Fishing.NativeMouse.LeftUp();
        }
    }

    private void WriteTrackingMetrics(
        string phase,
        Client.Services.Fishing.ReelMetrics? metrics,
        double? progress,
        CSharpTrackingDecision? decision)
    {
        if (!EnsureTrackingMetricsMapping())
        {
            return;
        }

        try
        {
            var view = trackingMetricsAccessor!;
            var sequence = view.ReadUInt32(SequenceOffset) + 1;
            view.Write(SequenceOffset, sequence | 1U);
            Thread.MemoryBarrier();

            view.Write(MagicOffset, TrackingMetricsMagic);
            view.Write(MetricsPhaseOffset, PhaseCode(phase));
            view.Write(MetricsHasMetricsOffset, metrics is null ? 0U : 1U);
            view.Write(MetricsTickMsOffset, (ulong)Environment.TickCount64);
            view.Write(MetricsFishCenterOffset, metrics?.FishCenter ?? 0.0);
            view.Write(MetricsPlayerbarCenterOffset, metrics?.PlayerbarCenter ?? 0.0);
            view.Write(MetricsPlayerbarWidthOffset, metrics?.PlayerbarWidth ?? 0.0);
            view.Write(MetricsProgressOffset, progress ?? 0.0);
            view.Write(MetricsHasProgressOffset, progress.HasValue ? 1U : 0U);
            view.Write(MetricsShakeVisibleOffset, string.Equals(phase, "SHAKE", StringComparison.OrdinalIgnoreCase) ? 1U : 0U);
            view.Write(MetricsReelVisibleOffset, metrics is null ? 0U : 1U);
            view.Write(68, 0U);
            view.Write(MetricsHasDecisionOffset, decision?.HasDecision == true ? 1U : 0U);
            view.Write(MetricsDesiredHoldOffset, decision?.DesiredHold == true ? 1U : 0U);
            view.Write(MetricsDecisionModeOffset, (uint)(decision?.Mode ?? 0));
            view.Write(84, 0U);
            view.Write(MetricsDecisionErrorOffset, decision?.Error ?? 0.0);
            view.Write(MetricsDecisionControlOffset, decision?.Control ?? 0.0);
            view.Write(104, 0UL);
            view.Write(MetricsDecisionAppliedOffset, decision?.AppliedByCSharp == true ? 1U : 0U);
            view.Write(116, 0U);

            Thread.MemoryBarrier();
            view.Write(SequenceOffset, (sequence | 1U) + 1U);
        }
        catch
        {
            ResetTrackingMetricsMapping();
        }
    }

    private static uint PhaseCode(string phase)
    {
        return phase.ToUpperInvariant() switch
        {
            "SHAKE" => 1U,
            "TRACKING" or "FISHING" => 2U,
            _ => 0U
        };
    }

    private bool TryGetClientOrigin(IntPtr handle, out double x, out double y)
    {
        var now = Environment.TickCount64;
        if (handle != IntPtr.Zero && handle == lastRobloxHandle && now - lastOriginRefreshMs < 100)
        {
            x = cachedOriginX;
            y = cachedOriginY;
            return true;
        }

        x = 0;
        y = 0;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        POINT pt = new() { X = 0, Y = 0 };
        if (!ClientToScreen(handle, ref pt))
        {
            return false;
        }

        x = pt.X;
        y = pt.Y;
        lastRobloxHandle = handle;
        cachedOriginX = x;
        cachedOriginY = y;
        lastOriginRefreshMs = now;
        return true;
    }

    private static IntPtr FindWindowForProcess(int processId)
    {
        IntPtr found = IntPtr.Zero;

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd))
            {
                return true;
            }

            GetWindowThreadProcessId(hWnd, out uint windowProcessId);
            if (windowProcessId != (uint)processId)
            {
                return true;
            }

            found = hWnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private sealed class OverlayWindow : Window
    {
        private readonly OverlayCanvas canvas = new();
        private Rect fishRect;
        private Rect controlRect;

        public OverlayWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            IsHitTestVisible = false;
            ShowActivated = false;

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            Content = canvas;
            SourceInitialized += (_, _) => MakeClickThrough();
        }

        public void SetBoxes(Rect fish, Rect control)
        {
            if (fish == fishRect && control == controlRect)
            {
                return;
            }

            fishRect = fish;
            controlRect = control;
            canvas.SetBoxes(fish, control);
        }

        private void MakeClickThrough()
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOOLWINDOW = 0x80;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }

    private sealed class OverlayCanvas : FrameworkElement
    {
        private Rect fishRect;
        private Rect controlRect;

        public void SetBoxes(Rect fish, Rect control)
        {
            fishRect = fish;
            controlRect = control;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (fishRect.Width > 1 && fishRect.Height > 1)
            {
                drawingContext.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(255, 255, 96, 96)), 2), fishRect);
            }

            if (controlRect.Width > 1 && controlRect.Height > 1)
            {
                drawingContext.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(255, 96, 200, 255)), 2), controlRect);
            }
        }
    }

    private readonly record struct OverlaySnapshot(
        bool Visible,
        ulong TickMs,
        double FishX,
        double FishY,
        double FishW,
        double FishH,
        double ControlX,
        double ControlY,
        double ControlW,
        double ControlH);

    private readonly record struct CSharpTrackingDecision(
        bool HasDecision,
        bool DesiredHold,
        int Mode,
        double Error,
        double Control,
        bool AppliedByCSharp);
}
