#include "enchant_detector.hpp"

#include <algorithm>
#include <cctype>

#include "enchant_catalog.hpp"

namespace macro_port
{
namespace
{
std::string Normalize(const std::string& s)
{
    std::string out;
    out.reserve(s.size());
    for (char ch : s)
    {
        if (std::isalnum(static_cast<unsigned char>(ch)) != 0)
        {
            out.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(ch))));
        }
    }
    return out;
}
}

std::string EnchantDetector::FindEnchant(const std::string& text)
{
    if (text.empty()) return {};
    for (const auto& enchant : EnchantCatalog::All())
    {
        if (text.find(enchant) != std::string::npos)
        {
            return enchant;
        }

        auto nt = Normalize(text);
        auto ne = Normalize(enchant);
        if (!ne.empty() && nt.find(ne) != std::string::npos)
        {
            return enchant;
        }
    }
    return {};
}
}
