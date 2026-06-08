#include <cstdlib>
#include <iostream>

#include "tracking3_controller.hpp"

using macro_port::ReelMetrics;
using macro_port::Tracking3Controller;
using macro_port::Tracking3DecisionMode;
using macro_port::Tracking3Settings;

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
    Tracking3Controller c;
    Tracking3Settings s;
    c.Reset(0.0);

    ReelMetrics m{};
    m.fishCenter = 0.7;
    m.playerbarCenter = 0.2;
    m.playerbarWidth = 0.1;

    auto d1 = c.Update(m, s, 0.05);
    failed += Assert(d1.mode == Tracking3DecisionMode::Warmup, "warmup-mode");

    auto d2 = c.Update(m, s, 0.25);
    failed += Assert(d2.mode != Tracking3DecisionMode::Warmup, "post-warmup");

    ReelMetrics edge{};
    edge.fishCenter = 0.8;
    edge.playerbarCenter = 0.05;
    edge.playerbarWidth = 0.1;
    auto d3 = c.Update(edge, s, 0.30);
    failed += Assert(d3.mode == Tracking3DecisionMode::EdgeRecovery, "edge-recovery");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}

