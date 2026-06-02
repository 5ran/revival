#pragma once

namespace macro_port
{
enum class FishingHoldAction
{
    None,
    Press,
    Release,
};

class FishingHoldGate
{
public:
    bool Held() const { return held_; }

    FishingHoldAction Decide(bool desired, long long nowMs, int delayMs);
    FishingHoldAction ForceRelease(long long nowMs);
    void Reset();

private:
    bool held_ = false;
    long long lastActionAt_ = 0;
};
}

