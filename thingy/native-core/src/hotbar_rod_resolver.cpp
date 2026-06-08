#include "hotbar_rod_resolver.hpp"

#include <algorithm>
#include <cctype>
#include <regex>

namespace macro_port
{
namespace
{
bool EqualsIgnoreCase(const std::string& a, const std::string& b)
{
    if (a.size() != b.size()) return false;
    for (size_t i = 0; i < a.size(); ++i)
    {
        if (std::tolower(static_cast<unsigned char>(a[i])) != std::tolower(static_cast<unsigned char>(b[i]))) return false;
    }
    return true;
}

std::string ReadSlotText(IRobloxMemory* memory, std::uint64_t slot)
{
    auto nameInstance = memory->FindChildByName(slot, "ItemName");
    if (nameInstance == 0) return {};
    return HotbarRodResolver::NormalizeRodDisplayText(memory->ReadGuiText(nameInstance));
}
}

std::string HotbarRodResolver::GetHotbarRodDisplayText(IRobloxMemory* memory, int rodSlot)
{
    if (memory == nullptr) return {};

    auto playerGui = memory->FindPlayerGui();
    auto backpack = playerGui == 0 ? 0 : memory->FindDescendantByName(playerGui, "backpack");
    auto hotbar = backpack == 0 ? 0 : memory->FindChildByName(backpack, "hotbar");
    if (hotbar == 0) return {};

    std::vector<std::uint64_t> slots;
    for (auto child : memory->ReadChildren(hotbar))
    {
        if (EqualsIgnoreCase(memory->ReadName(child), "ItemTemplate"))
        {
            slots.push_back(child);
        }
    }

    if (slots.empty()) return {};
    int idx = std::clamp(rodSlot, 1, 9) - 1;
    if (idx >= static_cast<int>(slots.size())) idx = static_cast<int>(slots.size()) - 1;

    auto selected = ReadSlotText(memory, slots[static_cast<size_t>(idx)]);
    if (!selected.empty()) return selected;

    for (auto slot : slots)
    {
        auto text = ReadSlotText(memory, slot);
        if (!text.empty()) return text;
    }

    return {};
}

std::vector<std::string> HotbarRodResolver::GetHotbarTotems(IRobloxMemory* memory)
{
    if (memory == nullptr) return {};

    auto playerGui = memory->FindPlayerGui();
    auto backpack = playerGui == 0 ? 0 : memory->FindDescendantByName(playerGui, "backpack");
    auto hotbar = backpack == 0 ? 0 : memory->FindChildByName(backpack, "hotbar");
    if (hotbar == 0) return {};

    std::vector<std::string> out;
    for (auto slot : memory->ReadChildren(hotbar))
    {
        auto text = ReadSlotText(memory, slot);
        if (text.find("Totem") != std::string::npos || text.find("totem") != std::string::npos)
        {
            out.push_back(text);
        }
    }

    std::sort(out.begin(), out.end());
    out.erase(std::unique(out.begin(), out.end()), out.end());
    return out;
}

std::string HotbarRodResolver::NormalizeRodDisplayText(const std::string& text)
{
    if (text.empty()) return {};

    std::string normalized = text;
    normalized = std::regex_replace(normalized, std::regex("\\r"), "\n");
    normalized = std::regex_replace(normalized, std::regex("<[^>]+>"), "");
    normalized = std::regex_replace(normalized, std::regex("[ \\t]+"), " ");
    normalized = std::regex_replace(normalized, std::regex("\\n+"), "\n");

    while (!normalized.empty() && std::isspace(static_cast<unsigned char>(normalized.front())) != 0) normalized.erase(normalized.begin());
    while (!normalized.empty() && std::isspace(static_cast<unsigned char>(normalized.back())) != 0) normalized.pop_back();
    return normalized;
}
}
