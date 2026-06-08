#include "world_status_reader.hpp"

#include <algorithm>
#include <cctype>

namespace macro_port
{
namespace
{
std::string ToLower(std::string s)
{
    std::transform(s.begin(), s.end(), s.begin(), [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
    return s;
}

bool Contains(const std::string& text, const std::string& needle)
{
    return ToLower(text).find(ToLower(needle)) != std::string::npos;
}
}

WorldStatusSnapshot WorldStatusReader::FromTexts(
    const std::string& eventText,
    const std::string& weatherText,
    const std::string& cycleText,
    const std::vector<std::string>& allStatuses,
    const std::vector<std::string>& chatEntries)
{
    WorldStatusSnapshot out{};

    std::string allStatusText;
    for (const auto& s : allStatuses)
    {
        if (!allStatusText.empty())
        {
            allStatusText.push_back(' ');
        }
        allStatusText += s;
    }

    std::string combined = eventText + " " + weatherText + " " + allStatusText;
    out.weather = ResolveWeather(combined);
    out.cycle = ResolveCycle(cycleText, allStatusText);

    for (const auto& entry : chatEntries)
    {
        if (Contains(entry, "There is currently a Shiny surge"))
        {
            out.shinySurge = true;
            out.sparklingSurge = false;
            out.mutationSurge = false;
        }
        else if (Contains(entry, "Shiny Surge is now over"))
        {
            out.shinySurge = false;
        }

        if (Contains(entry, "Today is the Day of the Luminous") || Contains(entry, "Tonight is the Night of the Luminous"))
        {
            out.shinySurge = false;
            out.sparklingSurge = true;
            out.mutationSurge = false;
        }
        else if (Contains(entry, "Night of the Luminous is now over"))
        {
            out.sparklingSurge = false;
        }

        if (Contains(entry, "There is currently a Mutation surge"))
        {
            out.shinySurge = false;
            out.sparklingSurge = false;
            out.mutationSurge = true;
        }
        else if (Contains(entry, "Mutation Surge is now over"))
        {
            out.mutationSurge = false;
        }
    }

    return out;
}

std::string WorldStatusReader::ResolveWeather(const std::string& text)
{
    if (Contains(text, "aurora")) return "Aurora Borealis";
    if (Contains(text, "starfall")) return "Starfall";
    if (Contains(text, "eclipse")) return "Eclipse";
    if (Contains(text, "rainbow")) return "Rainbow";
    if (Contains(text, "rain")) return "Rain";
    if (Contains(text, "wind")) return "Windy";
    if (Contains(text, "fog")) return "Foggy";
    if (Contains(text, "clear")) return "Clear";
    return {};
}

std::string WorldStatusReader::ResolveCycle(const std::string& cycleText, const std::string& allStatusText)
{
    if (Contains(cycleText, "night")) return "Night";
    if (Contains(cycleText, "day")) return "Day";

    std::string scrubbed = allStatusText;
    auto lowered = ToLower(scrubbed);
    auto marker = ToLower("Night of the Luminous");
    auto pos = lowered.find(marker);
    if (pos != std::string::npos)
    {
        scrubbed.erase(pos, marker.size());
    }

    if (Contains(scrubbed, "night")) return "Night";
    if (Contains(scrubbed, "day")) return "Day";
    return {};
}
}
