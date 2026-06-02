#include <cstdlib>
#include <iostream>
#include <unordered_map>

#include "runtime_tracker_engine.hpp"

using macro_port::FishingCastingMode;
using macro_port::FishingRuntimeContext;
using macro_port::GuiBounds;
using macro_port::HoldApplier;
using macro_port::IInputActuator;
using macro_port::IRobloxMemory;
using macro_port::RuntimeTrackerEngine;
using macro_port::RuntimeTrackerEngineSettings;
using macro_port::RuntimeTrackerTickInput;
using macro_port::TrackerOrchestrator;
using macro_port::Tracking1Controller;
using macro_port::UDim;

class FakeActuator final : public IInputActuator
{
public:
    void LeftDown() override {}
    void LeftUp() override {}
    void RightDown() override { ++rightDowns; }
    void RightUp() override { ++rightUps; }
    int rightDowns = 0;
    int rightUps = 0;
};

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
        return it != visible.end() && it->second;
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
        return it == bounds.end() ? std::optional<GuiBounds>{} : std::optional<GuiBounds>{it->second};
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

    // Primary reel
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
    m.positions[301] = UDim{0.70f, 0, 0, 0};
    m.sizes[301] = UDim{0.10f, 0, 0, 0};
    m.positions[302] = UDim{0.20f, 0, 0, 0};
    m.sizes[302] = UDim{0.20f, 0, 0, 0};
    m.childByName["300:progress"] = 303;
    m.childByName["303:bar"] = 304;
    m.names[304] = "bar";
    m.sizes[304] = UDim{0.20f, 0, 0, 0};

    // Secondary reel (right side)
    m.children[100] = {200, 210};
    m.names[200] = "reel";
    m.names[210] = "reel";
    m.visible[210] = true;
    m.childByName["210:bar"] = 310;
    m.visible[310] = true;
    m.childByName["310:fish"] = 311;
    m.childByName["310:playerbar"] = 312;
    m.visible[311] = true;
    m.visible[312] = true;
    m.names[310] = "bar";
    m.names[311] = "fish";
    m.names[312] = "playerbar";
    m.positions[311] = UDim{0.80f, 0, 0, 0};
    m.sizes[311] = UDim{0.10f, 0, 0, 0};
    m.positions[312] = UDim{0.10f, 0, 0, 0};
    m.sizes[312] = UDim{0.20f, 0, 0, 0};
    m.bounds[300] = GuiBounds{100, 100, 300, 50};
    m.bounds[310] = GuiBounds{700, 100, 300, 50};

    FakeActuator input;
    HoldApplier hold(&input);
    FishingRuntimeContext runtime(&m);
    TrackerOrchestrator orchestrator;
    Tracking1Controller controller;
    RuntimeTrackerEngineSettings cfg;
    cfg.bellonaEnabled = true;
    cfg.mode = RuntimeTrackerEngineSettings::TrackingMode::Tracking3;
    RuntimeTrackerEngine engine(&runtime, &orchestrator, &controller, &input, &hold, cfg);

    engine.Start(1000, FishingCastingMode::Normal);
    (void)engine.Tick(RuntimeTrackerTickInput{1400, false, false, true, true, std::nullopt});
    auto r = engine.Tick(RuntimeTrackerTickInput{1700, false, false, true, false, std::nullopt});

    failed += Assert(r.rightHoldApplied, "bellona-right-active");
    failed += Assert(input.rightDowns >= 1, "bellona-rightdown");

    engine.Stop(1800);
    failed += Assert(input.rightUps >= 1, "bellona-rightup");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
