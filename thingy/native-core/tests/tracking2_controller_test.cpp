#include <cstdlib>
#include <iostream>

#include "tracking2_controller.hpp"

using macro_port::ReelMetrics;
using macro_port::Tracking2Controller;
using macro_port::Tracking2Settings;

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
    Tracking2Controller c;
    Tracking2Settings s;

    {
        ReelMetrics m{};
        m.fishCenter = 0.9;
        m.playerbarCenter = 0.05;
        m.playerbarWidth = 0.2;
        auto d = c.Update(m, s);
        failed += Assert(d.holding == true, "edge-left-hold");
    }

    c.Reset();

    {
        ReelMetrics m{};
        m.fishCenter = 0.1;
        m.playerbarCenter = 0.95;
        m.playerbarWidth = 0.2;
        auto d = c.Update(m, s);
        failed += Assert(d.holding == false, "edge-right-release");
    }

    c.Reset();

    {
        ReelMetrics m1{};
        m1.fishCenter = 0.8;
        m1.playerbarCenter = 0.3;
        m1.playerbarWidth = 0.1;
        (void)c.Update(m1, s);

        ReelMetrics m2{};
        m2.fishCenter = 0.81;
        m2.playerbarCenter = 0.31;
        m2.playerbarWidth = 0.1;
        auto d = c.Update(m2, s);
        failed += Assert(d.control >= 0.0 && d.control <= 1.0, "duty-clamped");
    }

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}

