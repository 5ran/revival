namespace Client.Services.Fishing;

internal sealed class Tracking2Settings
{
	public double CloseThreshold { get; init; } = 0.01;
	public double DerivativeGain { get; init; } = 0.55;
	public double EdgeBoundary { get; init; } = 0.1;
	public double NeutralDutyCycle { get; init; } = 0.5;
	public double PredictionStrength { get; init; } = 7.5;
	public double ProportionalGain { get; init; } = 0.42;
	public double Resilience { get; init; } = 0.0;
	public int UpdateRateMs { get; init; } = 21;
	public double VelocityDamping { get; init; } = 38;
}
