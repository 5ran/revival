#pragma once

#include <cstdint>
#include <string>
#include <unordered_map>
#include <unordered_set>
#include <vector>

namespace macro_port
{
struct TranquilityNote
{
    std::uint64_t id = 0;
    int laneIndex = 0;
    double yScale = 0.0;
    bool visible = false;
};

struct TranquilityTickResult
{
    std::vector<char> keysToPress;
};

class TranquilityController
{
public:
    void Reset();
    TranquilityTickResult Update(std::int64_t nowMs, const std::vector<TranquilityNote>& notes, const std::vector<char>& laneKeys);

private:
    std::unordered_set<std::uint64_t> hitNotes_;
    std::unordered_map<char, std::int64_t> lastKeySentAt_;
};
}
