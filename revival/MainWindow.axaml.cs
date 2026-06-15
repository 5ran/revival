using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Runtime.InteropServices;
using System.Threading;
using Client.Services;
using Client.ViewModels;

namespace Client;

/// <summary>
/// Main application window with custom chrome and hosted app content.
/// </summary>
public partial class MainWindow : Window
{
    private const int HotkeyToggleDebounceMs = 120;
    private const double NormalScale = 1.2;
    private const double NormalDesignWidth = 820;
    private const double NormalDesignHeight = 480;
    private const double NormalWidth = NormalDesignWidth * NormalScale;
    private const double NormalHeight = NormalDesignHeight * NormalScale;
    private const double CompactWidth = 440;
    private const double CompactHeight = 280;
    private readonly MainWindowViewModel _viewModel;
    private readonly GlobalHotkeyService _globalHotkeyService = new();
    private long _lastHotkeyToggleAt;
    private int _hotkeyToggleInFlight;

    /// <summary>
    /// Creates the main application window.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        AppLog.Info("MainWindow", $"Starting. log={AppLog.LogPath}");
        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = new PixelPoint(0, 0);

        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        _globalHotkeyService.SetHotkey(_viewModel.StartStopHotkey);
        _globalHotkeyService.Pressed += GlobalHotkeyService_OnPressed;
        _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        KeyDown += MainWindow_OnKeyDown;
        Loaded += MainWindow_OnLoaded;
        AddHandler(PointerReleasedEvent, InterfaceClick_OnPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void ViewModel_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsCompactMode))
        {
            return;
        }

        var compact = _viewModel.IsCompactMode;
        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyCompactSize(compact);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ApplyCompactSize(compact));
        }
    }

    private void ApplyCompactSize(bool compact)
    {
        MacroScaleRoot.Width = compact ? CompactWidth : NormalDesignWidth;
        MacroScaleRoot.Height = compact ? CompactHeight : NormalDesignHeight;
        MacroScaleRoot.RenderTransform = compact
            ? new ScaleTransform(1, 1)
            : new ScaleTransform(NormalScale, NormalScale);
        Width = compact ? CompactWidth : NormalWidth;
        Height = compact ? CompactHeight : NormalHeight;
        Dispatcher.UIThread.Post(() => ApplyWindowRegion(compact), DispatcherPriority.Render);
    }

    private void GlobalHotkeyService_OnPressed()
    {
        var now = Environment.TickCount64;
        var last = Interlocked.Read(ref _lastHotkeyToggleAt);
        if (now - last < HotkeyToggleDebounceMs)
        {
            AppLog.Info("Hotkey", $"MainWindow debounce suppressed. dt={now - last}ms");
            return;
        }

        Interlocked.Exchange(ref _lastHotkeyToggleAt, now);
        if (Interlocked.Exchange(ref _hotkeyToggleInFlight, 1) == 1)
        {
            AppLog.Info("Hotkey", "MainWindow in-flight suppressed.");
            return;
        }

        AppLog.Info("Hotkey", "MainWindow accepted press; dispatching toggle.");
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await _viewModel.ToggleMacroFromHotkeyAsync();
            }
            finally
            {
                Volatile.Write(ref _hotkeyToggleInFlight, 0);
                AppLog.Info("Hotkey", "MainWindow toggle dispatch complete.");
            }
        });
    }

    private void MainWindow_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel.HandleKey(e.Key))
        {
            _globalHotkeyService.SetHotkey(_viewModel.StartStopHotkey);
            e.Handled = true;
        }
    }

    private async void MainWindow_OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_OnLoaded;
        AppLog.Info("MainWindow", "Loaded; starting macro shell.");
        await _viewModel.InitializeAsync();
        ApplyWindowRegion(_viewModel.IsCompactMode);
    }

    private static void InterfaceClick_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left || e.Source is not Control source)
        {
            return;
        }

        if (source is Button or ToggleButton or ToggleSwitch or ComboBox or Slider ||
            source.FindAncestorOfType<Button>() is not null ||
            source.FindAncestorOfType<ToggleButton>() is not null ||
            source.FindAncestorOfType<ToggleSwitch>() is not null ||
            source.FindAncestorOfType<ComboBox>() is not null ||
            source.FindAncestorOfType<Slider>() is not null)
        {
            InterfaceSoundService.PlayClick();
        }
    }

    private void ApplyWindowRegion(bool compact)
    {
        if (!OperatingSystem.IsWindows() || TryGetPlatformHandle()?.Handle is not { } handle || handle == IntPtr.Zero)
        {
            return;
        }

        var scale = RenderScaling;
        var width = ToPixels(compact ? CompactWidth : NormalWidth, scale);
        var height = ToPixels(compact ? CompactHeight : NormalHeight, scale);
        var bodyCornerDiameter = ToPixels(compact ? 28 : 28 * NormalScale, scale);

        if (compact)
        {
            SetWindowRgn(handle, CreateRoundRectRgn(0, 0, width + 1, height + 1, bodyCornerDiameter, bodyCornerDiameter), true);
            return;
        }

        SetWindowRgn(handle, CreateRoundRectRgn(0, 0, width + 1, height + 1, bodyCornerDiameter, bodyCornerDiameter), true);
    }

    private static int ToPixels(double value, double scale) => (int)Math.Round(value * scale);

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control source && source.FindAncestorOfType<Button>() is not null)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
        _globalHotkeyService.Dispose();
        base.OnClosed(e);
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr window, IntPtr region, bool redraw);

}
