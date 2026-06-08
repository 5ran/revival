#pragma once

#include <string>
#include <vector>

#include "roblox_memory.hpp"

namespace macro_port
{
class HotbarRodResolver
{
public:
    static std::string GetHotbarRodDisplayText(IRobloxMemory* memory, int rodSlot);
    static std::vector<std::string> GetHotbarTotems(IRobloxMemory* memory);
    static std::string NormalizeRodDisplayText(const std::string& text);
};
}
