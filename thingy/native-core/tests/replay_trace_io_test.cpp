#include <cstdlib>
#include <fstream>
#include <iostream>
#include <string>
#include <vector>

#include "replay_trace_io.hpp"

using macro_port::ReplayFrame;
using macro_port::ReplayTraceIo;

static int Assert(bool condition, const char* name)
{
    if (!condition)
    {
        std::cerr << "FAIL: " << name << "\n";
        return 1;
    }
    return 0;
}

int main()
{
    int failed = 0;
    const std::string path = "replay_trace_io_test.csv";
    {
        std::ofstream out(path);
        out << "now_ms,phase,hold_applied,right_hold_applied,automation_gate_open,shake_visible,cast_power_ready,perfect_cast_release,completion_override\n";
        out << "1200,Casted,false,false,false,false,true,true,null\n";
        out << "1600,Fishing,false,false,false,false,true,false,false\n";
    }

    std::vector<ReplayFrame> frames;
    std::string error;
    const bool ok = ReplayTraceIo::LoadFramesFromCsv(path, frames, error);
    failed += Assert(ok, "load-ok");
    failed += Assert(frames.size() == 2, "load-count");
    failed += Assert(frames[0].input.nowMs == 1200, "first-now");
    failed += Assert(frames[1].input.completionOverride.has_value() && !frames[1].input.completionOverride.value(), "completion-override");

    std::remove(path.c_str());

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
