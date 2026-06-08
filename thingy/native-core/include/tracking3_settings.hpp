#pragma once

#include <string>

namespace macro_port
{
struct Tracking3Settings
{
    std::string castMode = "perfect";
    double castPowerCustom = 96.0;
    int castTimeoutMs = 15000;
    int preCastDelayMs = 400;
    int postCastDelayMs = 150;
    bool castOnTimeout = true;
    int fishingActionDelayMs = 0;
    double completionThreshold = 99.5;
    int shakeIntervalMs = 25;
    int updateRateMs = 10;

    double edgeBoundary = 0.1;
    double closeThreshold = 0.0055;
    double predictionStrength = 13.0;
    double resilience = 0.0;
    bool enableHardCorrection = true;

    double kp = 2.7;
    double ki = 0.08;
    double kd = 3.6;
    double pdClamp = 48.0;
    double integralClamp = 130.0;
    double barRatioFromSide = 0.6;
    double centerZoneRatio = 0.05;
    double centerPulsePeriodS = 0.016;
    double centerPulseHoldS = 0.013;
    double centerReleaseBlipS = 0.006;
    double centerWeakPeriodS = 0.017;
    double centerWeakHoldS = 0.01;
    double onThreshold = 3.5;
    double offThreshold = 1.4;
    bool usePrediction = false;
    double fishPredT = 0.23;
    double barPredT = 0.045;
    double rightMoveLeadT = 0.16;
    double holdAcceleration = 0.62;
    double releaseAcceleration = -0.3;
    double maxVelocity = 1.05;
    double fishVelAlpha = 0.9;
    double barVelAlpha = 0.86;
    double positionAlpha = 0.98;
    double controlAlpha = 0.99;
};
}

