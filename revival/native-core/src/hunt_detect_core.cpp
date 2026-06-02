#include "hunt_detect_core.hpp"

#include <algorithm>

namespace macro_port
{
namespace
{
std::string Lower(std::string s)
{
    std::transform(s.begin(), s.end(), s.begin(), [](unsigned char c){ return static_cast<char>(std::tolower(c)); });
    return s;
}

bool ContainsI(const std::string& hay, const std::string& needle)
{
    return Lower(hay).find(Lower(needle)) != std::string::npos;
}
}

std::vector<HuntMatchRule> HuntDetectCore::DefaultRules()
{
    return {
        {"Megalodon", "has been spotted"},
        {"Ancient Megalodon", "has been spotted"},
        {"Kraken", "has been spotted"},
        {"Leviathan", "has been summoned"},
        {"Orca Migration", "has begun"},
        {"Whale Migration", "has begun"},
        {"Olympian Devil", "has been summoned"},
        {"Bloop Fish", "has emerged"},
        {"Sunken Chests", ""},
        {"Earthquake", ""}
    };
}

HuntDetection HuntDetectCore::Detect(const std::vector<std::string>& chatEntries, const std::vector<HuntMatchRule>& selectedRules)
{
    for (const auto& entry : chatEntries)
    {
        for (const auto& rule : selectedRules)
        {
            if (!ContainsI(entry, rule.title))
            {
                continue;
            }

            if (!rule.phrase.empty() && !ContainsI(entry, rule.phrase))
            {
                continue;
            }

            return HuntDetection{true, rule.title, entry};
        }
    }

    return HuntDetection{};
}
}
