#include <cstdlib>
#include <iostream>

#include "hold_applier.hpp"

using macro_port::HoldApplier;
using macro_port::IInputActuator;

class FakeActuator final : public IInputActuator
{
public:
    void LeftDown() override { ++downs; }
    void LeftUp() override { ++ups; }

    int downs = 0;
    int ups = 0;
};

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
    FakeActuator f;
    HoldApplier applier(&f);

    applier.Apply(true, 1000, 0);
    failed += Assert(f.downs == 1 && f.ups == 0, "first-press");

    applier.Apply(true, 1010, 0);
    failed += Assert(f.downs == 1 && f.ups == 0, "dedup-press");

    applier.Apply(false, 1020, 100);
    failed += Assert(f.downs == 1 && f.ups == 0, "delay-block-release");

    applier.Apply(false, 1200, 100);
    failed += Assert(f.downs == 1 && f.ups == 1, "release");

    applier.Apply(true, 1300, 0);
    failed += Assert(f.downs == 2 && f.ups == 1, "second-press");

    applier.Release(1400);
    failed += Assert(f.downs == 2 && f.ups == 2, "force-release");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}

