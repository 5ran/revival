#include "hotbar_rod_reader.hpp"

#include "fishing_runtime_context.hpp"

namespace macro_port
{
std::string HotbarRodReader::GetHotbarRodDisplayText(IRobloxMemory* memory)
{
    return HotbarRodResolver::GetHotbarRodDisplayText(memory, HotbarSlotSettings::RodSlot);
}

std::string HotbarRodReader::GetHotbarRodName(IRobloxMemory* memory)
{
    return GetHotbarRodDisplayText(memory);
}

std::vector<std::string> HotbarRodReader::GetMasterlineOverlayRodNames(IRobloxMemory* memory)
{
    FishingRuntimeContext runtime(memory);
    return runtime.GetMasterlineOverlayRodNames();
}
}
