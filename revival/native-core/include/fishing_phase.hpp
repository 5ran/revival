#pragma once

namespace macro_port
{
enum class FishingPhase
{
    Off,
    Casting,
    Casted,
    Shake,
    Fishing,
    Suspended,
    Error
};

enum class FishingCastingMode
{
    Normal,
    Perfect
};
}
