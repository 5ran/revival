#pragma once

#include <optional>

#include "reel_metrics.hpp"
#include "tracking3_settings.hpp"

namespace macro_port
{
enum class Tracking3DecisionMode
{
    Warmup,
    EdgeRecovery,
    HardCorrection,
    FineTracking,
    CenterPulse,
};

struct Tracking3Decision
{
    double error = 0.0;
    double control = 0.0;
    bool desiredHolding = false;
    Tracking3DecisionMode mode = Tracking3DecisionMode::Warmup;
};

class Tracking3Controller
{
public:
    Tracking3Decision Update(const ReelMetrics& metrics, const Tracking3Settings& settings, double nowSeconds);
    void Reset(double nowSeconds = 0.0);

private:
    void UpdateFineState(const ReelMetrics& metrics, const Tracking3Settings& settings, double now);
    Tracking3Decision ComputeFine(const ReelMetrics& metrics, const Tracking3Settings& settings, double now, double rawError);

    static double PositiveModulo(double value, double modulus);
    static double Clamp(double value, double min, double max);

    std::optional<double> lastPlayerbarCenter_;
    std::optional<double> lastFishPos_;

    bool hasLastFrame_ = false;
    double lastTime_ = 0.0;
    double prevFishX_ = 0.0;
    double prevBarCenter_ = 0.0;
    double fishVelocityEma_ = 0.0;
    double barVelocityEma_ = 0.0;
    double smoothFishX_ = 0.0;
    double smoothBarCenter_ = 0.0;
    double smoothControl_ = 0.0;
    double errorIntegral_ = 0.0;
    double centerPulseReleaseUntil_ = 0.0;
    bool wasInStableZone_ = false;
    double stableHybridUntil_ = 0.0;
    double trackingWarmupUntil_ = 0.0;
    double clockStartSeconds_ = 0.0;
};
}
