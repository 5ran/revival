#pragma once

#include <cstdint>
#include <string>
#include <vector>

namespace macro_port
{
struct RgbColor
{
    std::uint8_t r = 255;
    std::uint8_t g = 255;
    std::uint8_t b = 255;
};

class EnchantColors
{
public:
    static RgbColor GetEnchantColor(const std::string& enchantName);
    static std::vector<std::string> GetKnownEnchantNames();
    static std::string FindEnchantName(const std::string& text);
};
}
