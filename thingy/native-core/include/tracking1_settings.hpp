#pragma once

namespace macro_port
{
struct Tracking1Settings
{
    double kp = 1.25;
    double ki = 0.06;
    double kd = 1.85;
    double pdClamp = 34.0;
    double integralClamp = 130.0;
    double barRatioFromSide = 0.6;
    double centerZoneRatio = 0.05;
    double centerPulsePeriodS = 0.024;
    double centerPulseHoldS = 0.1;
    double centerReleaseBlipS = 0.006;
    double centerWeakPeriodS = 0.026;
    double centerWeakHoldS = 0.006;
    double onThreshold = 7.0;
    double offThreshold = 3.5;
    bool usePrediction = true;
    double fishPredT = 0.165;
    double barPredT = 0.028;
    double rightMoveLeadT = 0.085;
    double fishVelAlpha = 0.82;
    double barVelAlpha = 0.78;
    double positionAlpha = 0.9;
    double controlAlpha = 0.94;
};
}

