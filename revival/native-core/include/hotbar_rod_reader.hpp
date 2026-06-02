#pragma once

#include <string>
#include <vector>

#include "hotbar_rod_resolver.hpp"
#include "hotbar_slot_settings.hpp"

namespace macro_port
{
struct HotbarRodSnapshot
{
    std::string rodName;
    std::string equippedToolName;
    bool isEquipped = false;
};

class HotbarRodReader
{
public:
    static std::string GetHotbarRodDisplayText(IRobloxMemory* memory);
    static std::string GetHotbarRodName(IRobloxMemory* memory);
    static std::vector<std::string> GetMasterlineOverlayRodNames(IRobloxMemory* memory);
};
}
