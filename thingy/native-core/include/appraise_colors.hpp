#pragma once

#include <cstdint>
#include <string>
#include <vector>

#include "enchant_colors.hpp"

namespace macro_port
{
class AppraiseColors
{
public:
    static RgbColor GetAppraiseColor(const std::string& mutationName);
    static std::vector<std::string> GetKnownMutationNames();
};
}
