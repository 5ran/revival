#include <windows.h>

#include <chrono>
#include <cstring>
#include <cstdint>
#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <iterator>
#include <memory>
#include <mutex>
#include <optional>
#include <sstream>
#include <string>
#include <thread>
#include <atomic>
#include <ctime>
#include <unordered_map>

#include "automation_input_gate.hpp"
#include "aquarium_sequence_runner.hpp"
#include "auto_angler_runner.hpp"
#include "auto_enchant_runner.hpp"
#include "auto_sovereign_recharge_runner.hpp"
#include "fishing_runtime_context.hpp"
#include "hold_applier.hpp"
#include "hunt_detect_core.hpp"
#include "native_mouse.hpp"
#include "local_offsets_source.hpp"
#include "offsets_source_provider.hpp"
#include "tracking2_appraise_runner.hpp"
#include "treasure_ref_port.hpp"
#include "win32_roblox_memory.hpp"
#include "runtime_tracker_engine.hpp"
#include "tracker_orchestrator.hpp"
#include "tracking1_controller.hpp"

namespace {

constexpr wchar_t kPipeName[] = L"\\\\.\\pipe\\OpenMacroSwift.Core";
constexpr wchar_t kOverlayMappingName[] = L"Local\\OpenMacroSwiftOverlay";
constexpr wchar_t kTrackingMetricsMappingName[] = L"Local\\OpenMacroSwiftTrackingMetrics";
constexpr std::uint32_t kTrackingMetricsMagic = 0x4F4D5452; // OMTR
constexpr std::uint64_t kTrackingMetricsFreshMs = 500;
std::mutex g_debugLogMutex;
std::ofstream g_debugLogFile;

#pragma pack(push, 1)
struct OverlaySharedState {
    std::uint32_t magic = 0x4F4D4F56; // OMOV
    std::uint32_t sequence = 0;
    std::uint32_t visible = 0;
    std::uint32_t reserved = 0;
    std::uint64_t tickMs = 0;
    double fishX = 0.0;
    double fishY = 0.0;
    double fishW = 0.0;
    double fishH = 0.0;
    double controlX = 0.0;
    double controlY = 0.0;
    double controlW = 0.0;
    double controlH = 0.0;
};

struct TrackingMetricsSharedState {
    std::uint32_t magic = kTrackingMetricsMagic;
    std::uint32_t sequence = 0;
    std::uint32_t phase = 0; // 0 casting, 1 shake, 2 tracking
    std::uint32_t hasMetrics = 0;
    std::uint64_t tickMs = 0;
    double fishCenter = 0.0;
    double playerbarCenter = 0.0;
    double playerbarWidth = 0.0;
    double progress = 0.0;
    std::uint32_t hasProgress = 0;
    std::uint32_t shakeVisible = 0;
    std::uint32_t reelVisible = 0;
    std::uint32_t reserved = 0;
    std::uint32_t hasDecision = 0;
    std::uint32_t desiredHold = 0;
    std::uint32_t inputHeld = 0;
    std::uint32_t decisionMode = 0;
    std::uint32_t decisionReserved = 0;
    double decisionError = 0.0;
    double decisionControl = 0.0;
    std::uint64_t reserved2 = 0;
    std::uint32_t decisionApplied = 0;
    std::uint32_t reserved3 = 0;
};
#pragma pack(pop)

struct TrackingMetricsSnapshot {
    std::uint32_t phase = 0;
    bool hasMetrics = false;
    bool shakeVisible = false;
    bool reelVisible = false;
    std::uint64_t tickMs = 0;
    macro_port::ReelMetrics metrics{};
    std::optional<double> progress;
    bool hasDecision = false;
    bool desiredHold = false;
    bool inputHeld = false;
    bool decisionApplied = false;
    int decisionMode = 0;
    std::optional<double> decisionError;
    std::optional<double> decisionControl;
};

class OverlaySharedMemory {
public:
    ~OverlaySharedMemory()
    {
        Close();
    }

    bool Open()
    {
        if (mapping_ != nullptr && view_ != nullptr) {
            return true;
        }

        mapping_ = CreateFileMappingW(
            INVALID_HANDLE_VALUE,
            nullptr,
            PAGE_READWRITE,
            0,
            static_cast<DWORD>(sizeof(OverlaySharedState)),
            kOverlayMappingName);
        if (mapping_ == nullptr) {
            return false;
        }

        view_ = static_cast<OverlaySharedState*>(MapViewOfFile(mapping_, FILE_MAP_ALL_ACCESS, 0, 0, sizeof(OverlaySharedState)));
        if (view_ == nullptr) {
            Close();
            return false;
        }

        Clear();
        return true;
    }

    void Clear()
    {
        if (view_ == nullptr) {
            return;
        }

        Write(false, static_cast<std::uint64_t>(GetTickCount64()), std::nullopt, std::nullopt);
    }

    void Write(
        bool visible,
        std::uint64_t tickMs,
        const std::optional<macro_port::GuiBounds>& fishBounds,
        const std::optional<macro_port::GuiBounds>& controlBounds)
    {
        if (view_ == nullptr) {
            return;
        }

        const auto seq = view_->sequence + 1;
        view_->sequence = seq | 1U;
        std::atomic_thread_fence(std::memory_order_release);

        view_->magic = 0x4F4D4F56;
        view_->visible = visible ? 1U : 0U;
        view_->tickMs = tickMs;
        if (visible && fishBounds.has_value() && controlBounds.has_value()) {
            view_->fishX = fishBounds->x;
            view_->fishY = fishBounds->y;
            view_->fishW = fishBounds->width;
            view_->fishH = fishBounds->height;
            view_->controlX = controlBounds->x;
            view_->controlY = controlBounds->y;
            view_->controlW = controlBounds->width;
            view_->controlH = controlBounds->height;
        } else {
            view_->fishX = 0.0;
            view_->fishY = 0.0;
            view_->fishW = 0.0;
            view_->fishH = 0.0;
            view_->controlX = 0.0;
            view_->controlY = 0.0;
            view_->controlW = 0.0;
            view_->controlH = 0.0;
        }

        std::atomic_thread_fence(std::memory_order_release);
        view_->sequence = (seq | 1U) + 1U;
    }

private:
    void Close()
    {
        if (view_ != nullptr) {
            UnmapViewOfFile(view_);
            view_ = nullptr;
        }

        if (mapping_ != nullptr) {
            CloseHandle(mapping_);
            mapping_ = nullptr;
        }
    }

    HANDLE mapping_ = nullptr;
    OverlaySharedState* view_ = nullptr;
};

class TrackingMetricsSharedMemory {
public:
    ~TrackingMetricsSharedMemory()
    {
        Close();
    }

    void Write(
        std::uint32_t phase,
        bool hasMetrics,
        std::uint64_t tickMs,
        const std::optional<macro_port::ReelMetrics>& metrics,
        const std::optional<double>& progress,
        bool shakeVisible,
        bool reelVisible,
        bool desiredHold,
        std::optional<double> decisionError,
        std::optional<double> decisionControl,
        std::uint32_t decisionMode,
        bool decisionApplied)
    {
        if (!Open()) {
            return;
        }

        const auto seq = view_->sequence + 1U;
        view_->sequence = seq | 1U;
        std::atomic_thread_fence(std::memory_order_release);

        view_->magic = kTrackingMetricsMagic;
        view_->phase = phase;
        view_->hasMetrics = hasMetrics ? 1U : 0U;
        view_->tickMs = tickMs;
        view_->fishCenter = metrics.has_value() ? metrics->fishCenter : 0.0;
        view_->playerbarCenter = metrics.has_value() ? metrics->playerbarCenter : 0.0;
        view_->playerbarWidth = metrics.has_value() ? metrics->playerbarWidth : 0.0;
        view_->progress = progress.value_or(0.0);
        view_->hasProgress = progress.has_value() ? 1U : 0U;
        view_->shakeVisible = shakeVisible ? 1U : 0U;
        view_->reelVisible = reelVisible ? 1U : 0U;
        view_->hasDecision = 1U;
        view_->desiredHold = desiredHold ? 1U : 0U;
        view_->inputHeld = decisionApplied ? 1U : 0U;
        view_->decisionMode = decisionMode;
        view_->decisionError = decisionError.value_or(0.0);
        view_->decisionControl = decisionControl.value_or(0.0);
        view_->decisionApplied = decisionApplied ? 1U : 0U;

        std::atomic_thread_fence(std::memory_order_release);
        view_->sequence = (seq | 1U) + 1U;
    }

    std::optional<TrackingMetricsSnapshot> TryRead()
    {
        if (!Open()) {
            return std::nullopt;
        }

        for (int attempt = 0; attempt < 3; ++attempt) {
            const auto sequenceStart = view_->sequence;
            if ((sequenceStart & 1U) != 0U) {
                continue;
            }

            std::atomic_thread_fence(std::memory_order_acquire);
            if (view_->magic != kTrackingMetricsMagic) {
                return std::nullopt;
            }

            TrackingMetricsSnapshot snapshot{};
            snapshot.phase = view_->phase;
            snapshot.hasMetrics = view_->hasMetrics != 0;
            snapshot.shakeVisible = view_->shakeVisible != 0;
            snapshot.reelVisible = view_->reelVisible != 0;
            snapshot.tickMs = view_->tickMs;
            snapshot.metrics = macro_port::ReelMetrics{
                view_->fishCenter,
                view_->playerbarCenter,
                view_->playerbarWidth
            };
            if (view_->hasProgress != 0) {
                snapshot.progress = view_->progress;
            }
            snapshot.hasDecision = view_->hasDecision != 0;
            snapshot.desiredHold = view_->desiredHold != 0;
            snapshot.inputHeld = view_->inputHeld != 0;
            snapshot.decisionApplied = view_->decisionApplied != 0;
            snapshot.decisionMode = static_cast<int>(view_->decisionMode);
            if (snapshot.hasDecision) {
                snapshot.decisionError = view_->decisionError;
                snapshot.decisionControl = view_->decisionControl;
            }

            std::atomic_thread_fence(std::memory_order_acquire);
            const auto sequenceEnd = view_->sequence;
            if (sequenceStart == sequenceEnd && (sequenceEnd & 1U) == 0U) {
                return snapshot;
            }
        }

        return std::nullopt;
    }

private:
    bool Open()
    {
        if (mapping_ != nullptr && view_ != nullptr) {
            return true;
        }

        mapping_ = OpenFileMappingW(FILE_MAP_READ, FALSE, kTrackingMetricsMappingName);
        if (mapping_ == nullptr) {
            return false;
        }

        view_ = static_cast<TrackingMetricsSharedState*>(
            MapViewOfFile(mapping_, FILE_MAP_READ, 0, 0, sizeof(TrackingMetricsSharedState)));
        if (view_ == nullptr) {
            Close();
            return false;
        }

        return true;
    }

    void Close()
    {
        if (view_ != nullptr) {
            UnmapViewOfFile(view_);
            view_ = nullptr;
        }

        if (mapping_ != nullptr) {
            CloseHandle(mapping_);
            mapping_ = nullptr;
        }
    }

    HANDLE mapping_ = nullptr;
    TrackingMetricsSharedState* view_ = nullptr;
};

struct CoreState {
    std::atomic<bool> running{false};
    std::chrono::steady_clock::time_point startedAt{};
    mutable std::mutex mutex;
    std::unordered_map<std::string, std::string> settings{
        {"TrackingMode", "Hybrid"},
        {"CastingMode", "Normal"},
        {"RodSlot", "1"},
        {"Hotkey", "F3"},
    };
    macro_port::LocalOffsetsSource* offsets = nullptr;
};

class NativeInputActuator final : public macro_port::IInputActuator {
public:
    void LeftDown() override { macro_port::NativeMouse::LeftDown(); }
    void LeftUp() override { macro_port::NativeMouse::LeftUp(); }
    void RightDown() override { macro_port::NativeMouse::RightDown(); }
    void RightUp() override { macro_port::NativeMouse::RightUp(); }
};

std::string readSetting(const CoreState& state, const std::string& key, const std::string& fallback);
macro_port::RuntimeTrackerEngineSettings::TrackingMode parseTrackingMode(const std::string& value);
macro_port::FishingCastingMode parseCastingMode(const std::string& value);
std::string phaseToString(macro_port::FishingPhase phase);
std::filesystem::path debugLogPath();
void debugLog(const std::string& message);
void debugLogCommand(const std::string& command);
std::string boolText(bool value);
std::string optionalDoubleText(const std::optional<double>& value, int precision = 3);
bool parseBoolSetting(const CoreState& state, const std::string& key, bool fallback = false);
double parseDoubleSetting(const CoreState& state, const std::string& key, double fallback);
int parseIntSetting(const CoreState& state, const std::string& key, int fallback);

class RuntimeHost {
public:
    RuntimeHost(CoreState& state, macro_port::LocalOffsetsSource& offsets)
        : state_(state),
          memory_(&offsets),
          runtime_(&memory_),
          hold_(&inputActuator_)
    {
        overlayShared_.Open();
        macro_port::TrackerOrchestratorSettings orchestratorSettings{};
        // Allow brief memory-read flicker without resetting fishing state.
        orchestratorSettings.fishingLostGraceMs = 220;
        orchestrator_ = macro_port::TrackerOrchestrator(orchestratorSettings);
        RebuildEngine();
    }

    ~RuntimeHost()
    {
        Stop();
    }

    void Start()
    {
        bool expected = false;
        if (!running_.compare_exchange_strong(expected, true)) {
            return;
        }

        stopRequested_.store(false);
        {
            std::lock_guard<std::mutex> lock(state_.mutex);
            state_.running = true;
            state_.startedAt = std::chrono::steady_clock::now();
            state_.settings["Phase"] = "CASTING";
            state_.settings["Message"] = "Runtime starting.";
            state_.settings["InputStatus"] = "Released";
            state_.settings["Progress"] = "---";
        }

        RebuildEngine();
        if (engine_ != nullptr) {
            engine_->Start(nowMs(), parseCastingMode(readSetting(state_, "CastingMode", "Normal")));
        }
        debugLog("runtime.start tracking=" + readSetting(state_, "TrackingMode", "Hybrid") +
            " casting=" + readSetting(state_, "CastingMode", "Normal"));

        worker_ = std::thread([this]() { Run(); });
    }

    void Stop()
    {
        if (!running_.exchange(false)) {
            return;
        }

        stopRequested_.store(true);
        if (worker_.joinable()) {
            worker_.join();
        }

        if (engine_ != nullptr) {
            engine_->Stop(nowMs());
        }

        {
            std::lock_guard<std::mutex> lock(state_.mutex);
            state_.running = false;
            state_.settings["Phase"] = "OFF";
            state_.settings["Message"] = "Runtime stopped.";
            state_.settings["InputStatus"] = "Idle";
            state_.settings["Progress"] = "---";
            state_.settings["CurrentFish"] = "---";
            state_.settings["AutoAnglerStatus"] = "Idle";
            state_.settings["AppraiseStatus"] = "Idle";
            state_.settings["AppraiseHeldFish"] = "---";
            state_.settings["TreasureStatus"] = "Idle";
            state_.settings["SovereignStatus"] = "Idle";
            state_.settings["TotemStatus"] = "Idle";
        }
        debugLog("runtime.stop");
        overlayShared_.Clear();
        trackingMetricsShared_.Write(
            static_cast<std::uint32_t>(macro_port::FishingPhase::Off),
            false,
            nowMs(),
            std::nullopt,
            std::nullopt,
            false,
            false,
            false,
            std::nullopt,
            std::nullopt,
            0U,
            false);
    }

    bool IsRunning() const
    {
        return running_.load();
    }

private:
    static constexpr std::int64_t kPerfectPowerDetectBurstMs = 12;
    static constexpr std::int64_t kPerfectPowerDetectStepMs = 2;
    static constexpr std::int64_t kPerfectReleaseMicroPollMs = 14;
    static constexpr std::int64_t kPerfectReleaseMicroPollStepMs = 2;
    static constexpr double kPerfectCastTargetPercent = 96.0;
    static constexpr double kPerfectCastNearWindowPercent = 1.0;
    static constexpr int kActiveLoopSleepMs = 20;
    static constexpr int kCastingLoopSleepMs = 6;
    static constexpr int kIdleLoopSleepMs = 10;
    static constexpr int kActiveTickLogEveryN = 1;
    static constexpr int kIdleTickLogEveryN = 20;

    static std::int64_t nowMs()
    {
        return static_cast<std::int64_t>(GetTickCount64());
    }

    std::optional<double> TryAcquirePowerBarPercentQuickly()
    {
        const auto start = nowMs();
        while (nowMs() - start <= kPerfectPowerDetectBurstMs)
        {
            auto sample = runtime_.GetPowerBarPercent();
            if (sample.has_value())
            {
                return sample;
            }

            Sleep(static_cast<DWORD>(kPerfectPowerDetectStepMs));
        }

        return std::nullopt;
    }

    bool TryReleasePerfectCastNearTarget(double targetPower)
    {
        const auto start = nowMs();
        while (nowMs() - start <= kPerfectReleaseMicroPollMs)
        {
            auto sample = runtime_.GetPowerBarPercent();
            if (sample.has_value() && *sample >= targetPower)
            {
                return true;
            }

            Sleep(static_cast<DWORD>(kPerfectReleaseMicroPollStepMs));
        }

        return false;
    }

    void RebuildEngine()
    {
        macro_port::RuntimeTrackerEngineSettings settings{};
        settings.mode = parseTrackingMode(readSetting(state_, "TrackingMode", "Hybrid"));
        settings.startupAssistEnabled = _stricmp(readSetting(state_, "StartupAssistEnabled", "false").c_str(), "true") == 0;
        settings.bellonaEnabled = _stricmp(readSetting(state_, "BellonaEnabled", "false").c_str(), "true") == 0;
        settings.fishingActionDelayMs = 0;
        engine_ = std::make_unique<macro_port::RuntimeTrackerEngine>(
            &runtime_,
            &orchestrator_,
            &controller_,
            &inputActuator_,
            &hold_,
            settings,
            nullptr,
            &automationGate_);
    }

    void Run()
    {
        std::uint32_t tickCounter = 0;
        macro_port::FishingPhase lastLoggedPhase = macro_port::FishingPhase::Casting;
        std::string lastLoggedMessage;
        std::string lastLoggedTrackingBranch;

        while (!stopRequested_.load()) {
            if (!IsRunning()) {
                break;
            }

            std::string attachError;
            if (!memory_.EnsureAttached(&attachError)) {
                UpdateSharedStatus("WAITING", attachError.empty() ? "Waiting for Roblox." : attachError, std::nullopt, false);
                debugLog("tick.wait_attach error=\"" + (attachError.empty() ? std::string("Waiting for Roblox.") : attachError) + "\"");
                Sleep(250);
                continue;
            }

            const auto nativeReelVisible = runtime_.IsReelGuiVisible();
            const auto reelContext = runtime_.GetReelContext();
            const auto nativeMetrics = reelContext.has_value() ? runtime_.ReadMetrics(*reelContext) : std::optional<macro_port::ReelMetrics>{};
            const auto nativeProgress = runtime_.GetFishingCompletionPercent();
            const bool nativeShakeVisible = runtime_.IsShakeButtonVisible();

            std::string metricSource = "native";
            auto reelVisible = nativeReelVisible;
            auto metrics = nativeMetrics;
            auto progress = nativeProgress;
            auto shakeVisible = nativeShakeVisible;
            const auto currentMode = parseCastingMode(readSetting(state_, "CastingMode", "Normal"));
            auto powerPercent = runtime_.GetPowerBarPercent();
            bool perfectCastRelease = false;
            if (currentMode == macro_port::FishingCastingMode::Perfect)
            {
                if (!powerPercent.has_value())
                {
                    powerPercent = TryAcquirePowerBarPercentQuickly();
                }

                if (powerPercent.has_value())
                {
                    if (*powerPercent >= kPerfectCastTargetPercent)
                    {
                        perfectCastRelease = true;
                    }
                    else if (*powerPercent >= (kPerfectCastTargetPercent - kPerfectCastNearWindowPercent))
                    {
                        perfectCastRelease = TryReleasePerfectCastNearTarget(kPerfectCastTargetPercent);
                    }
                }
            }

            macro_port::RuntimeTrackerTickInput input{};
            input.nowMs = nowMs();
            input.suspended = false;
            input.hasSensorSnapshot = true;
            input.reelVisibleSnapshot = reelVisible;
            input.hasMetricsSnapshot = metrics.has_value();
            if (metrics.has_value())
            {
                input.metricsSnapshot = *metrics;
            }
            input.progressSnapshot = progress;
            input.shakeVisible = shakeVisible;
            input.completionOverride = progress.has_value() ? std::optional<bool>(*progress >= 99.5) : std::nullopt;
            input.castPowerReady = currentMode == macro_port::FishingCastingMode::Perfect
                ? powerPercent.has_value()
                : true;
            input.castPowerPercent = currentMode == macro_port::FishingCastingMode::Perfect ? powerPercent : std::nullopt;
            input.perfectCastRelease = currentMode == macro_port::FishingCastingMode::Perfect && perfectCastRelease;

            const auto result = engine_ != nullptr ? engine_->Tick(input) : macro_port::RuntimeTrackerTickResult{};
            if (result.clickShake) {
                macro_port::NativeMouse::LeftDown();
                macro_port::NativeMouse::LeftUp();
            }

            if (result.phase != lastLoggedPhase || result.trackingBranch != lastLoggedTrackingBranch) {
                std::ostringstream stateLog;
                stateLog.setf(std::ios::fixed);
                stateLog.precision(3);
                stateLog
                    << "track.state"
                    << " phase=" << phaseToString(result.phase)
                    << " branch=" << (result.trackingBranch.empty() ? std::string("n/a") : result.trackingBranch)
                    << " mode=" << (result.trackingDecisionMode.empty() ? std::string("n/a") : result.trackingDecisionMode)
                    << " holdDesired=" << boolText(result.holdDesired)
                    << " holdApplied=" << boolText(result.holdApplied)
                    << " progress=" << optionalDoubleText(progress)
                    << " power=" << optionalDoubleText(powerPercent)
                    << " fish=" << (metrics.has_value() ? std::to_string(metrics->fishCenter) : std::string("null"))
                    << " bar=" << (metrics.has_value() ? std::to_string(metrics->playerbarCenter) : std::string("null"))
                    << " width=" << (metrics.has_value() ? std::to_string(metrics->playerbarWidth) : std::string("null"));
                debugLog(stateLog.str());
            }

            if (result.phase == macro_port::FishingPhase::Fishing && result.trackingBranch == "Tracking1") {
                std::ostringstream t1Log;
                t1Log.setf(std::ios::fixed);
                t1Log.precision(3);
                t1Log
                    << "track.t1"
                    << " mode=" << (result.trackingDecisionMode.empty() ? std::string("n/a") : result.trackingDecisionMode)
                    << " error=" << optionalDoubleText(result.trackingError)
                    << " control=" << optionalDoubleText(result.trackingControl)
                    << " holdDesired=" << boolText(result.holdDesired)
                    << " holdApplied=" << boolText(result.holdApplied)
                    << " progress=" << optionalDoubleText(progress)
                    << " fish=" << (metrics.has_value() ? std::to_string(metrics->fishCenter) : std::string("null"))
                    << " bar=" << (metrics.has_value() ? std::to_string(metrics->playerbarCenter) : std::string("null"))
                    << " width=" << (metrics.has_value() ? std::to_string(metrics->playerbarWidth) : std::string("null"));
                debugLog(t1Log.str());
            }

            ++tickCounter;
            const bool activePhase = result.phase == macro_port::FishingPhase::Fishing
                || result.phase == macro_port::FishingPhase::Shake;
            const std::uint32_t logInterval = activePhase ? kActiveTickLogEveryN : kIdleTickLogEveryN;
            const bool logTick = result.phase != lastLoggedPhase
                || result.message != lastLoggedMessage
                || (tickCounter % logInterval) == 0;
            if (logTick) {
                std::ostringstream tickLog;
                tickLog.setf(std::ios::fixed);
                tickLog.precision(3);
                tickLog
                    << "tick"
                    << " mode=" << readSetting(state_, "TrackingMode", "Hybrid")
                    << " casting=" << readSetting(state_, "CastingMode", "Normal")
                    << " phase=" << phaseToString(result.phase)
                    << " msg=\"" << result.message << "\""
                    << " metricSource=" << metricSource
                    << " nativeFish=" << (nativeMetrics.has_value() ? std::to_string(nativeMetrics->fishCenter) : std::string("null"))
                    << " nativeBar=" << (nativeMetrics.has_value() ? std::to_string(nativeMetrics->playerbarCenter) : std::string("null"))
                    << " nativeWidth=" << (nativeMetrics.has_value() ? std::to_string(nativeMetrics->playerbarWidth) : std::string("null"))
                    << " reel=" << boolText(reelVisible)
                    << " shake=" << boolText(shakeVisible)
                    << " metrics=" << boolText(metrics.has_value())
                    << " engineMetrics=" << boolText(result.hasMetrics)
                    << " fish=" << (metrics.has_value() ? std::to_string(metrics->fishCenter) : std::string("null"))
                    << " bar=" << (metrics.has_value() ? std::to_string(metrics->playerbarCenter) : std::string("null"))
                    << " width=" << (metrics.has_value() ? std::to_string(metrics->playerbarWidth) : std::string("null"))
                    << " progress=" << optionalDoubleText(progress)
                    << " power=" << optionalDoubleText(powerPercent)
                    << " castReady=" << boolText(input.castPowerReady)
                    << " castRelease=" << boolText(input.perfectCastRelease)
                    << " holdDesired=" << boolText(result.holdDesired)
                    << " holdApplied=" << boolText(result.holdApplied)
                    << " clickShake=" << boolText(result.clickShake)
                    << " startupAssist=" << boolText(result.startupAssistActive)
                    << " branch=" << (result.trackingBranch.empty() ? std::string("n/a") : result.trackingBranch)
                    << " decisionMode=" << (result.trackingDecisionMode.empty() ? std::string("n/a") : result.trackingDecisionMode)
                    << " trackError=" << optionalDoubleText(result.trackingError)
                    << " trackControl=" << optionalDoubleText(result.trackingControl);
                debugLog(tickLog.str());
                if (result.startupAssistActive) {
                    std::ostringstream assistLog;
                    assistLog.setf(std::ios::fixed);
                    assistLog.precision(3);
                    assistLog
                        << "startup_assist"
                        << " mode=" << readSetting(state_, "TrackingMode", "Hybrid")
                        << " phase=" << phaseToString(result.phase)
                        << " fish=" << (metrics.has_value() ? std::to_string(metrics->fishCenter) : std::string("null"))
                        << " bar=" << (metrics.has_value() ? std::to_string(metrics->playerbarCenter) : std::string("null"))
                        << " width=" << (metrics.has_value() ? std::to_string(metrics->playerbarWidth) : std::string("null"))
                        << " holdDesired=" << boolText(result.holdDesired)
                        << " holdApplied=" << boolText(result.holdApplied)
                        << " progress=" << optionalDoubleText(progress);
                    debugLog(assistLog.str());
                }
                lastLoggedPhase = result.phase;
                lastLoggedMessage = result.message;
                lastLoggedTrackingBranch = result.trackingBranch;
            }

            trackingMetricsShared_.Write(
                static_cast<std::uint32_t>(result.phase),
                metrics.has_value(),
                input.nowMs,
                metrics,
                progress,
                shakeVisible,
                reelVisible,
                result.holdDesired,
                result.trackingError,
                result.trackingControl,
                0U,
                result.holdApplied);
            UpdateSharedStatus(phaseToString(result.phase), result.message, progress, result.holdDesired, metrics, reelContext);
            UpdateAutomationModules(input.nowMs, progress, powerPercent);
            int loopSleepMs = kIdleLoopSleepMs;
            if (result.phase == macro_port::FishingPhase::Fishing || metrics.has_value() || reelVisible || shakeVisible) {
                loopSleepMs = kActiveLoopSleepMs;
            } else if (result.phase == macro_port::FishingPhase::Casting) {
                loopSleepMs = kCastingLoopSleepMs;
            }
            Sleep(static_cast<DWORD>(loopSleepMs));
        }
    }

    void UpdateAutomationModules(
        std::int64_t now,
        const std::optional<double>& progress,
        const std::optional<double>& powerPercent)
    {
        const bool autoAquariumEnabled = parseBoolSetting(state_, "AutoAquariumEnabled");
        const bool autoTotemEnabled = parseBoolSetting(state_, "AutoTotemEnabled");
        const bool autoSovereignEnabled = parseBoolSetting(state_, "AutoSovereignRechargeEnabled");
        const bool huntDetectEnabled = parseBoolSetting(state_, "HuntDetectEnabled");
        const bool autoAnglerEnabled = parseBoolSetting(state_, "AutoAnglerEnabled");
        const bool autoEnchantEnabled = parseBoolSetting(state_, "AutoEnchantEnabled");
        const bool autoAppraiseEnabled = parseBoolSetting(state_, "AutoAppraiseEnabled");
        const bool autoTreasureEnabled = parseBoolSetting(state_, "AutoTreasureEnabled");

        std::string aquariumStatus = autoAquariumEnabled ? "Ready." : "Idle";
        std::string totemStatus = autoTotemEnabled ? "Waiting for cycle window." : "Idle";
        std::string sovereignStatus = autoSovereignEnabled ? "Waiting for power read." : "Idle";
        std::string huntStatus = huntDetectEnabled ? "Watching chat." : "Idle";
        std::string anglerStatus = autoAnglerEnabled ? "Ready." : "Idle";
        std::string currentFish = readSetting(state_, "CurrentFish", "---");
        std::string enchantStatus = autoEnchantEnabled ? "Waiting for rod." : "Idle";
        std::string currentEnchant = readSetting(state_, "CurrentEnchant", "---");
        std::string appraiseStatus = autoAppraiseEnabled ? "Ready." : "Idle";
        std::string appraiseFish = readSetting(state_, "AppraiseHeldFish", "---");
        std::string treasureStatus = autoTreasureEnabled ? "Ready." : "Idle";

        if (autoAquariumEnabled) {
            const auto delayMinutes = parseDoubleSetting(state_, "AutoAquariumCycleDelayMinutes", 65.0);
            const auto result = aquariumRunner_.Step(macro_port::AquariumSequenceInput{
                now,
                static_cast<long long>(delayMinutes * 60.0 * 1000.0),
                true,
                true,
                true
            });
            if (result.state == macro_port::AquariumSequenceStepState::Completed) {
                aquariumStatus = "Cycle complete.";
            } else if (result.clickAquariumButton || result.clickFeedAnchor || result.feedClicks > 0 ||
                       result.burstScrollUpCount > 0 || result.scrollDownCount > 0 || result.clickCenter) {
                aquariumStatus = "Running cycle.";
            } else {
                aquariumStatus = "Waiting for next action.";
            }
        } else {
            aquariumRunner_.Reset();
        }

        if (autoSovereignEnabled) {
            const auto minPercent = parseDoubleSetting(state_, "SovereignMinPercent", 45.0);
            const auto maxPercent = parseDoubleSetting(state_, "SovereignMaxPercent", 92.0);
            macro_port::AutoSovereignRechargeInputs inputs{};
            inputs.inputGateAvailable = true;
            inputs.inventoryTargetsReady = true;
            inputs.relicFound = true;
            inputs.enchantButtonReady = true;
            const auto result = sovereignRunner_.Step(now, minPercent, maxPercent, powerPercent.value_or(-1.0), inputs);
            sovereignStatus = result.status.empty() ? sovereignStatus : result.status;
        } else {
            sovereignRunner_.Reset();
        }

        if (autoAnglerEnabled) {
            macro_port::AutoAnglerSettings settings{};
            settings.clickX = parseIntSetting(state_, "AutoAnglerClickX", 0);
            settings.clickY = parseIntSetting(state_, "AutoAnglerClickY", 0);
            const auto result = autoAnglerRunner_.Step(now, settings, currentFish == "---" ? "None" : currentFish, true, true);
            anglerStatus = result.status;
            currentFish = result.currentFish.empty() ? currentFish : result.currentFish;
        } else {
            autoAnglerRunner_.Reset();
        }

        if (autoEnchantEnabled) {
            const auto target = readSetting(state_, "SelectedTargetEnchant", "Sea Overlord");
            const auto mode = _stricmp(readSetting(state_, "EnchantMode", "Gamepass").c_str(), "Normal") == 0
                ? macro_port::EnchantRollMode::Normal
                : macro_port::EnchantRollMode::Gamepass;
            const auto result = autoEnchantRunner_.Step(now, target, mode, currentEnchant == "---" ? "None" : currentEnchant, true);
            enchantStatus = result.status;
            currentEnchant = result.snapshot.enchant.empty() ? currentEnchant : result.snapshot.enchant;
        } else {
            autoEnchantRunner_.Reset();
        }

        if (autoAppraiseEnabled) {
            macro_port::AppraiseSettings settings{};
            settings.mode = _stricmp(readSetting(state_, "AppraiseMode", "Gamepass").c_str(), "Normal") == 0
                ? macro_port::AppraiseRunMode::Normal
                : macro_port::AppraiseRunMode::Gamepass;
            settings.gamepassSpeed = parseDoubleSetting(state_, "GamepassSpeed", 0.6);
            settings.clickX = parseIntSetting(state_, "AppraiseClickX", 0);
            settings.clickY = parseIntSetting(state_, "AppraiseClickY", 0);
            settings.requireShiny = parseBoolSetting(state_, "RequireShiny");
            settings.requireSparkling = parseBoolSetting(state_, "RequireSparkling");
            settings.requireTiny = parseBoolSetting(state_, "RequireTiny");
            settings.requireSmall = parseBoolSetting(state_, "RequireSmall");
            settings.requireBig = parseBoolSetting(state_, "RequireBig");
            settings.requireGiant = parseBoolSetting(state_, "RequireGiant");
            const auto selectedMutation = readSetting(state_, "SelectedMutation", "None");
            if (!selectedMutation.empty() && _stricmp(selectedMutation.c_str(), "None") != 0) {
                settings.baseMutations.push_back(selectedMutation);
            }
            const auto result = appraiseRunner_.Step(now, settings, false, true);
            appraiseStatus = result.status;
        } else {
            appraiseRunner_.Reset();
        }

        if (autoTreasureEnabled) {
            const bool completed = progress.has_value() && *progress >= 99.9;
            const auto result = treasureRunner_.RunStep(true, completed, 1);
            treasureStatus = result.status;
            if (result.state == macro_port::TreasureStepState::Completed) {
                treasureRunner_.Reset();
            }
        } else {
            treasureRunner_.Reset();
        }

        if (autoTotemEnabled) {
            const auto selectedTotem = readSetting(state_, "SelectedTotem", "Aurora Totem");
            const bool stayDay = parseBoolSetting(state_, "StayDay");
            const bool stayNight = parseBoolSetting(state_, "StayNight");
            totemStatus = "Ready: " + selectedTotem;
            if (stayDay && stayNight) {
                totemStatus += " (cycle locked)";
            } else if (stayDay) {
                totemStatus += " (stay day)";
            } else if (stayNight) {
                totemStatus += " (stay night)";
            }
        }

        if (huntDetectEnabled) {
            const auto selected = readSetting(state_, "SelectedHuntTargets", "");
            huntStatus = selected.empty() ? "Watching all configured hunts." : "Watching selected hunts.";
        }

        std::lock_guard<std::mutex> lock(state_.mutex);
        state_.settings["AutoAquariumStatus"] = aquariumStatus;
        state_.settings["TotemStatus"] = totemStatus;
        state_.settings["SovereignStatus"] = sovereignStatus;
        state_.settings["HuntDetectStatus"] = huntStatus;
        state_.settings["AutoAnglerStatus"] = anglerStatus;
        state_.settings["CurrentFish"] = currentFish;
        state_.settings["EnchantStatus"] = enchantStatus;
        state_.settings["CurrentEnchant"] = currentEnchant;
        state_.settings["AppraiseStatus"] = appraiseStatus;
        state_.settings["AppraiseHeldFish"] = appraiseFish;
        state_.settings["TreasureStatus"] = treasureStatus;
    }

    void UpdateSharedStatus(
        const std::string& phase,
        const std::string& message,
        const std::optional<double>& progress,
        bool inputHeld,
        const std::optional<macro_port::ReelMetrics>& metrics = std::optional<macro_port::ReelMetrics>{},
        const std::optional<macro_port::ReelContext>& reelContext = std::optional<macro_port::ReelContext>{})
    {
        auto clearOverlay = [this]() {
            state_.settings["OverlayVisible"] = "false";
            state_.settings["OverlayFishX"] = "";
            state_.settings["OverlayFishY"] = "";
            state_.settings["OverlayFishW"] = "";
            state_.settings["OverlayFishH"] = "";
            state_.settings["OverlayControlX"] = "";
            state_.settings["OverlayControlY"] = "";
            state_.settings["OverlayControlW"] = "";
            state_.settings["OverlayControlH"] = "";
        };

        std::lock_guard<std::mutex> lock(state_.mutex);
        state_.settings["Phase"] = phase;
        state_.settings["Message"] = message;
        if (progress.has_value()) {
            std::ostringstream stream;
            stream.setf(std::ios::fixed);
            stream.precision(1);
            stream << *progress;
            state_.settings["Progress"] = stream.str();
        } else {
            state_.settings["Progress"] = "---";
        }
        state_.settings["InputStatus"] = inputHeld ? "Held" : "Released";
        debugLog("status.write phase=" + phase +
            " input=" + (inputHeld ? std::string("Held") : std::string("Released")) +
            " progress=" + (progress.has_value() ? optionalDoubleText(progress) : std::string("---")) +
            " msg=\"" + message + "\"");

        if (phase != "TRACKING" || !reelContext.has_value())
        {
            clearOverlay();
            overlayShared_.Clear();
            return;
        }

        const auto fishBounds = memory_.ReadGuiBounds(reelContext->fish, false);
        const auto controlBounds = memory_.ReadGuiBounds(reelContext->playerbar, false);
        if (!fishBounds.has_value() || !controlBounds.has_value())
        {
            clearOverlay();
            overlayShared_.Clear();
            return;
        }

        auto toText = [](float value) {
            std::ostringstream stream;
            stream.setf(std::ios::fixed);
            stream.precision(2);
            stream << value;
            return stream.str();
        };

        state_.settings["OverlayVisible"] = "true";
        state_.settings["OverlayFishX"] = toText(fishBounds->x);
        state_.settings["OverlayFishY"] = toText(fishBounds->y);
        state_.settings["OverlayFishW"] = toText(fishBounds->width);
        state_.settings["OverlayFishH"] = toText(fishBounds->height);
        state_.settings["OverlayControlX"] = toText(controlBounds->x);
        state_.settings["OverlayControlY"] = toText(controlBounds->y);
        state_.settings["OverlayControlW"] = toText(controlBounds->width);
        state_.settings["OverlayControlH"] = toText(controlBounds->height);
        overlayShared_.Write(true, nowMs(), fishBounds, controlBounds);
    }

    CoreState& state_;
    macro_port::Win32RobloxMemory memory_;
    macro_port::FishingRuntimeContext runtime_;
    NativeInputActuator inputActuator_;
    macro_port::HoldApplier hold_;
    macro_port::TrackerOrchestrator orchestrator_{};
    macro_port::Tracking1Controller controller_{};
    macro_port::InMemoryAutomationInputGate automationGate_{};
    macro_port::AquariumSequenceRunner aquariumRunner_{};
    macro_port::AutoSovereignRechargeRunner sovereignRunner_{};
    macro_port::AutoAnglerRunner autoAnglerRunner_{};
    macro_port::AutoEnchantRunner autoEnchantRunner_{};
    macro_port::Tracking2AppraiseRunner appraiseRunner_{};
    macro_port::TreasureAppraiser treasureRunner_{};
    OverlaySharedMemory overlayShared_{};
    TrackingMetricsSharedMemory trackingMetricsShared_{};
    std::unique_ptr<macro_port::RuntimeTrackerEngine> engine_;
    std::thread worker_;
    std::atomic<bool> stopRequested_{false};
    std::atomic<bool> running_{false};
};

RuntimeHost* g_runtimeHost = nullptr;

std::string jsonEscape(const std::string& value) {
    std::string out;
    out.reserve(value.size() + 8);
    for (const char ch : value) {
        switch (ch) {
        case '\\': out += "\\\\"; break;
        case '"': out += "\\\""; break;
        case '\n': out += "\\n"; break;
        case '\r': out += "\\r"; break;
        case '\t': out += "\\t"; break;
        default: out += ch; break;
        }
    }
    return out;
}

std::string getJsonString(const std::string& json, const std::string& key) {
    const std::string marker = "\"" + key + "\"";
    const auto keyPos = json.find(marker);
    if (keyPos == std::string::npos) return {};

    const auto colon = json.find(':', keyPos + marker.size());
    if (colon == std::string::npos) return {};

    const auto firstQuote = json.find('"', colon + 1);
    if (firstQuote == std::string::npos) return {};

    std::string value;
    bool escaped = false;
    for (auto i = firstQuote + 1; i < json.size(); ++i) {
        const char ch = json[i];
        if (escaped) {
            value += ch;
            escaped = false;
            continue;
        }
        if (ch == '\\') {
            escaped = true;
            continue;
        }
        if (ch == '"') break;
        value += ch;
    }
    return value;
}

std::string runtimeText(const CoreState& state) {
    if (!state.running.load()) return "00:00:00";

    const auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(
        std::chrono::steady_clock::now() - state.startedAt);
    const auto hours = elapsed.count() / 3600;
    const auto minutes = (elapsed.count() % 3600) / 60;
    const auto seconds = elapsed.count() % 60;

    std::ostringstream stream;
    stream.width(2);
    stream.fill('0');
    stream << hours << ':';
    stream.width(2);
    stream << minutes << ':';
    stream.width(2);
    stream << seconds;
    return stream.str();
}

std::filesystem::path executableDirectory() {
    wchar_t buffer[MAX_PATH]{};
    const DWORD length = GetModuleFileNameW(nullptr, buffer, MAX_PATH);
    if (length == 0 || length >= MAX_PATH) {
        return std::filesystem::current_path();
    }
    return std::filesystem::path(buffer).parent_path();
}

std::filesystem::path debugLogPath() {
    return executableDirectory() / L"openmacro_core_debug.log";
}

void debugLog(const std::string& message) {
    std::lock_guard<std::mutex> lock(g_debugLogMutex);
    if (!g_debugLogFile.is_open()) {
        g_debugLogFile.open(debugLogPath(), std::ios::app);
        if (!g_debugLogFile) {
            return;
        }
    }

    std::time_t now = std::time(nullptr);
    std::tm localTime{};
#if defined(_WIN32)
    localtime_s(&localTime, &now);
#else
    localtime_r(&now, &localTime);
#endif
    char stamp[32]{};
    std::strftime(stamp, sizeof(stamp), "%Y-%m-%d %H:%M:%S", &localTime);
    g_debugLogFile << "[" << stamp << "] " << message << "\n";
    g_debugLogFile.flush();
}

void debugLogCommand(const std::string& command) {
    if (command == "GetStatus" || command == "GetStats" || command == "GetOverlay") {
        return;
    }
    debugLog("ipc.command " + (command.empty() ? std::string("<empty>") : command));
}

std::string boolText(bool value) {
    return value ? "1" : "0";
}

std::string optionalDoubleText(const std::optional<double>& value, int precision) {
    if (!value.has_value()) {
        return "null";
    }

    std::ostringstream stream;
    stream.setf(std::ios::fixed);
    stream.precision(precision);
    stream << *value;
    return stream.str();
}

bool loadOffsets(macro_port::LocalOffsetsSource& offsets, std::string& error) {
    return offsets.LoadFromFile(executableDirectory() / L"offsets.json", &error);
}

std::filesystem::path settingsPath() {
    return executableDirectory() / L"openmacro_core_settings.json";
}

void saveSettings(const CoreState& state);

std::string readSetting(const CoreState& state, const std::string& key, const std::string& fallback = {}) {
    std::lock_guard<std::mutex> lock(state.mutex);
    auto it = state.settings.find(key);
    return it == state.settings.end() ? fallback : it->second;
}

bool parseBoolSetting(const CoreState& state, const std::string& key, bool fallback) {
    const auto value = readSetting(state, key, fallback ? "true" : "false");
    return _stricmp(value.c_str(), "true") == 0 ||
           _stricmp(value.c_str(), "1") == 0 ||
           _stricmp(value.c_str(), "yes") == 0 ||
           _stricmp(value.c_str(), "on") == 0;
}

double parseDoubleSetting(const CoreState& state, const std::string& key, double fallback) {
    const auto value = readSetting(state, key, "");
    if (value.empty()) {
        return fallback;
    }

    char* end = nullptr;
    const double parsed = std::strtod(value.c_str(), &end);
    return end != value.c_str() ? parsed : fallback;
}

int parseIntSetting(const CoreState& state, const std::string& key, int fallback) {
    const auto value = readSetting(state, key, "");
    if (value.empty()) {
        return fallback;
    }

    char* end = nullptr;
    const long parsed = std::strtol(value.c_str(), &end, 10);
    return end != value.c_str() ? static_cast<int>(parsed) : fallback;
}

void writeSetting(CoreState& state, const std::string& key, const std::string& value) {
    {
        std::lock_guard<std::mutex> lock(state.mutex);
        state.settings[key] = value;
    }
    saveSettings(state);
    debugLog("setting.update key=" + key + " value=\"" + value + "\"");
}

macro_port::RuntimeTrackerEngineSettings::TrackingMode parseTrackingMode(const std::string& value) {
    if (_stricmp(value.c_str(), "Hybrid") == 0 || _stricmp(value.c_str(), "Tracking 3") == 0 || _stricmp(value.c_str(), "Tracking3") == 0) {
        return macro_port::RuntimeTrackerEngineSettings::TrackingMode::Tracking3;
    }

    if (_stricmp(value.c_str(), "Spam") == 0 || _stricmp(value.c_str(), "Tracking 2") == 0 || _stricmp(value.c_str(), "Tracking2") == 0) {
        return macro_port::RuntimeTrackerEngineSettings::TrackingMode::Tracking2;
    }

    if (_stricmp(value.c_str(), "Predict") == 0 ||
        _stricmp(value.c_str(), "Tracking 1") == 0 ||
        _stricmp(value.c_str(), "Tracking1") == 0)
    {
        return macro_port::RuntimeTrackerEngineSettings::TrackingMode::Tracking1;
    }

    return macro_port::RuntimeTrackerEngineSettings::TrackingMode::Tracking1;
}

macro_port::FishingCastingMode parseCastingMode(const std::string& value) {
    return (_stricmp(value.c_str(), "Perfect Cast") == 0 || _stricmp(value.c_str(), "Perfect") == 0)
        ? macro_port::FishingCastingMode::Perfect
        : macro_port::FishingCastingMode::Normal;
}

std::string phaseToString(macro_port::FishingPhase phase) {
    switch (phase) {
    case macro_port::FishingPhase::Off: return "OFF";
    case macro_port::FishingPhase::Casting: return "CASTING";
    case macro_port::FishingPhase::Casted: return "CASTED";
    case macro_port::FishingPhase::Shake: return "SHAKE";
    case macro_port::FishingPhase::Fishing: return "TRACKING";
    case macro_port::FishingPhase::Suspended: return "SUSPENDED";
    case macro_port::FishingPhase::Error: return "ERROR";
    default: return "UNKNOWN";
    }
}

std::string unescapeJsonString(const std::string& value) {
    std::string out;
    out.reserve(value.size());
    bool escaped = false;
    for (char ch : value) {
        if (escaped) {
            switch (ch) {
            case 'n': out += '\n'; break;
            case 'r': out += '\r'; break;
            case 't': out += '\t'; break;
            case '\\': out += '\\'; break;
            case '"': out += '"'; break;
            default: out += ch; break;
            }
            escaped = false;
            continue;
        }

        if (ch == '\\') {
            escaped = true;
            continue;
        }

        out += ch;
    }

    return out;
}

void loadSettings(CoreState& state) {
    std::lock_guard<std::mutex> lock(state.mutex);
    std::ifstream file(settingsPath());
    if (!file) {
        return;
    }

    const std::string json((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
    std::size_t pos = 0;
    while (true) {
        const auto keyStart = json.find('"', pos);
        if (keyStart == std::string::npos) {
            break;
        }

        const auto keyEnd = json.find('"', keyStart + 1);
        if (keyEnd == std::string::npos) {
            break;
        }

        const auto colon = json.find(':', keyEnd + 1);
        if (colon == std::string::npos) {
            break;
        }

        const auto valueStart = json.find('"', colon + 1);
        if (valueStart == std::string::npos) {
            break;
        }

        const auto valueEnd = json.find('"', valueStart + 1);
        if (valueEnd == std::string::npos) {
            break;
        }

        const auto key = json.substr(keyStart + 1, keyEnd - keyStart - 1);
        const auto value = unescapeJsonString(json.substr(valueStart + 1, valueEnd - valueStart - 1));
        if (!key.empty()) {
            state.settings[key] = value;
        }

        pos = valueEnd + 1;
    }
}

void saveSettings(const CoreState& state) {
    std::ofstream file(settingsPath(), std::ios::trunc);
    if (!file) {
        return;
    }

    file << "{";
    bool first = true;
    std::lock_guard<std::mutex> lock(state.mutex);
    for (const auto& [key, value] : state.settings) {
        if (!first) {
            file << ",";
        }
        file << "\"" << jsonEscape(key) << "\":\"" << jsonEscape(value) << "\"";
        first = false;
    }
    file << "}\n";
}

std::string statusJson(const CoreState& state) {
    const auto tracker = readSetting(state, "TrackingMode", "Hybrid");
    const auto casting = readSetting(state, "CastingMode", "Normal");
    const auto rodSlot = readSetting(state, "RodSlot", "1");
    const auto hotkey = readSetting(state, "Hotkey", "F3");
    const auto phase = readSetting(state, "Phase", "OFF");
    const auto message = readSetting(state, "Message", "Idle");
    const bool offsetsLoaded = state.offsets != nullptr && state.offsets->IsPopulated();
    const auto offsetsVersion = offsetsLoaded ? state.offsets->Version() : std::string{};
    const auto offsetsCount = offsetsLoaded ? state.offsets->Count() : 0;

    std::ostringstream response;
    response << "{\"ok\":true"
             << ",\"running\":" << (state.running.load() ? "true" : "false")
             << ",\"status\":\"" << (state.running.load() ? "Running" : "Stopped") << "\""
             << ",\"runtime\":\"" << runtimeText(state) << "\""
             << ",\"hotkey\":\"" << jsonEscape(hotkey) << "\""
             << ",\"trackingMode\":\"" << jsonEscape(tracker) << "\""
             << ",\"TrackingMode\":\"" << jsonEscape(tracker) << "\""
             << ",\"castingMode\":\"" << jsonEscape(casting) << "\""
             << ",\"CastingMode\":\"" << jsonEscape(casting) << "\""
             << ",\"rodSlot\":\"" << jsonEscape(rodSlot) << "\""
             << ",\"RodSlot\":\"" << jsonEscape(rodSlot) << "\""
             << ",\"updateStatus\":\"Up to date\""
             << ",\"phase\":\"" << jsonEscape(phase) << "\""
             << ",\"message\":\"" << jsonEscape(message) << "\""
             << ",\"offsetsLoaded\":" << (offsetsLoaded ? "true" : "false")
             << ",\"offsetsVersion\":\"" << jsonEscape(offsetsVersion) << "\""
             << ",\"offsetsCount\":" << offsetsCount;

    {
        std::lock_guard<std::mutex> lock(state.mutex);
        for (const auto& [key, value] : state.settings) {
            if (key == "TrackingMode" || key == "CastingMode" || key == "RodSlot" || key == "Hotkey" || key == "Phase" || key == "Message") {
                continue;
            }

            response << ",\"" << jsonEscape(key) << "\":\"" << jsonEscape(value) << "\"";
        }
    }
    response << "}";
    return response.str();
}

std::string statsJson(const CoreState& state) {
    const auto caught = readSetting(state, "Caught", "0");
    const auto lost = readSetting(state, "Lost", "0");
    const auto successRate = readSetting(state, "SuccessRate", "100%");
    const auto currentFish = readSetting(state, "CurrentFish", "None");
    const auto phase = readSetting(state, "Phase", "Idle");
    const auto progress = readSetting(state, "Progress", "---");
    const auto input = readSetting(state, "InputStatus", "Idle");
    const auto rod = readSetting(state, "EquippedRod", "---");
    const auto currentEnchant = readSetting(state, "CurrentEnchant", "---");
    const auto appraiseStatus = readSetting(state, "AppraiseStatus", "---");
    const auto appraiseFish = readSetting(state, "AppraiseHeldFish", "---");
    const auto treasureStatus = readSetting(state, "TreasureStatus", "Idle");
    const auto sovereignStatus = readSetting(state, "SovereignStatus", "Idle");
    const auto totemStatus = readSetting(state, "TotemStatus", "Waiting for cycle window.");

    std::ostringstream response;
    response << "{\"ok\":true"
             << ",\"caught\":\"" << jsonEscape(caught) << "\""
             << ",\"lost\":\"" << jsonEscape(lost) << "\""
             << ",\"successRate\":\"" << jsonEscape(successRate) << "\""
             << ",\"currentFish\":\"" << jsonEscape(currentFish) << "\""
             << ",\"phase\":\"" << jsonEscape(phase) << "\""
             << ",\"progress\":\"" << jsonEscape(progress) << "\""
             << ",\"input\":\"" << jsonEscape(input) << "\""
             << ",\"equippedRod\":\"" << jsonEscape(rod) << "\""
             << ",\"currentEnchant\":\"" << jsonEscape(currentEnchant) << "\""
             << ",\"appraiseStatus\":\"" << jsonEscape(appraiseStatus) << "\""
             << ",\"appraiseHeldFish\":\"" << jsonEscape(appraiseFish) << "\""
             << ",\"treasureStatus\":\"" << jsonEscape(treasureStatus) << "\""
             << ",\"sovereignStatus\":\"" << jsonEscape(sovereignStatus) << "\""
             << ",\"totemStatus\":\"" << jsonEscape(totemStatus) << "\""
             << "}";
    return response.str();
}

std::string overlayJson(const CoreState& state) {
    auto valueOf = [&](const std::unordered_map<std::string, std::string>& settings, const char* key, const char* fallback) {
        auto it = settings.find(key);
        return it == settings.end() ? std::string(fallback) : it->second;
    };

    const auto running = state.running.load() ? "true" : "false";
    std::string phase;
    std::string visible;
    std::string fishX;
    std::string fishY;
    std::string fishW;
    std::string fishH;
    std::string controlX;
    std::string controlY;
    std::string controlW;
    std::string controlH;
    {
        std::lock_guard<std::mutex> lock(state.mutex);
        phase = valueOf(state.settings, "Phase", "OFF");
        visible = valueOf(state.settings, "OverlayVisible", "false");
        fishX = valueOf(state.settings, "OverlayFishX", "");
        fishY = valueOf(state.settings, "OverlayFishY", "");
        fishW = valueOf(state.settings, "OverlayFishW", "");
        fishH = valueOf(state.settings, "OverlayFishH", "");
        controlX = valueOf(state.settings, "OverlayControlX", "");
        controlY = valueOf(state.settings, "OverlayControlY", "");
        controlW = valueOf(state.settings, "OverlayControlW", "");
        controlH = valueOf(state.settings, "OverlayControlH", "");
    }

    std::ostringstream response;
    response << "{\"ok\":true"
             << ",\"running\":" << running
             << ",\"phase\":\"" << jsonEscape(phase) << "\""
             << ",\"OverlayVisible\":\"" << jsonEscape(visible) << "\""
             << ",\"OverlayFishX\":\"" << jsonEscape(fishX) << "\""
             << ",\"OverlayFishY\":\"" << jsonEscape(fishY) << "\""
             << ",\"OverlayFishW\":\"" << jsonEscape(fishW) << "\""
             << ",\"OverlayFishH\":\"" << jsonEscape(fishH) << "\""
             << ",\"OverlayControlX\":\"" << jsonEscape(controlX) << "\""
             << ",\"OverlayControlY\":\"" << jsonEscape(controlY) << "\""
             << ",\"OverlayControlW\":\"" << jsonEscape(controlW) << "\""
             << ",\"OverlayControlH\":\"" << jsonEscape(controlH) << "\""
             << "}";
    return response.str();
}

std::string handleRequest(CoreState& state, const std::string& request) {
    const std::string command = getJsonString(request, "command");
    debugLogCommand(command);

    if (command == "StartMacro") {
        if (g_runtimeHost != nullptr) {
            g_runtimeHost->Start();
        } else {
            if (!state.running.load()) {
                state.running = true;
                state.startedAt = std::chrono::steady_clock::now();
            }
        }
        return statusJson(state);
    }

    if (command == "StopMacro") {
        if (g_runtimeHost != nullptr) {
            g_runtimeHost->Stop();
        } else {
            state.running = false;
        }
        return statusJson(state);
    }

    if (command == "UpdateSetting") {
        const std::string key = getJsonString(request, "key");
        const std::string value = getJsonString(request, "value");
        if (!key.empty()) {
            writeSetting(state, key, value);
        }
        return statusJson(state);
    }

    if (command == "GetStats") {
        return statsJson(state);
    }

    if (command == "GetStatus") {
        return statusJson(state);
    }

    if (command == "GetOverlay") {
        return overlayJson(state);
    }

    if (command == "ReloadOffsets") {
        if (state.offsets == nullptr) {
            return "{\"ok\":false,\"error\":\"Offsets source unavailable\"}";
        }

        std::string error;
        if (!loadOffsets(*state.offsets, error)) {
            return std::string("{\"ok\":false,\"error\":\"") + jsonEscape(error) + "\"}";
        }

        return statusJson(state);
    }

    if (command == "RebindHotkey") {
        return statusJson(state);
    }

    return "{\"ok\":false,\"error\":\"Unknown command\"}";
}

bool writeLine(HANDLE pipe, const std::string& line) {
    const std::string payload = line + "\n";
    DWORD written = 0;
    return WriteFile(pipe, payload.data(), static_cast<DWORD>(payload.size()), &written, nullptr) != FALSE;
}

void serveClient(HANDLE pipe, CoreState& state) {
    std::string pending;
    char buffer[4096]{};

    while (true) {
        DWORD bytesRead = 0;
        const BOOL ok = ReadFile(pipe, buffer, sizeof(buffer), &bytesRead, nullptr);
        if (!ok || bytesRead == 0) break;

        pending.append(buffer, buffer + bytesRead);
        std::size_t newline = std::string::npos;
        while ((newline = pending.find('\n')) != std::string::npos) {
            std::string request = pending.substr(0, newline);
            pending.erase(0, newline + 1);
            if (!request.empty() && request.back() == '\r') {
                request.pop_back();
            }
            writeLine(pipe, handleRequest(state, request));
        }
    }
}

} // namespace

int main() {
    {
        std::lock_guard<std::mutex> lock(g_debugLogMutex);
        g_debugLogFile.open(debugLogPath(), std::ios::trunc);
        if (g_debugLogFile) {
            g_debugLogFile.close();
            g_debugLogFile.open(debugLogPath(), std::ios::app);
        }
    }
    debugLog("core.start");

    CoreState state;
    loadSettings(state);

    macro_port::LocalOffsetsSource offsets;
    state.offsets = &offsets;
    macro_port::OffsetsSourceProvider::Register(&offsets);

    RuntimeHost runtimeHost(state, offsets);
    g_runtimeHost = &runtimeHost;

    std::string offsetsError;
    if (loadOffsets(offsets, offsetsError)) {
        std::cout << "Loaded " << offsets.Count() << " offsets from " << offsets.Path().string();
        if (!offsets.Version().empty()) {
            std::cout << " version=" << offsets.Version();
        }
        std::cout << '\n';
    } else {
        std::cerr << "Offsets load failed: " << offsetsError << '\n';
    }

    std::cout << "OpenMacro Swift core IPC listening on " << "\\\\.\\pipe\\OpenMacroSwift.Core" << '\n';

    while (true) {
        HANDLE pipe = CreateNamedPipeW(
            kPipeName,
            PIPE_ACCESS_DUPLEX,
            PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
            PIPE_UNLIMITED_INSTANCES,
            8192,
            8192,
            0,
            nullptr);

        if (pipe == INVALID_HANDLE_VALUE) {
            std::cerr << "CreateNamedPipe failed: " << GetLastError() << '\n';
            return 1;
        }

        const BOOL connected = ConnectNamedPipe(pipe, nullptr)
            ? TRUE
            : (GetLastError() == ERROR_PIPE_CONNECTED);

        if (connected) {
            serveClient(pipe, state);
        }

        DisconnectNamedPipe(pipe);
        CloseHandle(pipe);
    }
}
