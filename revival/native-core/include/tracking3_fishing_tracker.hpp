#pragma once

#include <cstdint>

#include "ifishing_tracker.hpp"
#include "runtime_tracker_engine.hpp"

namespace macro_port
{
class Tracking3FishingTracker : public IFishingTracker
{
public:
    explicit Tracking3FishingTracker(RuntimeTrackerEngine* engine) : engine_(engine) {}
    FishingTrackerMode Mode() const override { return FishingTrackerMode::Tracking3; }
    void Start(std::int64_t nowMs, FishingCastingMode mode) override { if (engine_ != nullptr) engine_->Start(nowMs, mode); running_ = true; }
    void Stop(std::int64_t nowMs) override { if (engine_ != nullptr) engine_->Stop(nowMs); running_ = false; }
    RuntimeTrackerTickResult Tick(const RuntimeTrackerTickInput& input) { return engine_ == nullptr ? RuntimeTrackerTickResult{} : engine_->Tick(input); }
    FishingTrackerStatus GetStatus() const override { return FishingTrackerStatus{running_, "RUNNING", running_ ? "Tracking3 active." : "Tracking3 stopped.", std::nullopt, false}; }

private:
    RuntimeTrackerEngine* engine_ = nullptr;
    bool running_ = false;
};
}
