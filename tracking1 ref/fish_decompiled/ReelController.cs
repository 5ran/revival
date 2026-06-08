using System;
using System.Diagnostics;

internal sealed class ReelController
{
	private readonly Stopwatch stopwatch = Stopwatch.StartNew();

	private bool holding;

	private bool hasLastFrame;

	private double lastTime;

	private double prevFishX;

	private double prevBarCenter;

	private double fishVelocityEma;

	private double barVelocityEma;

	private double smoothFishX;

	private double smoothBarCenter;

	private double smoothControl;

	private double errorIntegral;

	private double centerPulseReleaseUntil;

	private bool casting;

	private bool castHolding;

	private double nextCastChangeTime;

	public ReelControlState UpdateTracking(ReelSnapshot snapshot, ReelControlSettings settings)
	{
		if (casting)
		{
			casting = false;
			castHolding = false;
			nextCastChangeTime = 0.0;
			Release();
			ResetTrackingState();
		}
		ReelControlState reelControlState = Compute(snapshot, settings);
		SetHold(reelControlState.Holding);
		return reelControlState;
	}

	public ReelControlState UpdateCasting()
	{
		double totalSeconds = stopwatch.Elapsed.TotalSeconds;
		if (!casting)
		{
			ResetTrackingState();
			casting = true;
			castHolding = false;
			nextCastChangeTime = 0.0;
		}
		if (totalSeconds >= nextCastChangeTime)
		{
			castHolding = !castHolding;
			SetHold(castHolding);
			nextCastChangeTime = totalSeconds + 0.2;
		}
		return new ReelControlState(0.0, 0.0, castHolding);
	}

	public void Reset()
	{
		Release();
		casting = false;
		castHolding = false;
		nextCastChangeTime = 0.0;
		ResetTrackingState();
	}

	public void Release()
	{
		SetHold(value: false);
	}

	private void ResetTrackingState()
	{
		hasLastFrame = false;
		lastTime = 0.0;
		prevFishX = 0.0;
		prevBarCenter = 0.0;
		fishVelocityEma = 0.0;
		barVelocityEma = 0.0;
		smoothFishX = 0.0;
		smoothBarCenter = 0.0;
		smoothControl = 0.0;
		errorIntegral = 0.0;
		centerPulseReleaseUntil = 0.0;
	}

	private ReelControlState Compute(ReelSnapshot snapshot, ReelControlSettings settings)
	{
		double totalSeconds = stopwatch.Elapsed.TotalSeconds;
		double num = (hasLastFrame ? Math.Max(0.001, totalSeconds - lastTime) : 0.016);
		double num2 = snapshot.FishCenterX;
		double num3 = snapshot.PlayerbarCenterX;
		double num4 = Math.Max(1.0, snapshot.PlayerbarWidth);
		if (!hasLastFrame)
		{
			smoothFishX = num2;
			smoothBarCenter = num3;
			prevFishX = num2;
			prevBarCenter = num3;
			lastTime = totalSeconds;
			hasLastFrame = true;
		}
		else
		{
			double num5 = Clamp(settings.PositionAlpha, 0.2, 0.92);
			double num6 = Clamp(settings.PositionAlpha, 0.2, 0.92);
			smoothFishX = num5 * num2 + (1.0 - num5) * smoothFishX;
			smoothBarCenter = num6 * num3 + (1.0 - num6) * smoothBarCenter;
		}
		double num7 = (smoothFishX - prevFishX) / num;
		double num8 = (smoothBarCenter - prevBarCenter) / num;
		fishVelocityEma = settings.FishVelAlpha * num7 + (1.0 - settings.FishVelAlpha) * fishVelocityEma;
		barVelocityEma = settings.BarVelAlpha * num8 + (1.0 - settings.BarVelAlpha) * barVelocityEma;
		prevFishX = smoothFishX;
		prevBarCenter = smoothBarCenter;
		lastTime = totalSeconds;
		double num15;
		if (settings.UsePrediction)
		{
			double num9 = smoothFishX - smoothBarCenter;
			double num10 = Math.Max(2.0, num4 * 0.18);
			double num11 = Math.Max(2.0, num4 * 0.12);
			double num12 = Clamp(fishVelocityEma * settings.FishPredT, 0.0 - num10, num10);
			double num13 = Clamp(barVelocityEma * settings.BarPredT, 0.0 - num11, num11);
			double num14 = smoothFishX + 0.5 * num12 - (smoothBarCenter + 0.35 * num13);
			num15 = 0.65 * num9 + 0.35 * num14;
		}
		else
		{
			num15 = smoothFishX - smoothBarCenter;
		}
		if (fishVelocityEma > 0.0 && num15 > (0.0 - num4) * 0.1)
		{
			double max = Math.Max(2.0, num4 * 0.22);
			num15 += Clamp(fishVelocityEma * settings.RightMoveLeadT, 0.0, max);
		}
		double num16 = Clamp(num4 * 0.035, 1.5, 5.0);
		num15 += num16;
		errorIntegral = Clamp(errorIntegral + num15 * num, 0.0 - settings.IntegralClamp, settings.IntegralClamp);
		double num17 = num4 * settings.BarRatioFromSide;
		double num18 = snapshot.Container.Left;
		double num19 = snapshot.Container.Right;
		double val = ((settings.PdClamp > 0.0) ? settings.PdClamp : 30.0);
		val = Math.Max(val, settings.OnThreshold + 1.0);
		double num20;
		if (smoothFishX < num18 + num17)
		{
			num20 = 0.0 - val;
		}
		else if (smoothFishX > num19 - num17)
		{
			num20 = val;
		}
		else
		{
			double num21 = fishVelocityEma - barVelocityEma;
			num20 = settings.Kp * num15 + settings.Ki * errorIntegral + settings.Kd * num21;
			if (settings.PdClamp > 0.0)
			{
				num20 = Clamp(num20, 0.0 - settings.PdClamp, settings.PdClamp);
			}
		}
		double control = (smoothControl = settings.ControlAlpha * num20 + (1.0 - settings.ControlAlpha) * smoothControl);
		bool flag = DecideHold(settings, totalSeconds, num15, control, num4);
		return new ReelControlState(num15, control, flag);
	}

	private bool DecideHold(ReelControlSettings settings, double now, double error, double control, double boxLen)
	{
		double num = Math.Max(2.0, boxLen * settings.CenterZoneRatio);
		if (Math.Abs(error) <= num)
		{
			if (now < centerPulseReleaseUntil)
			{
				return false;
			}
			if (control > settings.OnThreshold)
			{
				bool flag = PositiveModulo(now, settings.CenterPulsePeriodS) < Math.Max(0.001, settings.CenterPulseHoldS);
				if (flag)
				{
					centerPulseReleaseUntil = now + Math.Max(0.0, settings.CenterReleaseBlipS);
				}
				return flag;
			}
			if (control < 0.0 - settings.OnThreshold)
			{
				return false;
			}
			if (control > 0.0)
			{
				return PositiveModulo(now, settings.CenterWeakPeriodS) < Math.Max(0.001, settings.CenterWeakHoldS);
			}
			return false;
		}
		if (control > settings.OnThreshold)
		{
			return true;
		}
		if (control < 0.0 - settings.OnThreshold)
		{
			return false;
		}
		if (Math.Abs(control) < settings.OffThreshold)
		{
			return false;
		}
		return false;
	}

	private void SetHold(bool value)
	{
		if (value != holding)
		{
			if (value)
			{
				MouseInput.LeftDown();
			}
			else
			{
				MouseInput.LeftUp();
			}
			holding = value;
		}
	}

	private static double PositiveModulo(double value, double modulus)
	{
		modulus = Math.Max(0.001, modulus);
		double num = value % modulus;
		return (num < 0.0) ? (num + modulus) : num;
	}

	private static double Clamp(double value, double min, double max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}
}
