#include <cstdlib>
#include <iostream>

#include "tracker_orchestrator.hpp"

using macro_port::FishingCastingMode;
using macro_port::FishingPhase;
using macro_port::TrackerOrchestrator;
using macro_port::TrackerOrchestratorSettings;
using macro_port::TrackerTickInput;

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

    TrackerOrchestratorSettings s;
    s.preCastDelayMs = 0;
    s.castTimeoutMs = 900;
    s.perfectCastTargetPercent = 96.0;
    s.perfectCastNearWindowPercent = 1.0;
    s.perfectReleaseMicroPollMs = 14;
    s.castOnTimeout = true;

    TrackerOrchestrator o(s);
    o.Start(1000, FishingCastingMode::Perfect);

    auto a = o.Tick(TrackerTickInput{1010, false, false, false, false, false, true, false, 94.0});
    failed += Assert(a.phase == FishingPhase::Casting && a.holdLeft, "perfect-charge");

    auto b = o.Tick(TrackerTickInput{1020, false, false, false, false, false, true, true, 95.2});
    failed += Assert(b.phase == FishingPhase::Casted && !b.holdLeft, "perfect-near-target-release");

    TrackerOrchestrator o2(s);
    o2.Start(2000, FishingCastingMode::Perfect);
    auto c = o2.Tick(TrackerTickInput{2005, false, false, false, false, false, true, false, 50.0});
    failed += Assert(c.phase == FishingPhase::Casting && c.holdLeft, "timeout-pre");
    auto d = o2.Tick(TrackerTickInput{8000, false, false, false, false, false, true, false, 50.0});
    failed += Assert(d.phase == FishingPhase::Casted && !d.holdLeft, "timeout-release");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
