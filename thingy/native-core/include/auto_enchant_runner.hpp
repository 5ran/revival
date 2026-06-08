#pragma once

#include <cstdint>
#include <string>

namespace macro_port
{
enum class EnchantRollMode
{
    Gamepass,
    Normal,
};

struct EnchantSnapshot
{
    std::string enchant;
};

struct EnchantRunnerResult
{
    bool completed = false;
    bool failed = false;
    std::string status;
    EnchantSnapshot snapshot;
};

class AutoEnchantRunner
{
public:
    void Reset();
    EnchantRunnerResult Step(
        std::int64_t nowMs,
        const std::string& targetEnchant,
        EnchantRollMode mode,
        const std::string& currentEnchant,
        bool targetsReady);

private:
    std::int64_t nextActionAt_ = 0;
    std::int64_t lastActionAt_ = 0;
};
}
