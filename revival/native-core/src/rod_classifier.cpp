#include "rod_classifier.hpp"

#include <algorithm>
#include <cctype>
#include <regex>

namespace macro_port
{
namespace
{
std::string Normalize(const std::string& text)
{
    if (text.empty())
    {
        return {};
    }

    static const std::regex tagRegex("<[^>]+>");
    auto stripped = std::regex_replace(text, tagRegex, "");

    std::string lowered;
    lowered.reserve(stripped.size());
    for (unsigned char c : stripped)
    {
        lowered.push_back(static_cast<char>(std::tolower(c)));
    }

    const auto first = lowered.find_first_not_of(" \t\r\n");
    if (first == std::string::npos)
    {
        return {};
    }
    const auto last = lowered.find_last_not_of(" \t\r\n");
    return lowered.substr(first, last - first + 1);
}

bool Contains(const std::string& haystack, const char* needle)
{
    return haystack.find(needle) != std::string::npos;
}
}

RodKind RodClassifier::Classify(const std::string& displayText)
{
    const auto text = Normalize(displayText);
    if (text.empty())
    {
        return RodKind::Default;
    }

    if (Contains(text, "bellona") && Contains(text, "waraxe"))
    {
        return RodKind::BellonaWaraxe;
    }

    if (Contains(text, "masterline"))
    {
        return RodKind::MasterlineRod;
    }

    if (Contains(text, "tranquility"))
    {
        return RodKind::Tranquility;
    }

    if (Contains(text, "pinion"))
    {
        return RodKind::Pinion;
    }

    if (Contains(text, "dreambreaker"))
    {
        return RodKind::Dreambreaker;
    }

    if (Contains(text, "requiem"))
    {
        return RodKind::Requiem;
    }

    if (Contains(text, "splitbranch") && Contains(text, "twig"))
    {
        return RodKind::SplitbranchTwig;
    }

    if (Contains(text, "migu"))
    {
        return RodKind::MiguRod;
    }

    return RodKind::Default;
}
}

