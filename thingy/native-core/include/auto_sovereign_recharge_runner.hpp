#pragma once

#include <cstdint>
#include <string>

namespace macro_port
{
struct AutoSovereignRechargeInputs
{
    bool inputGateAvailable = true;
    bool inventoryTargetsReady = true;
    bool relicFound = true;
    bool enchantButtonReady = true;
};

struct AutoSovereignRechargeStepResult
{
    bool failed = false;
    std::string status;
    double currentPowerPercent = -1.0;
};

class AutoSovereignRechargeRunner
{
public:
    void Reset();
    AutoSovereignRechargeStepResult Step(
        std::int64_t nowMs,
        double minPercent,
        double maxPercent,
        double currentPowerPercent,
        const AutoSovereignRechargeInputs& inputs);

private:
    enum class State
    {
        Idle,
        OpenInventory,
        SearchRelic,
        ClickRelic,
        ClickEnchant,
        WaitRecharge,
    };

    bool recharging_ = false;
    State state_ = State::Idle;
    std::int64_t nextStepAt_ = 0;
    int missingRelicRetries_ = 0;
};
}
