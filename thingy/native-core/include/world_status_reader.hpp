#pragma once

#include <cstdint>
#include <string>
#include <vector>

namespace macro_port
{
struct WorldStatusSnapshot
{
    std::string weather;
    std::string cycle;
    bool shinySurge = false;
    bool sparklingSurge = false;
    bool mutationSurge = false;
};

class WorldStatusReader
{
public:
    static WorldStatusSnapshot FromTexts(
        const std::string& eventText,
        const std::string& weatherText,
        const std::string& cycleText,
        const std::vector<std::string>& allStatuses,
        const std::vector<std::string>& chatEntries);

private:
    static std::string ResolveWeather(const std::string& text);
    static std::string ResolveCycle(const std::string& cycleText, const std::string& allStatusText);
};
}
