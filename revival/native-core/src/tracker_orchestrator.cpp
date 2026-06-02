#include "tracker_orchestrator.hpp"

#include <algorithm>

namespace macro_port
{
TrackerOrchestrator::TrackerOrchestrator(TrackerOrchestratorSettings settings) : settings_(settings)
{
}

void TrackerOrchestrator::Start(std::int64_t nowMs, FishingCastingMode mode)
{
    running_ = true;
    castingMode_ = mode;
    phase_ = FishingPhase::Casting;
    holdLeft_ = false;
    castStartedAt_ = nowMs;
    castReleasedAt_ = 0;
    lastShakedAt_ = 0;
    fishingLostAt_ = 0;
    perfectNearTargetSince_ = 0;
    normalCastHolding_ = false;
    normalCastNextChangeAt_ = 0;
    normalCastHoldStartedAt_ = 0;
    seenReelThisRun_ = false;
    fishingInputReadyAt_ = 0;
    suspendMessage_.clear();
}

void TrackerOrchestrator::Stop()
{
    running_ = false;
    phase_ = FishingPhase::Off;
    holdLeft_ = false;
    castStartedAt_ = 0;
    castReleasedAt_ = 0;
    lastShakedAt_ = 0;
    fishingLostAt_ = 0;
    perfectNearTargetSince_ = 0;
    normalCastHolding_ = false;
    normalCastNextChangeAt_ = 0;
    normalCastHoldStartedAt_ = 0;
    seenReelThisRun_ = false;
    fishingInputReadyAt_ = 0;
}

void TrackerOrchestrator::Suspend(const std::string& message)
{
    if (!running_)
    {
        return;
    }

    phase_ = FishingPhase::Suspended;
    holdLeft_ = false;
    suspendMessage_ = message;
}

void TrackerOrchestrator::Resume()
{
    if (!running_ || phase_ != FishingPhase::Suspended)
    {
        return;
    }

    phase_ = FishingPhase::Casting;
    castStartedAt_ = 0;
    suspendMessage_.clear();
}

TrackerTickOutput TrackerOrchestrator::Tick(const TrackerTickInput& input)
{
    if (!running_)
    {
        return TrackerTickOutput{FishingPhase::Off, false, false, false, "Tracker stopped."};
    }

    if (input.suspended)
    {
        phase_ = FishingPhase::Suspended;
        holdLeft_ = false;
        return TrackerTickOutput{phase_, false, false, false,
            suspendMessage_.empty() ? "Suspended." : suspendMessage_};
    }

    if (phase_ == FishingPhase::Suspended)
    {
        Resume();
    }

    switch (phase_)
    {
    case FishingPhase::Casting:
        return TickCasting(input);
    case FishingPhase::Casted:
        return TickCasted(input);
    case FishingPhase::Shake:
        return TickShake(input);
    case FishingPhase::Fishing:
        return TickFishing(input);
    case FishingPhase::Error:
        return TrackerTickOutput{phase_, false, false, false, "Error state."};
    case FishingPhase::Off:
    case FishingPhase::Suspended:
    default:
        return TrackerTickOutput{phase_, false, false, false, "Idle."};
    }
}

TrackerTickOutput TrackerOrchestrator::TickCasting(const TrackerTickInput& input)
{
    if (castStartedAt_ == 0)
    {
        castStartedAt_ = input.nowMs;
    }

    const auto elapsed = input.nowMs - castStartedAt_;
    const auto timeout = std::max<std::int64_t>(5000, settings_.castTimeoutMs);
    if (elapsed >= timeout)
    {
        if (castingMode_ == FishingCastingMode::Normal)
        {
            return ResetToCasting("Cast timeout; recasting.", input.nowMs);
        }

        if (settings_.castOnTimeout)
        {
            holdLeft_ = false;
            castReleasedAt_ = input.nowMs;
            perfectNearTargetSince_ = 0;
            phase_ = FishingPhase::Casted;
            return TrackerTickOutput{phase_, false, false, false, "Cast timeout release."};
        }
        return ResetToCasting("Cast timeout; recasting.", input.nowMs);
    }

    holdLeft_ = true;
    if (elapsed < settings_.preCastDelayMs)
    {
        return TrackerTickOutput{phase_, holdLeft_, false, false, "Waiting for pre-cast delay."};
    }

    if (castingMode_ == FishingCastingMode::Normal)
    {
        if (normalCastNextChangeAt_ == 0)
        {
            normalCastNextChangeAt_ = input.nowMs;
            normalCastHolding_ = false;
            normalCastHoldStartedAt_ = 0;
        }

        if (input.nowMs >= normalCastNextChangeAt_)
        {
            normalCastHolding_ = !normalCastHolding_;
            normalCastNextChangeAt_ = input.nowMs + 200;
        }

        holdLeft_ = normalCastHolding_;
        if (holdLeft_)
        {
            if (normalCastHoldStartedAt_ == 0)
            {
                normalCastHoldStartedAt_ = input.nowMs;
            }
            else if (input.nowMs - normalCastHoldStartedAt_ >= 1200)
            {
                holdLeft_ = false;
                normalCastHolding_ = false;
                normalCastHoldStartedAt_ = 0;
                normalCastNextChangeAt_ = input.nowMs + 200;
                return TrackerTickOutput{phase_, false, false, false, "Casting watchdog reset."};
            }
        }
        else
        {
            normalCastHoldStartedAt_ = 0;
        }

        if (input.shakeVisible)
        {
            holdLeft_ = false;
            normalCastHolding_ = false;
            normalCastHoldStartedAt_ = 0;
            phase_ = FishingPhase::Shake;
            lastShakedAt_ = 0;
            return TrackerTickOutput{phase_, false, false, false, "Ready to shake."};
        }

        if (input.reelVisible && input.hasMetrics)
        {
            if (input.progressPercent.has_value() && *input.progressPercent >= settings_.completionThreshold)
            {
                return TrackerTickOutput{phase_, holdLeft_, false, false, "Waiting for fresh reel cycle."};
            }
            holdLeft_ = false;
            normalCastHolding_ = false;
            normalCastHoldStartedAt_ = 0;
            phase_ = FishingPhase::Fishing;
            fishingLostAt_ = 0;
            seenReelThisRun_ = true;
            fishingInputReadyAt_ = input.nowMs + settings_.reelInputSettleMs;
            return TrackerTickOutput{phase_, false, false, false, "Entered fishing."};
        }

        return TrackerTickOutput{phase_, holdLeft_, false, false, "Waiting for active reel minigame."};
    }

    if (castingMode_ == FishingCastingMode::Perfect)
    {
        if (!input.castPowerReady)
        {
            perfectNearTargetSince_ = 0;
            return TrackerTickOutput{phase_, holdLeft_, false, false, "Waiting for cast power bar."};
        }

        bool shouldRelease = false;
        if (input.castPowerPercent.has_value())
        {
            const auto power = *input.castPowerPercent;
            if (power >= settings_.perfectCastTargetPercent)
            {
                shouldRelease = true;
            }
            else if (power >= settings_.perfectCastTargetPercent - settings_.perfectCastNearWindowPercent)
            {
                if (perfectNearTargetSince_ == 0)
                {
                    perfectNearTargetSince_ = input.nowMs;
                }
                if (input.nowMs - perfectNearTargetSince_ <= settings_.perfectReleaseMicroPollMs && input.perfectCastRelease)
                {
                    shouldRelease = true;
                }
            }
            else
            {
                perfectNearTargetSince_ = 0;
            }
        }

        if (!shouldRelease && input.perfectCastRelease)
        {
            shouldRelease = true;
        }

        if (!shouldRelease)
        {
            return TrackerTickOutput{phase_, holdLeft_, false, false, "Charging perfect cast."};
        }
    }

    holdLeft_ = false;
    castReleasedAt_ = input.nowMs;
    perfectNearTargetSince_ = 0;
    normalCastHolding_ = false;
    normalCastNextChangeAt_ = 0;
    normalCastHoldStartedAt_ = 0;
    phase_ = FishingPhase::Casted;
    return TrackerTickOutput{phase_, holdLeft_, false, false, "Cast released."};
}

TrackerTickOutput TrackerOrchestrator::TickCasted(const TrackerTickInput& input)
{
    if (castReleasedAt_ == 0)
    {
        castReleasedAt_ = input.nowMs;
    }

    if (input.nowMs - castReleasedAt_ < settings_.postCastDelayMs)
    {
        return TrackerTickOutput{phase_, false, false, false, "Waiting after cast."};
    }

    if (input.shakeVisible)
    {
        phase_ = FishingPhase::Shake;
        lastShakedAt_ = 0;
        return TrackerTickOutput{phase_, false, false, false, "Ready to shake."};
    }

    if (input.reelVisible && input.hasMetrics)
    {
        if (input.progressPercent.has_value() && *input.progressPercent >= settings_.completionThreshold)
        {
            return TrackerTickOutput{phase_, false, false, false, "Waiting for fresh reel cycle."};
        }
        phase_ = FishingPhase::Fishing;
        fishingLostAt_ = 0;
        seenReelThisRun_ = true;
        fishingInputReadyAt_ = input.nowMs + settings_.reelInputSettleMs;
        return TrackerTickOutput{phase_, false, false, false, "Entered fishing."};
    }

    return TrackerTickOutput{phase_, false, false, false, "Waiting for shake button."};
}

TrackerTickOutput TrackerOrchestrator::TickShake(const TrackerTickInput& input)
{
    if (!input.shakeVisible)
    {
        if (input.reelVisible)
        {
            phase_ = FishingPhase::Fishing;
            fishingLostAt_ = 0;
            return TrackerTickOutput{phase_, false, false, false, "Entered fishing."};
        }

        return TrackerTickOutput{phase_, false, false, false, "Waiting for shake button."};
    }

    const auto canShake = (lastShakedAt_ == 0) || (input.nowMs - lastShakedAt_ >= settings_.shakeIntervalMs);
    if (canShake)
    {
        lastShakedAt_ = input.nowMs;
    }

    return TrackerTickOutput{phase_, false, canShake, false, "Sending shake input."};
}

TrackerTickOutput TrackerOrchestrator::TickFishing(const TrackerTickInput& input)
{
    if (!input.reelVisible || !input.hasMetrics)
    {
        if (fishingLostAt_ == 0)
        {
            fishingLostAt_ = input.nowMs;
        }

        const auto graceMs = seenReelThisRun_ ? settings_.reelReacquireGraceMs : settings_.fishingLostGraceMs;
        if (input.nowMs - fishingLostAt_ >= graceMs)
        {
            return ResetToCasting("Lost reel state; recasting.", input.nowMs);
        }

        return TrackerTickOutput{phase_, false, false, false, seenReelThisRun_ ? "Reacquiring reel." : "Fishing signal unstable."};
    }

    if (fishingInputReadyAt_ != 0 && input.nowMs < fishingInputReadyAt_)
    {
        return TrackerTickOutput{phase_, false, false, false, "Reel detected; releasing cast input."};
    }
    fishingInputReadyAt_ = 0;
    fishingLostAt_ = 0;
    if (input.completionReached)
    {
        return ResetToCasting("Catch complete; recasting.", input.nowMs);
    }

    return TrackerTickOutput{phase_, false, false, false, "Fishing minigame active."};
}

TrackerTickOutput TrackerOrchestrator::ResetToCasting(const char* message, std::int64_t nowMs)
{
    phase_ = FishingPhase::Casting;
    holdLeft_ = false;
    castStartedAt_ = nowMs;
    castReleasedAt_ = 0;
    lastShakedAt_ = 0;
    fishingLostAt_ = 0;
    perfectNearTargetSince_ = 0;
    normalCastHolding_ = false;
    normalCastNextChangeAt_ = 0;
    normalCastHoldStartedAt_ = 0;
    seenReelThisRun_ = false;
    fishingInputReadyAt_ = 0;
    return TrackerTickOutput{phase_, false, false, true, message};
}
}
