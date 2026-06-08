#include "fishing_hold_gate.hpp"

namespace macro_port
{
FishingHoldAction FishingHoldGate::Decide(bool desired, long long nowMs, int delayMs)
{
    if (desired == held_)
    {
        return FishingHoldAction::None;
    }

    if (delayMs > 0 && lastActionAt_ != 0 && nowMs - lastActionAt_ < delayMs)
    {
        return FishingHoldAction::None;
    }

    held_ = desired;
    lastActionAt_ = nowMs;
    return desired ? FishingHoldAction::Press : FishingHoldAction::Release;
}

FishingHoldAction FishingHoldGate::ForceRelease(long long nowMs)
{
    if (!held_)
    {
        return FishingHoldAction::None;
    }

    held_ = false;
    lastActionAt_ = nowMs;
    return FishingHoldAction::Release;
}

void FishingHoldGate::Reset()
{
    held_ = false;
    lastActionAt_ = 0;
}
}

