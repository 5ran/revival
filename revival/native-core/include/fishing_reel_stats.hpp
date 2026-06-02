#pragma once

namespace macro_port
{
struct FishingReelStats
{
    int catches = 0;
    int misses = 0;

    static FishingReelStats Empty()
    {
        return FishingReelStats{};
    }
};
}
