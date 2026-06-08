#include "auto_totem_boundary.hpp"

namespace macro_port
{
void AutoTotemBoundary::Reset()
{
    completionLatchedAtMs_ = 0;
}

void AutoTotemBoundary::LatchCompletion(std::int64_t nowMs)
{
    if (completionLatchedAtMs_ == 0)
    {
        completionLatchedAtMs_ = nowMs;
    }
}

bool AutoTotemBoundary::IsOpen(std::int64_t nowMs, bool perfectCastingHolding, bool castBarSeen) const
{
    const bool latched = completionLatchedAtMs_ != 0;
    const bool settleElapsed = latched && (nowMs - completionLatchedAtMs_ >= postCatchSettleMs_);
    return ComputeBoundary(latched, settleElapsed, perfectCastingHolding, castBarSeen);
}

bool AutoTotemBoundary::ComputeBoundary(bool completionLatched, bool settleElapsed, bool perfectCastingHolding, bool castBarSeen)
{
    return completionLatched && settleElapsed && !perfectCastingHolding && !castBarSeen;
}
}
