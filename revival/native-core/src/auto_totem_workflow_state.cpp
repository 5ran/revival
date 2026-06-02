#include "auto_totem_workflow_state.hpp"

namespace macro_port
{
namespace
{
bool IsWorkflowPendingResult(AutoTotemWorkflowInput::Result r)
{
    return r == AutoTotemWorkflowInput::Result::None;
}
}

AutoTotemWorkflowState::AutoTotemWorkflowState(AutoTotemWorkflowSettings settings) : settings_(settings)
{
}

void AutoTotemWorkflowState::Reset()
{
    pending_ = false;
    awaitFishCycle_ = false;
    sawFishingSinceRun_ = false;
    awaitStartedAt_ = 0;
    retryAfterAt_ = 0;
    lastMaintainCheckAt_ = 0;
    queuedAt_ = 0;
}

void AutoTotemWorkflowState::ScheduleRetry(std::int64_t nowMs)
{
    retryAfterAt_ = nowMs + settings_.retryDelayMs;
}

AutoTotemWorkflowOutput AutoTotemWorkflowState::Update(const AutoTotemWorkflowInput& in)
{
    AutoTotemWorkflowOutput out{};
    out.pending = pending_;
    out.awaitFishCycle = awaitFishCycle_;
    out.shouldBlockCasting = pending_ && in.boundaryOpen;

    if (!in.runtimeEnabled)
    {
        Reset();
        out.reason = "runtime-disabled";
        return out;
    }

    if (awaitFishCycle_)
    {
        if (in.reelVisible)
        {
            sawFishingSinceRun_ = true;
            out.awaitFishCycle = true;
            out.reason = "await-fishing-cycle-active";
            return out;
        }

        if (sawFishingSinceRun_ || (awaitStartedAt_ != 0 && in.nowMs - awaitStartedAt_ >= settings_.awaitFishCycleTimeoutMs))
        {
            awaitFishCycle_ = false;
            sawFishingSinceRun_ = false;
            awaitStartedAt_ = 0;
            lastMaintainCheckAt_ = in.nowMs - 1000;
            out.awaitFishCycle = false;
            out.reason = "await-cycle-released";
            return out;
        }

        out.awaitFishCycle = true;
        out.reason = "await-cycle-waiting";
        return out;
    }

    if (!pending_ && in.nowMs < retryAfterAt_)
    {
        out.action = AutoTotemWorkflowAction::WaitRetry;
        out.reason = "retry-cooldown";
        return out;
    }

    if (pending_ && !in.boundaryOpen)
    {
        out.action = AutoTotemWorkflowAction::HoldAddon;
        out.pending = true;
        out.shouldBlockCasting = true;
        out.reason = "pending-wait-boundary";
        return out;
    }

    if (pending_ && in.boundaryOpen)
    {
        out.action = AutoTotemWorkflowAction::StartWorkflow;
        out.reason = "pending-start-workflow-requested";

        if (IsWorkflowPendingResult(in.workflowResult))
        {
            out.pending = true;
            out.reason = "pending-start-workflow-await-result";
            return out;
        }

        if (in.workflowResult == AutoTotemWorkflowInput::Result::Success)
        {
            awaitFishCycle_ = true;
            sawFishingSinceRun_ = false;
            awaitStartedAt_ = in.nowMs;
            lastMaintainCheckAt_ = in.nowMs;
            pending_ = false;
            out.pending = false;
            out.awaitFishCycle = true;
            out.reason = "workflow-success-await";
        }
        else if (in.workflowResult == AutoTotemWorkflowInput::Result::BlockedByGate ||
                 in.workflowResult == AutoTotemWorkflowInput::Result::ActiveStateUnreadable)
        {
            pending_ = true;
            out.pending = true;
            out.shouldBlockCasting = true;
            out.reason = in.workflowResult == AutoTotemWorkflowInput::Result::BlockedByGate
                ? "workflow-blocked-gate-requeue"
                : "workflow-active-unreadable-requeue";
        }
        else
        {
            pending_ = false;
            out.pending = false;
            ScheduleRetry(in.nowMs);
            out.reason = "workflow-failed-retry";
        }

        return out;
    }

    if (!pending_ && !in.workflowNeedsRun)
    {
        lastMaintainCheckAt_ = in.nowMs;
        out.reason = "conditions-satisfied";
        return out;
    }

    if (!pending_ && in.workflowNeedsRun)
    {
        if (!in.boundaryOpen)
        {
            if (in.nowMs - lastMaintainCheckAt_ < settings_.maintainIntervalMs)
            {
                out.reason = "maintain-throttle";
                return out;
            }

            pending_ = true;
            queuedAt_ = in.nowMs;
            lastMaintainCheckAt_ = in.nowMs;
            out.action = AutoTotemWorkflowAction::QueuePending;
            out.pending = true;
            out.reason = "queue-pending";
            return out;
        }

        out.action = AutoTotemWorkflowAction::StartWorkflow;
        out.reason = "start-workflow-immediate-requested";
        if (IsWorkflowPendingResult(in.workflowResult))
        {
            out.reason = "start-workflow-immediate-await-result";
            return out;
        }
        if (in.workflowResult == AutoTotemWorkflowInput::Result::Success)
        {
            awaitFishCycle_ = true;
            sawFishingSinceRun_ = false;
            awaitStartedAt_ = in.nowMs;
            lastMaintainCheckAt_ = in.nowMs;
            out.awaitFishCycle = true;
            out.reason = "workflow-success-await";
        }
        else if (in.workflowResult == AutoTotemWorkflowInput::Result::BlockedByGate ||
                 in.workflowResult == AutoTotemWorkflowInput::Result::ActiveStateUnreadable)
        {
            pending_ = true;
            queuedAt_ = in.nowMs;
            lastMaintainCheckAt_ = in.nowMs;
            out.pending = true;
            out.shouldBlockCasting = true;
            out.reason = in.workflowResult == AutoTotemWorkflowInput::Result::BlockedByGate
                ? "workflow-blocked-gate-requeue"
                : "workflow-active-unreadable-requeue";
        }
        else
        {
            ScheduleRetry(in.nowMs);
            out.reason = "workflow-failed-retry";
        }
        return out;
    }

    return out;
}
}
