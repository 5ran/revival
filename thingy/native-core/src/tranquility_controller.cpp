#include "tranquility_controller.hpp"

namespace macro_port
{
namespace
{
constexpr double kHitYMin = 0.78;
constexpr double kHitYMax = 0.90;
constexpr std::int64_t kKeyCooldownMs = 30;
}

void TranquilityController::Reset()
{
    hitNotes_.clear();
    lastKeySentAt_.clear();
}

TranquilityTickResult TranquilityController::Update(
    std::int64_t nowMs,
    const std::vector<TranquilityNote>& notes,
    const std::vector<char>& laneKeys)
{
    TranquilityTickResult out{};
    std::unordered_set<std::uint64_t> seen;

    for (const auto& note : notes)
    {
        if (!note.visible || note.id == 0 || note.laneIndex <= 0 || note.laneIndex > static_cast<int>(laneKeys.size()))
        {
            continue;
        }

        seen.insert(note.id);
        if (hitNotes_.count(note.id) > 0 || note.yScale < kHitYMin || note.yScale > kHitYMax)
        {
            continue;
        }

        char key = laneKeys[static_cast<size_t>(note.laneIndex - 1)];
        auto it = lastKeySentAt_.find(key);
        if (it != lastKeySentAt_.end() && nowMs - it->second < kKeyCooldownMs)
        {
            continue;
        }

        out.keysToPress.push_back(key);
        lastKeySentAt_[key] = nowMs;
        hitNotes_.insert(note.id);
    }

    for (auto it = hitNotes_.begin(); it != hitNotes_.end(); )
    {
        if (seen.count(*it) == 0) it = hitNotes_.erase(it); else ++it;
    }

    return out;
}
}
