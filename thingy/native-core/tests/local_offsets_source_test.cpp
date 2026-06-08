#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <iostream>

#include "local_offsets_source.hpp"

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
    namespace fs = std::filesystem;
    const auto path = fs::temp_directory_path() / "openmacro_local_offsets_source_test.json";
    {
        std::ofstream output(path, std::ios::binary);
        output << R"({
  "RobloxVersion": "version-test",
  "Total Offsets": 999,
  "FakeDataModelPointer": "0x1234",
  "offsets": {
    "Instance": {
      "Name": "0xB0",
      "Parent": 112
    },
    "GuiObject": {
      "Text": "0xDC0"
    }
  }
})";
    }

    macro_port::LocalOffsetsSource source;
    std::string error;
    int failed = 0;
    failed += Assert(source.LoadFromFile(path, &error), "load");
    failed += Assert(source.IsPopulated(), "populated");
    failed += Assert(source.Version() == "version-test", "version");

    std::uint64_t value = 0;
    failed += Assert(source.TryGetOffset("FakeDataModelPointer", value) && value == 0x1234, "flat");
    failed += Assert(source.TryGetOffset("Instance.Name", value) && value == 0xB0, "nested-string-hex");
    failed += Assert(source.TryGetOffset("Instance.Parent", value) && value == 112, "nested-number");
    failed += Assert(source.TryGetOffset("GuiObject.Text", value) && value == 0xDC0, "nested-gui");
    failed += Assert(!source.TryGetOffset("RobloxVersion", value), "version-not-offset");
    failed += Assert(!source.TryGetOffset("Total Offsets", value), "metadata-not-offset");

    {
        std::ofstream output(path, std::ios::binary);
        output << R"({
  "Roblox Version": "version-theo",
  "FakeDataModelPointer": "0x2222"
})";
    }

    failed += Assert(source.LoadFromFile(path, &error), "load-theo");
    failed += Assert(source.Version() == "version-theo", "theo-version");
    failed += Assert(source.Count() == 1, "theo-count");

    {
        std::ofstream output(path, std::ios::binary);
        output << R"({
  "Roblox Version": "version-theo-nested",
  "Offsets": {
    "Instance": {
      "Name": 176
    }
  }
})";
    }

    failed += Assert(source.LoadFromFile(path, &error), "load-theo-nested");
    failed += Assert(source.Version() == "version-theo-nested", "theo-nested-version");
    failed += Assert(source.TryGetOffset("Instance.Name", value) && value == 176, "theo-nested-unprefixed");
    failed += Assert(!source.TryGetOffset("Offsets.Instance.Name", value), "theo-nested-no-container-prefix");

    fs::remove(path);

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }

    return 1;
}
