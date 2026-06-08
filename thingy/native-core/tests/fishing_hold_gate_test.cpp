#include <cstdlib>
#include <iostream>

#include "fishing_hold_gate.hpp"

using macro_port::FishingHoldAction;
using macro_port::FishingHoldGate;

static int AssertEq(FishingHoldAction expected, FishingHoldAction actual, const char* name)
{
    if (expected != actual)
    {
        std::cerr << "FAIL: " << name << "\n";
        return 1;
    }
    return 0;
}

int main()
{
    int failed = 0;
    FishingHoldGate gate;

    failed += AssertEq(FishingHoldAction::Press, gate.Decide(true, 1000, 0), "press");
    failed += AssertEq(FishingHoldAction::None, gate.Decide(true, 1010, 0), "same-state");
    failed += AssertEq(FishingHoldAction::None, gate.Decide(false, 1020, 100), "delay-block");
    failed += AssertEq(FishingHoldAction::Release, gate.Decide(false, 1200, 100), "release");
    failed += AssertEq(FishingHoldAction::None, gate.ForceRelease(1300), "already-released");
    failed += AssertEq(FishingHoldAction::Press, gate.Decide(true, 1400, 0), "press-again");
    failed += AssertEq(FishingHoldAction::Release, gate.ForceRelease(1500), "force-release");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }

    return 1;
}

