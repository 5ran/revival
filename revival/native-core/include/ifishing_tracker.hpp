#pragma once

#include <cstdint>

#include "fishing_phase.hpp"
#include "fishing_tracker_mode.hpp"
#include "fishing_tracker_status.hpp"

namespace macro_port
{
class IFishingTracker
{
public:
    virtual ~IFishingTracker() = default;
    virtual FishingTrackerMode Mode() const = 0;
    virtual void Start(std::int64_t nowMs, FishingCastingMode castingMode) = 0;
    virtual void Stop(std::int64_t nowMs) = 0;
    virtual FishingTrackerStatus GetStatus() const = 0;
};
}
