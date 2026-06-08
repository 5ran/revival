#pragma once

#include <cstdint>
#include <functional>
#include <optional>
#include <string>
#include <unordered_map>
#include <vector>

#include "offsets_source.hpp"
#include "roblox_memory.hpp"

namespace macro_port
{
class Win32RobloxMemory final : public IRobloxMemory
{
public:
    explicit Win32RobloxMemory(const IOffsetsSource* offsets);
    ~Win32RobloxMemory() override;

    bool EnsureAttached(std::string* error = nullptr);
    void Detach();

    bool TryResolveOffset(const std::string& key, std::uint64_t& value) const;
    std::uint64_t GetOffsetOrZero(const std::string& key) const;

    std::uint64_t FindPlayerGui() override;
    std::uint64_t FindWorkspace();
    std::uint64_t GetLocalPlayerAddress();
    std::uint64_t FindChildByClass(std::uint64_t parent, const std::string& className);
    std::uint64_t FindDescendantFrameByName(std::uint64_t root, const std::string& name);
    std::string ReadClassName(std::uint64_t instance);
    std::uint64_t FindChildByName(std::uint64_t parent, const std::string& name) override;
    std::uint64_t FindDescendantByName(std::uint64_t root, const std::string& name) override;
    std::vector<std::uint64_t> ReadChildren(std::uint64_t instance) override;
    std::string ReadName(std::uint64_t instance) override;
    bool IsVisible(std::uint64_t instance, const std::string& offsetKey) override;
    UDim ReadFramePosition(std::uint64_t frame) override;
    UDim ReadFrameSize(std::uint64_t frame) override;
    std::optional<GuiBounds> ReadGuiBounds(std::uint64_t instance, bool visibleRequired) override;
    std::string ReadGuiText(std::uint64_t instance) override;
    std::uint64_t ReadPtr(std::uint64_t address);
    std::int32_t ReadInt32(std::uint64_t address);
    std::uint8_t ReadByte(std::uint64_t address);
    std::string ReadString(std::uint64_t address);
    static bool IsValidAddress(std::uint64_t address);

private:
    std::uint64_t GetDataModel();
    std::uint64_t GetLocalPlayer();
    std::string ReadClass(std::uint64_t instance);
    std::uint64_t FindDescendantByClass(std::uint64_t root, const std::string& className);
    std::uint64_t FindDescendant(std::uint64_t root, const std::function<bool(std::uint64_t)>& predicate);

    float ReadFloat(std::uint64_t address);
    std::optional<std::vector<std::uint8_t>> ReadBytes(std::uint64_t address, int count);

    static bool IsVectorRange(std::uint64_t start, std::uint64_t end);
    static bool IsReasonableFloat(float value);
    static bool EqualsIgnoreCase(const std::string& a, const std::string& b);

    const IOffsetsSource* offsets_ = nullptr;
    void* processHandle_ = nullptr;
    std::uint32_t processId_ = 0;
    std::uint64_t baseAddress_ = 0;

    static const std::unordered_map<std::string, std::string> kOffsetAliases;
};
}
