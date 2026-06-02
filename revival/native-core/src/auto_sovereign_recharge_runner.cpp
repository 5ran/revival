#include "auto_sovereign_recharge_runner.hpp"

#include <algorithm>

namespace macro_port
{
void AutoSovereignRechargeRunner::Reset()
{
    recharging_ = false;
    state_ = State::Idle;
    nextStepAt_ = 0;
    missingRelicRetries_ = 0;
}

AutoSovereignRechargeStepResult AutoSovereignRechargeRunner::Step(
    std::int64_t nowMs,
    double minPercent,
    double maxPercent,
    double currentPowerPercent,
    const AutoSovereignRechargeInputs& inputs)
{
    AutoSovereignRechargeStepResult out{};
    minPercent = std::clamp(minPercent, 0.0, 100.0);
    maxPercent = std::clamp(maxPercent, minPercent, 100.0);

    if (!recharging_)
    {
        if (currentPowerPercent < 0.0)
        {
            out.status = "Waiting for power read.";
            return out;
        }

        if (currentPowerPercent >= minPercent)
        {
            out.status = "Power healthy.";
            out.currentPowerPercent = currentPowerPercent;
            return out;
        }

        recharging_ = true;
        state_ = State::OpenInventory;
        nextStepAt_ = nowMs;
        missingRelicRetries_ = 0;
    }

    if (!inputs.inputGateAvailable)
    {
        out.status = "Waiting for other automation.";
        out.currentPowerPercent = currentPowerPercent;
        return out;
    }

    if (currentPowerPercent >= maxPercent)
    {
        Reset();
        out.status = "Power recharged.";
        out.currentPowerPercent = currentPowerPercent;
        return out;
    }

    if (nowMs < nextStepAt_)
    {
        out.status = "Recharging...";
        out.currentPowerPercent = currentPowerPercent;
        return out;
    }

    switch (state_)
    {
    case State::OpenInventory:
        state_ = State::SearchRelic;
        nextStepAt_ = nowMs + 200;
        break;
    case State::SearchRelic:
        if (!inputs.inventoryTargetsReady)
        {
            out.failed = true;
            out.status = "Inventory targets not found.";
            return out;
        }
        if (!inputs.relicFound)
        {
            ++missingRelicRetries_;
            if (missingRelicRetries_ >= 3)
            {
                out.failed = true;
                out.status = "Enchant Relic not found.";
                return out;
            }
            nextStepAt_ = nowMs + 600;
            out.status = "Relic missing; retrying.";
            out.currentPowerPercent = currentPowerPercent;
            return out;
        }
        state_ = State::ClickRelic;
        nextStepAt_ = nowMs + 250;
        break;
    case State::ClickRelic:
        state_ = State::ClickEnchant;
        nextStepAt_ = nowMs + 350;
        break;
    case State::ClickEnchant:
        if (!inputs.enchantButtonReady)
        {
            out.failed = true;
            out.status = "Enchant button not found.";
            return out;
        }
        state_ = State::WaitRecharge;
        nextStepAt_ = nowMs + 500;
        break;
    case State::WaitRecharge:
        state_ = State::SearchRelic;
        nextStepAt_ = nowMs + 500;
        break;
    case State::Idle:
    default:
        state_ = State::OpenInventory;
        nextStepAt_ = nowMs;
        break;
    }

    out.status = "Recharging...";
    out.currentPowerPercent = currentPowerPercent;
    return out;
}
}
