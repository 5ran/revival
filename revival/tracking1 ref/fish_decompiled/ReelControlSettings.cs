internal sealed class ReelControlSettings
{
    public string CastMode { get; set; } = "normal";
    public double CastPowerCustom { get; set; } = 96.0;
    public int CastTimeoutMs { get; set; } = 15000;
    public int PreCastDelayMs { get; set; } = 400;
    public int PostCastDelayMs { get; set; } = 150;
    public bool CastOnTimeout { get; set; } = true;
    public int FishingActionDelayMs { get; set; } = 0;
    public bool FishSkipEnabled { get; set; } = false;
    public bool FishSkipNormal { get; set; } = true;
    public bool FishSkipLegendaryMythic { get; set; } = true;
    public double CompletionThreshold { get; set; } = 99.5;
    public int ShakeIntervalMs { get; set; } = 25;
    public int UpdateRateMs { get; set; } = 10;

    public double EdgeBoundary { get; init; } = 0.1;
    public double CloseThreshold { get; init; } = 0.0055;
    public double PredictionStrength { get; init; } = 13.0;
    public double Resilience { get; init; } = 0.0;
    public bool EnableHardCorrection { get; init; } = true;

    public double Kp { get; init; } = 2.7;
    public double Ki { get; init; } = 0.08;
    public double Kd { get; init; } = 3.6;
    public double PdClamp { get; init; } = 48.0;
    public double IntegralClamp { get; init; } = 130.0;
    public double BarRatioFromSide { get; init; } = 0.6;
    public double CenterZoneRatio { get; init; } = 0.05;
    public double CenterPulsePeriodS { get; init; } = 0.016;
    public double CenterPulseHoldS { get; init; } = 0.013;
    public double CenterReleaseBlipS { get; init; } = 0.006;
    public double CenterWeakPeriodS { get; init; } = 0.017;
    public double CenterWeakHoldS { get; init; } = 0.01;
    public double OnThreshold { get; init; } = 3.5;
    public double OffThreshold { get; init; } = 1.4;
    public bool UsePrediction { get; init; } = false;
    public double FishPredT { get; init; } = 0.23;
    public double BarPredT { get; init; } = 0.045;
    public double RightMoveLeadT { get; init; } = 0.16;
    public double HoldAcceleration { get; init; } = 0.62;
    public double ReleaseAcceleration { get; init; } = -0.3;
    public double MaxVelocity { get; init; } = 1.05;
    public double FishVelAlpha { get; init; } = 0.9;
    public double BarVelAlpha { get; init; } = 0.86;
    public double PositionAlpha { get; init; } = 0.98;
    public double ControlAlpha { get; init; } = 0.99;
}
