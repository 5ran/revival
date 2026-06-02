#include "appraise_colors.hpp"

#include <unordered_map>

namespace macro_port
{
namespace
{
const std::unordered_map<std::string, RgbColor> kColors{
    {"Shiny", {255,245,180}}, {"Sparkling", {255,240,170}}, {"Big", {120,255,120}},
    {"Giant", {120,255,120}}, {"Tiny", {140,255,220}}, {"Hexed", {255,0,0}},
    {"Mythical", {255,80,170}}, {"Shrouded", {180,255,180}}};
}

RgbColor AppraiseColors::GetAppraiseColor(const std::string& mutationName)
{
    auto it = kColors.find(mutationName);
    return it == kColors.end() ? RgbColor{} : it->second;
}

std::vector<std::string> AppraiseColors::GetKnownMutationNames()
{
    std::vector<std::string> out;
    out.reserve(kColors.size());
    for (const auto& kv : kColors) out.push_back(kv.first);
    return out;
}
}
