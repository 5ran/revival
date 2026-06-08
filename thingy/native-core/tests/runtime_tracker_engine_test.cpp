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
using macro_port::RuntimeTrackerTickInput;
using macro_port::TrackerOrchestrator;
using macro_port::Tracking1Controller;
using macro_port::UDim;

class FakeActuator final : public IInputActuator
{
public:
    void LeftDown() override { ++downs; }
    void LeftUp() override { ++ups; }
    void RightDown() override { ++rightDowns; }
    void RightUp() override { ++rightUps; }
    int downs = 0;
    int ups = 0;
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

static RuntimeTrackerTickInput MakeInput(
    std::int64_t now,
    bool reelVisible,
    bool hasMetrics,
    bool shakeVisible,
    std::optional<bool> completion = std::nullopt)
{
    RuntimeTrackerTickInput input{};
    input.nowMs = now;
    input.hasSensorSnapshot = true;
    input.reelVisibleSnapshot = reelVisible;
    input.hasMetricsSnapshot = hasMetrics;
    input.shakeVisible = shakeVisible;
    input.completionOverride = completion;
    return input;
}

int main()
{
    int failed = 0;
    FakeMemory m;

    // Reel tree
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
    m.positions[301] = UDim{0.45f, 0, 0, 0};
    m.sizes[301] = UDim{0.08f, 0, 0, 0};
    m.positions[302] = UDim{0.42f, 0, 0, 0};
    m.sizes[302] = UDim{0.20f, 0, 0, 0};
    m.childByName["300:progress"] = 303;
    m.childByName["303:bar"] = 304;
    m.names[304] = "bar";
    m.sizes[304] = UDim{0.15f, 0, 0, 0};

    FakeActuator actuator;
    HoldApplier hold(&actuator);
    FishingRuntimeContext runtime(&m);
    TrackerOrchestrator orchestrator(macro_port::TrackerOrchestratorSettings{
        0, 0, 5000, 100, 100, 96.0, 1.0, 14, true});
    Tracking1Controller controller;
    RuntimeTrackerEngine engine(&runtime, &orchestrator, &controller, &actuator, &hold);

    engine.Start(1000, FishingCastingMode::Normal);

    auto r1 = engine.Tick(MakeInput(1010, true, true, false));
    failed += Assert(r1.phase != macro_port::FishingPhase::Off, "phase-casting");

    auto r2 = engine.Tick(MakeInput(1400, true, true, true));
    failed += Assert(r2.phase != macro_port::FishingPhase::Off, "phase-running");

    // Ensure we enter fishing before testing completion recast.
    (void)engine.Tick(MakeInput(1600, true, false, true));

    // Force completion path via override and verify recast reset.
    auto r3 = engine.Tick(MakeInput(1800, true, false, true, true));
    failed += Assert(r3.phase != macro_port::FishingPhase::Off, "completion-recast");

    engine.Stop(1900);

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
