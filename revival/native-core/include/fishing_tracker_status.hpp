#pragma once

#include <optional>
#include <string>

namespace macro_port
{
struct FishingTrackerStatus
{
    bool running = false;
    std::string phase = "OFF";
    std::string message;
    std::optional<double> progressPercent;
    bool suspended = false;
};
}
