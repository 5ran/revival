#pragma once

#include <cstdint>
#include <string>

#include "replay_runner.hpp"
#include "replay_trace_io.hpp"

namespace macro_port
{
class ParityRunner
{
public:
    static bool RunCsvTrace(
        RuntimeTrackerEngine* engine,
        const std::string& csvPath,
        std::int64_t startNowMs,
        FishingCastingMode castingMode,
        ReplayReport& outReport,
        std::string& error);
};
}
