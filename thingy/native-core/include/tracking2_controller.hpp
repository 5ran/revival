#pragma once

#include <optional>

#include "reel_metrics.hpp"
#include "tracking1_controller.hpp"
#include "tracking2_settings.hpp"

namespace macro_port
{
class Tracking2Controller
{
public:
    ReelDecision Update(const ReelMetrics& metrics, const Tracking2Settings& settings);
    void Reset();

private:
    static double Clamp(double value, double min, double max);

    std::optional<double> lastPlayerbarPos_;
    std::optional<double> lastFishPos_;
    double pwmAccumulator_ = 0.0;
};
}

