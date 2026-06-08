#include <cstdlib>
#include <iostream>

#include "rod_profile.hpp"

using macro_port::DreambreakerRodProfile;
using macro_port::NoteTarget;
using macro_port::PinionRodProfile;
using macro_port::ReelMetrics;
using macro_port::RodKind;
using macro_port::RodProfile;

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

    {
        auto profile = RodProfile::For(RodKind::Requiem);
        failed += Assert(profile->Kind() == RodKind::Requiem, "factory-kind");
        failed += Assert(profile->FishingActionDelayMs() == 160, "requiem-delay");
    }

    {
        DreambreakerRodProfile profile;
        failed += Assert(profile.TransformHold(true, 39.9) == true, "dreambreaker-before");
        failed += Assert(profile.TransformHold(true, 40.0) == false, "dreambreaker-after");
    }

    {
        PinionRodProfile profile;
        ReelMetrics m{};
        m.fishCenter = 0.50;
        m.playerbarCenter = 0.50;
        m.playerbarWidth = 0.20;

        auto out1 = profile.AdjustTarget(m, NoteTarget{0.55, 0.0});
        failed += Assert(out1.fishCenter > 0.50, "pinion-adjust");

        profile.Reset();
        auto out2 = profile.AdjustTarget(m, std::nullopt);
        failed += Assert(out2.fishCenter == m.fishCenter, "pinion-null-note");
    }

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }

    return 1;
}

