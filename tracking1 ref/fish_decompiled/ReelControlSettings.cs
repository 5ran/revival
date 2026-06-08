internal sealed class ReelControlSettings
{
	public double Kp = 1.25;

	public double Ki = 0.06;

	public double Kd = 1.85;

	public double PdClamp = 34.0;

	public double IntegralClamp = 130.0;

	public double BarRatioFromSide = 0.6;

	public double CenterZoneRatio = 0.05;

	public double CenterPulsePeriodS = 0.024;

	public double CenterPulseHoldS = 0.1;

	public double CenterReleaseBlipS = 0.006;

	public double CenterWeakPeriodS = 0.026;

	public double CenterWeakHoldS = 0.006;

	public double OnThreshold = 7.0;

	public double OffThreshold = 3.5;

	public bool UsePrediction = true;

	public double FishPredT = 0.165;

	public double BarPredT = 0.028;

	public double RightMoveLeadT = 0.085;

	public double FishVelAlpha = 0.82;

	public double BarVelAlpha = 0.78;

	public double PositionAlpha = 0.9;

	public double ControlAlpha = 0.94;
}
