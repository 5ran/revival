#pragma once

#include <string>
#include <unordered_map>
#include <vector>

namespace macro_port
{
struct HuntMatchRule
{
    std::string title;
    std::string phrase;
};

struct HuntDetection
{
    bool matched = false;
    std::string title;
    std::string sourceText;
};

class HuntDetectCore
{
public:
    static std::vector<HuntMatchRule> DefaultRules();
    static HuntDetection Detect(const std::vector<std::string>& chatEntries, const std::vector<HuntMatchRule>& selectedRules);
};
}
