#include "fishing_runtime_context.hpp"
#include "win32_roblox_memory.hpp"

#include <algorithm>
#include <cctype>
#include <chrono>
#include <cmath>

namespace macro_port
{
namespace
{
constexpr std::int64_t kReelGuiReverifyIntervalMs = 500;
constexpr std::int64_t kPowerBarStickyMs = 160;

std::int64_t NowMs()
{
    const auto now = std::chrono::steady_clock::now().time_since_epoch();
    return std::chrono::duration_cast<std::chrono::milliseconds>(now).count();
}

bool EqualsIgnoreCase(const std::string& a, const std::string& b)
{
    if (a.size() != b.size())
    {
        return false;
    }

    for (size_t i = 0; i < a.size(); ++i)
    {
        if (std::tolower(static_cast<unsigned char>(a[i])) != std::tolower(static_cast<unsigned char>(b[i])))
        {
            return false;
        }
    }

    return true;
}

bool StartsWithIgnoreCase(const std::string& value, const std::string& prefix)
{
    if (value.size() < prefix.size())
    {
        return false;
    }

    for (size_t i = 0; i < prefix.size(); ++i)
    {
        if (std::tolower(static_cast<unsigned char>(value[i])) != std::tolower(static_cast<unsigned char>(prefix[i])))
        {
            return false;
        }
    }

    return true;
}

std::string Trim(const std::string& s)
{
    size_t start = 0;
    while (start < s.size() && std::isspace(static_cast<unsigned char>(s[start])) != 0)
    {
        ++start;
    }

    size_t end = s.size();
    while (end > start && std::isspace(static_cast<unsigned char>(s[end - 1])) != 0)
    {
        --end;
    }

    return s.substr(start, end - start);
}
}

FishingRuntimeContext::FishingRuntimeContext(IRobloxMemory* memory) : memory_(memory)
{
}

void FishingRuntimeContext::ResetCache()
{
    reelGui_ = 0;
    bar_ = 0;
    fish_ = 0;
    playerbar_ = 0;
    progressBar_ = 0;
    powerBar_ = 0;
    shakeGui_ = 0;
    shakeSafezone_ = 0;
    shakeButton_ = 0;
    reelGuiVerifiedAtMs_ = 0;
    lastPowerPercent_.reset();
    lastPowerSeenAtMs_ = 0;
}

std::optional<ReelContext> FishingRuntimeContext::GetReelContext()
{
    auto reelGui = GetReelGui();
    if (reelGui == 0 || !IsReelGuiVisible(reelGui))
    {
        ResetReelCache();
        return std::nullopt;
    }

    if (IsCached(bar_, "bar") && IsCached(fish_, "fish") && IsCached(playerbar_, "playerbar") &&
        memory_->IsVisible(bar_, "FrameVisible") && memory_->IsVisible(fish_, "FrameVisible") &&
        memory_->IsVisible(playerbar_, "FrameVisible"))
    {
        return ReelContext{bar_, fish_, playerbar_};
    }

    auto built = BuildReelContextFromGui(reelGui);
    if (!built.has_value())
    {
        ResetReelCache();
        return std::nullopt;
    }

    bar_ = built->bar;
    fish_ = built->fish;
    playerbar_ = built->playerbar;
    return built;
}

bool FishingRuntimeContext::IsReelGuiVisible(std::uint64_t reelGui)
{
    reelGui = reelGui == 0 ? GetReelGui() : reelGui;
    return reelGui != 0 && memory_->IsVisible(reelGui, "ScreenGuiEnabled");
}

std::optional<double> FishingRuntimeContext::GetFishingCompletionPercent()
{
    auto reelGui = GetReelGui();
    if (reelGui == 0)
    {
        progressBar_ = 0;
        return std::nullopt;
    }

    if (!IsCached(progressBar_, "bar"))
    {
        auto bar = IsCached(bar_, "bar") ? bar_ : memory_->FindChildByName(reelGui, "bar");
        if (bar == 0)
        {
            return std::nullopt;
        }

        auto progressFrame = memory_->FindChildByName(bar, "progress");
        progressBar_ = progressFrame == 0 ? 0 : memory_->FindChildByName(progressFrame, "bar");
    }

    if (progressBar_ == 0)
    {
        return std::nullopt;
    }

    return Clamp(static_cast<double>(memory_->ReadFrameSize(progressBar_).xScale) * 100.0, 0.0, 100.0);
}

std::optional<double> FishingRuntimeContext::GetFishingCompletionPercentFromBar(std::uint64_t bar)
{
    if (bar == 0)
    {
        return std::nullopt;
    }

    auto progressFrame = memory_->FindChildByName(bar, "progress");
    auto progressBar = progressFrame == 0 ? 0 : memory_->FindChildByName(progressFrame, "bar");
    if (progressBar == 0)
    {
        return std::nullopt;
    }

    return Clamp(static_cast<double>(memory_->ReadFrameSize(progressBar).xScale) * 100.0, 0.0, 100.0);
}

std::optional<double> FishingRuntimeContext::GetPowerBarPercent()
{
    const auto now = NowMs();
    if (!IsCached(powerBar_, "bar"))
    {
        powerBar_ = ResolvePowerBar();
    }

    if (powerBar_ == 0)
    {
        if (lastPowerPercent_.has_value() && now - lastPowerSeenAtMs_ <= kPowerBarStickyMs)
        {
            return lastPowerPercent_;
        }
        return std::nullopt;
    }

    const auto scaleY = static_cast<double>(memory_->ReadFrameSize(powerBar_).yScale);
    if (std::isnan(scaleY) || std::isinf(scaleY) || scaleY < -0.05 || scaleY > 1.5)
    {
        powerBar_ = 0;
        if (lastPowerPercent_.has_value() && now - lastPowerSeenAtMs_ <= kPowerBarStickyMs)
        {
            return lastPowerPercent_;
        }
        return std::nullopt;
    }

    const auto percent = Clamp(scaleY * 100.0, 0.0, 100.0);
    lastPowerPercent_ = percent;
    lastPowerSeenAtMs_ = now;
    return percent;
}

bool FishingRuntimeContext::IsShakeButtonVisible()
{
    auto playerGui = memory_->FindPlayerGui();
    if (playerGui == 0)
    {
        ResetShakeCache();
        return false;
    }

    if (!IsCached(shakeGui_, "shakeui"))
    {
        shakeGui_ = memory_->FindChildByName(playerGui, "shakeui");
        if (shakeGui_ == 0)
        {
            shakeGui_ = memory_->FindDescendantByName(playerGui, "shakeui");
        }
    }

    if (shakeGui_ == 0 || !memory_->IsVisible(shakeGui_, "ScreenGuiEnabled"))
    {
        ResetShakeCache();
        return false;
    }

    if (!IsCached(shakeSafezone_, "safezone"))
    {
        shakeSafezone_ = memory_->FindChildByName(shakeGui_, "safezone");
        if (shakeSafezone_ == 0)
        {
            shakeSafezone_ = memory_->FindDescendantByName(shakeGui_, "safezone");
        }
    }

    if (shakeSafezone_ == 0)
    {
        shakeButton_ = 0;
        return false;
    }

    if (!IsCached(shakeButton_, "button"))
    {
        shakeButton_ = memory_->FindChildByName(shakeSafezone_, "button");
        if (shakeButton_ == 0)
        {
            shakeButton_ = memory_->FindDescendantByName(shakeSafezone_, "button");
        }
    }

    if (shakeButton_ == 0 || !memory_->IsVisible(shakeButton_, "FrameVisible"))
    {
        return false;
    }

    if (auto* winMemory = dynamic_cast<Win32RobloxMemory*>(memory_); winMemory != nullptr)
    {
        const auto klass = winMemory->ReadClassName(shakeButton_);
        return EqualsIgnoreCase(klass, "ImageButton");
    }

    return true;
}

std::optional<ReelMetrics> FishingRuntimeContext::ReadMetrics(const ReelContext& context)
{
    if (!IsReelGuiVisible() || !memory_->IsVisible(context.bar, "FrameVisible") ||
        !memory_->IsVisible(context.fish, "FrameVisible") || !memory_->IsVisible(context.playerbar, "FrameVisible"))
    {
        ResetReelCache();
        return std::nullopt;
    }

    auto fishPosition = memory_->ReadFramePosition(context.fish);
    auto fishSize = memory_->ReadFrameSize(context.fish);
    auto playerbarPosition = memory_->ReadFramePosition(context.playerbar);
    auto playerbarSize = memory_->ReadFrameSize(context.playerbar);

    auto fishCenter = static_cast<double>(fishPosition.xScale + fishSize.xScale / 2.0f);
    auto playerbarCenter = static_cast<double>(playerbarPosition.xScale);
    auto playerbarWidth = std::max(0.001, static_cast<double>(playerbarSize.xScale));

    if (!IsReasonableScale(fishCenter) || !IsReasonableScale(playerbarCenter) || playerbarWidth > 1.5)
    {
        ResetReelCache();
        return std::nullopt;
    }

    return ReelMetrics{fishCenter, playerbarCenter, playerbarWidth};
}

std::vector<ReelLocatedContext> FishingRuntimeContext::GetOrderedReelContexts()
{
    auto playerGui = memory_->FindPlayerGui();
    if (playerGui == 0)
    {
        return {};
    }

    std::vector<ReelLocatedContext> contexts;
    for (auto child : memory_->ReadChildren(playerGui))
    {
        if (!EqualsIgnoreCase(memory_->ReadName(child), "reel") || !memory_->IsVisible(child, "ScreenGuiEnabled"))
        {
            continue;
        }

        auto ctx = BuildReelContextFromGui(child);
        if (!ctx.has_value())
        {
            continue;
        }

        auto bounds = memory_->ReadGuiBounds(ctx->bar, false);
        if (!bounds.has_value())
        {
            continue;
        }

        contexts.push_back(ReelLocatedContext{*ctx, static_cast<double>(bounds->x)});
    }

    std::sort(contexts.begin(), contexts.end(), [](const ReelLocatedContext& a, const ReelLocatedContext& b) {
        return a.barX < b.barX;
    });

    return contexts;
}

std::vector<std::string> FishingRuntimeContext::GetMasterlineOverlayRodNames()
{
    auto playerGui = memory_->FindPlayerGui();
    if (playerGui == 0)
    {
        return {};
    }

    auto hud = memory_->FindChildByName(playerGui, "hud");
    if (hud == 0)
    {
        hud = memory_->FindDescendantByName(playerGui, "hud");
    }
    if (hud == 0)
    {
        return {};
    }

    auto safezone = memory_->FindDescendantByName(hud, "safezone");
    auto statuses = safezone == 0 ? 0 : memory_->FindDescendantByName(safezone, "statuses");
    if (statuses == 0)
    {
        return {};
    }

    std::uint64_t masterlineStatus = 0;
    for (auto child : memory_->ReadChildren(statuses))
    {
        if (StartsWithIgnoreCase(memory_->ReadName(child), "Masterline"))
        {
            masterlineStatus = child;
            break;
        }
    }

    if (masterlineStatus == 0)
    {
        return {};
    }

    auto tooltip = memory_->FindChildByName(masterlineStatus, "tooltip");
    if (tooltip == 0)
    {
        tooltip = memory_->FindDescendantByName(masterlineStatus, "tooltip");
    }
    if (tooltip == 0)
    {
        return {};
    }

    auto text = memory_->ReadGuiText(tooltip);
    if (text.empty())
    {
        return {};
    }

    std::vector<std::string> names;
    std::string line;
    for (char ch : text)
    {
        if (ch == '\r')
        {
            continue;
        }

        if (ch == '\n')
        {
            auto trimmed = Trim(line);
            if (!trimmed.empty())
            {
                if (!trimmed.empty() && static_cast<unsigned char>(trimmed[0]) == 0xE2)
                {
                    auto pos = trimmed.find(' ');
                    if (pos != std::string::npos)
                    {
                        trimmed = Trim(trimmed.substr(pos + 1));
                    }
                }

                if (!trimmed.empty())
                {
                    names.push_back(trimmed);
                }
            }
            line.clear();
            continue;
        }

        line.push_back(ch);
    }

    auto trimmed = Trim(line);
    if (!trimmed.empty())
    {
        names.push_back(trimmed);
    }

    return names;
}

std::uint64_t FishingRuntimeContext::GetReelGui()
{
    const auto nowMs = NowMs();
    const bool needsReverify = reelGuiVerifiedAtMs_ == 0 ||
        nowMs - reelGuiVerifiedAtMs_ >= kReelGuiReverifyIntervalMs ||
        !IsCached(reelGui_, "reel");
    if (!needsReverify)
    {
        return reelGui_;
    }

    auto playerGui = memory_->FindPlayerGui();
    auto live = playerGui == 0 ? 0 : memory_->FindChildByName(playerGui, "reel");

    if (live != reelGui_)
    {
        bar_ = 0;
        fish_ = 0;
        playerbar_ = 0;
        progressBar_ = 0;
    }

    reelGui_ = live;
    reelGuiVerifiedAtMs_ = nowMs;
    return reelGui_;
}

std::uint64_t FishingRuntimeContext::ResolvePowerBar()
{
    auto* winMemory = dynamic_cast<Win32RobloxMemory*>(memory_);
    if (winMemory == nullptr)
    {
        return 0;
    }

    auto workspace = winMemory->FindWorkspace();
    auto localPlayer = winMemory->GetLocalPlayerAddress();
    if (workspace == 0 || localPlayer == 0)
    {
        return 0;
    }

    auto playerName = memory_->ReadName(localPlayer);
    if (playerName.empty())
    {
        return 0;
    }

    auto character = memory_->FindChildByName(workspace, playerName);
    auto rootPart = character == 0 ? 0 : memory_->FindChildByName(character, "HumanoidRootPart");
    auto powerGui = rootPart == 0 ? 0 : memory_->FindChildByName(rootPart, "power");
    auto bar = powerGui == 0 ? 0 : winMemory->FindDescendantFrameByName(powerGui, "bar");
    if (bar != 0)
    {
        return bar;
    }

    powerGui = rootPart == 0 ? 0 : memory_->FindDescendantByName(rootPart, "power");
    bar = powerGui == 0 ? 0 : winMemory->FindDescendantFrameByName(powerGui, "bar");
    if (bar != 0)
    {
        return bar;
    }

    powerGui = character == 0 ? 0 : memory_->FindDescendantByName(character, "power");
    return powerGui == 0 ? 0 : winMemory->FindDescendantFrameByName(powerGui, "bar");
}

std::optional<ReelContext> FishingRuntimeContext::BuildReelContextFromGui(std::uint64_t reelGui)
{
    auto bar = memory_->FindChildByName(reelGui, "bar");
    if (bar == 0 || !memory_->IsVisible(bar, "FrameVisible"))
    {
        return std::nullopt;
    }

    auto fish = memory_->FindChildByName(bar, "fish");
    if (fish == 0)
    {
        fish = memory_->FindDescendantByName(bar, "fish");
    }

    auto playerbar = memory_->FindChildByName(bar, "playerbar");
    if (playerbar == 0)
    {
        playerbar = memory_->FindDescendantByName(bar, "playerbar");
    }

    if (fish == 0 || playerbar == 0 || !memory_->IsVisible(fish, "FrameVisible") ||
        !memory_->IsVisible(playerbar, "FrameVisible"))
    {
        return std::nullopt;
    }

    return ReelContext{bar, fish, playerbar};
}

bool FishingRuntimeContext::IsCached(std::uint64_t address, const std::string& expectedName)
{
    return address != 0 && EqualsIgnoreCase(memory_->ReadName(address), expectedName);
}

void FishingRuntimeContext::ResetReelCache()
{
    reelGui_ = 0;
    bar_ = 0;
    fish_ = 0;
    playerbar_ = 0;
    progressBar_ = 0;
    reelGuiVerifiedAtMs_ = 0;
}

void FishingRuntimeContext::ResetShakeCache()
{
    shakeGui_ = 0;
    shakeSafezone_ = 0;
    shakeButton_ = 0;
}

double FishingRuntimeContext::Clamp(double value, double min, double max)
{
    return value < min ? min : value > max ? max : value;
}

bool FishingRuntimeContext::IsReasonableScale(double value)
{
    return value >= -0.05 && value <= 1.05;
}
}
