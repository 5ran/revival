#include "auto_enchant_runner.hpp"

namespace macro_port
{
void AutoEnchantRunner::Reset()
{
    nextActionAt_ = 0;
    lastActionAt_ = 0;
}

EnchantRunnerResult AutoEnchantRunner::Step(
    std::int64_t nowMs,
    const std::string& targetEnchant,
    EnchantRollMode mode,
    const std::string& currentEnchant,
    bool targetsReady)
{
    EnchantRunnerResult out{};
    out.snapshot.enchant = currentEnchant;

    if (targetEnchant.empty())
    {
        out.failed = true;
        out.status = "Select a target enchant.";
        return out;
    }

    if (currentEnchant == targetEnchant)
    {
        out.completed = true;
        out.status = "Found target enchant.";
        return out;
    }

    if (!targetsReady)
    {
        out.failed = true;
        out.status = mode == EnchantRollMode::Gamepass ? "Enchant button not found." : "Confirm button not found.";
        return out;
    }

    if (nowMs < nextActionAt_ || (lastActionAt_ != 0 && nowMs - lastActionAt_ < 350))
    {
        out.status = "Rolling.";
        return out;
    }

    lastActionAt_ = nowMs;
    nextActionAt_ = nowMs + 350;
    out.status = "Rolling.";
    return out;
}
}
