using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Client.Services.Fishing;

internal readonly record struct BellonaDebugBox(double X, double Y, double Width, double Height, Color Color, string? Label = null);

internal static class BellonaDebugOverlayService
{
    private static readonly bool Enabled =
        !string.Equals(Environment.GetEnvironmentVariable("BELLONA_DEBUG_OVERLAY"), "0", StringComparison.Ordinal);

    static BellonaDebugOverlayService()
    {
        AppLog.Fishing("Overlay", $"Bellona debug overlay enabled={Enabled} env={Environment.GetEnvironmentVariable("BELLONA_DEBUG_OVERLAY") ?? "<null>"}");
    }

    private static BellonaDebugOverlayWindow? _window;

    public static void Update(IReadOnlyList<BellonaDebugBox> boxes)
    {
        if (!Enabled)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_window is null)
            {
                _window = new BellonaDebugOverlayWindow();
                _window.Show();
            }

            _window.SetBoxes(boxes);
        });
    }

    public static void Hide()
    {
        if (!Enabled)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_window is null)
            {
                return;
            }

            _window.SetBoxes(Array.Empty<BellonaDebugBox>());
        });
    }
}

internal sealed class BellonaDebugOverlayWindow : Window
{
    private readonly BellonaDebugOverlayCanvas _canvas;

    public BellonaDebugOverlayWindow()
    {
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        CanResize = false;
        Background = Brushes.Transparent;
        Opacity = 1.0;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        TransparencyBackgroundFallback = Brushes.Transparent;

        var screens = Screens.Primary;
        var bounds = screens?.Bounds ?? new PixelRect(0, 0, 1920, 1080);
        Position = new PixelPoint(bounds.X, bounds.Y);
        Width = bounds.Width;
        Height = bounds.Height;

        _canvas = new BellonaDebugOverlayCanvas();
        Content = _canvas;

        Opened += (_, _) => ApplyNativeClickThrough();
    }

    public void SetBoxes(IReadOnlyList<BellonaDebugBox> boxes)
    {
        _canvas.SetBoxes(boxes);
    }

    private void ApplyNativeClickThrough()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var handle = TryGetPlatformHandle();
            if (handle?.Handle is not { } hwnd || hwnd == IntPtr.Zero)
            {
                return;
            }

            var exStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            exStyle |= WsExTransparent | WsExLayered | WsExToolWindow | WsExNoActivate;
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(exStyle));
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SwpNomove | SwpNosize | SwpNoactivate | SwpFramechanged);
        }
        catch
        {
        }
    }

    private const int GwlExStyle = -20;
    private const long WsExLayered = 0x00080000L;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNomove = 0x0002;
    private const uint SwpNosize = 0x0001;
    private const uint SwpNoactivate = 0x0010;
    private const uint SwpFramechanged = 0x0020;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}

internal sealed class BellonaDebugOverlayCanvas : Control
{
    private IReadOnlyList<BellonaDebugBox> _boxes = Array.Empty<BellonaDebugBox>();

    public void SetBoxes(IReadOnlyList<BellonaDebugBox> boxes)
    {
        _boxes = boxes;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        foreach (var box in _boxes)
        {
            if (box.Width <= 0 || box.Height <= 0)
            {
                continue;
            }

            var pen = new Pen(new SolidColorBrush(box.Color), 2);
            var center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
            context.DrawEllipse(null, pen, center, box.Width / 2, box.Height / 2);

            if (!string.IsNullOrWhiteSpace(box.Label))
            {
                var typeface = new Typeface("Segoe UI");
                var formatted = new FormattedText(
                    box.Label,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    12,
                    new SolidColorBrush(box.Color));
                context.DrawText(formatted, new Point(box.X + box.Width + 4, box.Y - 2));
            }
        }
    }
}
