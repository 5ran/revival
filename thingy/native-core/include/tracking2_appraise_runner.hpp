#pragma once

#include <cstdint>
#include <string>
#include <vector>

namespace macro_port
{
enum class AppraiseRunMode
{
    Gamepass,
    Normal,
};

struct AppraiseSettings
{
    std::vector<std::string> baseMutations;
    bool requireShiny = false;
    bool requireSparkling = false;
    bool requireTiny = false;
    bool requireSmall = false;
    bool requireBig = false;
    bool requireGiant = false;
    int clickX = 0;
    int clickY = 0;
    double gamepassSpeed = 1.0;
    AppraiseRunMode mode = AppraiseRunMode::Normal;
};

struct AppraiseStepResult
{
    bool completed = false;
    bool failed = false;
    std::string status;
};

class Tracking2AppraiseRunner
{
public:
    void Reset();
    AppraiseStepResult Step(std::int64_t nowMs, const AppraiseSettings& settings, bool foundDesiredMutation, bool enchantButtonReady);

private:
    std::string state_ = "IDLE";
    std::int64_t nextActionAt_ = 0;
};
}
