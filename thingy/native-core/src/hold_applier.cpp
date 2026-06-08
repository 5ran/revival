#include "hold_applier.hpp"

namespace macro_port
{
void HoldApplier::Apply(bool desiredHold, long long nowMs, int actionDelayMs)
{
    const auto action = gate_.Decide(desiredHold, nowMs, actionDelayMs);
    if (!actuator_)
    {
        return;
    }

    if (action == FishingHoldAction::Press)
    {
        actuator_->LeftDown();
    }
    else if (action == FishingHoldAction::Release)
    {
        actuator_->LeftUp();
    }
}

void HoldApplier::Release(long long nowMs)
{
    const auto action = gate_.ForceRelease(nowMs);
    if (actuator_ && action == FishingHoldAction::Release)
    {
        actuator_->LeftUp();
    }
}

void HoldApplier::Reset()
{
    gate_.Reset();
}
}

