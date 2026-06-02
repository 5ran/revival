#pragma once

#include <cstdint>
#include <string>
#include <vector>

#include "runtime_tracker_engine.hpp"

namespace macro_port
{
struct ReplayFrame
{
    RuntimeTrackerTickInput input{};
    RuntimeTrackerTickResult expected{};
    bool checkPhase = true;
    bool checkHoldApplied = true;
    bool checkRightHoldApplied = false;
    bool checkAutomationGate = false;
};

struct ReplayMismatch
{
    std::size_t index = 0;
    std::string field;
    std::string expected;
    std::string actual;
};

struct ReplayReport
{
    bool pass = true;
    std::vector<ReplayMismatch> mismatches;
};

class ReplayRunner
{
public:
    static ReplayReport Run(
        RuntimeTrackerEngine* engine,
        std::int64_t startNowMs,
        FishingCastingMode castingMode,
        const std::vector<ReplayFrame>& frames);
};
}
