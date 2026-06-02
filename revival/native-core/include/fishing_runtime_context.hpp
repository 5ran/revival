#pragma once

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

#include "reel_metrics.hpp"
#include "roblox_memory.hpp"

namespace macro_port
{
struct ReelContext
{
    std::uint64_t bar = 0;
    std::uint64_t fish = 0;
    std::uint64_t playerbar = 0;
};

struct ReelLocatedContext
{
    ReelContext context;
    double barX = 0.0;
};

class FishingRuntimeContext
{
public:
    explicit FishingRuntimeContext(IRobloxMemory* memory);

    void ResetCache();
    std::optional<ReelContext> GetReelContext();
    bool IsReelGuiVisible(std::uint64_t reelGui = 0);
    std::optional<double> GetFishingCompletionPercent();
    std::optional<double> GetFishingCompletionPercentFromBar(std::uint64_t bar);
    std::optional<double> GetPowerBarPercent();
    bool IsShakeButtonVisible();
    std::optional<ReelMetrics> ReadMetrics(const ReelContext& context);
    std::vector<ReelLocatedContext> GetOrderedReelContexts();
    std::vector<std::string> GetMasterlineOverlayRodNames();

private:
    std::uint64_t GetReelGui();
    std::optional<ReelContext> BuildReelContextFromGui(std::uint64_t reelGui);
    bool IsCached(std::uint64_t address, const std::string& expectedName);
    void ResetReelCache();
    void ResetShakeCache();
    std::uint64_t ResolvePowerBar();
    static double Clamp(double value, double min, double max);
    static bool IsReasonableScale(double value);

    IRobloxMemory* memory_ = nullptr;
    std::uint64_t reelGui_ = 0;
    std::uint64_t bar_ = 0;
    std::uint64_t fish_ = 0;
    std::uint64_t playerbar_ = 0;
    std::uint64_t progressBar_ = 0;
    std::uint64_t powerBar_ = 0;
    std::uint64_t shakeGui_ = 0;
    std::uint64_t shakeSafezone_ = 0;
    std::uint64_t shakeButton_ = 0;
    std::int64_t reelGuiVerifiedAtMs_ = 0;
    std::optional<double> lastPowerPercent_;
    std::int64_t lastPowerSeenAtMs_ = 0;
};
}
