#include "tracking3_controller.hpp"

#include <algorithm>
#include <cmath>

namespace macro_port
{
Tracking3Decision Tracking3Controller::Update(const ReelMetrics& metrics, const Tracking3Settings& settings, double now)
{
    const auto relNow = now - clockStartSeconds_;
    const auto fishPos = metrics.fishCenter;
    const auto playerbarCenter = metrics.playerbarCenter;
    const auto playerbarWidth = metrics.playerbarWidth;

    if (!lastPlayerbarCenter_.has_value()) lastPlayerbarCenter_ = playerbarCenter;
    if (!lastFishPos_.has_value()) lastFishPos_ = fishPos;
    const auto playerbarVelocity = playerbarCenter - *lastPlayerbarCenter_;
    lastPlayerbarCenter_ = playerbarCenter;
    lastFishPos_ = fishPos;

    const auto error = fishPos - playerbarCenter;

    if (relNow < trackingWarmupUntil_)
    {
        UpdateFineState(metrics, settings, relNow);
        const auto warmupDeadzone = std::max(0.015, playerbarWidth * 0.04);
        bool warmupHold;
        if (error > warmupDeadzone)
        {
            warmupHold = true;
        }
        else if (error < -warmupDeadzone)
        {
            warmupHold = false;
        }
        else
        {
            warmupHold = false;
        }
        return Tracking3Decision{error, warmupHold ? 1.0 : 0.0, warmupHold, Tracking3DecisionMode::Warmup};
    }

    if (playerbarCenter < settings.edgeBoundary)
    {
        UpdateFineState(metrics, settings, relNow);
        return Tracking3Decision{error, 1.0, true, Tracking3DecisionMode::EdgeRecovery};
    }
    if (playerbarCenter > 1.0 - settings.edgeBoundary)
    {
        UpdateFineState(metrics, settings, relNow);
        return Tracking3Decision{error, 0.0, false, Tracking3DecisionMode::EdgeRecovery};
    }

    if (settings.enableHardCorrection && std::abs(error) > settings.closeThreshold)
    {
        const auto predictionScale = settings.predictionStrength * (1.0 - settings.resilience);
        const auto predicted = playerbarCenter + playerbarVelocity * predictionScale;
        const auto predictedError = fishPos - predicted;
        const auto sameSideAfterPrediction = error * predictedError > 0;
        const auto approachingTarget = error * playerbarVelocity > 0;
        const auto remainingDistance = std::max(0.0, std::abs(error) - settings.closeThreshold);
        const auto brakeLookahead = std::abs(playerbarVelocity) * 8.0;
        const auto needsPreSlow = approachingTarget && brakeLookahead >= remainingDistance;

        if (sameSideAfterPrediction && !needsPreSlow)
        {
            UpdateFineState(metrics, settings, relNow);
            const auto hardHold = error > 0;
            return Tracking3Decision{error, hardHold ? 1.0 : 0.0, hardHold, Tracking3DecisionMode::HardCorrection};
        }
    }

    return ComputeFine(metrics, settings, relNow, error);
}

void Tracking3Controller::Reset(double nowSeconds)
{
    lastPlayerbarCenter_.reset();
    lastFishPos_.reset();
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
    wasInStableZone_ = false;
    stableHybridUntil_ = 0.0;
    clockStartSeconds_ = nowSeconds;
    trackingWarmupUntil_ = 0.2;
}

void Tracking3Controller::UpdateFineState(const ReelMetrics& metrics, const Tracking3Settings& settings, double now)
{
    const auto fishX = metrics.fishCenter * 1000.0;
    const auto barCenter = metrics.playerbarCenter * 1000.0;
    const auto dt = hasLastFrame_ ? std::max(0.001, now - lastTime_) : 0.016;

    if (!hasLastFrame_)
    {
        smoothFishX_ = fishX;
        smoothBarCenter_ = barCenter;
        prevFishX_ = fishX;
        prevBarCenter_ = barCenter;
        lastTime_ = now;
        hasLastFrame_ = true;
        return;
    }

    const auto alpha = Clamp(settings.positionAlpha, 0.2, 0.92);
    smoothFishX_ = alpha * fishX + (1.0 - alpha) * smoothFishX_;
    smoothBarCenter_ = alpha * barCenter + (1.0 - alpha) * smoothBarCenter_;

    const auto fishVelocity = (smoothFishX_ - prevFishX_) / dt;
    const auto barVelocity = (smoothBarCenter_ - prevBarCenter_) / dt;
    fishVelocityEma_ = settings.fishVelAlpha * fishVelocity + (1.0 - settings.fishVelAlpha) * fishVelocityEma_;
    barVelocityEma_ = settings.barVelAlpha * barVelocity + (1.0 - settings.barVelAlpha) * barVelocityEma_;
    const auto measuredMaxVelocity = std::max(1.0, settings.maxVelocity * 1000.0);
    barVelocityEma_ = Clamp(barVelocityEma_, -measuredMaxVelocity, measuredMaxVelocity);

    prevFishX_ = smoothFishX_;
    prevBarCenter_ = smoothBarCenter_;
    lastTime_ = now;
}

Tracking3Decision Tracking3Controller::ComputeFine(const ReelMetrics& metrics, const Tracking3Settings& settings, double now, double rawError)
{
    const auto fishX = metrics.fishCenter * 1000.0;
    const auto barCenter = metrics.playerbarCenter * 1000.0;
    const auto barWidth = std::max(1.0, metrics.playerbarWidth * 1000.0);
    const auto dt = hasLastFrame_ ? std::max(0.001, now - lastTime_) : 0.016;

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

    const auto fishVelocityRaw = (smoothFishX_ - prevFishX_) / dt;
    const auto barVelocityRaw = (smoothBarCenter_ - prevBarCenter_) / dt;
    fishVelocityEma_ = settings.fishVelAlpha * fishVelocityRaw + (1.0 - settings.fishVelAlpha) * fishVelocityEma_;
    barVelocityEma_ = settings.barVelAlpha * barVelocityRaw + (1.0 - settings.barVelAlpha) * barVelocityEma_;
    const auto measuredMaxVelocity = std::max(1.0, settings.maxVelocity * 1000.0);
    barVelocityEma_ = Clamp(barVelocityEma_, -measuredMaxVelocity, measuredMaxVelocity);

    prevFishX_ = smoothFishX_;
    prevBarCenter_ = smoothBarCenter_;
    lastTime_ = now;

    double error;
    if (settings.usePrediction)
    {
        const auto baseError = smoothFishX_ - smoothBarCenter_;
        const auto fishLead = Clamp(fishVelocityEma_ * settings.fishPredT, -std::max(2.0, barWidth * 0.18), std::max(2.0, barWidth * 0.18));
        const auto holdingForPred = smoothControl_ > 0;
        const auto measuredAcceleration = (holdingForPred ? settings.holdAcceleration : settings.releaseAcceleration) * 1000.0;
        const auto predictedBarVelocity = Clamp(barVelocityEma_ + measuredAcceleration * settings.barPredT, -measuredMaxVelocity, measuredMaxVelocity);
        const auto barLead = Clamp(predictedBarVelocity * settings.barPredT, -std::max(2.0, barWidth * 0.12), std::max(2.0, barWidth * 0.12));
        const auto predictedError = smoothFishX_ + 0.25 * fishLead - (smoothBarCenter_ + 0.175 * barLead);
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
    const auto clamp = std::max(settings.pdClamp > 0 ? settings.pdClamp : 30.0, settings.onThreshold + 1.0);
    double rawControl;
    if (smoothFishX_ < sideMargin)
    {
        rawControl = -clamp;
    }
    else if (smoothFishX_ > 1000.0 - sideMargin)
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

    const auto centerZone = std::max(2.0, barWidth * settings.centerZoneRatio);
    bool desiredHolding;
    Tracking3DecisionMode mode;

    if (std::abs(error) <= centerZone)
    {
        const auto enteringStable = !wasInStableZone_;
        wasInStableZone_ = true;
        if (enteringStable)
        {
            stableHybridUntil_ = now + 3.0;
        }

        if (now < stableHybridUntil_)
        {
            mode = Tracking3DecisionMode::FineTracking;
            if (control > settings.onThreshold)
            {
                desiredHolding = true;
            }
            else if (control < -settings.onThreshold)
            {
                desiredHolding = false;
            }
            else
            {
                desiredHolding = std::abs(control) >= settings.offThreshold && false;
            }
        }
        else
        {
            mode = Tracking3DecisionMode::CenterPulse;
            if (now < centerPulseReleaseUntil_)
            {
                desiredHolding = false;
            }
            else if (control > settings.onThreshold)
            {
                const auto hold = PositiveModulo(now, settings.centerPulsePeriodS) < std::max(0.001, settings.centerPulseHoldS);
                if (hold)
                {
                    centerPulseReleaseUntil_ = now + std::max(0.0, settings.centerReleaseBlipS);
                }
                desiredHolding = hold;
            }
            else if (control < -settings.onThreshold)
            {
                desiredHolding = false;
            }
            else
            {
                desiredHolding = control > 0 && PositiveModulo(now, settings.centerWeakPeriodS) < std::max(0.001, settings.centerWeakHoldS);
            }
        }
    }
    else
    {
        wasInStableZone_ = false;
        mode = Tracking3DecisionMode::FineTracking;
        if (control > settings.onThreshold)
        {
            desiredHolding = true;
        }
        else if (control < -settings.onThreshold)
        {
            desiredHolding = false;
        }
        else
        {
            desiredHolding = std::abs(control) >= settings.offThreshold && false;
        }
    }

    return Tracking3Decision{rawError, control, desiredHolding, mode};
}

double Tracking3Controller::PositiveModulo(double value, double modulus)
{
    modulus = std::max(0.001, modulus);
    auto result = std::fmod(value, modulus);
    return result < 0 ? result + modulus : result;
}

double Tracking3Controller::Clamp(double value, double min, double max)
{
    return value < min ? min : value > max ? max : value;
}
}
