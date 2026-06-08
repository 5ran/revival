#include "enchant_catalog.hpp"

namespace macro_port
{
const std::vector<std::string>& EnchantCatalog::All()
{
    static const std::vector<std::string> kAll{
        "Abyssal","Blessed","Blood Reckoning","Breezed","Chaotic","Chronos","Clever","Controlled",
        "Divine","Flashline","Ghastly","Hasty","Hunter","Insight","Long","Lucky","Momentum",
        "Mutated","Noir","Quality","Resilient","Scavenger","Sea King","Scrapper","Steady",
        "Storming","Swift","Unbreakable","Wormhole"};
    return kAll;
}
}
