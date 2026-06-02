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
    TrackerOrchestrator orchestrator(TrackerOrchestratorSettings{100, 50, 1200, 30, 80});

    orchestrator.Start(1000, FishingCastingMode::Perfect);

    auto t1 = orchestrator.Tick(TrackerTickInput{1010, false, false, false, false, false, true, false});
    failed += Assert(t1.phase == FishingPhase::Casting && t1.holdLeft, "casting-hold");

    auto t2 = orchestrator.Tick(TrackerTickInput{1200, false, false, false, false, false, true, true});
    failed += Assert(t2.phase == FishingPhase::Casted && !t2.holdLeft, "casted-release");

    auto t3 = orchestrator.Tick(TrackerTickInput{1220, false, false, false, false, false, true, false});
    failed += Assert(t3.phase == FishingPhase::Casted, "post-cast-wait");

    auto t4 = orchestrator.Tick(TrackerTickInput{1300, false, false, true, false, false, true, false});
    failed += Assert(t4.phase == FishingPhase::Shake && !t4.clickShake, "enter-shake");

    auto t5 = orchestrator.Tick(TrackerTickInput{1335, false, false, true, false, false, true, false});
    failed += Assert(t5.phase == FishingPhase::Shake && t5.clickShake, "shake-click");

    auto t6 = orchestrator.Tick(TrackerTickInput{1370, false, true, false, true, false, true, false});
    failed += Assert(t6.phase == FishingPhase::Fishing, "enter-fishing");

    auto t7 = orchestrator.Tick(TrackerTickInput{1420, false, true, false, true, false, true, false});
    failed += Assert(t7.phase == FishingPhase::Fishing && !t7.resetCycle, "stay-fishing");

    auto t8 = orchestrator.Tick(TrackerTickInput{1500, false, true, false, true, true, true, false});
    failed += Assert(t8.phase == FishingPhase::Casting && t8.resetCycle, "completion-recast");

    auto t9 = orchestrator.Tick(TrackerTickInput{1520, true, false, false, false, false, true, false});
    failed += Assert(t9.phase == FishingPhase::Suspended, "suspend");

    auto t10 = orchestrator.Tick(TrackerTickInput{1600, false, false, false, false, false, true, false});
    failed += Assert(t10.phase == FishingPhase::Casting, "resume-casting");

    TrackerOrchestrator normal(TrackerOrchestratorSettings{100, 50, 1200, 30, 80});
    normal.Start(1000, FishingCastingMode::Normal);
    auto n1 = normal.Tick(TrackerTickInput{1010, false, false, false, false, false, true, false});
    failed += Assert(n1.phase == FishingPhase::Casting, "normal-phase-casting");
    failed += Assert(n1.message == "Waiting for pre-cast delay.", "normal-precast-message");
    auto n2 = normal.Tick(TrackerTickInput{1220, false, false, false, false, false, true, false});
    failed += Assert(n2.phase == FishingPhase::Casting, "normal-still-casting");
    failed += Assert(n2.message == "Waiting for active reel minigame.", "normal-wait-message");
    auto n3 = normal.Tick(TrackerTickInput{1435, false, false, false, false, false, true, false});
    failed += Assert(n2.holdLeft != n3.holdLeft, "normal-pulse-toggle");
    auto n4 = normal.Tick(TrackerTickInput{1600, false, false, true, false, false, true, false});
    failed += Assert(n4.phase == FishingPhase::Shake, "normal-enter-shake");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }

    return 1;
}
