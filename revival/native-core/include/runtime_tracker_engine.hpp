#pragma once

#include <cstdint>
#include <optional>
#include <string>

#include "fishing_runtime_context.hpp"
#include "hold_applier.hpp"
#include "input_actuator.hpp"
#include "bellona_dual_controller.hpp"
#include "bellona_side_assigner.hpp"
#include "auto_totem_boundary.hpp"
#include "auto_totem_workflow_state.hpp"
#include "auto_totem_workflow_executor.hpp"
#include "automation_input_gate.hpp"
#include "tracker_orchestrator.hpp"
#include "tracking1_controller.hpp"
#include "tracking1_settings.hpp"
#include "tracking2_controller.hpp"
#include "tracking2_settings.hpp"
#include "tracking3_controller.hpp"
#include "tracking3_settings.hpp"

namespace macro_port
{
struct RuntimeTrackerEngineSettings
{
    enum class TrackingMode
    {
        Tracking1,
        Tracking2,
        Tracking3,
    };

    TrackingMode mode = TrackingMode::Tracking1;
    Tracking1Settings tracking1{};
    Tracking2Settings tracking2{};
    Tracking3Settings tracking3{};
    bool startupAssistEnabled = false;
    std::int64_t startupAssistDurationMs = 800;
    Tracking2Settings startupAssistTracking2{};
    bool perfectCastBurstCacheEnabled = true;
    std::int64_t perfectPowerStickyMs = 12;
    int fishingActionDelayMs = 0;
    bool bellonaEnabled = false;
    BellonaDualSettings bellona{};
    BellonaSideAssignerSettings bellonaSide{};
    AutoTotemWorkflowSettings autoTotem{};
    std::string autoTotemGateOwner = "AUTO_TOTEM";
    int autoTotemGatePriority = 200;
};

struct RuntimeTrackerTickInput
{
    std::int64_t nowMs = 0;
    bool suspended = false;
    bool hasSensorSnapshot = false;
    bool reelVisibleSnapshot = false;
    bool hasMetricsSnapshot = false;
    ReelMetrics metricsSnapshot{};
    std::optional<double> progressSnapshot;
    bool shakeVisible = false;
    bool castPowerReady = true;
    bool perfectCastRelease = false;
    std::optional<double> castPowerPercent;
    std::optional<bool> completionOverride;
    bool hasExternalTrackingDecision = false;
    bool externalDesiredHold = false;
    bool externalDecisionApplied = false;
    int externalDecisionMode = 0;
    std::optional<double> externalDecisionError;
    std::optional<double> externalDecisionControl;
    bool autoTotemRuntimeEnabled = true;
    bool autoTotemWorkflowNeedsRun = false;
    AutoTotemWorkflowInput::Result autoTotemWorkflowResult = AutoTotemWorkflowInput::Result::None;
};

struct RuntimeTrackerTickResult
{
    FishingPhase phase = FishingPhase::Off;
    bool hasMetrics = false;
    bool holdDesired = false;
    bool holdApplied = false;
    bool rightHoldApplied = false;
    bool clickShake = false;
    bool startupAssistActive = false;
    bool automationGateOpen = false;
    AutoTotemWorkflowAction autoTotemAction = AutoTotemWorkflowAction::None;
    bool autoTotemPending = false;
    bool autoTotemAwaitFishCycle = false;
    bool autoTotemShouldBlockCasting = false;
    std::string autoTotemReason;
    std::string trackingBranch;
    std::string trackingDecisionMode;
    std::optional<double> trackingError;
    std::optional<double> trackingControl;
    std::optional<double> progressPercent;
    std::string message;
};

class RuntimeTrackerEngine
{
public:
    RuntimeTrackerEngine(
        FishingRuntimeContext* runtime,
        TrackerOrchestrator* orchestrator,
        Tracking1Controller* controller,
        IInputActuator* inputActuator,
        HoldApplier* holdApplier,
        RuntimeTrackerEngineSettings settings = {},
        IAutoTotemWorkflowExecutor* autoTotemExecutor = nullptr,
        IAutomationInputGate* automationInputGate = nullptr);

    void Start(std::int64_t nowMs, FishingCastingMode mode);
    void Stop(std::int64_t nowMs);
    RuntimeTrackerTickResult Tick(const RuntimeTrackerTickInput& input);

private:
    FishingRuntimeContext* runtime_ = nullptr;
    TrackerOrchestrator* orchestrator_ = nullptr;
    Tracking1Controller* controller_ = nullptr;
    Tracking2Controller tracking2Controller_{};
    Tracking3Controller tracking3Controller_{};
    Tracking1Controller bellonaRightTracking1_{};
    Tracking2Controller bellonaRightTracking2_{};
    Tracking3Controller bellonaRightTracking3_{};
    HoldApplier* holdApplier_ = nullptr;
    IInputActuator* inputActuator_ = nullptr;
    RuntimeTrackerEngineSettings settings_{};
    BellonaDualController bellonaRight_{};
    BellonaSideAssigner bellonaSide_{};
    bool rightAppliedHolding_ = false;
    bool completionArmed_ = false;
    FishingPhase lastPhase_ = FishingPhase::Off;
    bool hadMetricsLastTick_ = false;
    bool startupAssistActive_ = false;
    std::int64_t startupAssistStartedAt_ = 0;
    double startupAssistStartFishCenter_ = 0.0;
    std::int64_t lastCastPowerSeenAt_ = 0;
    std::optional<double> lastCastPowerPercent_;
    AutoTotemBoundary automationGate_{};
    AutoTotemWorkflowState autoTotemState_{};
    IAutoTotemWorkflowExecutor* autoTotemExecutor_ = nullptr;
    IAutomationInputGate* automationInputGate_ = nullptr;
    bool autoTotemWorkflowInFlight_ = false;
    bool autoTotemGateHeld_ = false;
};
}
