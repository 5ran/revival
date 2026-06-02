#include "enchant_colors.hpp"

#include <algorithm>
#include <cctype>
#include <unordered_map>

namespace macro_port
{
namespace
{
const std::unordered_map<std::string, RgbColor> kColors{
    {"Abyssal", {52,70,180}}, {"Blessed", {255,70,170}}, {"Hasty", {255,210,90}},
    {"Lucky", {120,255,180}}, {"Quality", {180,255,80}}, {"Swift", {180,255,255}},
    {"Tryhard", {255,0,0}}, {"Vicious", {255,120,90}}, {"Wise", {180,120,255}}};

std::string Lower(std::string s)
{
    std::transform(s.begin(), s.end(), s.begin(), [](unsigned char c){ return static_cast<char>(std::tolower(c)); });
    return s;
}
}

RgbColor EnchantColors::GetEnchantColor(const std::string& enchantName)
{
    auto it = kColors.find(enchantName);
    return it == kColors.end() ? RgbColor{} : it->second;
}

std::vector<std::string> EnchantColors::GetKnownEnchantNames()
{
    std::vector<std::string> out;
    out.reserve(kColors.size());
    for (const auto& kv : kColors) out.push_back(kv.first);
    return out;
}

std::string EnchantColors::FindEnchantName(const std::string& text)
{
    auto lt = Lower(text);
    for (const auto& kv : kColors)
    {
        if (lt.find(Lower(kv.first)) != std::string::npos)
        {
            return kv.first;
        }
    }
    return {};
}
}
