#pragma once

#include "reel_metrics.hpp"
#include "tracking1_settings.hpp"

namespace macro_port
{
struct ReelDecision
{
    double error = 0.0;
    double control = 0.0;
    bool holding = false;
};

class Tracking1Controller
{
public:
    bool IsHolding() const { return holding_; }
    void SetPredictionWarmupSeconds(double seconds);
    ReelDecision UpdateTracking(const ReelMetrics& metrics, const Tracking1Settings& settings, double nowSeconds);
    ReelDecision UpdateCasting(double nowSeconds);
    void Reset();
    void Release();

private:
    ReelDecision Compute(const ReelMetrics& metrics, const Tracking1Settings& settings, double nowSeconds);
    bool DecideHold(const Tracking1Settings& settings, double now, double error, double control, double boxLen);
    void SetHold(bool value);
    void ResetTrackingState();

    static double Clamp(double value, double min, double max);
    static double PositiveModulo(double value, double modulus);

    bool holding_ = false;
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
    bool casting_ = false;
    bool castHolding_ = false;
    double nextCastChangeTime_ = 0.0;
    bool clockInitialized_ = false;
    double clockStartSeconds_ = 0.0;
};
}
