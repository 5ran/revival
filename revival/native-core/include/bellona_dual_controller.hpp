#pragma once

#include <cstdint>
#include <optional>

#include "reel_metrics.hpp"

namespace macro_port
{
struct BellonaDualSettings
{
    int pressDebounceMs = 30;
    int lossGraceMs = 220;
    int staleWatchdogMs = 700;
    int hybridHoldBiasMs = 35;
    double motionDeltaThreshold = 0.0015;
    bool useHybridHoldBias = false;
};

class BellonaDualController
{
public:
    explicit BellonaDualController(BellonaDualSettings settings = {});

    void Reset();
    bool Filter(
        bool desiredHoldingRaw,
        const std::optional<ReelMetrics>& rightMetrics,
        std::int64_t nowMs,
        bool modeTracking3Like);
    bool RightHolding() const { return rightHolding_; }

private:
    bool ApplyHybridHoldBias(bool desiredHolding, const ReelMetrics& metrics, std::int64_t nowMs) const;
    bool SmoothPressOnly(bool desiredHolding, std::int64_t nowMs);
    void UpdateMotionWatchdog(const ReelMetrics& metrics, std::int64_t nowMs);

    BellonaDualSettings settings_{};
    bool rightHolding_ = false;
    bool rawDesiredHolding_ = false;
    std::int64_t rawDesiredChangedAt_ = 0;
    std::int64_t lastMetricsSeenAt_ = 0;
    std::int64_t rightHybridHoldUntilAt_ = 0;
    std::optional<double> lastFishCenter_;
    std::optional<double> lastBarCenter_;
    std::int64_t staleSinceAt_ = 0;
};
}
