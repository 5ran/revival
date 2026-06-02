#pragma once

#include <optional>
#include <vector>

#include "fishing_runtime_context.hpp"

namespace macro_port
{
struct BellonaSideAssignerSettings
{
    double singleToLeftThreshold = 0.42;
    double singleToRightThreshold = 0.58;
};

class BellonaSideAssigner
{
public:
    explicit BellonaSideAssigner(BellonaSideAssignerSettings settings = {}) : settings_(settings) {}

    void Reset();
    std::optional<ReelLocatedContext> SelectRight(const std::vector<ReelLocatedContext>& ordered);

private:
    BellonaSideAssignerSettings settings_{};
    bool singleReelAssignRight_ = false;
};
}
