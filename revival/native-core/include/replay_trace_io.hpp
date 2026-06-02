#pragma once

#include <string>
#include <vector>

#include "replay_runner.hpp"

namespace macro_port
{
class ReplayTraceIo
{
public:
    // CSV schema (header required):
    // now_ms,phase,hold_applied,right_hold_applied,automation_gate_open,shake_visible,cast_power_ready,perfect_cast_release,completion_override
    static bool LoadFramesFromCsv(const std::string& path, std::vector<ReplayFrame>& outFrames, std::string& error);
};
}
