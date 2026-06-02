#include "replay_runner.hpp"

namespace macro_port
{
namespace
{
void AddMismatch(ReplayReport& report, std::size_t index, const char* field, const std::string& expected, const std::string& actual)
{
    report.pass = false;
    report.mismatches.push_back(ReplayMismatch{index, field, expected, actual});
}

std::string BoolString(bool v)
{
    return v ? "true" : "false";
}

std::string PhaseString(FishingPhase phase)
{
    switch (phase)
    {
    case FishingPhase::Off: return "Off";
    case FishingPhase::Casting: return "Casting";
    case FishingPhase::Casted: return "Casted";
    case FishingPhase::Shake: return "Shake";
    case FishingPhase::Fishing: return "Fishing";
    case FishingPhase::Suspended: return "Suspended";
    case FishingPhase::Error: return "Error";
    default: return "Unknown";
    }
}
}

ReplayReport ReplayRunner::Run(
    RuntimeTrackerEngine* engine,
    std::int64_t startNowMs,
    FishingCastingMode castingMode,
    const std::vector<ReplayFrame>& frames)
{
    ReplayReport report{};
    if (engine == nullptr)
    {
        report.pass = false;
        report.mismatches.push_back(ReplayMismatch{0, "engine", "non-null", "null"});
        return report;
    }

    engine->Start(startNowMs, castingMode);
    for (std::size_t i = 0; i < frames.size(); ++i)
    {
        const auto& frame = frames[i];
        const auto actual = engine->Tick(frame.input);

        if (frame.checkPhase && actual.phase != frame.expected.phase)
        {
            AddMismatch(report, i, "phase", PhaseString(frame.expected.phase), PhaseString(actual.phase));
        }
        if (frame.checkHoldApplied && actual.holdApplied != frame.expected.holdApplied)
        {
            AddMismatch(report, i, "holdApplied", BoolString(frame.expected.holdApplied), BoolString(actual.holdApplied));
        }
        if (frame.checkRightHoldApplied && actual.rightHoldApplied != frame.expected.rightHoldApplied)
        {
            AddMismatch(report, i, "rightHoldApplied", BoolString(frame.expected.rightHoldApplied), BoolString(actual.rightHoldApplied));
        }
        if (frame.checkAutomationGate && actual.automationGateOpen != frame.expected.automationGateOpen)
        {
            AddMismatch(report, i, "automationGateOpen", BoolString(frame.expected.automationGateOpen), BoolString(actual.automationGateOpen));
        }
    }
    if (!frames.empty())
    {
        engine->Stop(frames.back().input.nowMs);
    }
    else
    {
        engine->Stop(startNowMs);
    }
    return report;
}
}
