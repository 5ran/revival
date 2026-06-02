#include <cstdlib>
#include <iostream>

#include "tracking1_controller.hpp"

using macro_port::ReelMetrics;
using macro_port::Tracking1Controller;
using macro_port::Tracking1Settings;

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
    Tracking1Controller c;
    Tracking1Settings s;

    ReelMetrics m{};
    m.fishCenter = 0.8;
    m.playerbarCenter = 0.2;
    m.playerbarWidth = 0.1;

    auto d1 = c.UpdateTracking(m, s, 0.0);
    auto d2 = c.UpdateTracking(m, s, 0.02);
    failed += Assert(d1.control > 0 || d2.control > 0, "control-positive");

    auto cast1 = c.UpdateCasting(1.0);
    auto cast2 = c.UpdateCasting(1.3);
    failed += Assert(cast1.holding != cast2.holding, "casting-toggle");

    c.Release();
    failed += Assert(!c.IsHolding(), "release");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }

    return 1;
}

