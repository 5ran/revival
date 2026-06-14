using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Client.Services;
using Client.Services.Fishing;

namespace Client.ViewModels;

public sealed class AutoAnglerViewModel : ViewModelBase
{
    private static readonly bool AnglerDiagnosticMode = string.Equals(
        Environment.GetEnvironmentVariable("OPENMACRO_ANGLER_DIAG"),
        "1",
        StringComparison.Ordinal);
    private static readonly bool AnglerSelfTestMode = string.Equals(
        Environment.GetEnvironmentVariable("OPENMACRO_ANGLER_SELFTEST"),
        "1",
        StringComparison.Ordinal);

    private static readonly bool HideCurrentFish = string.Equals(
        Environment.GetEnvironmentVariable("OPENMACRO_HIDE_ANGLER_FISH"),
        "1",
        StringComparison.Ordinal);

    private readonly AutoAnglerRunner _runner = new();
    private readonly Timer _timer;
    private bool _autoAnglerEnabled;
    private bool _isRunning;
    private string _statusText = "---";
    private string _currentFishText = "None";
    private string _clickXText = string.Empty;
    private string _clickYText = string.Empty;
    private bool _captureNextClick;
    private bool _lastLeftDown;
    private int _tickInProgress;
    private DateTimeOffset _nextSelfTestAt = DateTimeOffset.MinValue;
    private int _completingDotPhase;
    private bool _suppressAutoToggle;

    public AutoAnglerViewModel()
    {
        UseCursorPositionCommand = new RelayCommand(_ => UseCursorPositionAsync());
        _timer = new Timer(_ => Tick(), null, TimeSpan.FromMilliseconds(120), TimeSpan.FromMilliseconds(120));
    }

    public bool AutoAnglerEnabled
    {
        get => _autoAnglerEnabled;
        set
        {
            if (!SetProperty(ref _autoAnglerEnabled, value))
            {
                return;
            }

            AppLog.Fishing("AutoAnglerVM", $"AutoAnglerEnabled changed -> {value}");
            if (!value && IsRunning)
            {
                AppLog.Fishing("AutoAnglerVM", "Checkbox disabled while running; stopping.");
                _ = StopAsync();
            }
            else if (value && !_suppressAutoToggle && !IsRunning)
            {
                AppLog.Fishing("AutoAnglerVM", "Checkbox enabled; starting angler automatically.");
                _ = StartAsync();
            }
            else if (!IsRunning)
            {
                StatusText = "---";
            }

            if (value)
            {
                TryRefreshFishPreview();
            }
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string CurrentFishText
    {
        get => _currentFishText;
        private set => SetProperty(ref _currentFishText, value);
    }

    public string ClickXText
    {
        get => _clickXText;
        set => SetProperty(ref _clickXText, value);
    }

    public string ClickYText
    {
        get => _clickYText;
        set => SetProperty(ref _clickYText, value);
    }

    public RelayCommand UseCursorPositionCommand { get; }

    public Task ToggleAsync()
    {
        return IsRunning ? StopAsync() : StartAsync();
    }

    public Task StartAsync()
    {
        AppLog.Info("MacroMode", "AutoAngler StartAsync requested.");
        if (IsRunning)
        {
            AppLog.Fishing("AutoAnglerVM", "StartAsync ignored because angler is already running.");
            return Task.CompletedTask;
        }

        var settings = BuildSettings();
        AppLog.Fishing("AutoAnglerVM", $"StartAsync settings click=({settings.ClickX},{settings.ClickY}) enabled={AutoAnglerEnabled} running={IsRunning}");
        if (settings.ClickX <= 0 || settings.ClickY <= 0)
        {
            _suppressAutoToggle = true;
            AutoAnglerEnabled = true;
            _suppressAutoToggle = false;
            IsRunning = false;
            StatusText = "Set a click point first.";
            AppLog.Fishing("AutoAnglerVM", "StartAsync aborted: click point missing.");
            return Task.CompletedTask;
        }

        _suppressAutoToggle = true;
        AutoAnglerEnabled = true;
        _suppressAutoToggle = false;
        _runner.Reset();
        IsRunning = true;
        StatusText = "Starting...";
        try
        {
            var initialFish = _runner.ReadCurrentQuestFish();
            var displayFish = GetDisplayFish(initialFish);
            CurrentFishText = displayFish;
            AppLog.Fishing("AutoAnglerVM", $"StartAsync initial fish={displayFish}");
        }
        catch
        {
            AppLog.Fishing("AutoAnglerVM", "StartAsync initial fish read failed; tick loop will retry.");
        }

        AppLog.Info("MacroMode", $"AutoAngler started. currentFish={CurrentFishText}");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        AppLog.Info("MacroMode", "AutoAngler StopAsync requested.");
        if (!IsRunning)
        {
            AppLog.Fishing("AutoAnglerVM", "StopAsync ignored because angler is already stopped.");
            AutoAnglerEnabled = false;
            return Task.CompletedTask;
        }

        IsRunning = false;
        _runner.Reset();
        StatusText = "---";
        CurrentFishText = "None";
        AppLog.Fishing("AutoAnglerVM", "Auto Angler stopped and runner reset.");
        AppLog.Info("MacroMode", "AutoAngler stopped.");
        return Task.CompletedTask;
    }

    private void Tick()
    {
        if (Interlocked.Exchange(ref _tickInProgress, 1) != 0)
        {
            return;
        }

        try
        {
            AppLog.Fishing("AutoAnglerVM", $"Tick enter running={IsRunning} enabled={AutoAnglerEnabled} status={StatusText} click=({ClickXText},{ClickYText})");
            Dispatcher.UIThread.Post(TryCaptureClickPoint);

            if (!IsRunning)
            {
                if (AutoAnglerEnabled)
                {
                    TryRefreshFishPreview();
                }

                if (AnglerSelfTestMode)
                {
                    TryRunSelfTestProbe();
                }

                return;
            }

            try
            {
                var diagnostic = AnglerDiagnosticMode
                    ? _runner.ReadCurrentQuestFishDiagnostic()
                    : new AutoAnglerQuestDiagnostic(_runner.ReadCurrentQuestFish(), string.Empty);
                Dispatcher.UIThread.Post(() =>
                {
                    var displayFish = GetDisplayFish(diagnostic.Fish);
                    CurrentFishText = displayFish;
                    if (AnglerDiagnosticMode && !string.IsNullOrWhiteSpace(diagnostic.Summary))
                    {
                        StatusText = diagnostic.Summary;
                    }
                });
            }
            catch
            {
                AppLog.Fishing("AutoAnglerVM", "Tick fish diagnostic read failed; continuing.");
            }

            var settings = BuildSettings();
            var result = _runner.Step(settings);
            AppLog.Fishing("AutoAnglerVM", $"Tick step result failed={result.Failed} status={result.Status} fish={result.CurrentFish}");
            Dispatcher.UIThread.Post(() =>
            {
                var displayFish = GetDisplayFish(result.CurrentFish);
                CurrentFishText = displayFish;
                StatusText = BuildStatusText(result.Status);

                if (result.Failed)
                {
                    IsRunning = false;
                    _runner.Reset();
                    StatusText = result.Status;
                }
            });
        }
        catch (Exception ex)
        {
            AppLog.FishingError("AutoAnglerVM", $"Tick failed: {ex.Message}", ex);
            Dispatcher.UIThread.Post(() =>
            {
                IsRunning = false;
                _runner.Reset();
                StatusText = ex.Message;
            });
        }
        finally
        {
            Interlocked.Exchange(ref _tickInProgress, 0);
        }
    }

    private AutoAnglerSettings BuildSettings()
    {
        var clickX = int.TryParse((ClickXText ?? string.Empty).Trim(), out var x) ? x : 0;
        var clickY = int.TryParse((ClickYText ?? string.Empty).Trim(), out var y) ? y : 0;
        return new AutoAnglerSettings(clickX, clickY);
    }

    private Task UseCursorPositionAsync()
    {
        _captureNextClick = true;
        _lastLeftDown = NativeMouse.IsLeftButtonDown();
        StatusText = "Click once to set.";
        AppLog.Fishing("AutoAnglerVM", "Cursor capture armed.");
        return Task.CompletedTask;
    }

    private void TryCaptureClickPoint()
    {
        if (!_captureNextClick)
        {
            return;
        }

        var isDown = NativeMouse.IsLeftButtonDown();
        if (isDown && !_lastLeftDown)
        {
            var (x, y) = NativeMouse.GetCursorPosition();
            ClickXText = x.ToString();
            ClickYText = y.ToString();
            _captureNextClick = false;
            StatusText = $"Click Location set: {x}, {y}.";
            AppLog.Fishing("AutoAnglerVM", $"Click point captured at {x},{y}.");
        }

        _lastLeftDown = isDown;
    }

    private static string GetDisplayFish(string fish)
    {
        if (string.IsNullOrWhiteSpace(fish) || string.Equals(fish, "None", StringComparison.OrdinalIgnoreCase))
        {
            return "None";
        }

        return HideCurrentFish ? "Detected" : fish;
    }

    private void TryRefreshFishPreview()
    {
        try
        {
            var fish = _runner.ReadCurrentQuestFish();
            var displayFish = GetDisplayFish(fish);
            CurrentFishText = displayFish;
            AppLog.Fishing("AutoAnglerVM", $"Preview fish refresh -> {displayFish}");
        }
        catch
        {
            AppLog.Fishing("AutoAnglerVM", "Preview fish refresh failed.");
        }
    }

    private void TryRunSelfTestProbe()
    {
        var now = DateTimeOffset.UtcNow;
        if (now < _nextSelfTestAt)
        {
            return;
        }

        _nextSelfTestAt = now.AddSeconds(2);
        try
        {
            var diagnostic = _runner.ReadCurrentQuestFishDiagnostic();
            AppLog.Info("AutoAnglerSelfTest", $"{diagnostic.Summary} -> fish={diagnostic.Fish}");
            StatusText = "---";
            CurrentFishText = GetDisplayFish(diagnostic.Fish);
            AppLog.Fishing("AutoAnglerVM", $"Self-test probe -> {diagnostic.Summary}");
        }
        catch (Exception ex)
        {
            AppLog.Info("AutoAnglerSelfTest", $"probe-failed: {ex.Message}");
            AppLog.FishingError("AutoAnglerVM", $"Self-test probe failed: {ex.Message}", ex);
        }
    }

    private string BuildStatusText(string status)
    {
        if (status.StartsWith("COUNTDOWN:", StringComparison.OrdinalIgnoreCase))
        {
            var raw = status["COUNTDOWN:".Length..];
            if (int.TryParse(raw, out var seconds))
            {
                if (seconds < 0)
                {
                    seconds = 0;
                }

                var minutes = seconds / 60;
                var remainingSeconds = seconds % 60;
                return $"{minutes}m {remainingSeconds:00}s";
            }
        }

        if (string.Equals(status, "COMPLETING", StringComparison.OrdinalIgnoreCase))
        {
            _completingDotPhase = (_completingDotPhase % 3) + 1;
            return "Completing" + new string('.', _completingDotPhase);
        }

        return "---";
    }
}
