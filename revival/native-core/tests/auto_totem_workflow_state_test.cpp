#include <cstdlib>
#include <iostream>

#include "auto_totem_workflow_state.hpp"

using macro_port::AutoTotemWorkflowAction;
using macro_port::AutoTotemWorkflowInput;
using Result = macro_port::AutoTotemWorkflowInput::Result;
using macro_port::AutoTotemWorkflowSettings;
using macro_port::AutoTotemWorkflowState;

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
    AutoTotemWorkflowState state(AutoTotemWorkflowSettings{30000, 180000, 500});

    auto a = state.Update(AutoTotemWorkflowInput{1000, true, false, false, true, Result::None});
    failed += Assert(a.action == AutoTotemWorkflowAction::QueuePending && a.pending, "queue-pending");

    auto b = state.Update(AutoTotemWorkflowInput{1100, true, false, false, true, Result::None});
    failed += Assert(b.action == AutoTotemWorkflowAction::HoldAddon && b.shouldBlockCasting, "hold-until-boundary");

    auto c0 = state.Update(AutoTotemWorkflowInput{1900, true, false, true, true, Result::None});
    failed += Assert(c0.action == AutoTotemWorkflowAction::StartWorkflow && c0.pending, "start-await-result");
    auto c = state.Update(AutoTotemWorkflowInput{2000, true, false, true, true, Result::Success});
    failed += Assert(c.action == AutoTotemWorkflowAction::StartWorkflow && c.awaitFishCycle, "start-success-await");

    auto d = state.Update(AutoTotemWorkflowInput{2200, true, true, false, false, Result::None});
    failed += Assert(d.awaitFishCycle, "await-seen-fishing");

    auto e = state.Update(AutoTotemWorkflowInput{2400, true, false, false, false, Result::None});
    failed += Assert(!e.awaitFishCycle && e.reason == "await-cycle-released", "await-release");

    auto f = state.Update(AutoTotemWorkflowInput{3000, true, false, true, true, Result::Failed});
    failed += Assert(f.action == AutoTotemWorkflowAction::StartWorkflow && f.reason == "workflow-failed-retry", "workflow-failed");

    auto g = state.Update(AutoTotemWorkflowInput{3500, true, false, false, false, Result::None});
    failed += Assert(g.action == AutoTotemWorkflowAction::WaitRetry, "retry-cooldown");

    auto h = state.Update(AutoTotemWorkflowInput{5000, false, false, false, false, Result::None});
    failed += Assert(h.reason == "runtime-disabled", "runtime-disabled-reset");

    auto i1 = state.Update(AutoTotemWorkflowInput{8000, true, false, false, true, Result::None});
    auto i2 = state.Update(AutoTotemWorkflowInput{8600, true, false, true, true, Result::BlockedByGate});
    failed += Assert(i2.pending && i2.reason == "workflow-blocked-gate-requeue", "blocked-requeue");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
