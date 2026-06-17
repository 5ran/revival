using System;

namespace Client.Services.Fishing;

internal sealed class LullabyRodProfile : RodProfile
{
    public override RodKind Kind => RodKind.Lullaby;

    public override int FishingActionDelayMs
        => (int)Math.Round(Math.Max(0.0, LullabySettings.Current.ClickDelaySeconds) * 1000.0);

    public override bool TransformHold(bool desiredHold, double? progress)
    {
        var config = LullabySettings.Current;
        if (LullabySettings.SelectedModeName == "None")
        {
            return desiredHold;
        }

        if (progress is null)
        {
            return desiredHold;
        }

        var now = Environment.TickCount64;
        var intervalMs = Math.Max(25, (int)Math.Round(Math.Max(0.01, config.ClickDelaySeconds) * 1000.0));
        if (!_spamMode && progress.Value >= config.ProgressSpamAt)
        {
            _spamMode = true;
            _pulseOn = true;
            _nextToggleAt = now + intervalMs;
            return true;
        }

        if (_spamMode)
        {
            if (progress.Value <= config.ProgressStopSpamBelow)
            {
                _spamMode = false;
                _pulseOn = false;
                _nextToggleAt = 0;
                return desiredHold;
            }

            if (_nextToggleAt == 0)
            {
                _nextToggleAt = now + intervalMs;
                _pulseOn = true;
                return true;
            }

            while (now >= _nextToggleAt)
            {
                _pulseOn = !_pulseOn;
                _nextToggleAt += intervalMs;
            }

            return _pulseOn;
        }

        return desiredHold;
    }

    public override void Reset()
    {
        _spamMode = false;
        _pulseOn = false;
        _nextToggleAt = 0;
    }

    private bool _spamMode;
    private bool _pulseOn;
    private long _nextToggleAt;
}
