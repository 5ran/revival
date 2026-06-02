#pragma once

#include <string>

namespace macro_port
{
struct Tracking2Settings
{
    double closeThreshold = 0.01;
    double derivativeGain = 0.55;
    double edgeBoundary = 0.1;
    double neutralDutyCycle = 0.5;
    double predictionStrength = 7.5;
    double proportionalGain = 0.42;
    double resilience = 0.0;
    int updateRateMs = 21;
    double velocityDamping = 38.0;
    std::string castMode = "perfect";
    double castPowerCustom = 96.0;
    int castTimeoutMs = 15000;
    int preCastDelayMs = 0;
    int postCastDelayMs = 300;
    bool castOnTimeout = true;
    int fishingActionDelayMs = 0;
    double completionThreshold = 99.5;
    int shakeIntervalMs = 25;
};
}

