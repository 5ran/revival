#pragma once

#include <cstdint>

#include "auto_totem_workflow_state.hpp"

namespace macro_port
{
class IAutoTotemWorkflowExecutor
{
public:
    virtual ~IAutoTotemWorkflowExecutor() = default;
    virtual void RequestStart(std::int64_t nowMs) = 0;
    virtual AutoTotemWorkflowInput::Result PollResult(std::int64_t nowMs) = 0;
};
}
