#include "bellona_dual_controller.hpp"

#include <cmath>

namespace macro_port
{
BellonaDualController::BellonaDualController(BellonaDualSettings settings) : settings_(settings)
{
}

void BellonaDualController::Reset()
{
    rightHolding_ = false;
    rawDesiredHolding_ = false;
    rawDesiredChangedAt_ = 0;
    lastMetricsSeenAt_ = 0;
    rightHybridHoldUntilAt_ = 0;
    lastFishCenter_.reset();
    lastBarCenter_.reset();
    staleSinceAt_ = 0;
}

bool BellonaDualController::Filter(
    bool desiredHoldingRaw,
    const std::optional<ReelMetrics>& rightMetrics,
    std::int64_t nowMs,
    bool modeTracking3Like)
{
    if (!rightMetrics.has_value())
    {
        if (lastMetricsSeenAt_ != 0 && nowMs - lastMetricsSeenAt_ <= settings_.lossGraceMs)
        {
            return rightHolding_;
        }

        Reset();
        return false;
    }

    lastMetricsSeenAt_ = nowMs;
    UpdateMotionWatchdog(*rightMetrics, nowMs);

    bool desired = desiredHoldingRaw;
    if (modeTracking3Like || settings_.useHybridHoldBias)
    {
        desired = ApplyHybridHoldBias(desired, *rightMetrics, nowMs);
    }

    if (modeTracking3Like)
    {
        rightHolding_ = desired;
        return desired;
    }

    rightHolding_ = SmoothPressOnly(desired, nowMs);
    return rightHolding_;
}

bool BellonaDualController::ApplyHybridHoldBias(bool desiredHolding, const ReelMetrics& metrics, std::int64_t nowMs) const
{
    if (desiredHolding)
    {
        const_cast<BellonaDualController*>(this)->rightHybridHoldUntilAt_ = nowMs + settings_.hybridHoldBiasMs;
        return true;
    }

    if (rightHolding_ &&
        nowMs < rightHybridHoldUntilAt_ &&
        std::abs(metrics.fishCenter - metrics.playerbarCenter) <= 0.022)
    {
        return true;
    }

    return false;
}

bool BellonaDualController::SmoothPressOnly(bool desiredHolding, std::int64_t nowMs)
{
    if (desiredHolding != rawDesiredHolding_)
    {
        rawDesiredHolding_ = desiredHolding;
        rawDesiredChangedAt_ = nowMs;
        return rightHolding_;
    }

    if (desiredHolding == rightHolding_)
    {
        return desiredHolding;
    }

    if (!desiredHolding)
    {
        return false;
    }

    if (nowMs - rawDesiredChangedAt_ < settings_.pressDebounceMs)
    {
        return rightHolding_;
    }

    return true;
}

void BellonaDualController::UpdateMotionWatchdog(const ReelMetrics& metrics, std::int64_t nowMs)
{
    const bool moved = !lastFishCenter_.has_value() || !lastBarCenter_.has_value() ||
        std::abs(metrics.fishCenter - *lastFishCenter_) >= settings_.motionDeltaThreshold ||
        std::abs(metrics.playerbarCenter - *lastBarCenter_) >= settings_.motionDeltaThreshold;

    if (moved)
    {
        staleSinceAt_ = 0;
    }
    else if (staleSinceAt_ == 0)
    {
        staleSinceAt_ = nowMs;
    }

    if (staleSinceAt_ != 0 && nowMs - staleSinceAt_ >= settings_.staleWatchdogMs)
    {
        staleSinceAt_ = nowMs;
    }

    lastFishCenter_ = metrics.fishCenter;
    lastBarCenter_ = metrics.playerbarCenter;
}
}
