#include <cstdlib>
#include <iostream>
#include <unordered_map>

#include "fishing_runtime_context.hpp"

using macro_port::FishingRuntimeContext;
using macro_port::GuiBounds;
using macro_port::IRobloxMemory;
using macro_port::ReelContext;
using macro_port::UDim;

class FakeMemory final : public IRobloxMemory
{
public:
    std::uint64_t FindPlayerGui() override { return playerGui; }

    std::uint64_t FindChildByName(std::uint64_t parent, const std::string& name) override
    {
        auto key = std::to_string(parent) + ":" + name;
        auto it = childByName.find(key);
        return it == childByName.end() ? 0 : it->second;
    }

    std::uint64_t FindDescendantByName(std::uint64_t root, const std::string& name) override
    {
        return FindChildByName(root, name);
    }

    std::vector<std::uint64_t> ReadChildren(std::uint64_t instance) override
    {
        auto it = children.find(instance);
        return it == children.end() ? std::vector<std::uint64_t>{} : it->second;
    }

    std::string ReadName(std::uint64_t instance) override
    {
        auto it = names.find(instance);
        return it == names.end() ? std::string{} : it->second;
    }

    bool IsVisible(std::uint64_t instance, const std::string& offsetKey) override
    {
        (void)offsetKey;
        auto it = visible.find(instance);
        return it == visible.end() ? false : it->second;
    }

    UDim ReadFramePosition(std::uint64_t frame) override { return positions[frame]; }
    UDim ReadFrameSize(std::uint64_t frame) override { return sizes[frame]; }

    std::optional<GuiBounds> ReadGuiBounds(std::uint64_t instance, bool visibleRequired) override
    {
        if (visibleRequired && !IsVisible(instance, "FrameVisible"))
        {
            return std::nullopt;
        }

        auto it = bounds.find(instance);
        if (it == bounds.end())
        {
            return std::nullopt;
        }

        return it->second;
    }

    std::string ReadGuiText(std::uint64_t instance) override
    {
        auto it = text.find(instance);
        return it == text.end() ? std::string{} : it->second;
    }

    std::uint64_t playerGui = 100;
    std::unordered_map<std::string, std::uint64_t> childByName;
    std::unordered_map<std::uint64_t, std::vector<std::uint64_t>> children;
    std::unordered_map<std::uint64_t, std::string> names;
    std::unordered_map<std::uint64_t, bool> visible;
    std::unordered_map<std::uint64_t, UDim> positions;
    std::unordered_map<std::uint64_t, UDim> sizes;
    std::unordered_map<std::uint64_t, GuiBounds> bounds;
    std::unordered_map<std::uint64_t, std::string> text;
};

static int Assert(bool condition, const char* name)
{
    if (!condition)
    {
        std::cerr << "FAIL: " << name << "\n";
        return 1;
    }

    return 0;
}

int main()
{
    int failed = 0;
    FakeMemory m;

    // Reel tree.
    m.childByName["100:reel"] = 200;
    m.visible[200] = true;
    m.childByName["200:bar"] = 300;
    m.visible[300] = true;
    m.childByName["300:fish"] = 301;
    m.childByName["300:playerbar"] = 302;
    m.visible[301] = true;
    m.visible[302] = true;
    m.names[300] = "bar";
    m.names[301] = "fish";
    m.names[302] = "playerbar";

    m.positions[301] = UDim{0.40f, 0, 0, 0};
    m.sizes[301] = UDim{0.10f, 0, 0, 0};
    m.positions[302] = UDim{0.45f, 0, 0, 0};
    m.sizes[302] = UDim{0.20f, 0, 0, 0};

    m.childByName["300:progress"] = 303;
    m.childByName["303:bar"] = 304;
    m.names[304] = "bar";
    m.sizes[304] = UDim{0.96f, 0, 0, 0};

    FishingRuntimeContext ctx(&m);

    auto reel = ctx.GetReelContext();
    failed += Assert(reel.has_value(), "reel-context");

    auto metrics = reel.has_value() ? ctx.ReadMetrics(*reel) : std::nullopt;
    failed += Assert(metrics.has_value(), "read-metrics");
    failed += Assert(metrics.has_value() && metrics->fishCenter > 0.44 && metrics->fishCenter < 0.46, "fish-center");

    auto progress = ctx.GetFishingCompletionPercent();
    failed += Assert(progress.has_value() && *progress > 95.9 && *progress < 96.1, "progress");

    // Masterline parsing.
    m.childByName["100:hud"] = 400;
    m.childByName["400:safezone"] = 401;
    m.childByName["401:statuses"] = 402;
    m.children[402] = {403};
    m.names[403] = "Masterlineabc";
    m.childByName["403:tooltip"] = 404;
    m.text[404] = "Wind Elemental\nNates Blade";

    auto rods = ctx.GetMasterlineOverlayRodNames();
    failed += Assert(rods.size() == 2, "masterline-count");
    failed += Assert(rods.size() == 2 && rods[0] == "Wind Elemental" && rods[1] == "Nates Blade", "masterline-values");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }

    return 1;
}
