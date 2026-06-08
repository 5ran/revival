#include "tracking2_controller.hpp"

#include <cmath>

namespace macro_port
{
ReelDecision Tracking2Controller::Update(const ReelMetrics& metrics, const Tracking2Settings& settings)
{
    const auto fishPos = metrics.fishCenter;
    const auto playerbarPos = metrics.playerbarCenter;

    if (!lastPlayerbarPos_.has_value()) lastPlayerbarPos_ = playerbarPos;
    if (!lastFishPos_.has_value()) lastFishPos_ = fishPos;

    const auto playerbarVelocity = playerbarPos - *lastPlayerbarPos_;
    const auto fishVelocity = fishPos - *lastFishPos_;
    lastPlayerbarPos_ = playerbarPos;
    lastFishPos_ = fishPos;

    const auto error = fishPos - playerbarPos;
    if (playerbarPos < settings.edgeBoundary)
    {
        return ReelDecision{error, 1.0, true};
    }

    if (playerbarPos > 1.0 - settings.edgeBoundary)
    {
        return ReelDecision{error, 0.0, false};
    }

    const auto predictionScale = settings.predictionStrength * (1.0 - settings.resilience);
    const auto predicted = playerbarPos + playerbarVelocity * predictionScale;
    const auto predictedError = fishPos - predicted;
    const auto sameSideAfterPrediction = error * predictedError > 0;
    const auto approachingTarget = error * playerbarVelocity > 0;
    const auto remainingDistance = std::max(0.0, std::abs(error) - settings.closeThreshold);
    const auto brakeLookahead = std::abs(playerbarVelocity) * 8.0;
    const auto needsPreSlow = approachingTarget && brakeLookahead >= remainingDistance;

    if (std::abs(error) > settings.closeThreshold && sameSideAfterPrediction && !needsPreSlow)
    {
        return error > 0
            ? ReelDecision{error, 1.0, true}
            : ReelDecision{error, 0.0, false};
    }

    double targetDuty;
    if (needsPreSlow && brakeLookahead > 0)
    {
        const auto brakeUrgency = 1.0 - std::min(1.0, remainingDistance / brakeLookahead);
        targetDuty = error > 0
            ? settings.neutralDutyCycle * (1.0 - brakeUrgency)
            : settings.neutralDutyCycle + (1.0 - settings.neutralDutyCycle) * brakeUrgency;
    }
    else
    {
        const auto adjustment = settings.proportionalGain * error +
            settings.derivativeGain * fishVelocity -
            settings.velocityDamping * playerbarVelocity;
        targetDuty = Clamp(settings.neutralDutyCycle + adjustment, 0.0, 1.0);
    }

    pwmAccumulator_ += targetDuty;
    if (pwmAccumulator_ >= 1.0)
    {
        pwmAccumulator_ -= 1.0;
        return ReelDecision{error, targetDuty, true};
    }

    return ReelDecision{error, targetDuty, false};
}

void Tracking2Controller::Reset()
{
    lastPlayerbarPos_.reset();
    lastFishPos_.reset();
    pwmAccumulator_ = 0.0;
}

double Tracking2Controller::Clamp(double value, double min, double max)
{
    return value < min ? min : value > max ? max : value;
}
}

