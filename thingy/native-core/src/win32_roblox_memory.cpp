#include "win32_roblox_memory.hpp"

#include <algorithm>
#include <cctype>
#include <cmath>
#include <cstring>
#include <queue>
#include <unordered_set>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <tlhelp32.h>

namespace macro_port
{
const std::unordered_map<std::string, std::string> Win32RobloxMemory::kOffsetAliases = {
    {"FakeDataModelPointer", "FakeDataModel.Pointer"},
    {"FakeDataModelToDataModel", "FakeDataModel.RealDataModel"},
    {"LocalPlayer", "Player.LocalPlayer"},
    {"Children", "Instance.ChildrenStart"},
    {"ChildrenEnd", "Instance.ChildrenEnd"},
    {"Name", "Instance.Name"},
    {"ClassDescriptor", "Instance.ClassDescriptor"},
    {"ClassDescriptorToClassName", "Instance.ClassName"},
    {"StringLength", "Misc.StringLength"},
    {"FramePositionX", "GuiObject.Position"},
    {"FrameSizeX", "GuiObject.Size"},
    {"ScreenGuiEnabled", "GuiObject.ScreenGui_Enabled"},
    {"FrameVisible", "GuiObject.Visible"},
    {"Text", "GuiObject.Text"},
    {"TextLabelText", "GuiObject.Text"},
    {"ContentText", "GuiObject.Text"},
    {"AbsolutePosition", "GuiBase2D.AbsolutePosition"},
    {"AbsoluteSize", "GuiBase2D.AbsoluteSize"},
};

Win32RobloxMemory::Win32RobloxMemory(const IOffsetsSource* offsets) : offsets_(offsets)
{
}

Win32RobloxMemory::~Win32RobloxMemory()
{
    Detach();
}

bool Win32RobloxMemory::EnsureAttached(std::string* error)
{
    if (processHandle_ != nullptr && processId_ != 0 && offsets_ != nullptr && offsets_->IsPopulated())
    {
        return true;
    }

    Detach();
    if (offsets_ == nullptr || !offsets_->IsPopulated())
    {
        if (error != nullptr)
        {
            *error = "Offsets unavailable.";
        }
        return false;
    }

    auto snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snapshot == INVALID_HANDLE_VALUE)
    {
        if (error != nullptr)
        {
            *error = "Failed to enumerate processes.";
        }
        return false;
    }

    PROCESSENTRY32 pe;
    std::memset(&pe, 0, sizeof(pe));
    pe.dwSize = sizeof(pe);
    std::uint32_t pid = 0;
    if (Process32First(snapshot, &pe) != FALSE)
    {
        do
        {
            const auto exe = std::string(pe.szExeFile);
            if (EqualsIgnoreCase(exe, "RobloxPlayerBeta.exe") || exe.find("Roblox") != std::string::npos)
            {
                pid = pe.th32ProcessID;
                break;
            }
        } while (Process32Next(snapshot, &pe) != FALSE);
    }
    CloseHandle(snapshot);

    if (pid == 0)
    {
        if (error != nullptr)
        {
            *error = "No Roblox process found.";
        }
        return false;
    }

    HANDLE process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (process == nullptr)
    {
        if (error != nullptr)
        {
            *error = "OpenProcess failed.";
        }
        return false;
    }

    auto modules = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, pid);
    if (modules == INVALID_HANDLE_VALUE)
    {
        CloseHandle(process);
        if (error != nullptr)
        {
            *error = "Failed to enumerate modules.";
        }
        return false;
    }

    MODULEENTRY32 me;
    std::memset(&me, 0, sizeof(me));
    me.dwSize = sizeof(me);
    std::uint64_t baseAddress = 0;
    if (Module32First(modules, &me) != FALSE)
    {
        baseAddress = reinterpret_cast<std::uint64_t>(me.modBaseAddr);
    }
    CloseHandle(modules);

    if (baseAddress == 0)
    {
        CloseHandle(process);
        if (error != nullptr)
        {
            *error = "Could not resolve Roblox base module.";
        }
        return false;
    }

    processHandle_ = process;
    processId_ = pid;
    baseAddress_ = baseAddress;
    return true;
}

void Win32RobloxMemory::Detach()
{
    if (processHandle_ != nullptr)
    {
        CloseHandle(static_cast<HANDLE>(processHandle_));
    }
    processHandle_ = nullptr;
    processId_ = 0;
    baseAddress_ = 0;
}

bool Win32RobloxMemory::TryResolveOffset(const std::string& key, std::uint64_t& value) const
{
    if (offsets_ != nullptr && offsets_->TryGetOffset(key, value))
    {
        return true;
    }

    auto it = kOffsetAliases.find(key);
    if (it == kOffsetAliases.end())
    {
        return false;
    }

    return offsets_ != nullptr && offsets_->TryGetOffset(it->second, value);
}

std::uint64_t Win32RobloxMemory::GetOffsetOrZero(const std::string& key) const
{
    std::uint64_t value = 0;
    return TryResolveOffset(key, value) ? value : 0;
}

std::uint64_t Win32RobloxMemory::FindPlayerGui()
{
    auto localPlayer = GetLocalPlayer();
    return localPlayer == 0 ? 0 : FindChildByClass(localPlayer, "PlayerGui");
}

std::uint64_t Win32RobloxMemory::FindWorkspace()
{
    auto dataModel = GetDataModel();
    for (auto child : ReadChildren(dataModel))
    {
        if (EqualsIgnoreCase(ReadName(child), "Workspace") || EqualsIgnoreCase(ReadClass(child), "Workspace"))
        {
            return child;
        }
    }
    return 0;
}

std::uint64_t Win32RobloxMemory::GetLocalPlayerAddress()
{
    return GetLocalPlayer();
}

std::uint64_t Win32RobloxMemory::FindDescendantFrameByName(std::uint64_t root, const std::string& name)
{
    return FindDescendant(root, [&](std::uint64_t item) {
        return EqualsIgnoreCase(ReadName(item), name) && EqualsIgnoreCase(ReadClass(item), "Frame");
    });
}

std::string Win32RobloxMemory::ReadClassName(std::uint64_t instance)
{
    return ReadClass(instance);
}

std::uint64_t Win32RobloxMemory::FindChildByName(std::uint64_t parent, const std::string& name)
{
    for (auto child : ReadChildren(parent))
    {
        if (EqualsIgnoreCase(ReadName(child), name))
        {
            return child;
        }
    }
    return 0;
}

std::uint64_t Win32RobloxMemory::FindDescendantByName(std::uint64_t root, const std::string& name)
{
    return FindDescendant(root, [&](std::uint64_t item) { return EqualsIgnoreCase(ReadName(item), name); });
}

std::vector<std::uint64_t> Win32RobloxMemory::ReadChildren(std::uint64_t instance)
{
    if (!IsValidAddress(instance))
    {
        return {};
    }

    auto childrenOff = GetOffsetOrZero("Children");
    auto childrenEndOff = GetOffsetOrZero("ChildrenEnd");
    if (childrenOff == 0 || childrenEndOff == 0)
    {
        return {};
    }

    auto listPtr = ReadPtr(instance + childrenOff);
    auto arrayStart = ReadPtr(listPtr);
    auto arrayEnd = ReadPtr(listPtr + childrenEndOff);
    if (!IsVectorRange(arrayStart, arrayEnd))
    {
        return {};
    }

    std::vector<std::uint64_t> out;
    for (auto entry = arrayStart; entry < arrayEnd && out.size() < 2000; entry += 0x10)
    {
        auto child = ReadPtr(entry);
        if (IsValidAddress(child))
        {
            out.push_back(child);
        }
    }
    return out;
}

std::string Win32RobloxMemory::ReadName(std::uint64_t instance)
{
    auto off = GetOffsetOrZero("Name");
    return off == 0 ? std::string{} : ReadString(ReadPtr(instance + off));
}

bool Win32RobloxMemory::IsVisible(std::uint64_t instance, const std::string& offsetKey)
{
    auto off = GetOffsetOrZero(offsetKey);
    return off == 0 || ReadByte(instance + off) != 0;
}

UDim Win32RobloxMemory::ReadFramePosition(std::uint64_t frame)
{
    auto off = GetOffsetOrZero("FramePositionX");
    if (off == 0)
    {
        return {};
    }
    return UDim{ReadFloat(frame + off), ReadInt32(frame + off + 0x4), ReadFloat(frame + off + 0x8), ReadInt32(frame + off + 0xC)};
}

UDim Win32RobloxMemory::ReadFrameSize(std::uint64_t frame)
{
    auto off = GetOffsetOrZero("FrameSizeX");
    if (off == 0)
    {
        return {};
    }
    return UDim{ReadFloat(frame + off), ReadInt32(frame + off + 0x4), ReadFloat(frame + off + 0x8), ReadInt32(frame + off + 0xC)};
}

std::optional<GuiBounds> Win32RobloxMemory::ReadGuiBounds(std::uint64_t instance, bool visibleRequired)
{
    if (!IsValidAddress(instance) || (visibleRequired && !IsVisible(instance, "FrameVisible")))
    {
        return std::nullopt;
    }

    auto posOff = GetOffsetOrZero("AbsolutePosition");
    auto sizeOff = GetOffsetOrZero("AbsoluteSize");
    if (posOff == 0 || sizeOff == 0)
    {
        return std::nullopt;
    }

    auto x = ReadFloat(instance + posOff);
    auto y = ReadFloat(instance + posOff + 0x4);
    auto width = ReadFloat(instance + sizeOff);
    auto height = ReadFloat(instance + sizeOff + 0x4);
    if (!IsReasonableFloat(x) || !IsReasonableFloat(y) || !IsReasonableFloat(width) || !IsReasonableFloat(height) || width <= 1 || height <= 1)
    {
        return std::nullopt;
    }

    return GuiBounds{x, y, width, height};
}

std::string Win32RobloxMemory::ReadGuiText(std::uint64_t instance)
{
    for (const auto& key : {"Text", "TextLabelText", "ContentText"})
    {
        auto off = GetOffsetOrZero(key);
        if (off == 0)
        {
            continue;
        }

        auto indirect = ReadString(ReadPtr(instance + off));
        if (!indirect.empty())
        {
            return indirect;
        }

        auto direct = ReadString(instance + off);
        if (!direct.empty())
        {
            return direct;
        }
    }

    return {};
}

std::uint64_t Win32RobloxMemory::GetDataModel()
{
    std::string error;
    if (!EnsureAttached(&error))
    {
        return 0;
    }

    auto fakePtrOff = GetOffsetOrZero("FakeDataModelPointer");
    auto dmOff = GetOffsetOrZero("FakeDataModelToDataModel");
    if (fakePtrOff == 0 || dmOff == 0)
    {
        return 0;
    }

    auto fakeDataModel = ReadPtr(baseAddress_ + fakePtrOff);
    auto dataModel = ReadPtr(fakeDataModel + dmOff);
    return IsValidAddress(dataModel) ? dataModel : 0;
}

std::uint64_t Win32RobloxMemory::GetLocalPlayer()
{
    auto players = FindDescendantByClass(GetDataModel(), "Players");
    if (players == 0)
    {
        return 0;
    }

    auto off = GetOffsetOrZero("LocalPlayer");
    if (off == 0)
    {
        return 0;
    }

    auto localPlayer = ReadPtr(players + off);
    return IsValidAddress(localPlayer) ? localPlayer : 0;
}

std::uint64_t Win32RobloxMemory::FindChildByClass(std::uint64_t parent, const std::string& className)
{
    for (auto child : ReadChildren(parent))
    {
        if (EqualsIgnoreCase(ReadClass(child), className))
        {
            return child;
        }
    }
    return 0;
}

std::string Win32RobloxMemory::ReadClass(std::uint64_t instance)
{
    auto classDescOff = GetOffsetOrZero("ClassDescriptor");
    auto classNameOff = GetOffsetOrZero("ClassDescriptorToClassName");
    if (classDescOff == 0 || classNameOff == 0)
    {
        return {};
    }

    auto descriptor = ReadPtr(instance + classDescOff);
    return ReadString(ReadPtr(descriptor + classNameOff));
}

std::uint64_t Win32RobloxMemory::FindDescendantByClass(std::uint64_t root, const std::string& className)
{
    return FindDescendant(root, [&](std::uint64_t item) { return EqualsIgnoreCase(ReadClass(item), className); });
}

std::uint64_t Win32RobloxMemory::FindDescendant(std::uint64_t root, const std::function<bool(std::uint64_t)>& predicate)
{
    if (!IsValidAddress(root))
    {
        return 0;
    }

    std::queue<std::pair<std::uint64_t, int>> queue;
    std::unordered_set<std::uint64_t> seen;
    queue.push({root, 0});
    int inspected = 0;
    while (!queue.empty() && inspected++ < 60000)
    {
        auto item = queue.front();
        queue.pop();

        if (seen.find(item.first) != seen.end())
        {
            continue;
        }
        seen.insert(item.first);

        if (predicate(item.first))
        {
            return item.first;
        }
        if (item.second >= 128)
        {
            continue;
        }
        for (auto child : ReadChildren(item.first))
        {
            queue.push({child, item.second + 1});
        }
    }

    return 0;
}

std::uint64_t Win32RobloxMemory::ReadPtr(std::uint64_t address)
{
    auto bytes = ReadBytes(address, sizeof(std::uint64_t));
    if (!bytes.has_value() || bytes->size() < sizeof(std::uint64_t))
    {
        return 0;
    }

    std::uint64_t value = 0;
    std::memcpy(&value, bytes->data(), sizeof(std::uint64_t));
    return value;
}

std::int32_t Win32RobloxMemory::ReadInt32(std::uint64_t address)
{
    auto bytes = ReadBytes(address, sizeof(std::int32_t));
    if (!bytes.has_value() || bytes->size() < sizeof(std::int32_t))
    {
        return 0;
    }

    std::int32_t value = 0;
    std::memcpy(&value, bytes->data(), sizeof(std::int32_t));
    return value;
}

std::uint8_t Win32RobloxMemory::ReadByte(std::uint64_t address)
{
    auto bytes = ReadBytes(address, 1);
    return (!bytes.has_value() || bytes->empty()) ? 0 : (*bytes)[0];
}

float Win32RobloxMemory::ReadFloat(std::uint64_t address)
{
    auto bytes = ReadBytes(address, sizeof(float));
    if (!bytes.has_value() || bytes->size() < sizeof(float))
    {
        return 0.0f;
    }

    float value = 0.0f;
    std::memcpy(&value, bytes->data(), sizeof(float));
    return value;
}

std::string Win32RobloxMemory::ReadString(std::uint64_t address)
{
    if (!IsValidAddress(address))
    {
        return {};
    }

    auto lenOff = GetOffsetOrZero("StringLength");
    if (lenOff == 0)
    {
        return {};
    }

    auto length = ReadInt32(address + lenOff);
    if (length <= 0 || length > 1000)
    {
        return {};
    }

    auto dataAddress = length > 15 ? ReadPtr(address) : address;
    auto bytes = ReadBytes(dataAddress, length);
    return bytes.has_value() ? std::string(bytes->begin(), bytes->end()) : std::string{};
}

std::optional<std::vector<std::uint8_t>> Win32RobloxMemory::ReadBytes(std::uint64_t address, int count)
{
    std::string error;
    if (!EnsureAttached(&error) || !IsValidAddress(address) || count <= 0 || processHandle_ == nullptr)
    {
        return std::nullopt;
    }

    std::vector<std::uint8_t> buffer(static_cast<size_t>(count));
    SIZE_T bytesRead = 0;
    BOOL ok = ReadProcessMemory(
        static_cast<HANDLE>(processHandle_),
        reinterpret_cast<LPCVOID>(address),
        buffer.data(),
        static_cast<SIZE_T>(count),
        &bytesRead);
    if (ok == FALSE || bytesRead != static_cast<SIZE_T>(count))
    {
        return std::nullopt;
    }
    return buffer;
}

bool Win32RobloxMemory::IsValidAddress(std::uint64_t address)
{
    return address >= 0x10000ULL && address <= 0x00007FFFFFFFFFFFULL;
}

bool Win32RobloxMemory::IsVectorRange(std::uint64_t start, std::uint64_t end)
{
    return IsValidAddress(start) && IsValidAddress(end) && end >= start && (end - start) <= 1048576ULL && ((end - start) % 0x10ULL == 0);
}

bool Win32RobloxMemory::IsReasonableFloat(float value)
{
    return !std::isnan(value) && !std::isinf(value) && std::abs(value) < 100000.0f;
}

bool Win32RobloxMemory::EqualsIgnoreCase(const std::string& a, const std::string& b)
{
    if (a.size() != b.size())
    {
        return false;
    }

    for (size_t i = 0; i < a.size(); ++i)
    {
        if (static_cast<unsigned char>(std::tolower(static_cast<unsigned char>(a[i]))) !=
            static_cast<unsigned char>(std::tolower(static_cast<unsigned char>(b[i]))))
        {
            return false;
        }
    }
    return true;
}
}
