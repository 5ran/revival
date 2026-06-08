#pragma once

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

namespace macro_port
{
struct UDim
{
    float xScale = 0.0f;
    int xOffset = 0;
    float yScale = 0.0f;
    int yOffset = 0;
};

struct GuiBounds
{
    float x = 0.0f;
    float y = 0.0f;
    float width = 0.0f;
    float height = 0.0f;
};

class IRobloxMemory
{
public:
    virtual ~IRobloxMemory() = default;

    virtual std::uint64_t FindPlayerGui() = 0;
    virtual std::uint64_t FindChildByName(std::uint64_t parent, const std::string& name) = 0;
    virtual std::uint64_t FindDescendantByName(std::uint64_t root, const std::string& name) = 0;
    virtual std::vector<std::uint64_t> ReadChildren(std::uint64_t instance) = 0;
    virtual std::string ReadName(std::uint64_t instance) = 0;
    virtual bool IsVisible(std::uint64_t instance, const std::string& offsetKey) = 0;
    virtual UDim ReadFramePosition(std::uint64_t frame) = 0;
    virtual UDim ReadFrameSize(std::uint64_t frame) = 0;
    virtual std::optional<GuiBounds> ReadGuiBounds(std::uint64_t instance, bool visibleRequired) = 0;
    virtual std::string ReadGuiText(std::uint64_t instance) = 0;
};
}
