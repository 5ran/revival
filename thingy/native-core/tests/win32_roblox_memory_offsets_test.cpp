#include <cstdlib>
#include <iostream>
#include <string>
#include <unordered_map>

#include "offsets_source.hpp"
#include "win32_roblox_memory.hpp"

using macro_port::IOffsetsSource;
using macro_port::Win32RobloxMemory;

class FakeOffsets final : public IOffsetsSource
{
public:
    bool IsPopulated() const override { return true; }

    bool TryGetOffset(const std::string& key, std::uint64_t& value) const override
    {
        auto it = values.find(key);
        if (it == values.end())
        {
            return false;
        }
        value = it->second;
        return true;
    }

    std::unordered_map<std::string, std::uint64_t> values;
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
    FakeOffsets offsets;
    offsets.values["Instance.Name"] = 0x48;
    offsets.values["GuiObject.Text"] = 0x90;
    offsets.values["FakeDataModel.Pointer"] = 0x1234;

    Win32RobloxMemory memory(&offsets);
    std::uint64_t value = 0;

    failed += Assert(memory.TryResolveOffset("Name", value) && value == 0x48, "alias-name");
    failed += Assert(memory.TryResolveOffset("Text", value) && value == 0x90, "alias-text");
    failed += Assert(memory.TryResolveOffset("FakeDataModelPointer", value) && value == 0x1234, "alias-datamodel");
    failed += Assert(!memory.TryResolveOffset("NoSuchOffset", value), "unknown-offset");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }

    return 1;
}
