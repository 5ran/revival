#include <cstdlib>
#include <iostream>

#include "automation_input_gate.hpp"

using macro_port::InMemoryAutomationInputGate;

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
    InMemoryAutomationInputGate gate;

    failed += Assert(gate.TryEnter("AUTO_AQUARIUM", 100), "first-enter");
    failed += Assert(!gate.TryEnter("AUTO_TOTEM", 200), "blocked-other-owner");
    failed += Assert(gate.IsHeldByOther("AUTO_TOTEM"), "held-by-other");
    failed += Assert(gate.TryEnter("AUTO_AQUARIUM", 100), "reenter-same-owner");

    gate.Exit("AUTO_TOTEM");
    failed += Assert(gate.IsHeldByOther("AUTO_TOTEM"), "wrong-owner-cant-exit");
    gate.Exit("AUTO_AQUARIUM");
    failed += Assert(gate.TryEnter("AUTO_TOTEM", 200), "enter-after-release");

    gate.Reset();
    failed += Assert(!gate.IsHeldByOther("AUTO_TOTEM"), "reset-clears");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
