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
};

class FakeMemory final : public IRobloxMemory
{
public:
    std::uint64_t FindPlayerGui() override { return 0; }
    std::uint64_t FindChildByName(std::uint64_t, const std::string&) override { return 0; }
    std::uint64_t FindDescendantByName(std::uint64_t, const std::string&) override { return 0; }
    std::vector<std::uint64_t> ReadChildren(std::uint64_t) override { return {}; }
    std::string ReadName(std::uint64_t) override { return {}; }
    bool IsVisible(std::uint64_t, const std::string&) override { return false; }
    UDim ReadFramePosition(std::uint64_t) override { return {}; }
    UDim ReadFrameSize(std::uint64_t) override { return {}; }
    std::optional<GuiBounds> ReadGuiBounds(std::uint64_t, bool) override { return std::nullopt; }
    std::string ReadGuiText(std::uint64_t) override { return {}; }
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
    FakeActuator a;
    HoldApplier hold(&a);
    FishingRuntimeContext runtime(&m);
    TrackerOrchestrator orchestrator;
    Tracking1Controller controller;
    RuntimeTrackerEngineSettings s;
    RuntimeTrackerEngine engine(&runtime, &orchestrator, &controller, &a, &hold, s);

    engine.Start(1000, FishingCastingMode::Normal);
    auto t1 = engine.Tick(RuntimeTrackerTickInput{1000, false, false, false, false, std::nullopt, true});
    failed += Assert(!t1.automationGateOpen, "gate-closed-before-settle");
    auto t2 = engine.Tick(RuntimeTrackerTickInput{1700, false, false, false, false, std::nullopt, true});
    failed += Assert(t2.automationGateOpen, "gate-open-after-settle");
    auto t3 = engine.Tick(RuntimeTrackerTickInput{1700, false, false, true, false, std::nullopt, true});
    failed += Assert(!t3.automationGateOpen, "gate-blocked-castbar");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
