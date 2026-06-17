using System;

namespace Client.Services.Fishing;

internal sealed class Tracking2Controller
{
	private double? lastPlayerbarPos;
	private double? lastFishPos;
	private double pwmAccumulator;

	public ReelControlState Update(ReelMetrics metrics, Tracking2Settings settings)
	{
		var fishPos = metrics.FishCenter;
		var playerbarPos = metrics.PlayerbarCenter;

		lastPlayerbarPos ??= playerbarPos;
		lastFishPos ??= fishPos;

		var playerbarVelocity = playerbarPos - lastPlayerbarPos.Value;
		var fishVelocity = fishPos - lastFishPos.Value;
		lastPlayerbarPos = playerbarPos;
		lastFishPos = fishPos;

		var error = fishPos - playerbarPos;
		if (playerbarPos < settings.EdgeBoundary)
		{
			return new ReelControlState(error, 1.0, true);
		}

		if (playerbarPos > 1.0 - settings.EdgeBoundary)
		{
			return new ReelControlState(error, 0.0, false);
		}

		var predictionScale = settings.PredictionStrength * (1.0 - settings.Resilience);
		var predicted = playerbarPos + playerbarVelocity * predictionScale;
		var predictedError = fishPos - predicted;
		var sameSideAfterPrediction = error * predictedError > 0;
		var approachingTarget = error * playerbarVelocity > 0;
		var remainingDistance = Math.Max(0.0, Math.Abs(error) - settings.CloseThreshold);
		var brakeLookahead = Math.Abs(playerbarVelocity) * 8.0;
		var needsPreSlow = approachingTarget && brakeLookahead >= remainingDistance;

		if (Math.Abs(error) > settings.CloseThreshold && sameSideAfterPrediction && !needsPreSlow)
		{
			return error > 0
				? new ReelControlState(error, 1.0, true)
				: new ReelControlState(error, 0.0, false);
		}

		double targetDuty;
		if (needsPreSlow && brakeLookahead > 0)
		{
			var brakeUrgency = 1.0 - Math.Min(1.0, remainingDistance / brakeLookahead);
			targetDuty = error > 0
				? settings.NeutralDutyCycle * (1.0 - brakeUrgency)
				: settings.NeutralDutyCycle + (1.0 - settings.NeutralDutyCycle) * brakeUrgency;
		}
		else
		{
			var adjustment = settings.ProportionalGain * error +
				settings.DerivativeGain * fishVelocity -
				settings.VelocityDamping * playerbarVelocity;
			targetDuty = Clamp(settings.NeutralDutyCycle + adjustment, 0.0, 1.0);
		}

		pwmAccumulator += targetDuty;
		if (pwmAccumulator >= 1.0)
		{
			pwmAccumulator -= 1.0;
			return new ReelControlState(error, targetDuty, true);
		}

		return new ReelControlState(error, targetDuty, false);
	}

	public void Reset()
	{
		lastPlayerbarPos = null;
		lastFishPos = null;
		pwmAccumulator = 0.0;
	}

	private static double Clamp(double value, double min, double max)
	{
		return value < min ? min : value > max ? max : value;
	}
}
