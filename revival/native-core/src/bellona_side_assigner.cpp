#include "bellona_side_assigner.hpp"

namespace macro_port
{
void BellonaSideAssigner::Reset()
{
    singleReelAssignRight_ = false;
}

std::optional<ReelLocatedContext> BellonaSideAssigner::SelectRight(const std::vector<ReelLocatedContext>& ordered)
{
    if (ordered.empty())
    {
        return std::nullopt;
    }

    if (ordered.size() >= 2)
    {
        singleReelAssignRight_ = true;
        return ordered.back();
    }

    const auto only = ordered.front();
    if (singleReelAssignRight_)
    {
        if (only.barX <= settings_.singleToLeftThreshold)
        {
            singleReelAssignRight_ = false;
        }
    }
    else
    {
        if (only.barX >= settings_.singleToRightThreshold)
        {
            singleReelAssignRight_ = true;
        }
    }

    return singleReelAssignRight_ ? std::optional<ReelLocatedContext>{only} : std::nullopt;
}
}
