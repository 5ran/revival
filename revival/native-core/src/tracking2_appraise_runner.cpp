#include "tracking2_appraise_runner.hpp"

namespace macro_port
{
void Tracking2AppraiseRunner::Reset()
{
    state_ = "IDLE";
    nextActionAt_ = 0;
}

AppraiseStepResult Tracking2AppraiseRunner::Step(
    std::int64_t nowMs,
    const AppraiseSettings& settings,
    bool foundDesiredMutation,
    bool enchantButtonReady)
{
    AppraiseStepResult out{};
    const bool hasAnyTarget =
        !settings.baseMutations.empty() ||
        settings.requireShiny || settings.requireSparkling ||
        settings.requireTiny || settings.requireSmall ||
        settings.requireBig || settings.requireGiant;

    if (!hasAnyTarget)
    {
        out.failed = true;
        out.status = "Choose at least one appraise target.";
        return out;
    }
    if (settings.mode == AppraiseRunMode::Normal && (settings.clickX <= 0 || settings.clickY <= 0))
    {
        out.failed = true;
        out.status = "Set a click point before appraising.";
        return out;
    }
    if (settings.mode == AppraiseRunMode::Gamepass && !enchantButtonReady)
    {
        out.failed = true;
        out.status = "Enchant button not found.";
        return out;
    }

    if (foundDesiredMutation)
    {
        out.completed = true;
        out.status = "Found desired mutation.";
        state_ = "IDLE";
        nextActionAt_ = 0;
        return out;
    }

    if (nowMs < nextActionAt_)
    {
        out.status = "COMPLETING";
        return out;
    }

    if (settings.mode == AppraiseRunMode::Gamepass)
    {
        nextActionAt_ = nowMs + static_cast<std::int64_t>(1000.0 / (settings.gamepassSpeed <= 0.0 ? 1.0 : settings.gamepassSpeed));
        out.status = "Still looking for target mutation.";
        return out;
    }

    if (state_ == "IDLE") { state_ = "CLICK_FIRST"; nextActionAt_ = nowMs + 70; }
    else if (state_ == "CLICK_FIRST") { state_ = "CLICK_SECOND"; nextActionAt_ = nowMs + 70; }
    else if (state_ == "CLICK_SECOND") { state_ = "WAIT_RESULT"; nextActionAt_ = nowMs + 70; }
    else { state_ = "CLICK_FIRST"; nextActionAt_ = nowMs + 70; }

    out.status = "Still looking for target mutation.";
    return out;
}
}
