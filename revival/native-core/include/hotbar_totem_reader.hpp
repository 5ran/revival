#pragma once

#include <string>
#include <vector>

#include "hotbar_rod_resolver.hpp"

namespace macro_port
{
class HotbarTotemReader
{
public:
    static std::vector<std::string> GetTotems(IRobloxMemory* memory)
    {
        return HotbarRodResolver::GetHotbarTotems(memory);
    }
};
}
