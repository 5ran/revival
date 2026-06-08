#include <cstdlib>
#include <iostream>
#include <unordered_map>

#include "runtime_tracker_engine.hpp"

using macro_port::FishingCastingMode;
using macro_port::FishingPhase;
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
    void LeftDown() override { ++downs; }
    void LeftUp() override { ++ups; }
    int downs = 0;
    int ups = 0;
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
    m.childByName["100:reel"] = 200;
    m.visible[200] = false; // no fishing context needed for this cast-phase test

    FakeActuator input;
    HoldApplier hold(&input);
    FishingRuntimeContext runtime(&m);
    TrackerOrchestrator orchestrator(macro_port::TrackerOrchestratorSettings{
        0, 250, 5000, 100, 100, 96.0, 1.0, 14, true});
    Tracking1Controller controller;
    RuntimeTrackerEngineSettings s;
    s.perfectCastBurstCacheEnabled = true;
    s.perfectPowerStickyMs = 20;
    RuntimeTrackerEngine engine(&runtime, &orchestrator, &controller, &input, &hold, s);

    engine.Start(1000, FishingCastingMode::Perfect);

    auto a = engine.Tick(RuntimeTrackerTickInput{1005, false, false, true, false, 95.2, std::nullopt});
    failed += Assert(a.phase == FishingPhase::Casting, "cache-step1-charging");

    // Simulate short bar flicker: no power visible, but release pulse arrives.
    auto b = engine.Tick(RuntimeTrackerTickInput{1015, false, false, false, true, std::nullopt, std::nullopt});
    failed += Assert(b.phase == FishingPhase::Casted, "cache-step2-release");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
