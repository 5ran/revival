#include <cstdlib>
#include <iostream>

#include "auto_totem_boundary.hpp"

using macro_port::AutoTotemBoundary;

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
    AutoTotemBoundary boundary(600);

    failed += Assert(!boundary.IsOpen(1000, false, false), "not-latched");
    boundary.LatchCompletion(1000);
    failed += Assert(!boundary.IsOpen(1200, false, false), "settle-not-elapsed");
    failed += Assert(boundary.IsOpen(1600, false, false), "open-after-settle");
    failed += Assert(!boundary.IsOpen(1600, true, false), "blocked-perfect-hold");
    failed += Assert(!boundary.IsOpen(1600, false, true), "blocked-cast-bar");

    failed += Assert(AutoTotemBoundary::ComputeBoundary(true, true, false, false), "compute-open");
    failed += Assert(!AutoTotemBoundary::ComputeBoundary(true, false, false, false), "compute-settle");

    boundary.Reset();
    failed += Assert(!boundary.IsOpen(2000, false, false), "reset-closes");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
