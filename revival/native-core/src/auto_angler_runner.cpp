#include "auto_angler_runner.hpp"

namespace macro_port
{
void AutoAnglerRunner::Reset()
{
    state_ = State::Start;
    nextStepAt_ = 0;
    currentFish_ = "None";
}

AutoAnglerStepResult AutoAnglerRunner::Step(
    std::int64_t nowMs,
    const AutoAnglerSettings& settings,
    const std::string& liveFish,
    bool inventoryTargetsReady,
    bool inventoryHasFishItem)
{
    AutoAnglerStepResult out{};
    if (settings.clickX <= 0 || settings.clickY <= 0)
    {
        out.failed = true;
        out.status = "Set a click point before starting Auto Angler.";
        out.currentFish = "None";
        return out;
    }

    currentFish_ = liveFish.empty() ? "None" : liveFish;
    if (nowMs < nextStepAt_)
    {
        out.status = "COMPLETING";
        out.currentFish = currentFish_;
        return out;
    }

    switch (state_)
    {
    case State::Start:
        state_ = State::ClickPointA;
        nextStepAt_ = nowMs + 2200;
        break;
    case State::ClickPointA:
        state_ = State::ReadFish;
        nextStepAt_ = nowMs + 1200;
        break;
    case State::ReadFish:
        state_ = State::SearchFish;
        nextStepAt_ = nowMs + 300;
        break;
    case State::SearchFish:
        if (!inventoryTargetsReady)
        {
            out.failed = true;
            out.status = "Inventory search targets not found.";
            out.currentFish = currentFish_;
            return out;
        }
        state_ = State::ClickSearchResult;
        nextStepAt_ = nowMs + 700;
        break;
    case State::ClickSearchResult:
        if (currentFish_ != "None" && !inventoryHasFishItem)
        {
            out.failed = true;
            out.status = "Could not find current fish in inventory.";
            out.currentFish = currentFish_;
            return out;
        }
        state_ = State::PressEAgain;
        nextStepAt_ = nowMs + 350;
        break;
    case State::PressEAgain:
        state_ = State::Completing;
        nextStepAt_ = nowMs + 500;
        break;
    case State::Completing:
        state_ = State::Start;
        nextStepAt_ = nowMs + 500;
        break;
    }

    out.status = state_ == State::Start ? "READY" : "COMPLETING";
    out.currentFish = currentFish_;
    return out;
}
}
