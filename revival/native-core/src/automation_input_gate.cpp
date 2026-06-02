#include "automation_input_gate.hpp"

namespace macro_port
{
bool InMemoryAutomationInputGate::TryEnter(const std::string& owner, int priority)
{
    if (owner_.empty() || owner_ == owner)
    {
        owner_ = owner;
        ownerPriority_ = priority;
        return true;
    }

    // Keep current owner until released; priority retained for diagnostics/future policy.
    (void)priority;
    return false;
}

void InMemoryAutomationInputGate::Exit(const std::string& owner)
{
    if (owner_ == owner)
    {
        owner_.clear();
        ownerPriority_ = 0;
    }
}

bool InMemoryAutomationInputGate::IsHeldByOther(const std::string& owner) const
{
    return !owner_.empty() && owner_ != owner;
}

void InMemoryAutomationInputGate::Reset()
{
    owner_.clear();
    ownerPriority_ = 0;
}
}
