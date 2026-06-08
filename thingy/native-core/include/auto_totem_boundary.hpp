#pragma once

#include <cstdint>

namespace macro_port
{
struct AutoTotemBoundaryState
{
    bool completionLatched = false;
    std::int64_t completionLatchedAtMs = 0;
    bool perfectCastingHolding = false;
    bool castBarSeen = false;
};

class AutoTotemBoundary
{
public:
    explicit AutoTotemBoundary(std::int64_t postCatchSettleMs = 600) : postCatchSettleMs_(postCatchSettleMs) {}

    void Reset();
    void LatchCompletion(std::int64_t nowMs);
    bool IsOpen(std::int64_t nowMs, bool perfectCastingHolding, bool castBarSeen) const;

    static bool ComputeBoundary(bool completionLatched, bool settleElapsed, bool perfectCastingHolding, bool castBarSeen);

private:
    std::int64_t postCatchSettleMs_ = 600;
    std::int64_t completionLatchedAtMs_ = 0;
};
}
