#include <cstdlib>
#include <iostream>
#include <unordered_map>
#include <vector>

#include "replay_runner.hpp"

using macro_port::FishingCastingMode;
using macro_port::FishingRuntimeContext;
using macro_port::GuiBounds;
using macro_port::HoldApplier;
using macro_port::IInputActuator;
using macro_port::IRobloxMemory;
using macro_port::ReplayFrame;
using macro_port::ReplayRunner;
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
    m.sizes[304] = UDim{0.10f, 0, 0, 0};

    FakeActuator a;
    HoldApplier hold(&a);
    FishingRuntimeContext runtime(&m);
    TrackerOrchestrator orchestrator;
    Tracking1Controller controller;
    RuntimeTrackerEngine engine(&runtime, &orchestrator, &controller, &a, &hold, RuntimeTrackerEngineSettings{});

    std::vector<ReplayFrame> passFrames;
    passFrames.push_back(ReplayFrame{
        RuntimeTrackerTickInput{1200, false, false, true, true, std::nullopt, std::nullopt},
        macro_port::RuntimeTrackerTickResult{macro_port::FishingPhase::Casted, false, false, false},
        false, false, false, false});
    passFrames.push_back(ReplayFrame{
        RuntimeTrackerTickInput{1600, false, false, true, false, std::nullopt, std::nullopt},
        macro_port::RuntimeTrackerTickResult{macro_port::FishingPhase::Fishing, false, false, false},
        false, false, false, false});

    auto passReport = ReplayRunner::Run(&engine, 1000, FishingCastingMode::Normal, passFrames);
    failed += Assert(passReport.pass, "replay-pass");

    std::vector<ReplayFrame> failFrames;
    failFrames.push_back(ReplayFrame{
        RuntimeTrackerTickInput{1200, false, false, true, true, std::nullopt, std::nullopt},
        macro_port::RuntimeTrackerTickResult{macro_port::FishingPhase::Fishing, false, false, false},
        true, false, false, false});
    auto failReport = ReplayRunner::Run(&engine, 1000, FishingCastingMode::Normal, failFrames);
    failed += Assert(!failReport.pass, "replay-fail-detected");
    failed += Assert(!failReport.mismatches.empty(), "replay-has-mismatch");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
