#pragma once

#include "auto_totem_mode.hpp"

namespace macro_port
{
enum class AutoTotemSpecial
{
    None,
    Shiny,
    Sparkling,
    Mutation,
};

enum class AutoTotemTimePreference
{
    None,
    Day,
    Night,
};

struct AutoTotemSettings
{
    bool enabled = false;
    const char* totemName = "None";
    AutoTotemSpecial special = AutoTotemSpecial::None;
    AutoTotemTimePreference timePreference = AutoTotemTimePreference::None;
    AutoTotemMode mode = AutoTotemMode::Expire;
    int intervalSeconds = 900;
    int useSettleDelayMs = 200;
    int timeChangeWaitMs = 1300;
    int maxSundialAttempts = 4;
};
}
