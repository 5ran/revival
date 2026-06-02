#pragma once

#include <cstdint>
#include <string>

namespace macro_port
{
struct AutoAnglerSettings
{
    int clickX = 0;
    int clickY = 0;
};

struct AutoAnglerStepResult
{
    bool failed = false;
    std::string status;
    std::string currentFish;
};

class AutoAnglerRunner
{
public:
    void Reset();
    AutoAnglerStepResult Step(
        std::int64_t nowMs,
        const AutoAnglerSettings& settings,
        const std::string& liveFish,
        bool inventoryTargetsReady,
        bool inventoryHasFishItem);

private:
    enum class State
    {
        Start,
        ClickPointA,
        ReadFish,
        SearchFish,
        ClickSearchResult,
        PressEAgain,
        Completing,
    };

    State state_ = State::Start;
    std::int64_t nextStepAt_ = 0;
    std::string currentFish_ = "None";
};
}
