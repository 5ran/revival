using System;
using System.Diagnostics;
using Client.Services.Fishing;

internal sealed class ReelController
{
    private const int PerfectCastStartDelayMs = 400;
    private const int MinTrackingActionDelayMs = 55;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private bool _holding;
    private long _lastTrackingActionAt;
    private double? _lastPlayerbarCenter;
    private double? _lastFishPos;
    private bool _hasLastFrame;
    private double _lastTime;
    private double _prevFishX;
    private double _prevBarCenter;
    private double _fishVelocityEma;
    private double _barVelocityEma;
    private double _smoothFishX;
    private double _smoothBarCenter;
    private double _smoothControl;
    private double _errorIntegral;
    private double _centerPulseReleaseUntil;
    private bool _wasInStableZone;
    private double _stableHybridUntil;
    private double _trackingWarmupUntil;
    private bool _casting;
    private bool _castHolding;
    private double _nextCastChangeTime;
    private long _castStartedAt;
    private long _castReleasedAt;
    private bool _castBarSeen;
    private double _lastShakedAt;

    public ReelControlState UpdateTracking(ReelMetrics metrics, ReelControlSettings settings, int actionDelayMs = 0)
    {
        if (_casting)
        {
            _casting = false;
            _castHolding = false;
            _nextCastChangeTime = 0;
            Release();
            ResetTrackingState();
        }

        var state = Compute(metrics, settings);
        SetTrackingHold(state.Holding, actionDelayMs);
        return state;
    }

    public ReelControlState UpdateCasting(string castMode, bool shakeVisible, Func<double?> getPowerBarPercent)
    {
        var now = _stopwatch.Elapsed.TotalSeconds;
        if (string.Equals(castMode, "perfect", StringComparison.OrdinalIgnoreCase))
        {
            if (!_casting)
            {
                ResetTrackingState();
                _casting = true;
                _castHolding = false;
                _castStartedAt = Environment.TickCount64;
                _castReleasedAt = 0;
                _castBarSeen = false;
                _lastShakedAt = 0.0;
            }

            if (Environment.TickCount64 - _castStartedAt < PerfectCastStartDelayMs)
            {
                _castHolding = true;
                SetHold(true);
                return new ReelControlState(0.0, 0.0, true);
            }

            var targetPower = 96.0;
            var power = getPowerBarPercent?.Invoke();
            if (_castReleasedAt == 0)
            {
                if (!_castHolding)
                {
                    _castHolding = true;
                    SetHold(true);
                }

                if (power is null)
                {
                    return new ReelControlState(0.0, 0.0, true);
                }

                _castBarSeen = true;
                if (power >= targetPower || TryReleasePerfectCastNearTarget(targetPower, getPowerBarPercent))
                {
                    _castHolding = false;
                    _castReleasedAt = Environment.TickCount64;
                    SetHold(false);
                    return new ReelControlState(0.0, 0.0, false);
                }

                return new ReelControlState(0.0, 0.0, true);
            }

            _castHolding = false;
            SetHold(false);
            if (shakeVisible &&
                (_lastShakedAt == 0.0 || Environment.TickCount64 - _lastShakedAt >= 25))
            {
                _lastShakedAt = Environment.TickCount64;
            }

            return new ReelControlState(0.0, 0.0, false);
        }

        if (!_casting)
        {
            ResetTrackingState();
            _casting = true;
            _castHolding = false;
            _nextCastChangeTime = 0.0;
        }

        if (now >= _nextCastChangeTime)
        {
            _castHolding = !_castHolding;
            SetHold(_castHolding);
            _nextCastChangeTime = now + 0.2;
        }

        return new ReelControlState(0.0, 0.0, _castHolding);
    }

    public void Reset()
    {
        Release();
        _casting = false;
        _castHolding = false;
        _nextCastChangeTime = 0.0;
        _castStartedAt = 0;
        _castReleasedAt = 0;
        _castBarSeen = false;
        _lastShakedAt = 0.0;
        ResetTrackingState();
    }

    public void Release()
    {
        SetHold(false);
    }

    private void ResetTrackingState()
    {
        _hasLastFrame = false;
        _lastPlayerbarCenter = null;
        _lastFishPos = null;
        _lastTime = 0;
        _prevFishX = 0;
        _prevBarCenter = 0;
        _fishVelocityEma = 0;
        _barVelocityEma = 0;
        _smoothFishX = 0;
        _smoothBarCenter = 0;
        _smoothControl = 0;
        _errorIntegral = 0;
        _centerPulseReleaseUntil = 0;
        _wasInStableZone = false;
        _stableHybridUntil = 0;
        _trackingWarmupUntil = _stopwatch.Elapsed.TotalSeconds + 0.2;
        _lastTrackingActionAt = 0;
    }

    private ReelControlState Compute(ReelMetrics metrics, ReelControlSettings settings)
    {
        var fishPos = metrics.FishCenter;
        var playerbarCenter = metrics.PlayerbarCenter;
        var playerbarWidth = metrics.PlayerbarWidth;

        _lastPlayerbarCenter ??= playerbarCenter;
        _lastFishPos ??= fishPos;
        var playerbarVelocity = playerbarCenter - _lastPlayerbarCenter.Value;
        _lastPlayerbarCenter = playerbarCenter;
        _lastFishPos = fishPos;

        var rawError = fishPos - playerbarCenter;
        var now = _stopwatch.Elapsed.TotalSeconds;

        if (now < _trackingWarmupUntil)
        {
            UpdateFineState(metrics, settings, now);
            var warmupDeadzone = Math.Max(0.015, playerbarWidth * 0.04);
            var warmupHold = rawError > warmupDeadzone;
            return new ReelControlState(rawError, warmupHold ? 1.0 : 0.0, warmupHold);
        }

        if (playerbarCenter < settings.EdgeBoundary)
        {
            UpdateFineState(metrics, settings, now);
            return new ReelControlState(rawError, 1.0, true);
        }

        if (playerbarCenter > 1.0 - settings.EdgeBoundary)
        {
            UpdateFineState(metrics, settings, now);
            return new ReelControlState(rawError, 0.0, false);
        }

        if (settings.EnableHardCorrection && Math.Abs(rawError) > settings.CloseThreshold)
        {
            var predictionScale = settings.PredictionStrength * (1.0 - settings.Resilience);
            var predicted = playerbarCenter + playerbarVelocity * predictionScale;
            var predictedError = fishPos - predicted;
            var sameSideAfterPrediction = rawError * predictedError > 0;
            var approachingTarget = rawError * playerbarVelocity > 0;
            var remainingDistance = Math.Max(0.0, Math.Abs(rawError) - settings.CloseThreshold);
            var brakeLookahead = Math.Abs(playerbarVelocity) * 8.0;
            var needsPreSlow = approachingTarget && brakeLookahead >= remainingDistance;

            if (sameSideAfterPrediction && !needsPreSlow)
            {
                UpdateFineState(metrics, settings, now);
                var hardHold = rawError > 0;
                return new ReelControlState(rawError, hardHold ? 1.0 : 0.0, hardHold);
            }
        }

        return ComputeFine(metrics, settings, now, rawError);
    }

    private void UpdateFineState(ReelMetrics metrics, ReelControlSettings settings, double now)
    {
        var fishX = metrics.FishCenter * 1000.0;
        var barCenter = metrics.PlayerbarCenter * 1000.0;
        var dt = _hasLastFrame ? Math.Max(0.001, now - _lastTime) : 0.016;

        if (!_hasLastFrame)
        {
            _smoothFishX = fishX;
            _smoothBarCenter = barCenter;
            _prevFishX = fishX;
            _prevBarCenter = barCenter;
            _lastTime = now;
            _hasLastFrame = true;
            return;
        }

        var alpha = Clamp(settings.PositionAlpha, 0.2, 0.92);
        _smoothFishX = alpha * fishX + (1.0 - alpha) * _smoothFishX;
        _smoothBarCenter = alpha * barCenter + (1.0 - alpha) * _smoothBarCenter;

        var fishVelocity = (_smoothFishX - _prevFishX) / dt;
        var barVelocity = (_smoothBarCenter - _prevBarCenter) / dt;
        _fishVelocityEma = settings.FishVelAlpha * fishVelocity + (1.0 - settings.FishVelAlpha) * _fishVelocityEma;
        _barVelocityEma = settings.BarVelAlpha * barVelocity + (1.0 - settings.BarVelAlpha) * _barVelocityEma;
        var measuredMaxVelocity = Math.Max(1.0, settings.MaxVelocity * 1000.0);
        _barVelocityEma = Clamp(_barVelocityEma, -measuredMaxVelocity, measuredMaxVelocity);

        _prevFishX = _smoothFishX;
        _prevBarCenter = _smoothBarCenter;
        _lastTime = now;
    }

    private ReelControlState ComputeFine(ReelMetrics metrics, ReelControlSettings settings, double now, double rawError)
    {
        var dt = _hasLastFrame ? Math.Max(0.001, now - _lastTime) : 0.016;
        var fishX = metrics.FishCenter * 1000.0;
        var barCenter = metrics.PlayerbarCenter * 1000.0;
        var barWidth = Math.Max(1.0, metrics.PlayerbarWidth * 1000.0);

        if (!_hasLastFrame)
        {
            _smoothFishX = fishX;
            _smoothBarCenter = barCenter;
            _prevFishX = fishX;
            _prevBarCenter = barCenter;
            _lastTime = now;
            _hasLastFrame = true;
        }
        else
        {
            var alpha = Clamp(settings.PositionAlpha, 0.2, 0.92);
            _smoothFishX = alpha * fishX + (1.0 - alpha) * _smoothFishX;
            _smoothBarCenter = alpha * barCenter + (1.0 - alpha) * _smoothBarCenter;
        }

        var fishVelocity = (_smoothFishX - _prevFishX) / dt;
        var barVelocity = (_smoothBarCenter - _prevBarCenter) / dt;
        _fishVelocityEma = settings.FishVelAlpha * fishVelocity + (1.0 - settings.FishVelAlpha) * _fishVelocityEma;
        _barVelocityEma = settings.BarVelAlpha * barVelocity + (1.0 - settings.BarVelAlpha) * _barVelocityEma;
        var measuredMaxVelocity = Math.Max(1.0, settings.MaxVelocity * 1000.0);
        _barVelocityEma = Clamp(_barVelocityEma, -measuredMaxVelocity, measuredMaxVelocity);
        _prevFishX = _smoothFishX;
        _prevBarCenter = _smoothBarCenter;
        _lastTime = now;

        double error;
        if (settings.UsePrediction)
        {
            var baseError = _smoothFishX - _smoothBarCenter;
            var fishLead = Clamp(_fishVelocityEma * settings.FishPredT, -Math.Max(2.0, barWidth * 0.18), Math.Max(2.0, barWidth * 0.18));
            var holdingForPred = _smoothControl > 0;
            var measuredAcceleration = (holdingForPred ? settings.HoldAcceleration : settings.ReleaseAcceleration) * 1000.0;
            var predictedBarVelocity = Clamp(_barVelocityEma + measuredAcceleration * settings.BarPredT, -measuredMaxVelocity, measuredMaxVelocity);
            var barLead = Clamp(predictedBarVelocity * settings.BarPredT, -Math.Max(2.0, barWidth * 0.12), Math.Max(2.0, barWidth * 0.12));
            var predictedError = _smoothFishX + 0.25 * fishLead - (_smoothBarCenter + 0.175 * barLead);
            error = 0.65 * baseError + 0.35 * predictedError;
        }
        else
        {
            error = _smoothFishX - _smoothBarCenter;
        }

        if (_fishVelocityEma > 0 && error > -barWidth * 0.1)
        {
            error += Clamp(_fishVelocityEma * settings.RightMoveLeadT, 0, Math.Max(2.0, barWidth * 0.22));
        }

        error += Clamp(barWidth * 0.035, 1.5, 5.0);
        _errorIntegral = Clamp(_errorIntegral + error * dt, -settings.IntegralClamp, settings.IntegralClamp);

        var sideMargin = barWidth * settings.BarRatioFromSide;
        var clamp = Math.Max(settings.PdClamp > 0 ? settings.PdClamp : 30.0, settings.OnThreshold + 1.0);
        double rawControl;
        if (_smoothFishX < sideMargin)
        {
            rawControl = -clamp;
        }
        else if (_smoothFishX > 1000.0 - sideMargin)
        {
            rawControl = clamp;
        }
        else
        {
            var relativeVelocity = _fishVelocityEma - _barVelocityEma;
            rawControl = settings.Kp * error + settings.Ki * _errorIntegral + settings.Kd * relativeVelocity;
            if (settings.PdClamp > 0)
            {
                rawControl = Clamp(rawControl, -settings.PdClamp, settings.PdClamp);
            }
        }

        var control = _smoothControl = settings.ControlAlpha * rawControl + (1.0 - settings.ControlAlpha) * _smoothControl;
        return new ReelControlState(rawError, control, DecideHold(settings, now, error, control, barWidth));
    }

    private bool DecideHold(ReelControlSettings settings, double now, double error, double control, double boxLen)
    {
        var centerZone = Math.Max(2.0, boxLen * settings.CenterZoneRatio);
        if (Math.Abs(error) <= centerZone)
        {
            var enteringStable = !_wasInStableZone;
            _wasInStableZone = true;
            if (enteringStable)
            {
                _stableHybridUntil = now + 3.0;
            }

            if (now < _stableHybridUntil)
            {
                if (control > settings.OnThreshold)
                {
                    return true;
                }

                if (control < -settings.OnThreshold)
                {
                    return false;
                }

                return false;
            }

            if (now < _centerPulseReleaseUntil)
            {
                return false;
            }

            if (control > settings.OnThreshold)
            {
                var hold = PositiveModulo(now, settings.CenterPulsePeriodS) < Math.Max(0.001, settings.CenterPulseHoldS);
                if (hold)
                {
                    _centerPulseReleaseUntil = now + Math.Max(0, settings.CenterReleaseBlipS);
                }

                return hold;
            }

            if (control < -settings.OnThreshold)
            {
                return false;
            }

            if (control > 0)
            {
                return PositiveModulo(now, settings.CenterWeakPeriodS) < Math.Max(0.001, settings.CenterWeakHoldS);
            }

            return false;
        }

        _wasInStableZone = false;
        if (control > settings.OnThreshold)
        {
            return true;
        }

        if (control < -settings.OnThreshold)
        {
            return false;
        }

        if (Math.Abs(control) < settings.OffThreshold)
        {
            return false;
        }

        return false;
    }

    private bool TryReleasePerfectCastNearTarget(double targetPower, Func<double?> getPowerBarPercent)
    {
        const int perfectReleaseMicroPollMs = 30;
        const int perfectReleaseMicroPollStepMs = 2;
        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start <= perfectReleaseMicroPollMs)
        {
            var sample = getPowerBarPercent?.Invoke();
            if (sample is { } p && p >= targetPower)
            {
                return true;
            }

            System.Threading.Thread.Sleep(perfectReleaseMicroPollStepMs);
        }

        return false;
    }

    private void SetHold(bool value)
    {
        if (value == _holding)
        {
            return;
        }

        if (value)
        {
            MouseInput.LeftDown();
        }
        else
        {
            MouseInput.LeftUp();
        }

        _holding = value;
    }

    private void SetTrackingHold(bool value, int actionDelayMs)
    {
        if (value == _holding)
        {
            return;
        }

        var now = Environment.TickCount64;
        var minDelayMs = Math.Max(MinTrackingActionDelayMs, actionDelayMs);
        if (_lastTrackingActionAt != 0 && now - _lastTrackingActionAt < minDelayMs)
        {
            return;
        }

        SetHold(value);
        _lastTrackingActionAt = now;
    }

    private static double PositiveModulo(double value, double modulus)
    {
        modulus = Math.Max(0.001, modulus);
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static double Clamp(double value, double min, double max)
    {
        return value < min ? min : value > max ? max : value;
    }
}
