#include "tracking1_controller.hpp"

#include <algorithm>
#include <cmath>

namespace macro_port
{
void Tracking1Controller::SetPredictionWarmupSeconds(double seconds)
{
    (void)seconds;
}

ReelDecision Tracking1Controller::UpdateTracking(const ReelMetrics& metrics, const Tracking1Settings& settings, double nowSeconds)
{
    if (!clockInitialized_)
    {
        clockInitialized_ = true;
        clockStartSeconds_ = nowSeconds;
    }
    const auto relNow = nowSeconds - clockStartSeconds_;

    if (casting_)
    {
        casting_ = false;
        castHolding_ = false;
        nextCastChangeTime_ = 0.0;
        Release();
        ResetTrackingState();
    }

    auto state = Compute(metrics, settings, relNow);
    SetHold(state.holding);
    return state;
}

ReelDecision Tracking1Controller::UpdateCasting(double nowSeconds)
{
    if (!clockInitialized_)
    {
        clockInitialized_ = true;
        clockStartSeconds_ = nowSeconds;
    }
    const auto relNow = nowSeconds - clockStartSeconds_;

    if (!casting_)
    {
        ResetTrackingState();
        casting_ = true;
        castHolding_ = false;
        nextCastChangeTime_ = 0.0;
    }

    if (relNow >= nextCastChangeTime_)
    {
        castHolding_ = !castHolding_;
        SetHold(castHolding_);
        nextCastChangeTime_ = relNow + 0.2;
    }

    return ReelDecision{0.0, 0.0, castHolding_};
}

void Tracking1Controller::Reset()
{
    Release();
    casting_ = false;
    castHolding_ = false;
    nextCastChangeTime_ = 0.0;
    clockInitialized_ = false;
    clockStartSeconds_ = 0.0;
    ResetTrackingState();
}

void Tracking1Controller::Release()
{
    SetHold(false);
}

void Tracking1Controller::ResetTrackingState()
{
    hasLastFrame_ = false;
    lastTime_ = 0.0;
    prevFishX_ = 0.0;
    prevBarCenter_ = 0.0;
    fishVelocityEma_ = 0.0;
    barVelocityEma_ = 0.0;
    smoothFishX_ = 0.0;
    smoothBarCenter_ = 0.0;
    smoothControl_ = 0.0;
    errorIntegral_ = 0.0;
    centerPulseReleaseUntil_ = 0.0;
}

ReelDecision Tracking1Controller::Compute(const ReelMetrics& metrics, const Tracking1Settings& settings, double now)
{
    const auto dt = hasLastFrame_ ? std::max(0.001, now - lastTime_) : 0.016;
    const auto fishX = metrics.fishCenter * 1000.0;
    const auto barCenter = metrics.playerbarCenter * 1000.0;
    const auto barWidth = std::max(1.0, metrics.playerbarWidth * 1000.0);

    if (!hasLastFrame_)
    {
        smoothFishX_ = fishX;
        smoothBarCenter_ = barCenter;
        prevFishX_ = fishX;
        prevBarCenter_ = barCenter;
        lastTime_ = now;
        hasLastFrame_ = true;
    }
    else
    {
        const auto alpha = Clamp(settings.positionAlpha, 0.2, 0.92);
        smoothFishX_ = alpha * fishX + (1.0 - alpha) * smoothFishX_;
        smoothBarCenter_ = alpha * barCenter + (1.0 - alpha) * smoothBarCenter_;
    }

    const auto fishVelocity = (smoothFishX_ - prevFishX_) / dt;
    const auto barVelocity = (smoothBarCenter_ - prevBarCenter_) / dt;
    fishVelocityEma_ = settings.fishVelAlpha * fishVelocity + (1.0 - settings.fishVelAlpha) * fishVelocityEma_;
    barVelocityEma_ = settings.barVelAlpha * barVelocity + (1.0 - settings.barVelAlpha) * barVelocityEma_;
    prevFishX_ = smoothFishX_;
    prevBarCenter_ = smoothBarCenter_;
    lastTime_ = now;

    double error;
    if (settings.usePrediction)
    {
        const auto baseError = smoothFishX_ - smoothBarCenter_;
        const auto fishLead = Clamp(fishVelocityEma_ * settings.fishPredT, -std::max(2.0, barWidth * 0.18), std::max(2.0, barWidth * 0.18));
        const auto barLead = Clamp(barVelocityEma_ * settings.barPredT, -std::max(2.0, barWidth * 0.12), std::max(2.0, barWidth * 0.12));
        const auto predictedError = smoothFishX_ + 0.5 * fishLead - (smoothBarCenter_ + 0.35 * barLead);
        error = 0.65 * baseError + 0.35 * predictedError;
    }
    else
    {
        error = smoothFishX_ - smoothBarCenter_;
    }

    if (fishVelocityEma_ > 0 && error > -barWidth * 0.1)
    {
        error += Clamp(fishVelocityEma_ * settings.rightMoveLeadT, 0, std::max(2.0, barWidth * 0.22));
    }

    error += Clamp(barWidth * 0.035, 1.5, 5.0);
    errorIntegral_ = Clamp(errorIntegral_ + error * dt, -settings.integralClamp, settings.integralClamp);

    const auto sideMargin = barWidth * settings.barRatioFromSide;
    const auto fishScaled = smoothFishX_;
    const auto clamp = std::max(settings.pdClamp > 0 ? settings.pdClamp : 30.0, settings.onThreshold + 1.0);

    double rawControl;
    if (fishScaled < sideMargin)
    {
        rawControl = -clamp;
    }
    else if (fishScaled > 1000.0 - sideMargin)
    {
        rawControl = clamp;
    }
    else
    {
        const auto relativeVelocity = fishVelocityEma_ - barVelocityEma_;
        rawControl = settings.kp * error + settings.ki * errorIntegral_ + settings.kd * relativeVelocity;
        if (settings.pdClamp > 0)
        {
            rawControl = Clamp(rawControl, -settings.pdClamp, settings.pdClamp);
        }
    }

    const auto control = smoothControl_ = settings.controlAlpha * rawControl + (1.0 - settings.controlAlpha) * smoothControl_;
    return ReelDecision{error, control, DecideHold(settings, now, error, control, barWidth)};
}

bool Tracking1Controller::DecideHold(const Tracking1Settings& settings, double now, double error, double control, double boxLen)
{
    const auto centerZone = std::max(2.0, boxLen * settings.centerZoneRatio);
    if (std::abs(error) <= centerZone)
    {
        if (now < centerPulseReleaseUntil_)
        {
            return false;
        }

        if (control > settings.onThreshold)
        {
            const auto hold = PositiveModulo(now, settings.centerPulsePeriodS) < std::max(0.001, settings.centerPulseHoldS);
            if (hold)
            {
                centerPulseReleaseUntil_ = now + std::max(0.0, settings.centerReleaseBlipS);
            }

            return hold;
        }

        if (control < -settings.onThreshold)
        {
            return false;
        }

        if (control > 0)
        {
            return PositiveModulo(now, settings.centerWeakPeriodS) < std::max(0.001, settings.centerWeakHoldS);
        }

        return false;
    }

    if (control > settings.onThreshold)
    {
        return true;
    }

    if (control < -settings.onThreshold)
    {
        return false;
    }

    if (std::abs(control) < settings.offThreshold)
    {
        return false;
    }

    return false;
}

void Tracking1Controller::SetHold(bool value)
{
    if (value == holding_)
    {
        return;
    }
    holding_ = value;
}

double Tracking1Controller::Clamp(double value, double min, double max)
{
    return value < min ? min : value > max ? max : value;
}

double Tracking1Controller::PositiveModulo(double value, double modulus)
{
    modulus = std::max(0.001, modulus);
    auto result = std::fmod(value, modulus);
    return result < 0 ? result + modulus : result;
}
}
