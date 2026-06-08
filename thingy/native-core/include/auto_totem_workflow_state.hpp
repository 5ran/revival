#pragma once

#include <cstdint>
#include <string>

namespace macro_port
{
struct AutoTotemWorkflowSettings
{
    std::int64_t awaitFishCycleTimeoutMs = 30000;
    std::int64_t retryDelayMs = 180000;
    std::int64_t maintainIntervalMs = 500;
};

struct AutoTotemWorkflowInput
{
    std::int64_t nowMs = 0;
    bool runtimeEnabled = true;
    bool reelVisible = false;
    bool boundaryOpen = false;
    bool workflowNeedsRun = false;
    enum class Result
    {
        None,
        Success,
        Failed,
        BlockedByGate,
        ActiveStateUnreadable,
    } workflowResult = Result::None;
};

enum class AutoTotemWorkflowAction
{
    None,
    QueuePending,
    HoldAddon,
    StartWorkflow,
    WaitRetry,
};

struct AutoTotemWorkflowOutput
{
    AutoTotemWorkflowAction action = AutoTotemWorkflowAction::None;
    bool pending = false;
    bool awaitFishCycle = false;
    bool shouldBlockCasting = false;
    std::string reason;
};

class AutoTotemWorkflowState
{
public:
    explicit AutoTotemWorkflowState(AutoTotemWorkflowSettings settings = {});

    void Reset();
    void ScheduleRetry(std::int64_t nowMs);
    AutoTotemWorkflowOutput Update(const AutoTotemWorkflowInput& in);

private:
    AutoTotemWorkflowSettings settings_{};
    bool pending_ = false;
    bool awaitFishCycle_ = false;
    bool sawFishingSinceRun_ = false;
    std::int64_t awaitStartedAt_ = 0;
    std::int64_t retryAfterAt_ = 0;
    std::int64_t lastMaintainCheckAt_ = 0;
    std::int64_t queuedAt_ = 0;
};
}
