#include "runtime_tracker_engine.hpp"

#include <cmath>
#include <string>

namespace macro_port
{
RuntimeTrackerEngine::RuntimeTrackerEngine(
    FishingRuntimeContext* runtime,
    TrackerOrchestrator* orchestrator,
    Tracking1Controller* controller,
    IInputActuator* inputActuator,
    HoldApplier* holdApplier,
    RuntimeTrackerEngineSettings settings,
    IAutoTotemWorkflowExecutor* autoTotemExecutor,
    IAutomationInputGate* automationInputGate)
    : runtime_(runtime),
      orchestrator_(orchestrator),
      controller_(controller),
      inputActuator_(inputActuator),
      holdApplier_(holdApplier),
      settings_(settings),
      bellonaRight_(settings.bellona),
      bellonaSide_(settings.bellonaSide),
      autoTotemState_(settings.autoTotem),
      autoTotemExecutor_(autoTotemExecutor),
      automationInputGate_(automationInputGate)
{
}

void RuntimeTrackerEngine::Start(std::int64_t nowMs, FishingCastingMode mode)
{
    if (orchestrator_ != nullptr)
    {
        orchestrator_->Start(nowMs, mode);
    }

    if (controller_ != nullptr)
    {
        controller_->Reset();
    }
    tracking2Controller_.Reset();
    tracking3Controller_.Reset(static_cast<double>(nowMs) / 1000.0);
    bellonaRightTracking1_.Reset();
    bellonaRightTracking2_.Reset();
    bellonaRightTracking3_.Reset(static_cast<double>(nowMs) / 1000.0);

    if (runtime_ != nullptr)
    {
        runtime_->ResetCache();
    }
    bellonaRight_.Reset();
    bellonaSide_.Reset();
    rightAppliedHolding_ = false;
    completionArmed_ = false;
    lastPhase_ = FishingPhase::Off;
    hadMetricsLastTick_ = false;
    startupAssistActive_ = false;
    startupAssistStartedAt_ = 0;
    startupAssistStartFishCenter_ = 0.0;
    lastCastPowerSeenAt_ = 0;
    lastCastPowerPercent_.reset();
    automationGate_.Reset();
    autoTotemState_.Reset();
    autoTotemWorkflowInFlight_ = false;
    autoTotemGateHeld_ = false;
}

void RuntimeTrackerEngine::Stop(std::int64_t nowMs)
{
    if (orchestrator_ != nullptr)
    {
        orchestrator_->Stop();
    }

    if (controller_ != nullptr)
    {
        controller_->Reset();
    }
    tracking2Controller_.Reset();
    tracking3Controller_.Reset(static_cast<double>(nowMs) / 1000.0);
    bellonaRightTracking1_.Reset();
    bellonaRightTracking2_.Reset();
    bellonaRightTracking3_.Reset(static_cast<double>(nowMs) / 1000.0);

    if (holdApplier_ != nullptr)
    {
        holdApplier_->Release(nowMs);
    }
    bellonaRight_.Reset();
    bellonaSide_.Reset();
    if (inputActuator_ != nullptr && rightAppliedHolding_)
    {
        inputActuator_->RightUp();
    }
    rightAppliedHolding_ = false;
    completionArmed_ = false;
    lastPhase_ = FishingPhase::Off;
    hadMetricsLastTick_ = false;
    startupAssistActive_ = false;
    startupAssistStartedAt_ = 0;
    startupAssistStartFishCenter_ = 0.0;
    lastCastPowerSeenAt_ = 0;
    lastCastPowerPercent_.reset();
    automationGate_.Reset();
    autoTotemState_.Reset();
    autoTotemWorkflowInFlight_ = false;
    if (automationInputGate_ != nullptr && autoTotemGateHeld_)
    {
        automationInputGate_->Exit(settings_.autoTotemGateOwner);
    }
    autoTotemGateHeld_ = false;
}

RuntimeTrackerTickResult RuntimeTrackerEngine::Tick(const RuntimeTrackerTickInput& input)
{
    RuntimeTrackerTickResult result{};
    if (runtime_ == nullptr || orchestrator_ == nullptr || holdApplier_ == nullptr)
    {
        result.phase = FishingPhase::Error;
        result.message = "RuntimeTrackerEngine dependencies missing.";
        return result;
    }

    bool reelVisible = false;
    std::optional<ReelMetrics> metrics;
    std::optional<double> progress;
    if (input.hasSensorSnapshot)
    {
        reelVisible = input.reelVisibleSnapshot;
        if (input.hasMetricsSnapshot)
        {
            metrics = input.metricsSnapshot;
        }
        progress = input.progressSnapshot;
    }
    else
    {
        reelVisible = runtime_->IsReelGuiVisible();
        const auto reelContext = runtime_->GetReelContext();
        metrics = reelContext.has_value() ? runtime_->ReadMetrics(*reelContext) : std::optional<ReelMetrics>{};
        progress = runtime_->GetFishingCompletionPercent();
    }
    result.progressPercent = progress;
    result.hasMetrics = metrics.has_value();

    // Arm completion only after we've observed a non-complete value in the
    // current fishing cycle. This prevents stale 100% bars from instantly
    // recasting right after entering fishing.
    if (lastPhase_ != FishingPhase::Fishing)
    {
        completionArmed_ = false;
    }
    if (progress.has_value() && *progress < 99.0)
    {
        completionArmed_ = true;
    }

    bool completionReached = false;
    if (input.completionOverride.has_value())
    {
        completionReached = completionArmed_ && *input.completionOverride;
    }
    else if (progress.has_value())
    {
        completionReached = completionArmed_ && (*progress >= 99.9);
    }
    if (completionReached)
    {
        automationGate_.LatchCompletion(input.nowMs);
    }

    bool effectiveCastPowerReady = input.castPowerReady;
    std::optional<double> effectiveCastPowerPercent = input.castPowerPercent;
    if (input.castPowerReady)
    {
        lastCastPowerSeenAt_ = input.nowMs;
        if (input.castPowerPercent.has_value())
        {
            lastCastPowerPercent_ = input.castPowerPercent;
        }
    }
    else if (settings_.perfectCastBurstCacheEnabled &&
        lastCastPowerSeenAt_ != 0 &&
        input.nowMs - lastCastPowerSeenAt_ <= settings_.perfectPowerStickyMs)
    {
        effectiveCastPowerReady = true;
        if (!effectiveCastPowerPercent.has_value() && lastCastPowerPercent_.has_value())
        {
            effectiveCastPowerPercent = lastCastPowerPercent_;
        }
    }

    const auto phaseOut = orchestrator_->Tick(TrackerTickInput{
        input.nowMs,
        input.suspended,
        reelVisible,
        input.shakeVisible,
        metrics.has_value(),
        completionReached,
        progress,
        effectiveCastPowerReady,
        input.perfectCastRelease,
        effectiveCastPowerPercent});
    result.phase = phaseOut.phase;
    result.clickShake = phaseOut.clickShake;
    result.message = phaseOut.message;
    result.automationGateOpen = automationGate_.IsOpen(
        input.nowMs,
        phaseOut.holdLeft,
        input.castPowerReady);
    AutoTotemWorkflowInput::Result workflowResult = input.autoTotemWorkflowResult;
    if (autoTotemExecutor_ != nullptr && autoTotemWorkflowInFlight_)
    {
        workflowResult = autoTotemExecutor_->PollResult(input.nowMs);
    }

    auto autoTotem = autoTotemState_.Update(AutoTotemWorkflowInput{
        input.nowMs,
        input.autoTotemRuntimeEnabled,
        reelVisible,
        result.automationGateOpen,
        input.autoTotemWorkflowNeedsRun,
        workflowResult});

    if (autoTotemExecutor_ != nullptr && autoTotem.action == AutoTotemWorkflowAction::StartWorkflow)
    {
        if (!autoTotemWorkflowInFlight_)
        {
            if (automationInputGate_ != nullptr)
            {
                if (!automationInputGate_->TryEnter(settings_.autoTotemGateOwner, settings_.autoTotemGatePriority))
                {
                    autoTotem = autoTotemState_.Update(AutoTotemWorkflowInput{
                        input.nowMs,
                        input.autoTotemRuntimeEnabled,
                        reelVisible,
                        result.automationGateOpen,
                        input.autoTotemWorkflowNeedsRun,
                        AutoTotemWorkflowInput::Result::BlockedByGate});
                }
                else
                {
                    autoTotemGateHeld_ = true;
                    autoTotemExecutor_->RequestStart(input.nowMs);
                    autoTotemWorkflowInFlight_ = true;
                }
            }
            else
            {
                autoTotemExecutor_->RequestStart(input.nowMs);
                autoTotemWorkflowInFlight_ = true;
            }
        }

        if (autoTotemWorkflowInFlight_)
        {
            auto polled = autoTotemExecutor_->PollResult(input.nowMs);
            if (polled != AutoTotemWorkflowInput::Result::None)
            {
                autoTotem = autoTotemState_.Update(AutoTotemWorkflowInput{
                    input.nowMs,
                    input.autoTotemRuntimeEnabled,
                    reelVisible,
                    result.automationGateOpen,
                    input.autoTotemWorkflowNeedsRun,
                    polled});
                autoTotemWorkflowInFlight_ = false;
                if (automationInputGate_ != nullptr && autoTotemGateHeld_)
                {
                    automationInputGate_->Exit(settings_.autoTotemGateOwner);
                }
                autoTotemGateHeld_ = false;
            }
        }
    }
    else if (workflowResult != AutoTotemWorkflowInput::Result::None)
    {
        autoTotemWorkflowInFlight_ = false;
        if (automationInputGate_ != nullptr && autoTotemGateHeld_)
        {
            automationInputGate_->Exit(settings_.autoTotemGateOwner);
        }
        autoTotemGateHeld_ = false;
    }
    result.autoTotemAction = autoTotem.action;
    result.autoTotemPending = autoTotem.pending;
    result.autoTotemAwaitFishCycle = autoTotem.awaitFishCycle;
    result.autoTotemShouldBlockCasting = autoTotem.shouldBlockCasting;
    result.autoTotemReason = autoTotem.reason;

    bool desiredHold = phaseOut.holdLeft;
    if (phaseOut.phase == FishingPhase::Fishing && metrics.has_value() && controller_ != nullptr)
    {
        const auto nowSeconds = static_cast<double>(input.nowMs) / 1000.0;
        const bool startupAssistEnabled = settings_.startupAssistEnabled ||
            settings_.mode == RuntimeTrackerEngineSettings::TrackingMode::Tracking1;
        const bool freshReelTick = !hadMetricsLastTick_;
        if (freshReelTick)
        {
            // Match the Swift tracker: a fresh reel cycle resets all active
            // controllers and releases any lingering hold state before the new
            // fishing loop starts evaluating metrics.
            if (controller_ != nullptr)
            {
                controller_->Reset();
            }
            tracking2Controller_.Reset();
            tracking3Controller_.Reset(static_cast<double>(input.nowMs) / 1000.0);
            bellonaRightTracking1_.Reset();
            bellonaRightTracking2_.Reset();
            bellonaRightTracking3_.Reset(static_cast<double>(input.nowMs) / 1000.0);
            holdApplier_->Release(input.nowMs);
            rightAppliedHolding_ = false;
            startupAssistActive_ = startupAssistEnabled;
            startupAssistStartedAt_ = input.nowMs;
            startupAssistStartFishCenter_ = metrics->fishCenter;
        }

        if (freshReelTick || phaseOut.message == "Reel detected; releasing cast input.")
        {
            controller_->Release();
            tracking2Controller_.Reset();
            tracking3Controller_.Reset(static_cast<double>(input.nowMs) / 1000.0);
            desiredHold = false;
            result.trackingBranch = "InputSettle";
            result.trackingDecisionMode = "InputSettle";
            result.holdDesired = false;
            holdApplier_->Release(input.nowMs);
            result.holdApplied = holdApplier_->IsHolding();
            hadMetricsLastTick_ = phaseOut.phase == FishingPhase::Fishing && metrics.has_value();
            if (result.phase != lastPhase_)
            {
                lastPhase_ = result.phase;
            }
            return result;
        }

        const bool startupAssistTimedOut = startupAssistActive_ &&
            input.nowMs - startupAssistStartedAt_ > settings_.startupAssistDurationMs;
        const bool startupAssistFishMoved = startupAssistActive_ &&
            std::abs(metrics->fishCenter - startupAssistStartFishCenter_) >= 0.0125;
        if (startupAssistTimedOut || startupAssistFishMoved)
        {
            startupAssistActive_ = false;
        }

        result.startupAssistActive = startupAssistActive_;
        if (input.hasExternalTrackingDecision)
        {
            desiredHold = input.externalDesiredHold;
            result.trackingBranch = "CSharpTracking";
            result.trackingDecisionMode = "CSharpMode" + std::to_string(input.externalDecisionMode);
            result.trackingError = input.externalDecisionError;
            result.trackingControl = input.externalDecisionControl;
            startupAssistActive_ = false;
            result.startupAssistActive = false;
        }
        else
        {
            desiredHold = false;
            result.trackingBranch = "CSharpTrackingMissing";
            result.trackingDecisionMode = "ReleaseUntilCSharpDecision";
            result.trackingError = std::nullopt;
            result.trackingControl = std::nullopt;
        }

        /*
        Native minigame tracking is intentionally disabled. The WPF process
        computes the same C# tracking decisions as the Swift client and passes
        only the desired hold state into this core.
        if (startupAssistActive_)
        {
            const auto assistDecision = tracking2Controller_.Update(*metrics, settings_.startupAssistTracking2);
            desiredHold = assistDecision.holding;
            result.trackingBranch = "StartupAssist";
            result.trackingDecisionMode = "StartupAssist";
            result.trackingError = assistDecision.error;
            result.trackingControl = assistDecision.control;
        }
        else
        {
            switch (settings_.mode)
            {
            case RuntimeTrackerEngineSettings::TrackingMode::Tracking2:
            {
                const auto decision = tracking2Controller_.Update(*metrics, settings_.tracking2);
                desiredHold = decision.holding;
                result.trackingBranch = "Tracking2";
                result.trackingError = decision.error;
                result.trackingControl = decision.control;
                break;
            }
            case RuntimeTrackerEngineSettings::TrackingMode::Tracking3:
            {
                const auto decision = tracking3Controller_.Update(*metrics, settings_.tracking3, nowSeconds);
                desiredHold = decision.desiredHolding;
                result.trackingBranch = "Tracking3";
                result.trackingDecisionMode =
                    decision.mode == Tracking3DecisionMode::Warmup ? "Warmup" :
                    decision.mode == Tracking3DecisionMode::EdgeRecovery ? "EdgeRecovery" :
                    decision.mode == Tracking3DecisionMode::HardCorrection ? "HardCorrection" :
                    decision.mode == Tracking3DecisionMode::FineTracking ? "FineTracking" :
                    decision.mode == Tracking3DecisionMode::CenterPulse ? "CenterPulse" :
                    "Unknown";
                result.trackingError = decision.error;
                result.trackingControl = decision.control;
                break;
            }
            case RuntimeTrackerEngineSettings::TrackingMode::Tracking1:
            default:
            {
                const auto decision = controller_->UpdateTracking(*metrics, settings_.tracking1, nowSeconds);
                desiredHold = decision.holding;
                result.trackingBranch = "Tracking1";
                result.trackingError = decision.error;
                result.trackingControl = decision.control;
                break;
            }
            }
        }
        */
    }
    else if (phaseOut.phase != FishingPhase::Fishing && controller_ != nullptr)
    {
        controller_->Release();
        startupAssistActive_ = false;
        startupAssistStartedAt_ = 0;
        startupAssistStartFishCenter_ = 0.0;
    }

    hadMetricsLastTick_ = phaseOut.phase == FishingPhase::Fishing && metrics.has_value();

    if (result.phase != lastPhase_)
    {
        lastPhase_ = result.phase;
    }

    result.holdDesired = desiredHold;
    if (input.hasExternalTrackingDecision && input.externalDecisionApplied)
    {
        result.holdApplied = desiredHold;
    }
    else
    {
        holdApplier_->Apply(desiredHold, input.nowMs, settings_.fishingActionDelayMs);
        result.holdApplied = holdApplier_->IsHolding();
    }

    if (phaseOut.resetCycle && controller_ != nullptr)
    {
        controller_->Reset();
        tracking2Controller_.Reset();
        tracking3Controller_.Reset(static_cast<double>(input.nowMs) / 1000.0);
    }

    if (settings_.bellonaEnabled)
    {
        std::optional<ReelMetrics> rightMetrics;
        if (reelVisible)
        {
            auto ordered = runtime_->GetOrderedReelContexts();
            auto rightContext = bellonaSide_.SelectRight(ordered);
            if (rightContext.has_value())
            {
                rightMetrics = runtime_->ReadMetrics(rightContext->context);
            }
        }

        bool rightDesired = false;
        if (rightMetrics.has_value())
        {
            const auto nowSeconds = static_cast<double>(input.nowMs) / 1000.0;
            switch (settings_.mode)
            {
            case RuntimeTrackerEngineSettings::TrackingMode::Tracking2:
                rightDesired = bellonaRightTracking2_.Update(*rightMetrics, settings_.tracking2).holding;
                break;
            case RuntimeTrackerEngineSettings::TrackingMode::Tracking3:
                // Keep Bellona right-side behavior aligned with hybrid-style control.
                rightDesired = bellonaRightTracking2_.Update(*rightMetrics, settings_.tracking2).holding;
                break;
            case RuntimeTrackerEngineSettings::TrackingMode::Tracking1:
            default:
                rightDesired = bellonaRightTracking1_.UpdateTracking(*rightMetrics, settings_.tracking1, nowSeconds).holding;
                break;
            }

            // Preserve a simple positional fallback so right-side control remains
            // active even when controller internals transiently return neutral.
            if (!rightDesired && rightMetrics->fishCenter > rightMetrics->playerbarCenter + 0.002)
            {
                rightDesired = true;
            }
        }
        rightDesired = bellonaRight_.Filter(
            rightDesired,
            rightMetrics,
            input.nowMs,
            settings_.mode == RuntimeTrackerEngineSettings::TrackingMode::Tracking3);

        if (inputActuator_ != nullptr)
        {
            if (rightDesired && !rightAppliedHolding_)
            {
                inputActuator_->RightDown();
                rightAppliedHolding_ = true;
            }
            else if (!rightDesired && rightAppliedHolding_)
            {
                inputActuator_->RightUp();
                rightAppliedHolding_ = false;
            }
        }
        result.rightHoldApplied = rightAppliedHolding_;
    }
    else
    {
        if (inputActuator_ != nullptr && rightAppliedHolding_)
        {
            inputActuator_->RightUp();
        }
        rightAppliedHolding_ = false;
        result.rightHoldApplied = false;
    }

    return result;
}
}
