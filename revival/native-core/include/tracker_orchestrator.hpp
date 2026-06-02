#pragma once

#include <cstdint>
#include <optional>
#include <string>

#include "fishing_phase.hpp"

namespace macro_port
{
struct TrackerOrchestratorSettings
{
    std::int64_t preCastDelayMs = 250;
    std::int64_t postCastDelayMs = 250;
    std::int64_t castTimeoutMs = 5000;
    std::int64_t shakeIntervalMs = 100;
    std::int64_t fishingLostGraceMs = 100;
    double perfectCastTargetPercent = 96.0;
    double perfectCastNearWindowPercent = 1.0;
    std::int64_t perfectReleaseMicroPollMs = 14;
    bool castOnTimeout = true;
    double completionThreshold = 99.5;
    std::int64_t reelInputSettleMs = 125;
    std::int64_t reelReacquireGraceMs = 2500;
};

struct TrackerTickInput
{
    std::int64_t nowMs = 0;
    bool suspended = false;
    bool reelVisible = false;
    bool shakeVisible = false;
    bool hasMetrics = false;
    bool completionReached = false;
    std::optional<double> progressPercent;
    bool castPowerReady = true;
    bool perfectCastRelease = false;
    std::optional<double> castPowerPercent;
};

struct TrackerTickOutput
{
    FishingPhase phase = FishingPhase::Off;
    bool holdLeft = false;
    bool clickShake = false;
    bool resetCycle = false;
    std::string message;
};

class TrackerOrchestrator
{
public:
    explicit TrackerOrchestrator(TrackerOrchestratorSettings settings = {});

    void Start(std::int64_t nowMs, FishingCastingMode mode);
    void Stop();
    void Suspend(const std::string& message);
    void Resume();

    TrackerTickOutput Tick(const TrackerTickInput& input);
    FishingPhase Phase() const { return phase_; }

private:
    TrackerTickOutput TickCasting(const TrackerTickInput& input);
    TrackerTickOutput TickCasted(const TrackerTickInput& input);
    TrackerTickOutput TickShake(const TrackerTickInput& input);
    TrackerTickOutput TickFishing(const TrackerTickInput& input);
    TrackerTickOutput ResetToCasting(const char* message, std::int64_t nowMs);

    TrackerOrchestratorSettings settings_;
    FishingCastingMode castingMode_ = FishingCastingMode::Normal;
    FishingPhase phase_ = FishingPhase::Off;
    std::string suspendMessage_;

    bool running_ = false;
    bool holdLeft_ = false;
    std::int64_t castStartedAt_ = 0;
    std::int64_t castReleasedAt_ = 0;
    std::int64_t lastShakedAt_ = 0;
    std::int64_t fishingLostAt_ = 0;
    std::int64_t perfectNearTargetSince_ = 0;
    bool normalCastHolding_ = false;
    std::int64_t normalCastNextChangeAt_ = 0;
    std::int64_t normalCastHoldStartedAt_ = 0;
    bool seenReelThisRun_ = false;
    std::int64_t fishingInputReadyAt_ = 0;
};
}
